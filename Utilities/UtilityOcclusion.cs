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

        private static QuadTreeBoundsXZ CalculateShadowBox(
            QuadTreeBoundsXZ casterBounds,
            float3 cameraPosition,
            float3 cameraDirection,
            float fixedShadowDistance)
        {
            // Simple footprint box: ground-level min corner to opposite top corner
            float2 minXZ = new float2(casterBounds.m_Bounds.min.x, casterBounds.m_Bounds.min.z);
            float2 maxXZ = new float2(casterBounds.m_Bounds.max.x, casterBounds.m_Bounds.max.z);

            float3 minCorner = new float3(minXZ.x, casterBounds.m_Bounds.min.y, minXZ.y);
            float3 maxCorner = new float3(maxXZ.x, casterBounds.m_Bounds.max.y, maxXZ.y);

            return new QuadTreeBoundsXZ(
                new Bounds3(minCorner, maxCorner),
                BoundsMask.AllLayers,
                0
            );
        }

        private static bool IsObjectOccluded(
            Entity candidateEntity,
            QuadTreeBoundsXZ candidateBounds,
            NativeList<QuadTreeBoundsXZ> shadowBoxes,
            NativeList<float> shadowCasterDistances,
            float3 cameraPosition,
            float3 cameraDirection)
        {
            if (shadowBoxes.Length == 0) return false;
            const float depthBuffer = 40f;

            var candidateCenter = (candidateBounds.m_Bounds.min + candidateBounds.m_Bounds.max) * 0.5f;
            var candidateDistance = math.distance(cameraPosition, candidateCenter);

            for (int i = 0; i < shadowBoxes.Length; i++)
            {
                var casterDistance = shadowCasterDistances[i];
                if (candidateDistance <= casterDistance + depthBuffer) continue;

                var shadowBox = shadowBoxes[i];
                if (candidateBounds.Intersect(shadowBox)) return true;
            }
            return false;
        }

		// Minimal terrain LOS occlusion test (POC). Just for testing right now.
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
            // Tech: one-pass per-candidate visibility test via ray-marching
            // User: for each object, shoot a line to its top and see if anything blocks it
            var result = new NativeList<(Entity, QuadTreeBoundsXZ)>(allocator);
            var collector = new CandidateAndShadowCasterCollector(
                cameraPosition, cameraDirection, maxProcessingDistance, 0.1f, 32);
            quadTree.Iterate(ref collector, 0);
            int candidateCount = collector.candidates.Length;
            var objectOccluders = new NativeList<(Entity, QuadTreeBoundsXZ)>(allocator);
            var sampleBounds = new Bounds3(default, default);
            float2 camXZ = new float2(cameraPosition.x, cameraPosition.z);
            float camY   = cameraPosition.y;
            int   steps  = math.max(2, samplesPerRay);
            float stepDistance = maxProcessingDistance / (steps - 1);

            for (int ci = 0; ci < candidateCount; ci++)
            {
                var (entity, bounds) = collector.candidates[ci];
                float3 center3  = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                float2 centerXZ = new float2(center3.x, center3.z);
                float dist      = math.distance(camXZ, centerXZ);
                if (dist < 1f || dist > maxProcessingDistance) continue;
                float2 dir = (centerXZ - camXZ) / dist;
                float objectTopY = bounds.m_Bounds.max.y;
                bool hidden = false;

                for (int s = 0; s < steps; s++)
                {
                    float r = stepDistance * s;
                    float2 sampleXZ = camXZ + dir * r;
                    sampleBounds.min = new float3(sampleXZ.x - 0.5f, -10000f, sampleXZ.y - 0.5f);
                    sampleBounds.max = new float3(sampleXZ.x + 0.5f,  10000f, sampleXZ.y + 0.5f);
                    float terrainY = TerrainUtils.GetHeightRange(ref terrainHeight, sampleBounds).max;
                    objectOccluders.Clear();
                    var rq = new RegionQueryCollector { searchRegion = new QuadTreeBoundsXZ(sampleBounds, BoundsMask.AllLayers, 0), results = objectOccluders };
                    quadTree.Iterate(ref rq, 0);
                    float maxY = terrainY;
                    for (int j = 0; j < objectOccluders.Length; j++)
                    {
                        var occBound = objectOccluders[j].Item2.m_Bounds;
                        float y = occBound.max.y;
                        if (y > maxY) maxY = y;
                    }
                    float tFrac = dist > 0f ? r / dist : 1f;
                    float losY = math.lerp(camY, objectTopY, tFrac);
                    if (maxY > losY - clearanceMeters)
                    {
                        s_log.Info($"Debug: culling {entity} at r={r:F1}, maxY={maxY:F1}, losY={losY:F1}");
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

        public struct CandidateAndShadowCasterCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public float3 cameraPosition;
            public float3 cameraDirectionXZ;
            public float maxDistance;
            public float minDot;
            public int maxCasterCount;

            private int casterCount;
            public NativeList<(Entity, QuadTreeBoundsXZ)> candidates;
            public NativeList<(Entity, QuadTreeBoundsXZ)> casters;
            private QuadTreeBoundsXZ searchBounds;

            public CandidateAndShadowCasterCollector(float3 cameraPos, float3 cameraDir, float searchRadius, float mDot, int maxShadowCasters)
            {
                cameraPosition = cameraPos;
                cameraDirectionXZ = math.normalizesafe(new float3(cameraDir.x, 0f, cameraDir.z), new float3(0f, 0f, 1f));
                maxDistance = searchRadius;
                minDot = mDot;
                maxCasterCount = maxShadowCasters;
                casterCount = 0;
                candidates = new NativeList<(Entity, QuadTreeBoundsXZ)>(Allocator.Temp);
                casters = new NativeList<(Entity, QuadTreeBoundsXZ)>(maxShadowCasters, Allocator.Temp);
                searchBounds = new QuadTreeBoundsXZ(new Bounds3(cameraPosition - maxDistance, cameraPosition + maxDistance), BoundsMask.AllLayers, 0);
            }

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                return bounds.Intersect(searchBounds);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
            {
                candidates.Add((item, bounds));

                if (casterCount >= maxCasterCount) return;

                float3 center = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                float3 toCenterXZ = math.normalizesafe(new float3(center.x - cameraPosition.x, 0f, center.z - cameraPosition.z), new float3(0f, 0f, 1f));
                if (math.dot(toCenterXZ, cameraDirectionXZ) < minDot) return;

                var objectSize = (bounds.m_Bounds.max - bounds.m_Bounds.min);
                var minDimension = math.min(math.min(objectSize.x, objectSize.y), objectSize.z);
                var maxDimension = math.max(math.max(objectSize.x, objectSize.y), objectSize.z);
                if (minDimension > 7f && maxDimension > 12f)
                {
                    casters.Add((item, bounds));
                    casterCount++;
                }
            }

            public void Dispose()
            {
                if (candidates.IsCreated) candidates.Dispose();
                if (casters.IsCreated) casters.Dispose();
            }
        }

        public struct RegionQueryCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public QuadTreeBoundsXZ searchRegion;
            public NativeList<(Entity, QuadTreeBoundsXZ)> results;

            public bool Intersect(QuadTreeBoundsXZ nodeBounds)
            {
                return nodeBounds.Intersect(searchRegion);
            }

            public void Iterate(QuadTreeBoundsXZ itemsBounds, Entity itemEntity)
            {
                results.Add((itemEntity, itemsBounds));
            }
        }
    }
}


