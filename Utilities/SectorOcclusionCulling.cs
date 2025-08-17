using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Simulation;
using Unity.Burst;
using Unity.Jobs;
using Game.Common;
using System.Runtime.InteropServices;
using Colossal.Logging;
using Game.Buildings;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Colossal.Internal.Gizmos;
using Game.Rendering;

namespace OcclusionCulling
{
    /// <summary>
    /// Implements a sector-based radial height map occlusion culling.
    /// </summary>
    public static class SectorOcclusionCulling
    {
        /// <summary>
        /// Per-sector horizon heights at discrete distances.
        /// </summary>
        public struct RadialHeightMap
        {
            public NativeArray<float> heights;
            public NativeHashSet<float3> points;
            public int sectorCount;
            public int binCount;
            public float maxDistance;
            public float clearance;
            internal float sectorAngleStep;
            internal float distanceStep;

            public void Dispose()
            {
                if (heights.IsCreated) heights.Dispose();
                if (points.IsCreated) points.Dispose();
            }
        }

        /// <summary>
        /// Build a radial height map from camera viewpoint.
        /// </summary>
        public static RadialHeightMap BuildRadialHeightMap(
            NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
            EntityManager entityManager,
            TerrainHeightData terrainHeight,
            float3 cameraPosition,
            float3 cameraForward,
            float fovDegrees,
            int sectorCount,
            int binCount,
            float maxDistance,
            float clearanceMeters,
            Allocator allocator,
            float occluderMaxDistance = 200f,
            int maxOccluders = 2)
        {
            // ensure true-geometry queries use the correct EntityManager
            OcclusionUtilities.entityManager = entityManager;
            var map = new RadialHeightMap
            {
                heights = new NativeArray<float>(sectorCount * binCount, allocator, NativeArrayOptions.ClearMemory),
                points = new NativeHashSet<float3>(1, allocator),
                sectorCount = sectorCount,
                binCount = binCount,
                maxDistance = maxDistance,
                clearance = clearanceMeters,
                sectorAngleStep = math.radians(fovDegrees) / sectorCount,
                distanceStep = maxDistance / (binCount - 1)
            };

            // prepare a reusable list for object occluders
            var occluders = new NativeList<CullingCandidate>(maxOccluders, Allocator.Temp);
            float2 forwardXZ = math.normalizesafe(new float2(cameraForward.x, cameraForward.z), new float2(0f, 1f));
            float halfFovRad = math.radians(fovDegrees) * 0.5f;
            var sampleBounds = new Bounds3(default, default);
            float camY = cameraPosition.y;

            for (int i = 0; i < map.sectorCount; i++)
            {
                float angle = -halfFovRad + (i + 0.5f) * map.sectorAngleStep;
                float sinA = math.sin(angle);
                float cosA = math.cos(angle);

                float2 dir2 = new float2(
                    forwardXZ.x * cosA - forwardXZ.y * sinA,
                    forwardXZ.x * sinA + forwardXZ.y * cosA
                );

                for (int j = 0; j < map.binCount; j++)
                {
                    int idx = i * map.binCount + j;
                    if (j == 0)
                    {
                        //map.heights[idx] = float.MinValue;
                        //continue;
                    }
                    float r = map.distanceStep * j;
                    float2 sampleXZ = new float2(cameraPosition.x, cameraPosition.z) + dir2 * r;
                    sampleBounds.min = new float3(sampleXZ.x - 0.1f, -10000f, sampleXZ.y - 0.1f);
                    sampleBounds.max = new float3(sampleXZ.x + 0.1f, 10000f, sampleXZ.y + 0.1f);

                    var heightRange = TerrainUtils.GetHeightRange(ref terrainHeight, sampleBounds);
                    float terrainAngle = math.atan2(heightRange.max - camY, r);
                    float maxAngle = math.max(map.heights[idx - 1], terrainAngle);
                    // fold in object occluders within threshold
                    if (r <= occluderMaxDistance)
                    {
                        occluders.Clear();
                        var region = new QuadTreeBoundsXZ(sampleBounds, BoundsMask.AllLayers, 0);
                        var rq = new OcclusionUtilities.RegionQueryCollector { searchRegion = region, results = occluders, maxCount = maxOccluders };
                        quadTree.Iterate(ref rq, 0);
                        for (int k = 0; k < occluders.Length; k++)
                        {
                            var occ = occluders[k];
                            float occAngle = math.atan2(occ.bounds.m_Bounds.max.y - camY, r);
                            maxAngle = math.max(maxAngle, occAngle);
                        }
                    }
                    map.heights[idx] = maxAngle;
                    map.points.Add(new float3(dir2.x, maxAngle, dir2.y));
                }
            }
            occluders.Dispose();
            return map;
        }

