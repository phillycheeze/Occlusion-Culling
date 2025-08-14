using Unity.Collections;
using Unity.Entities;
using Colossal.Collections;
using Unity.Mathematics;
using Game.Common;
using Colossal.Mathematics;
using Game.Rendering;
using System.Text.RegularExpressions;
using Colossal.Logging;

namespace OcclusionCulling
{
    public static class OcclusionUtilities
    {
        // Utility functions for performing shadow-based culling

        private static ILog s_log = Mod.log;

        /// <summary>
        /// Shadow box calculaton behind nearby objects
        /// </summary>
        /// <param name="casterBounds">The bounds of the object casting the shadow</param>
        /// <param name="cameraPosition">Camera position acting as light source</param>
        /// <param name="fixedShadowDistance">Fixed shadow length (100-200m)</param>
        /// <returns>Simple rectangular shadow volume on ground plane</returns>
        private static QuadTreeBoundsXZ CalculateShadowBox(
            QuadTreeBoundsXZ casterBounds,
            float3 cameraPosition,
            float3 cameraDirection,
            float fixedShadowDistance)
        {
            var objectCenter = (casterBounds.m_Bounds.min + casterBounds.m_Bounds.max) * 0.5f;
            var objectSize = (casterBounds.m_Bounds.max - casterBounds.m_Bounds.min);

            // TODO verify this direction makes sense?
            float3 toCasterXZ = new float3(objectCenter.x - cameraPosition.x, 0f, objectCenter.z - cameraPosition.z);
            float3 direction = math.normalizesafe(toCasterXZ, new float3(0f, 0f, 1f));
            
            var shadowEnd = objectCenter + (direction * fixedShadowDistance);

            var shadowMin = new float3(
                math.min(objectCenter.x, shadowEnd.x) - objectSize.x * 0.5f,
                objectCenter.y - 1f, // Should be ignored but add tiny thickness just in case
                math.min(objectCenter.z, shadowEnd.z) - objectSize.z * 0.5f
            );
            var shadowMax = new float3(
                math.max(objectCenter.x, shadowEnd.x) + objectSize.x * 0.5f,
                objectCenter.y + 1f,
                math.max(objectCenter.z, shadowEnd.z) + objectSize.z * 0.5f
            );

            var pad = 10f; // Temp to visually see culling happen
            shadowMin.x -= pad;
            shadowMin.z -= pad;
            shadowMax.x += pad;
            shadowMax.z += pad;
            return new QuadTreeBoundsXZ(new Bounds3(shadowMin, shadowMax), BoundsMask.AllLayers, 0);
        }

        /// <summary>
        /// Single-pass occlusion test - no separate "find occluded" step
        /// Integrated directly into main culling loop for maximum performance
        /// </summary>
        /// <param name="candidateEntity">Entity being tested for occlusion</param>
        /// <param name="candidateBounds">Bounds of the candidate object</param>
        /// <param name="shadowBoxes">Pre-calculated shadow volumes (5-10 boxes max)</param>
        /// <param name="shadowCasterDistances">Distances from camera to each shadow caster</param>
        /// <param name="cameraPosition">Camera position for depth testing</param>
        /// <returns>True if object should be culled (is occluded)</returns>
        private static bool IsObjectOccluded(
            Entity candidateEntity,
            QuadTreeBoundsXZ candidateBounds,
            NativeList<QuadTreeBoundsXZ> shadowBoxes,
            NativeList<float> shadowCasterDistances,
            float3 cameraPosition,
            float3 cameraDirection)
        {
            if (shadowBoxes.Length == 0) return false;
            const float depthBuffer = 40f; // Only consider culling entities past 40m, otherwise it may include itself (TODO: fix this logic)

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

        // Returns occluded entities with their tree bounds (no mutations). Single traversal + post-filter.
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

        /// <summary>
        /// Single-pass collector that gathers both candidate entities and a capped set of shadow casters.
        /// This avoids a second quad tree traversal.
        /// </summary>
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
                // Always traverse to collect all candidates within range
                return bounds.Intersect(searchBounds);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
            {
                // Collect as candidate for post-pass occlusion testing
                candidates.Add((item, bounds));

                // Optionally collect as caster if it meets direction/size and we haven't reached the cap
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
