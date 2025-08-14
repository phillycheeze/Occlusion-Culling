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
        /// Efficiently finds nearby, large objects using heirarchal spatial iteration
        /// </summary>
        /// <param name="quadTree">The static object search tree</param>
        /// <param name="cameraPosition">Current camera world position</param>
        /// <param name="maxDistance">Maximum distance to consider objects (200-500m)</param>
        /// <returns>Very small list of the largest shadow caster candidates</returns>
        private static NativeList<(Entity entity, QuadTreeBoundsXZ bounds)> FindShadowCasters(
            NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
            float3 cameraPosition,
            float3 cameraDirection,
            float maxDistance = 20f,
            float minDot = 0.1f
        )
        {
            var collector = new ShadowCasterCollector(cameraPosition, cameraDirection, maxDistance, minDot, 32);
            quadTree.Iterate(ref collector, 0);
            return collector.m_Bounds;
        }

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

        /// <summary>
        /// Simple cache for shadow data to avoid recalculating every frame
        /// </summary>
        private static class ShadowCache
        {
            // Cache shadow boxes for multiple frames when camera movement is minimal
            // public static int lastFrameCalculated = -1;
            // public static float3 lastCameraPosition;
            // public static NativeList<Bounds3> cachedShadowBoxes;
            // public static NativeList<float> cachedCasterDistances;
            // 
            // Reset cache when camera moves > 50m or game loads new area
            // Dramatically reduces per-frame shadow calculation overhead
        }

        /// <summary>
        /// Minimal iterator to find the first entity for testing
        /// </summary>
        public struct ShadowCasterCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public float3 cameraPosition;
            public float3 cameraDirection;
            public float maxDistance;
            public float minDot;
            public int maxCount;

            private int count;
            public NativeList<(Entity, QuadTreeBoundsXZ)> m_Bounds;
            private QuadTreeBoundsXZ searchBounds;

            public ShadowCasterCollector(float3 cameraPos, float3 cameraDir, float searchRadius, float mDot, int maxShadowCasters)
            {
                cameraPosition = cameraPos;
                cameraDirection = math.normalizesafe(new float3(cameraDir.x, 0f, cameraDir.z), new float3(0f, 0f, 1f));
                maxDistance = searchRadius;
                minDot = mDot;
                maxCount = maxShadowCasters;
                count = 0;
                m_Bounds = new NativeList<(Entity, QuadTreeBoundsXZ)>(maxShadowCasters, Allocator.Temp);

                searchBounds = new QuadTreeBoundsXZ(new Bounds3(cameraPosition - maxDistance, cameraPosition + maxDistance), BoundsMask.AllLayers, 0);
            }

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                return count < maxCount &&  bounds.Intersect(searchBounds);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
            {
                if (count >= maxCount) return;

                float3 center = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                float3 toCenterXZ = math.normalizesafe(new float3(center.x - cameraPosition.x, 0f, center.z - cameraPosition.z), new float3(0f, 0f, 1f));
                if (math.dot(toCenterXZ, cameraDirection) < minDot)
                {
                    return;
                }

                var objectSize = (bounds.m_Bounds.max - bounds.m_Bounds.min);
                var minDimension = math.min(math.min(objectSize.x, objectSize.y), objectSize.z);
                var maxDimension = math.max(math.max(objectSize.x, objectSize.y), objectSize.z);

                if(minDimension > 3f && maxDimension > 8f)
                {
                    m_Bounds.Add((item, bounds));
                    count++;
                }
            }
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