        /// <summary>
        /// Perform occlusion culling using the radial height map.
        /// </summary>
        public static NativeList<CullingCandidate> CullByRadialMap(
            NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
            EntityManager entityManager,
            TerrainHeightData terrainHeight,
            float3 cameraPosition,
            float3 cameraForward,
            out int nextIndex,
            out NativeHashSet<float3> points,
            Allocator allocator = Allocator.Temp,
            float fovDegrees = 90f,
            int sectorCount = 128,
            int binCount = 128,
            float maxDistance = 1000f,
            float clearanceMeters = 0.5f,
            float occluderMaxDistance = 200f,
            int maxOccluders = 2,
            int startIndex = 0,
            int maxResults = int.MaxValue)
        {
            Mod.log.Info($"SectorCulling: starting cullbyradialmap");
            // initialize and route EntityManager for queries
            OcclusionUtilities.entityManager = entityManager;
            // pagination start
            nextIndex = startIndex;
            // Build horizon map
            var map = BuildRadialHeightMap(quadTree, entityManager, terrainHeight, cameraPosition, cameraForward, fovDegrees, sectorCount, binCount, maxDistance, clearanceMeters, allocator, occluderMaxDistance, maxOccluders);
            Mod.log.Info($"SectorCulling: completed radial map, sample: {map.heights[0]}, sampleLast: {map.heights[map.heights.Length - 1]}");

            // Gather candidates using existing collector
            var collector = new OcclusionUtilities.CandidateCollector(cameraPosition, cameraForward, maxDistance);
            quadTree.Iterate(ref collector, 0);

            var result = new NativeList<CullingCandidate>(collector.candidates.Length, allocator);
            // Schedule and execute the burst-backed culling job
            var handle = ScheduleCullBySectorJob(
                collector.candidates,
                map,
                cameraPosition,
                cameraForward,
                fovDegrees,
                result,
                batchSize: 256,
                dependency: default);

            Mod.log.Info($"SectorCulling: Job completed, {result.Length} results found");
            handle.Complete();
            // Cleanup
            map.Dispose();
            collector.Dispose();
            nextIndex = 0;
            points = map.points;
            return result;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CullingCandidate
        {
            public Entity entity;
            public QuadTreeBoundsXZ bounds;
        }
        
        // Burst job for parallel sector culling
        //[BurstCompile]
        public struct CullBySectorJob : IJobFor
        {
            [ReadOnly] public NativeArray<CullingCandidate> candidates;
            [ReadOnly] public NativeArray<float> heights;
            [ReadOnly] public int sectorCount;
            [ReadOnly] public int binCount;
            [ReadOnly] public float halfFovRad;
            [ReadOnly] public float distanceStep;
            [ReadOnly] public float2 camXZ;
            [ReadOnly] public float2 forwardXZ;
            [ReadOnly] public float camY;
            [ReadOnly] public float clearance;
            public NativeList<CullingCandidate>.ParallelWriter results;

            public void Execute(int index)
            {
                var item = candidates[index];
                var entity = item.entity;
                var bounds = item.bounds;
                float3 center = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                float2 objXZ = new float2(center.x, center.z);
                float2 toObj = objXZ - camXZ;
                float dist = math.length(toObj);
                if (dist <= 0f) return;
                float angle = math.atan2(toObj.y, toObj.x) - math.atan2(forwardXZ.y, forwardXZ.x);
                // normalize to [-π,π]
                if (angle < -math.PI) angle += math.PI * 2;
                else if (angle > math.PI) angle -= math.PI * 2;
                if (index == 1)
                {
                    Mod.log.Info($"SectorCulling: sampledCandidateLoop. entity({entity.Index}), center({center}), objXZ({objXZ}), dist({dist}), angle({angle}), halfFovRad({halfFovRad})");
                }
                if (math.abs(angle) > halfFovRad) return;
                int sector = math.clamp((int)((angle + halfFovRad) / (2 * halfFovRad) * sectorCount), 0, sectorCount - 1);
                int bin = math.clamp((int)(dist / distanceStep), 0, binCount - 1);
                //float horizon = heights[sector * binCount + bin];
                //float objTopY = bounds.m_Bounds.max.y;
                float horizonAngle = heights[sector * binCount + bin];
                float objectHeightAdj = bounds.m_Bounds.max.y - clearance;
                float elevationAngle = math.atan2(objectHeightAdj - camY, dist);
                if (elevationAngle <= horizonAngle)
                {
                    results.AddNoResize(item);
                }
            }
        }

        /// <summary>
        /// Schedule the sector-culling job as an IJobFor.
        /// </summary>
        public static JobHandle ScheduleCullBySectorJob(
            NativeList<CullingCandidate> candidateList,
            RadialHeightMap map,
            float3 cameraPosition,
            float3 cameraForward,
            float fovDegrees,
            NativeList<CullingCandidate> resultList,
            int batchSize,
            JobHandle dependency)
        {
            // prepare inputs and output writer
            int count = candidateList.Length;
            var candidates = candidateList.AsArray();
            Mod.log.Info($"SectorCullingScheduler: {candidates.Length} vs {candidateList.Length}");
            resultList.Clear();
            var writer = resultList.AsParallelWriter();
            float2 camXZ = new float2(cameraPosition.x, cameraPosition.z);
            float2 forwardXZ = math.normalizesafe(new float2(cameraForward.x, cameraForward.z), new float2(0f,1f));
            float halfFovRad = math.radians(fovDegrees) * 0.5f;

            var job = new CullBySectorJob
            {
                candidates = candidates,
                heights = map.heights,
                sectorCount = map.sectorCount,
                binCount = map.binCount,
                halfFovRad = halfFovRad,
                distanceStep = map.distanceStep,
                camXZ = camXZ,
                forwardXZ = forwardXZ,
                camY = cameraPosition.y,
                clearance = map.clearance,
                results = writer
            };
            return IJobForExtensions.ScheduleParallelByRef(ref job, candidates.Length, batchSize, dependency);
        }
        
    }
}
