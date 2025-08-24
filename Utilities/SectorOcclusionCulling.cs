using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Profiling;

namespace OcclusionCulling
{
    /// <summary>
    /// Implements a sector-based radial height map occlusion culling.
    /// </summary>
    public class SectorOcclusionCulling
    {
        public ComponentLookup<PrefabRef> m_PrefabRefLookup { get; set; }
        public ComponentLookup<MeshData> m_MeshDataLookup { get; set; }
        public ComponentLookup<Transform> m_TransformLookup { get; set; }
        public BufferLookup<SubMesh> m_SubMeshLookup { get;  set; }

        public CullingConfig m_Config;

        public SectorOcclusionCulling(float3 cameraPosition, float3 cameraForward)
        {
            m_Config = new CullingConfig
            {
                fovDegrees = 90f,
                sectorCount = 128,
                binCount = 152,
                maxDistance = 2000f,
                clearanceMeters = 0.4f,
                occluderMaxDistance = 500f,
                maxOccluders = 8,
                startIndex = 0, //not used right now
                maxResults = int.MaxValue, // not used right now,
                cameraPosition = cameraPosition,
                cameraForward = cameraForward
            };
        }

        public struct CullingConfig
        {
            public float fovDegrees;
            public int sectorCount;
            public int binCount;
            public float maxDistance;
            public float clearanceMeters;
            public float occluderMaxDistance;
            public int maxOccluders;
            public int startIndex;
            public int maxResults;
            public float3 cameraPosition;
            public float3 cameraForward;
        }

        public CullingConfig GetConfig()
        {
            return m_Config;
        }

        public void SetConfig(CullingConfig cc)
        {
            m_Config = cc;
        }

        public struct RadialHeightMap
        {
            public NativeArray<float> heights;
            public int sectorCount;
            public int binCount;
            public float maxDistance;
            public float clearance;
            internal float sectorAngleStep;
            internal float distanceStep;

            public void Dispose(JobHandle handle)
            {
                if (heights.IsCreated) heights.Dispose(handle);
            }
        }

        [BurstCompile]
        public struct RadialMapForTerrain : IJobFor
        {
            public NativeArray<float> terrainHeights;
            [ReadOnly] public RadialHeightMap map;
            [ReadOnly] public TerrainHeightData terrainHeightData;
            [ReadOnly] public float2 forwardXZ;
            [ReadOnly] public float halfFovRad;
            [ReadOnly] public CullingConfig config;

            public void Execute(int index)
            {
                // Execute per sector as index, offset by 1
                int sector = index + 1;
                CullingConfig cc = config;

                float angle = -halfFovRad + (sector + 0.5f) * map.sectorAngleStep;
                float sinA = math.sin(angle);
                float cosA = math.cos(angle);

                float2 dir = new float2(
                    forwardXZ.x * cosA - forwardXZ.y * sinA,
                    forwardXZ.x * sinA + forwardXZ.y * cosA
                );

                NativeArray<float> results = new NativeArray<float>(map.binCount, Allocator.Temp);
                for(int j = 1; j <= map.binCount; j++)
                {
                    Bounds3 sampleBounds = default;
                    float r = map.distanceStep * j;
                    float2 sampleXZ = new float2(cc.cameraPosition.x, cc.cameraPosition.z) + dir * r;
                    
                    // TODO: dynamically adjust it to be wider as distance goes out, then lower sector count
                    sampleBounds.min = new float3(sampleXZ.x - 0.1f, -10000f, sampleXZ.y - 0.1f);
                    sampleBounds.max = new float3(sampleXZ.x + 0.1f, 10000f, sampleXZ.y + 0.1f);
                    var heightRange = TerrainUtils.GetHeightRange(ref terrainHeightData, sampleBounds);

                    float terrainAngle = math.atan2(heightRange.max - cc.cameraPosition.y - map.clearance, r);
                    float maxAngle = j == 1 ? terrainAngle : math.max(results[j - 2], terrainAngle);
                    results[j - 1] = maxAngle;
                    terrainHeights[(index * map.binCount) + (j-1)] = maxAngle; // Parallel write at exact index
                }


            }
        }

        [BurstCompile]
        public struct RadialMapForObjects : IJobFor
        {
            public NativeArray<float> objectHeights;

            [ReadOnly] public NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree;
            [ReadOnly] public RadialHeightMap map;
            [ReadOnly] public float2 forwardXZ;
            [ReadOnly] public float halfFovRad;
            [ReadOnly] public CullingConfig config;
            [ReadOnly] public ComponentLookup<PrefabRef> PrefabRefLookup;
            [ReadOnly] public ComponentLookup<MeshData> MeshDataLookup;
            [ReadOnly] public ComponentLookup<Transform> TransformLookup;
            [ReadOnly] public BufferLookup<SubMesh> SubMeshLookup;

