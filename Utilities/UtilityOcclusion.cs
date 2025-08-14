using Unity.Collections;
using Unity.Entities;
using Colossal.Collections;
using Unity.Mathematics;
using Game.Common;
using Colossal.Mathematics;
using Game.Rendering;
using Colossal.Logging;

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
            var objectCenter = (casterBounds.m_Bounds.min + casterBounds.m_Bounds.max) * 0.5f;
            var objectSize = (casterBounds.m_Bounds.max - casterBounds.m_Bounds.min);

            float3 toCasterXZ = new float3(objectCenter.x - cameraPosition.x, 0f, objectCenter.z - cameraPosition.z);
            float3 direction = math.normalizesafe(toCasterXZ, new float3(0f, 0f, 1f));
            var shadowEnd = objectCenter + (direction * fixedShadowDistance);

            var shadowMin = new float3(
                math.min(objectCenter.x, shadowEnd.x) - objectSize.x * 0.5f,
                objectCenter.y - 1f,
                math.min(objectCenter.z, shadowEnd.z) - objectSize.z * 0.5f
            );
            var shadowMax = new float3(
                math.max(objectCenter.x, shadowEnd.x) + objectSize.x * 0.5f,
                objectCenter.y + 1f,
                math.max(objectCenter.z, shadowEnd.z) + objectSize.z * 0.5f
            );

            const float pad = 0f;
            return new QuadTreeBoundsXZ(new Bounds3(
                new float3(shadowMin.x - pad, shadowMin.y, shadowMin.z - pad),
                new float3(shadowMax.x + pad, shadowMax.y, shadowMax.z + pad)
            ), BoundsMask.AllLayers, 0);
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

        public static NativeList<(Entity entity, QuadTreeBoundsXZ bounds)> FindOccludedEntities(
            NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
            float3 cameraPosition,
            float3 cameraDirection,
            float maxProcessingDistance = 250f,
            Allocator allocator = Allocator.TempJob)
        {
            var result = new NativeList<(Entity, QuadTreeBoundsXZ)>(allocator);

            var collector = new CandidateAndShadowCasterCollector(
                cameraPosition,
                cameraDirection,
                maxProcessingDistance,
                0.1f,
                32
            );
            quadTree.Iterate(ref collector, 0);

            if (collector.casters.Length == 0)
            {
                collector.Dispose();
                return result;
            }

		// Minimal terrain LOS occlusion test (POC). No jobs; coarse sampling; main-thread friendly for quick validation.
		// For each candidate, it samples terrain heights along the line from camera to object center and checks if
		// terrain rises above the line-of-sight to the object's top. If so, it's considered terrain-occluded.
		public static NativeList<(Entity entity, QuadTreeBoundsXZ bounds)> FindTerrainOccludedEntities(
			NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
			TerrainHeightData terrainHeight,
			float3 cameraPosition,
			float3 cameraDirection,
			float maxProcessingDistance = 600f,
			int samplesPerRay = 12,
			float clearanceMeters = 0.5f,
			Allocator allocator = Allocator.TempJob)
		{
			var result = new NativeList<(Entity, QuadTreeBoundsXZ)>(allocator);

			// Reuse existing collector to get a small set of forward-facing candidates within radius
			var collector = new CandidateAndShadowCasterCollector(
				cameraPosition,
				cameraDirection,
				maxProcessingDistance,
				0.1f,
				32
			);
			quadTree.Iterate(ref collector, 0);

			for (int i = 0; i < collector.candidates.Length; i++)
			{
				var (entity, bounds) = collector.candidates[i];
				float3 center = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
				float2 toXZ = new float2(center.x - cameraPosition.x, center.z - cameraPosition.z);
				float distXZ = math.length(toXZ);
				if (distXZ < 1f || distXZ > maxProcessingDistance)
					continue;

				// Use the object's top as a conservative visibility target
				float targetY = bounds.m_Bounds.max.y;
				float2 dirXZ = toXZ / distXZ;

				bool occludedByTerrain = false;
				int steps = math.max(2, samplesPerRay);
				for (int s = 1; s < steps; s++)
				{
					float r = (distXZ * s) / steps;
					float2 sampleXZ = new float2(cameraPosition.x, cameraPosition.z) + dirXZ * r;

					// Sample terrain height using a tiny bounds at this XZ
					var sampleBounds = new Bounds3(
						new float3(sampleXZ.x - 0.5f, -10000f, sampleXZ.y - 0.5f),
						new float3(sampleXZ.x + 0.5f,  10000f, sampleXZ.y + 0.5f)
					);
					float terrainY = TerrainUtils.GetHeightRange(ref terrainHeight, sampleBounds).max;

					// Expected LOS height at distance r (towards the object's top)
					float losY = math.lerp(cameraPosition.y, targetY, r / distXZ);

					if (terrainY > (losY - clearanceMeters))
					{
						occludedByTerrain = true;
						break;
					}
				}

				if (occludedByTerrain)
				{
					result.Add((entity, bounds));
				}
			}

			collector.Dispose();
			return result;
		}

            var shadowBoxes = new NativeList<QuadTreeBoundsXZ>(collector.casters.Length, Allocator.Temp);
            var casterDistances = new NativeList<float>(collector.casters.Length, Allocator.Temp);
            for (int i = 0; i < collector.casters.Length; i++)
            {
                var caster = collector.casters[i];
                var shadowBox = CalculateShadowBox(caster.bounds, cameraPosition, cameraDirection, maxProcessingDistance);
                var distance = math.distance(cameraPosition, (caster.bounds.m_Bounds.min + caster.bounds.m_Bounds.max) * 0.5f);
                shadowBoxes.Add(shadowBox);
                casterDistances.Add(distance);
            }

            for (int i = 0; i < collector.candidates.Length; i++)
            {
                var (entity, bounds) = collector.candidates[i];
                if (IsObjectOccluded(entity, bounds, shadowBoxes, casterDistances, cameraPosition, cameraDirection))
                {
                    result.Add((entity, bounds));
                }
            }

            shadowBoxes.Dispose();
            casterDistances.Dispose();
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
                if (minDimension > 3f && maxDimension > 8f)
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
    }
}


