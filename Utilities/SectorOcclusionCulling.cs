using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PerformanceTweaks.Utilities
{
    /// <summary>
    /// Implements a sector-based radial height map occlusion culling.
    /// </summary>
    public class SectorOcclusionCulling
    {
        public ComponentLookup<PrefabRef> m_PrefabRefLookup { get; set; }
        public ComponentLookup<MeshData> m_MeshDataLookup { get; set; }
        public ComponentLookup<Game.Objects.Transform> m_TransformLookup { get; set; }
        public BufferLookup<SubMesh> m_SubMeshLookup { get;  set; }

        public CullingConfig m_Config;

        public SectorOcclusionCulling(float3 cameraPosition, float3 cameraForward)
        {
            m_Config = new CullingConfig
            {
                fovDegrees = 90f,
                sectorCount = 96,
                binCount = 192,
                maxDistance = 3000f,
                clearanceMeters = 0.4f,
                occluderMaxDistance = 800f,
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

        /// <summary>
        /// Perform occlusion culling using the radial height map.
        /// </summary>
        public JobHandle CullByRadialMap(
            NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
            NativeParallelHashMap<Entity, OcclusionCullingStruct> visibleCandidates,
            TerrainHeightData terrainHeight,
            NativeQueue<KeyValuePair<Entity, QuadTreeBoundsXZ>> queue,
            out int nextIndex
        )
        {
            nextIndex = m_Config.startIndex;

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
                quadTree = visibleCandidates,
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
                visibleCandidates,
                map,
                m_Config,
                mapHandle,
                writer: queue.AsParallelWriter()
            );

            Mod.log.Info($"Summary: candidateCount({visibleCandidates.Count()}), mapHeightSize({map.heights.Length}), terrainCompleted?({terrainHandle.IsCompleted})");
            nextIndex = 0; // TODO implement batching

            terrainHeights.Dispose(mapHandle);
            objectHeights.Dispose(mapHandle);
            heights.Dispose(handle);
            return handle;
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
                int sector = index;
                CullingConfig cc = config;

                float angle = -halfFovRad + (sector + 0.5f) * map.sectorAngleStep;
                float sinA = math.sin(angle);
                float cosA = math.cos(angle);

                float2 dir = new float2(
                    forwardXZ.x * cosA - forwardXZ.y * sinA,
                    forwardXZ.x * sinA + forwardXZ.y * cosA
                );

                float previousResult = float.MinValue;
                for(int j = 0; j < map.binCount; j++)
                {
                    Bounds3 sampleBounds = default;
                    float r = map.distanceStep * (j+0.5f); // Put terrain max height into center of the bin, not edge of it.
                    float2 sampleXZ = new float2(cc.cameraPosition.x, cc.cameraPosition.z) + dir * r;

                    float stretch = r switch
                    {
                        >= 3000f => 0.5f,
                        >= 2000f => 0.35f,
                        >= 1000f => 0.15f,
                        _ => 0.1f
                    };
                    sampleBounds.min = new float3(sampleXZ.x - stretch, -3000f, sampleXZ.y - stretch);
                    sampleBounds.max = new float3(sampleXZ.x + stretch, 3000f, sampleXZ.y + stretch);
                    var heightRange = TerrainUtils.GetHeightRange(ref terrainHeightData, sampleBounds);

                    float terrainAngle = math.atan2(heightRange.max - cc.cameraPosition.y, r);
                    float maxAngle = math.max(previousResult, terrainAngle);
                    previousResult = maxAngle;
                    terrainHeights[(sector * map.binCount) + j] = maxAngle; // Parallel write at exact index
                }


            }
        }

        [BurstCompile]
        public struct RadialMapForObjects : IJobFor
        {
            public NativeArray<float> objectHeights;

            //[ReadOnly] public NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree;
            [ReadOnly] public NativeParallelHashMap<Entity, OcclusionCullingStruct> quadTree;
            [ReadOnly] public RadialHeightMap map;
            [ReadOnly] public float2 forwardXZ;
            [ReadOnly] public float halfFovRad;
            [ReadOnly] public CullingConfig config;
            [ReadOnly] public ComponentLookup<PrefabRef> PrefabRefLookup;
            [ReadOnly] public ComponentLookup<MeshData> MeshDataLookup;
            [ReadOnly] public ComponentLookup<Game.Objects.Transform> TransformLookup;
            [ReadOnly] public BufferLookup<SubMesh> SubMeshLookup;

            public void Execute(int index)
            {
                return;
                // Execute per sector as index, offset by 1
                int sector = index;
                CullingConfig cc = config;

                float angle = -halfFovRad + (sector + 0.5f) * map.sectorAngleStep;
                float sinA = math.sin(angle);
                float cosA = math.cos(angle);

                float2 dir = new float2(
                    forwardXZ.x * cosA - forwardXZ.y * sinA,
                    forwardXZ.x * sinA + forwardXZ.y * cosA
                );

                NativeList<KeyValuePair<Entity, QuadTreeBoundsXZ>> occluders = new(cc.maxOccluders, Allocator.Temp);

                float3 cameraDirectionXZ = math.normalizesafe(new float3(cc.cameraForward.x, 0f, cc.cameraForward.z), new float3(0f, 0f, 1f));
                float3 forwardPoint = cc.cameraPosition + (cameraDirectionXZ * cc.occluderMaxDistance);
                float3 min = new(math.min(cc.cameraPosition.x, forwardPoint.x), cc.cameraPosition.y - cc.maxDistance, math.min(cc.cameraPosition.z, forwardPoint.z));
                float3 max = new(math.max(cc.cameraPosition.x, forwardPoint.x), cc.cameraPosition.y + cc.maxDistance, math.max(cc.cameraPosition.z, forwardPoint.z));
                Bounds3 sampleBounds = new(min, max);

                var rq = new OcclusionUtilities.RegionQueryCollector
                {
                    searchRegion = sampleBounds,
                    results = occluders,
                    maxCount = cc.maxOccluders,
                    PrefabRefLookup = PrefabRefLookup,
                    MeshDataLookup = MeshDataLookup,
                    TransformLookup = TransformLookup,
                    SubMeshLookup = SubMeshLookup
                };
                //quadTree.Iterate(ref rq, 0);
                
                foreach (var occ in occluders)
                {
                    
                    ComputeRadialRangeForBounds(occ.Value, new float2(cameraDirectionXZ.x, cameraDirectionXZ.z), dir, cc.occluderMaxDistance, map.sectorAngleStep, map.sectorCount, out float minR, out float maxR, out int minSectorOffset, out int maxSectorOffset);
                    //int minBin = math.clamp((int)math.floor(minR / map.distanceStep), 0, cc.binCount - 1);
                    int maxBin = math.clamp((int)math.ceil(maxR / map.distanceStep), 0, cc.binCount - 1);

                    for(int j = maxBin; j< map.binCount; j++)
                    {
                        float r = map.distanceStep * j;
                        float occAngle = math.atan2(occ.Value.m_Bounds.max.y - cc.cameraPosition.y, r);
                        objectHeights[(sector * map.binCount) + j] = math.max(objectHeights[(sector * map.binCount) + j], occAngle);
                    }
                    
                }

            }

            static void ComputeRadialRangeForBounds(QuadTreeBoundsXZ occ, float2 camXZ, float2 sectorDir, float occluderMaxDistance, float sectorAngleStep, int sectorCount, out float minR, out float maxR, out int minSectorOffset, out int maxSectorOffset)
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

                // compute angular sector offsets relative to the provided sectorDir
                // center-based angle
                float3 center3 = (b.min + b.max) * 0.5f;
                float2 objXZ = new float2(center3.x, center3.z);
                float2 toObj = objXZ - camXZ;
                float dist = math.length(toObj);
                if (dist <= 0f)
                {
                    minSectorOffset = 0;
                    maxSectorOffset = 0;
                    return;
                }

                float camYawForSector = math.atan2(sectorDir.y, sectorDir.x);
                float angleCenter = math.atan2(toObj.y, toObj.x) - camYawForSector;
                if (angleCenter < -math.PI) angleCenter += math.PI * 2;
                else if (angleCenter > math.PI) angleCenter -= math.PI * 2;

                float halfWidth = (b.max.x - b.min.x) * 0.5f;
                float halfDepth = (b.max.z - b.min.z) * 0.5f;
                float objectRadius = math.length(new float2(halfWidth, halfDepth));
                float angRadius = math.atan2(objectRadius, dist);

                float minAngleOffset = angleCenter - angRadius;
                float maxAngleOffset = angleCenter + angRadius;

                // convert to sector offsets (relative to the provided sectorIndex = 0)
                minSectorOffset = (int)math.floor(minAngleOffset / sectorAngleStep);
                maxSectorOffset = (int)math.floor(maxAngleOffset / sectorAngleStep);

                // clamp offsets to reasonable range
                int clampVal = sectorCount;
                minSectorOffset = math.clamp(minSectorOffset, -clampVal, clampVal);
                maxSectorOffset = math.clamp(maxSectorOffset, -clampVal, clampVal);
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

        // Burst job for parallel sector culling
        [BurstCompile]
        public struct CandidateCollectorJob : IJob
        {
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> candidates;
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree;
            public NativeArray<int> candidateCount;
            [ReadOnly] public CullingConfig m_Config;

            public void Execute()
            {
                var collector = new OcclusionUtilities.CandidateCollector();

                float3 cameraDirectionXZ = math.normalizesafe(new float3(m_Config.cameraForward.x, 0f, m_Config.cameraForward.z), new float3(0f, 0f, 1f));
                collector.candidates = candidates;

                float3 forwardPoint = m_Config.cameraPosition + (cameraDirectionXZ * m_Config.maxDistance);
                float3 min = new(math.min(m_Config.cameraPosition.x, forwardPoint.x), m_Config.cameraPosition.y - (m_Config.maxDistance * 0.9f), math.min(m_Config.cameraPosition.z, forwardPoint.z));
                float3 max = new(math.max(m_Config.cameraPosition.x, forwardPoint.x), m_Config.cameraPosition.y + (m_Config.maxDistance * 0.4f), math.max(m_Config.cameraPosition.z, forwardPoint.z));
                collector.searchBounds = new QuadTreeBoundsXZ(
                    new Bounds3(min, max),
                    BoundsMask.NormalLayers,
                    0//RenderingUtils.CalculateLod(m_Config.maxDistance * m_Config.maxDistance, m_LodParameters); Switch to proper minLod filtering on the search tree
                );
                quadTree.Iterate(ref collector, 0);
                candidates = collector.candidates;
                candidateCount[0] = collector.candidateCount;
            }
        }


        // Burst job for parallel sector culling
        [BurstCompile]
        public struct CullBySectorJob : IJobFor
        {
            [ReadOnly] public NativeParallelHashMap<Entity, OcclusionCullingStruct> candidates;
            [ReadOnly] public NativeArray<Entity> indices;
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
                var entity = indices[index];
                var bounds = candidates[entity].m_Bounds;

                float sectorAngleStep = math.radians(config.fovDegrees) / config.sectorCount;

                float3 center = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                float2 objXZ = new float2(center.x, center.z);
                float2 toObj = objXZ - camXZ;
                float dist = math.length(toObj);
                
                if (dist <= 0f) return;

                float angle = math.atan2(toObj.y, toObj.x) - camYaw;

                if (angle < -math.PI) angle += math.PI * 2;
                else if (angle > math.PI) angle -= math.PI * 2;

                if (math.abs(angle) > halfFovRad) return;

                int sector = math.clamp((int)math.floor((angle + halfFovRad) / sectorAngleStep), 0, config.sectorCount - 1);
                int bin = math.clamp((int)(dist / distanceStep) - 1, 0, config.binCount - 1);
                
                float horizonCenter = heights[sector * config.binCount + bin];
                float objectHeightAdj = bounds.m_Bounds.max.y - config.clearanceMeters;
                float elevationAngle = math.atan2(objectHeightAdj - config.cameraPosition.y, dist);

                // If the height is almost the exact same angle, it's likely the same object
                // This is to prevent self-occlusion (may prevent true culls in rare circumstances, but likely faster)
                // float kSelfEpsilon = 1e-3f;
                // if (elevationAngle >= horizonCenter - kSelfEpsilon) return;

                if (elevationAngle < horizonCenter)
                {
                    // Additional checks to prevent self-culling and to ensure edges of object aren't still visible
                    float halfWidth = (bounds.m_Bounds.max.x - bounds.m_Bounds.min.x) * 0.5f;
                    float halfDepth = (bounds.m_Bounds.max.z - bounds.m_Bounds.min.z) * 0.5f;
                    float objectRadius = math.length(new float2(halfWidth, halfDepth));
                    float angRadius = math.atan2(objectRadius, dist);

                    int leftSector = math.clamp((int)math.floor((angle - angRadius + halfFovRad) / sectorAngleStep), 0, config.sectorCount - 1);
                    int rightSector = math.clamp((int)math.floor((angle + angRadius + halfFovRad) / sectorAngleStep), 0, config.sectorCount - 1);
                    float horizonLeft = heights[leftSector * config.binCount + bin];
                    float horizonRight = heights[rightSector * config.binCount + bin];

                    if (elevationAngle < horizonLeft && elevationAngle < horizonRight)
                    {
                        results.Enqueue(new KeyValuePair<Entity, QuadTreeBoundsXZ>(entity, bounds));
                    }
                }
            }
        }

        public static JobHandle ScheduleCullBySectorJob(

            NativeParallelHashMap<Entity, OcclusionCullingStruct> candidates,
            RadialHeightMap map,
            CullingConfig config,
            JobHandle dependency,
            NativeQueue<KeyValuePair<Entity, QuadTreeBoundsXZ>>.ParallelWriter writer)
        {
            // prepare inputs and output writer
            float2 camXZ = new float2(config.cameraPosition.x, config.cameraPosition.z);
            float2 forwardXZ = math.normalizesafe(new float2(config.cameraForward.x, config.cameraForward.z), new float2(0f,1f));
            float halfFovRad = math.radians(config.fovDegrees) * 0.5f;

            NativeHashMap<int, CullingConfig> hm_Config = new(1, Allocator.TempJob);
            NativeArray<Entity> indices = candidates.GetKeyArray(Allocator.TempJob);
            hm_Config.Add(0, config);

            // Is this a reasonable batchSize?
            int batchSize = 512;
            var job = new CullBySectorJob
            {
                candidates = candidates,
                indices = indices,
                heights = map.heights,
                camXZ = camXZ,
                forwardXZ = forwardXZ,
                camYaw = math.atan2(forwardXZ.y, forwardXZ.x),
                halfFovRad = halfFovRad,
                hm_Config = hm_Config,
                distanceStep = map.distanceStep,
                results = writer
            };
            JobHandle handle = job.ScheduleParallel(candidates.Count(), batchSize, dependency);
            hm_Config.Dispose(handle);
            indices.Dispose(handle);
            return handle;
        }
    }
}