            public void Execute(int index)
            {
                // Execute per sector as index, offset by 1
                int sector = index + 1;
                CullingConfig cc = config;

                float angle = -halfFovRad + (sector + 0.5f) * map.sectorAngleStep;
                float sinA = math.sin(angle);
                float cosA = math.cos(angle);

                float2 dir = new float2(
                    forwardXZ.x * cosA - forwardXZ.y * sinA,
                    forwardXZ.x * sinA + forwardXZ.y * cosA
                );

                NativeList<KeyValuePair<Entity, QuadTreeBoundsXZ>> occluders = new(cc.maxOccluders, Allocator.Temp);

                Bounds3 sampleBounds = default;
                float2 camXZ = new float2(cc.cameraPosition.x, cc.cameraPosition.z);
                float2 endXZ = new float2(cc.cameraPosition.x, cc.cameraPosition.z) + dir * cc.occluderMaxDistance;
                sampleBounds.min = new float3(camXZ.x - 0.1f, -3000f, camXZ.y - 0.1f);
                sampleBounds.max = new float3(endXZ.x + 0.1f, 10000f, endXZ.y + 0.1f);
                var region = new QuadTreeBoundsXZ(sampleBounds, BoundsMask.NormalLayers, 0);
                var rq = new OcclusionUtilities.RegionQueryCollector
                {
                    searchRegion = region,
                    results = occluders,
                    maxCount = cc.maxOccluders,
                    PrefabRefLookup = PrefabRefLookup,
                    MeshDataLookup = MeshDataLookup,
                    TransformLookup = TransformLookup,
                    SubMeshLookup = SubMeshLookup
                };
                quadTree.Iterate(ref rq, 0);

                foreach(var occ in occluders)
                {
                    ComputeRadialRangeForBounds(occ.Value, camXZ, dir, cc.occluderMaxDistance, out float minR, out float maxR);
                    int minBin = math.clamp((int)math.floor(minR / map.distanceStep), 0, cc.binCount - 1);
                    int maxBin = math.clamp((int)math.ceil(maxR / map.distanceStep), 0, cc.binCount - 1);

                    for(int j = minBin; j<= maxBin; j++)
                    {
                        float r = map.distanceStep * j;
                        float occAngle = math.atan2(occ.Value.m_Bounds.max.y - cc.cameraPosition.y - map.clearance, r);
                        objectHeights[sector * map.binCount + j] = math.max(objectHeights[sector * map.binCount + j], occAngle);
                    }
                    
                }
            }

            static void ComputeRadialRangeForBounds(QuadTreeBoundsXZ occ, float2 camXZ, float2 sectorDir, float occluderMaxDistance, out float minR, out float maxR)
            {
                Bounds3 b = occ.m_Bounds;
                float2 c00 = new float2(b.min.x, b.min.z);
                float2 c01 = new float2(b.min.x, b.max.z);
                float2 c10 = new float2(b.max.x, b.min.z);
                float2 c11 = new float2(b.max.x, b.max.z);

                float r0 = math.dot(c00 - camXZ, sectorDir);
                float r1 = math.dot(c01 - camXZ, sectorDir);
                float r2 = math.dot(c10 - camXZ, sectorDir);
                float r3 = math.dot(c11 - camXZ, sectorDir);

                minR = math.min(math.min(r0, r1), math.min(r2, r3));
                maxR = math.max(math.max(r0, r1), math.max(r2, r3));

                // clamp to forward-only range and occluder distance
                minR = math.max(minR, 0f);
                maxR = math.min(maxR, occluderMaxDistance);
            }
        }

        [BurstCompile]
        public struct MergeRadialMaps : IJob
        {
            [ReadOnly] public NativeArray<float> objectHeights;
            [ReadOnly] public NativeArray<float> terrainHeights;

            public NativeArray<float> heights;
            public void Execute()
            {
                if (heights.Length != objectHeights.Length || objectHeights.Length != terrainHeights.Length)
                {
                    throw new ArgumentException("boom");
                }
                for (int i = 0; i < heights.Length; i++)
                {
                    heights[i] = math.max(terrainHeights[i], objectHeights[i]);
                }
            }
        }

