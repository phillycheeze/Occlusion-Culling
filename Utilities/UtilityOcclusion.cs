using Unity.Collections;
using Unity.Entities;
using Colossal.Collections;
using Unity.Mathematics;
using Game.Common;
using Colossal.Mathematics;
using Game.Rendering;
using Colossal.Logging;
using Game.Simulation;

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
            // Tech: Prepare screen-space occlusion height map for terrain and objects
            // User: Shoot many view-lines from your camera and see how high each line is blocked
            int numRays = 32;
            int numSamples = math.max(2, samplesPerRay);
            var occlusionHeightMap = new NativeArray<float>(numRays * numSamples, Allocator.Temp);
            var objectOccluders = new NativeList<(Entity, QuadTreeBoundsXZ)>(allocator);

            float2 cameraXZ = new float2(cameraPosition.x, cameraPosition.z);
            float cameraHeight = cameraPosition.y;
            float2 cameraForwardXZ = math.normalize(new float2(cameraDirection.x, cameraDirection.z));
            float halfFovRad = math.radians(45f);

            Bounds3 sampleBounds = new Bounds3(
                new float3(0f, -10000f, 0f),
                new float3(0f,  10000f, 0f)
            );

            for (int rayIndex = 0; rayIndex < numRays; rayIndex++)
            {

                
                // Tech: compute normalized ray index [0=left edge, 1=right edge] across all rays
                // User: figure out which view-line from leftmost to rightmost this is
                float rayFrac = rayIndex / (float)(numRays - 1);

                // Tech: map normalized fraction into signed angle offset within half-field-of-view
                // User: tilt this view-line left or right across your viewing cone
                float rayAngle = (rayFrac * 2f - 1f) * halfFovRad;

                // Tech: compute cosine and sine once to rotate the camera's forward vector by rayAngle
                // User: turn your forward direction by that angle to get the ray direction
                float cosA = math.cos(rayAngle);
                float sinA = math.sin(rayAngle);

                float2 rayDirXZ = new float2(
                    cameraForwardXZ.x * cosA - cameraForwardXZ.y * sinA,
                    cameraForwardXZ.x * sinA + cameraForwardXZ.y * cosA
                );
                float stepDistance = maxProcessingDistance / numSamples;

                s_log.Info($"Inside FindTerrainOccludedEntities, first for loop processing ray: rayIndex({rayIndex}), rayAngle({rayAngle}), rayDirXZ({rayDirXZ}), cameraXZ({cameraXZ}), cameraForwardXZ({cameraForwardXZ})");
                for (int sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
                {
                    // Tech: march along the ray by equal steps
                    // User: pick points evenly spaced on the view-line
                    float distanceAlongRay = stepDistance * (sampleIndex + 1);
                    float2 sampleXZ = cameraXZ + rayDirXZ * distanceAlongRay;

                    sampleBounds.min.x = sampleXZ.x - 0.5f;
                    sampleBounds.min.z = sampleXZ.y - 0.5f;
                    sampleBounds.max.x = sampleXZ.x + 0.5f;
                    sampleBounds.max.z = sampleXZ.y + 0.5f;

                    // Tech: get terrain height at this sample
                    float terrainHeightSample = TerrainUtils.GetHeightRange(ref terrainHeight, sampleBounds).max;

                    // Tech: get highest object height in this area
                    var rq = new RegionQueryCollector {
                        searchRegion = new QuadTreeBoundsXZ(sampleBounds, BoundsMask.AllLayers, 0),
                        results = objectOccluders
                    };
                    quadTree.Iterate(ref rq, 0);

                    float highestOccluderY = terrainHeightSample;
                    for (int occIdx = 0; occIdx < objectOccluders.Length; occIdx++)
                    {
                        float topY = objectOccluders[occIdx].Item2.m_Bounds.max.y;
                        if (topY > highestOccluderY) highestOccluderY = topY;
                    }

                    occlusionHeightMap[rayIndex * numSamples + sampleIndex] = highestOccluderY;
                }

                s_log.Info($"Inside FindTerrainOccludedEntities, terrain result for rayIndex({rayIndex}), currentOcclusionHeightMapCount({occlusionHeightMap.Length})");
            }

            

            // Tech: gather candidate objects within view cone
            // User: pick the objects you might be looking at
            var candidateCollector = new CandidateAndShadowCasterCollector(
                cameraPosition,
                cameraDirection,
                maxProcessingDistance,
                0.1f,
                32
            );
            quadTree.Iterate(ref candidateCollector, 0);
            int candidateCount = candidateCollector.candidates.Length;
            var occludedEntities = new NativeList<(Entity, QuadTreeBoundsXZ)>(candidateCount, allocator);

            s_log.Info($"Inside FindTerrainOccludedEntities, found candidates: candidateCount({candidateCount})");

            for (int candIdx = 0; candIdx < candidateCount; candIdx++)
            {
                // Tech: compute object direction and distance
                // User: see where each object is relative to you
                var (entity, bounds) = candidateCollector.candidates[candIdx];
                float3 objectCenter = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                float2 toObjectXZ = new float2(objectCenter.x - cameraXZ.x, objectCenter.z - cameraXZ.y);
                float distToObject = math.length(toObjectXZ);
                if (distToObject < 1f || distToObject > maxProcessingDistance) continue;
                float2 dirToObject = math.normalize(toObjectXZ);

                // Tech: map direction to ray index
                float signedAngle = math.atan2(
                    cameraForwardXZ.x * dirToObject.y - cameraForwardXZ.y * dirToObject.x,
                    math.dot(cameraForwardXZ, dirToObject)
                );
                int rayIdx = (int)math.clamp((signedAngle / halfFovRad * 0.5f + 0.5f) * (numRays - 1), 0, numRays - 1);

                // Tech: map distance to sample index
                int sampleIdx = (int)math.clamp(distToObject / maxProcessingDistance * (numSamples-1), 0, numSamples - 1);

                float occlusionHeight = occlusionHeightMap[rayIdx * numSamples + sampleIdx];

                // Tech: compute line-of-sight height to object top
                float objectTopY = bounds.m_Bounds.max.y;
                float lodFrac = (sampleIdx + 1) / (float)numSamples;
                float losHeight = math.lerp(cameraHeight, objectTopY, lodFrac);

                // Tech: if occlusion height is higher, object is hidden
                // User: if a hill or another object blocks your view, this object is hidden
                if (occlusionHeight > (losHeight - clearanceMeters))
                {
                    occludedEntities.Add((entity, bounds));
                }
            }

            // Tech: clean up temporary buffers
            // User: finished checking all objects
            objectOccluders.Dispose();
            occlusionHeightMap.Dispose();
            candidateCollector.Dispose();

            return occludedEntities;
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


