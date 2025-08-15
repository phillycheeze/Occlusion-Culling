using Unity.Collections;
using Unity.Entities;
using Colossal.Collections;
using Unity.Mathematics;
using Game.Common;
using Colossal.Mathematics;
using Game.Rendering;
using Colossal.Logging;
using Game.Simulation;
using UnityEngine;

namespace OcclusionCulling
{
    public static class OcclusionUtilities
    {
        private static ILog s_log = Mod.log;


        // TODO:
        //   1. Switch to NativeQuadTrees instead of NativeLists sets for candidates result, then re-use that for occlusion testing
        //   2. Precompute step bounds first, then check all of them at once in the Iterator (prevent rescanning tree each time)
        // 
		public static NativeList<(Entity entity, QuadTreeBoundsXZ bounds)> FindTerrainOccludedEntities(
			NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
			TerrainHeightData terrainHeight,
			float3 cameraPosition,
			float3 cameraDirection,
			float maxProcessingDistance = 1000f,
			int samplesPerRay = 12,
			float clearanceMeters = 0.5f,
			Allocator allocator = Allocator.TempJob)
		{
            var collector = new CandidateCollector(cameraPosition, cameraDirection, maxProcessingDistance);
            quadTree.Iterate(ref collector, 0);

            
            var sampleBounds = new Bounds3(default, default);
            float2 camXZ = new float2(cameraPosition.x, cameraPosition.z);
            float camY   = cameraPosition.y;
            int   steps  = math.max(2, samplesPerRay);
            float stepDistance = maxProcessingDistance / (steps - 1);

            var result = new NativeList<(Entity, QuadTreeBoundsXZ)>(allocator);
            var objectOccluders = new NativeList<(Entity, QuadTreeBoundsXZ)>(allocator);

            for (int ci = 0; ci < collector.candidates.Length; ci++)
            {
                var (entity, bounds) = collector.candidates[ci];
                float3 center3  = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                float2 centerXZ = new float2(center3.x, center3.z);
                float dist      = math.distance(camXZ, centerXZ);

                // Skip if distance is too far
                if (dist < 1f || dist > maxProcessingDistance) continue;
                float2 dir = (centerXZ - camXZ) / dist;
                float objectTopY = bounds.m_Bounds.max.y;
                bool hidden = false;

                for (int s = 0; s < steps; s++)
                {
                    // Break if the step distance is further than the candidate's distance
                    float r = stepDistance * s;
                    if ( r >= dist )
                    {
                        break;
                    }
                    float2 sampleXZ = camXZ + dir * r;
                    sampleBounds.min = new float3(sampleXZ.x - 0.1f, -10000f, sampleXZ.y - 0.1f);
                    sampleBounds.max = new float3(sampleXZ.x + 0.1f,  10000f, sampleXZ.y + 0.1f);
                    float terrainY = TerrainUtils.GetHeightRange(ref terrainHeight, sampleBounds).max;
                    float maxY = terrainY;
                    float tFrac = dist > 0f ? r / dist : 1f;
                    float losY = math.lerp(camY, objectTopY, tFrac);

                    // TODO: don't run this for every single candidate
                    // cache objects in region first, then compare in candidate loop
                    // Consider optional setting to disable object-based culling
                    objectOccluders.Clear();
                    var rq = new RegionQueryCollector { searchRegion = new QuadTreeBoundsXZ(sampleBounds, BoundsMask.AllLayers, 0), results = objectOccluders, maxCount=2 };
                    quadTree.Iterate(ref rq, 0);
                    
                    for (int j = 0; j < objectOccluders.Length; j++)
                    {
                        var (occEnt, occBounds) = objectOccluders[j];
                        
                        // Don't occlude self, not sure if needed
                        if (occEnt.Equals(entity)) continue;
                        
                        // Skip if occluder is further than candidate, could move math to outside loop
                        float3 occCenter = (occBounds.m_Bounds.min + occBounds.m_Bounds.max) * 0.5f;
                        float occDist = math.distance(camXZ, new float2(occCenter.x, occCenter.z));
                        if (occDist > dist) continue;

                        float y = occBounds.m_Bounds.max.y;
                        if (y > maxY)
                        {
                            maxY = y;
                        }
                    }
                    
                    if (maxY > losY - clearanceMeters)
                    {
                        hidden = true;
                        break;
                    }
                }
                if (hidden)
                    result.Add((entity, bounds));
            }
            objectOccluders.Dispose();
            collector.Dispose();
            return result;
		}

        public struct CandidateCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public float3 cameraPosition;
            public float3 cameraDirectionXZ;
            public float maxDistance;

            public NativeList<(Entity, QuadTreeBoundsXZ)> candidates;
            private QuadTreeBoundsXZ searchBounds;

            public CandidateCollector(float3 cameraPos, float3 cameraDir, float searchRadius)
            {
                cameraPosition = cameraPos;
                cameraDirectionXZ = math.normalizesafe(new float3(cameraDir.x, 0f, cameraDir.z), new float3(0f, 0f, 1f));
                maxDistance = searchRadius;
                candidates = new NativeList<(Entity, QuadTreeBoundsXZ)>(2000, Allocator.Temp);

                float3 forwardPoint = cameraPosition + (cameraDirectionXZ * maxDistance);
                float3 min = new float3(math.min(cameraPosition.x, forwardPoint.x), cameraPosition.y - maxDistance, math.min(cameraPosition.z, forwardPoint.z));
                float3 max = new float3(math.max(cameraPosition.x, forwardPoint.x), cameraPosition.y + maxDistance, math.max(cameraPosition.z, forwardPoint.z));
                searchBounds = new QuadTreeBoundsXZ(
                    new Bounds3(min, max),
                    BoundsMask.AllLayers,
                    0
                );
            }

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                return bounds.Intersect(searchBounds);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
            {
                //if (bounds.m_MinLod == byte.MaxValue) return;
                candidates.Add((item, bounds));
            }

            public void Dispose()
            {
                if (candidates.IsCreated) candidates.Dispose();
            }
        }

        public struct RegionQueryCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public QuadTreeBoundsXZ searchRegion;
            public NativeList<(Entity, QuadTreeBoundsXZ)> results;
            public int maxCount;

            private int count;

            public bool Intersect(QuadTreeBoundsXZ nodeBounds)
            {
                return nodeBounds.Intersect(searchRegion);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
            {
                if (count >= maxCount) return;

                var objectSize = (bounds.m_Bounds.max - bounds.m_Bounds.min);
                var minDimension = math.min(math.min(objectSize.x, objectSize.y), objectSize.z);
                var maxDimension = math.max(math.max(objectSize.x, objectSize.y), objectSize.z);

                if (minDimension > 10f && maxDimension > 20f)
                {
                    results.Add((item, bounds));
                    count++;
                }
            }

            public void Dispose()
            {
                if (results.IsCreated) results.Dispose();
            }
        }
    }
}