        /// <summary>
        /// Perform occlusion culling using the radial height map.
        /// </summary>
        public JobHandle CullByRadialMap(
            NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
            NativeQuadTree<Entity, QuadTreeBoundsXZ> candidates,
            TerrainHeightData terrainHeight,
            NativeQueue<KeyValuePair<Entity, QuadTreeBoundsXZ>> queue,
            out int nextIndex
        )
        {
            nextIndex = m_Config.startIndex;
            var collector = new OcclusionUtilities.CandidateCollector();

            float3 cameraDirectionXZ = math.normalizesafe(new float3(m_Config.cameraForward.x, 0f, m_Config.cameraForward.z), new float3(0f, 0f, 1f));
            collector.candidates = candidates;

            float3 forwardPoint = m_Config.cameraPosition + (cameraDirectionXZ * m_Config.maxDistance);
            float3 min = new(math.min(m_Config.cameraPosition.x, forwardPoint.x), m_Config.cameraPosition.y - m_Config.maxDistance, math.min(m_Config.cameraPosition.z, forwardPoint.z));
            float3 max = new(math.max(m_Config.cameraPosition.x, forwardPoint.x), m_Config.cameraPosition.y + m_Config.maxDistance, math.max(m_Config.cameraPosition.z, forwardPoint.z));
            collector.searchBounds = new QuadTreeBoundsXZ(
                new Bounds3(min, max),
                BoundsMask.NormalLayers,
                0
            );
            quadTree.Iterate(ref collector, 0);

            NativeArray<float> heights = new(m_Config.sectorCount * m_Config.binCount, Allocator.TempJob);
            var map = new RadialHeightMap
            {
                heights = heights,
                sectorCount = m_Config.sectorCount,
                binCount = m_Config.binCount,
                maxDistance = m_Config.maxDistance,
                clearance = m_Config.clearanceMeters,
                sectorAngleStep = math.radians(m_Config.fovDegrees) / m_Config.sectorCount,
                distanceStep = m_Config.maxDistance / (m_Config.binCount - 1)
            };

            // prepare a reusable list for object occluders
            float2 forwardXZ = math.normalizesafe(new float2(m_Config.cameraForward.x, m_Config.cameraForward.z), new float2(0f, 1f));
            float halfFovRad = math.radians(m_Config.fovDegrees) * 0.5f;

            NativeArray<float> terrainHeights = new NativeArray<float>(map.heights.Length, Allocator.TempJob);
            RadialMapForTerrain terrainJob = new RadialMapForTerrain
            {
                terrainHeights = terrainHeights,
                map = map,
                terrainHeightData = terrainHeight,
                forwardXZ = forwardXZ,
                halfFovRad = halfFovRad,
                config = m_Config
            };
            JobHandle terrainHandle = terrainJob.ScheduleParallel(m_Config.sectorCount, 1, default);

            NativeArray<float> objectHeights = new NativeArray<float>(map.heights.Length, Allocator.TempJob);
            for (int i = 0; i < objectHeights.Length; i++)
            {
                objectHeights[i] = float.MinValue; // Not always set, so need min value
            }
            RadialMapForObjects objectsJob = new RadialMapForObjects
            {
                objectHeights = objectHeights,
                quadTree = collector.candidates,
                map = map,
                forwardXZ = forwardXZ,
                halfFovRad = halfFovRad,
                config = m_Config,
                PrefabRefLookup = m_PrefabRefLookup,
                MeshDataLookup = m_MeshDataLookup,
                TransformLookup = m_TransformLookup,
                SubMeshLookup = m_SubMeshLookup
            };
            JobHandle objectHandle = objectsJob.ScheduleParallel(m_Config.sectorCount, 1, default);

            // Wait for parallel terrain and object jobs to be done
            JobHandle both = JobHandle.CombineDependencies(terrainHandle, objectHandle);

            MergeRadialMaps mergeJob = new MergeRadialMaps
            {
                objectHeights = objectHeights,
                terrainHeights = terrainHeights,
                heights = map.heights
            };
            JobHandle mapHandle = mergeJob.Schedule(both);

            JobHandle handle = ScheduleCullBySectorJob(
                collector.candidates,
                collector.candidateCount,
                map,
                m_Config,
                mapHandle,
                writer: queue.AsParallelWriter()
            );

            Mod.log.Info($"Summary: candidateCount({collector.candidateCount}), mapHeightSize({map.heights.Length}), terrainCompleted?({terrainHandle.IsCompleted}), queueCount({queue.Count})");
            nextIndex = 0; // TODO implement batching

            terrainHeights.Dispose(terrainHandle);
            objectHeights.Dispose(objectHandle);
            heights.Dispose(handle);
            return handle;
        }
        
        // Burst job for parallel sector culling
        //[BurstCompile]
        public struct CullBySectorJob : IJobFor
        {
            [ReadOnly] public NativeList<KeyValuePair<Entity, QuadTreeBoundsXZ>> candidates;
            [ReadOnly] public NativeArray<float> heights;
            [ReadOnly] public float halfFovRad;
            [ReadOnly] public float2 camXZ;
            [ReadOnly] public float2 forwardXZ;
            [ReadOnly] public float camYaw;
            [ReadOnly] public float distanceStep;
            [ReadOnly] public NativeHashMap<int, CullingConfig> hm_Config;

            public NativeQueue<KeyValuePair<Entity, QuadTreeBoundsXZ>>.ParallelWriter results;

            public void Execute(int index)
            {
                var config = hm_Config.AsReadOnly()[0];
                var item = candidates[index];
                var entity = item.Key;
                var bounds = item.Value;
                float kSelfEpsilon = 1e-3f;
                float3 center = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                float2 objXZ = new float2(center.x, center.z);
                float2 toObj = objXZ - camXZ;
                float dist = math.length(toObj);
                
                if (dist <= 0f) return;

                float angle = math.atan2(toObj.y, toObj.x) - camYaw;

                if (angle < -math.PI) angle += math.PI * 2;
                else if (angle > math.PI) angle -= math.PI * 2;

                if (math.abs(angle) > halfFovRad) return;

                int sector = math.clamp((int)((angle + halfFovRad) / (2 * halfFovRad) * config.sectorCount), 0, config.sectorCount - 1);
                int bin = math.clamp((int)(dist / distanceStep), 0, config.binCount - 1);
                
                float horizonCenter = heights[sector * config.binCount + bin];
                float objectHeightAdj = bounds.m_Bounds.max.y - config.clearanceMeters;
                float elevationAngle = math.atan2(objectHeightAdj - config.cameraPosition.y, dist);

                // If the height is almost the exact same angle, it's likely the same object
                // This is to prevent self-occlusion (may prevent true culls in rare circumstances, but likely faster)
                if (elevationAngle >= horizonCenter - kSelfEpsilon) return;

                if (elevationAngle < horizonCenter)
                {
                    // Additional checks to prevent self-culling and to ensure edges of object aren't still visible
                    float halfWidth = (bounds.m_Bounds.max.x - bounds.m_Bounds.min.x) * 0.5f;
                    float halfDepth = (bounds.m_Bounds.max.z - bounds.m_Bounds.min.z) * 0.5f;
                    float objectRadius = math.length(new float2(halfWidth, halfDepth));
                    float angRadius = math.atan2(objectRadius, dist);

                    int leftSector = math.clamp((int)((angle - angRadius + halfFovRad) / (2 * halfFovRad) * config.sectorCount), 0, config.sectorCount - 1);
                    int rightSector = math.clamp((int)((angle + angRadius + halfFovRad) / (2 * halfFovRad) * config.sectorCount), 0, config.sectorCount - 1);
                    float horizonLeft = heights[leftSector * config.binCount + bin];
                    float horizonRight = heights[rightSector * config.binCount + bin];

                    if (elevationAngle < horizonLeft && elevationAngle < horizonRight)
                    {
                        results.Enqueue(new KeyValuePair<Entity, QuadTreeBoundsXZ>(entity, bounds));
                    }
                }
            }
        }

        /// <summary>
        /// Schedule the sector-culling job as an IJobFor.
        /// </summary>
        public static JobHandle ScheduleCullBySectorJob(

            NativeQuadTree<Entity, QuadTreeBoundsXZ> candidates,
            int candidateCount,
            RadialHeightMap map,
            CullingConfig config,
            JobHandle dependency,
            NativeQueue<KeyValuePair<Entity, QuadTreeBoundsXZ>>.ParallelWriter writer)
        {
            // prepare inputs and output writer
            var flattener = new OcclusionUtilities.TreeFlattenCollector();
            NativeList<KeyValuePair<Entity, QuadTreeBoundsXZ>> results = new(candidateCount, Allocator.TempJob);
            flattener.results = results;
            candidates.Iterate(ref flattener, 0);
            float2 camXZ = new float2(config.cameraPosition.x, config.cameraPosition.z);
            float2 forwardXZ = math.normalizesafe(new float2(config.cameraForward.x, config.cameraForward.z), new float2(0f,1f));
            float halfFovRad = math.radians(config.fovDegrees) * 0.5f;

            NativeHashMap<int, CullingConfig> hm_Config = new(1, Allocator.TempJob);
            hm_Config.Add(0, config);

            // Is this a reasonable batchSize?
            int batchSize = 256;
            var job = new CullBySectorJob
            {
                candidates = results,
                heights = map.heights,
                camXZ = camXZ,
                forwardXZ = forwardXZ,
                camYaw = math.atan2(forwardXZ.y, forwardXZ.x),
                halfFovRad = halfFovRad,
                hm_Config = hm_Config,
                distanceStep = map.distanceStep,
                results = writer
            };
            JobHandle handle = job.ScheduleParallel(flattener.results.Length, batchSize, dependency);
            hm_Config.Dispose(handle);
            results.Dispose(handle);
            return handle;
        }
    }
}
