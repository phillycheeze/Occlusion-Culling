using Unity.Collections;
using Unity.Entities;
using Colossal.Collections;
using Unity.Mathematics;
using Game.Common;
using Colossal.Mathematics;
using Game.Rendering;

namespace OcclusionCulling
{
    public static class OcclusionUtilities
    {
        // Utility functions for performing shadow-based culling

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
            float maxDistance = 25f)
        {
            var collector = new ShadowCasterCollector(cameraPosition, maxDistance, 8);
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

            float3 direction = math.normalize(objectCenter - cameraPosition);

            var shadowEnd = objectCenter + (direction * fixedShadowDistance);

            var shadowMin = new float3(
                math.min(objectCenter.x, shadowEnd.x) - objectSize.x * 0.5f,
                casterBounds.m_Bounds.min.y,
                math.min(objectCenter.z, shadowEnd.z) - objectSize.z * 0.5f
            );
            var shadowMax = new float3(
                math.max(objectCenter.x, shadowEnd.x) + objectSize.x * 0.5f,
                casterBounds.m_Bounds.max.y,
                math.max(objectCenter.z, shadowEnd.z) + objectSize.z * 0.5f
            );
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
            float3 cameraPosition)
        {
            if (shadowBoxes.Length == 0) return false;
            const float depthBuffer = 10f;

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

        // Returns occluded entities without mutating any component data
        public static NativeList<Entity> FindOccludedEntities(
            NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
            float3 cameraPosition,
            float3 cameraDirection,
            float maxProcessingDistance = 250f,
            Allocator allocator = Allocator.TempJob)
        {
            var result = new NativeList<Entity>(allocator);

            var shadowCasters = FindShadowCasters(quadTree, cameraPosition);
            if (shadowCasters.Length == 0)
            {
                shadowCasters.Dispose();
                return result;
            }

            var shadowBoxes = new NativeList<QuadTreeBoundsXZ>(shadowCasters.Length, Allocator.Temp);
            var casterDistances = new NativeList<float>(shadowCasters.Length, Allocator.Temp);

            for (int i = 0; i < shadowCasters.Length; i++)
            {
                var caster = shadowCasters[i];
                var shadowBox = CalculateShadowBox(caster.bounds, cameraPosition, cameraDirection, maxProcessingDistance);
                var distance = math.distance(cameraPosition, (caster.bounds.m_Bounds.min + caster.bounds.m_Bounds.max) * 0.5f);
                shadowBoxes.Add(shadowBox);
                casterDistances.Add(distance);
            }

            var collector = new OccludedCollector(result, shadowBoxes, casterDistances, cameraPosition, maxProcessingDistance);
            quadTree.Iterate(ref collector, 0);

            shadowCasters.Dispose();
            shadowBoxes.Dispose();
            casterDistances.Dispose();
            return result;
        }

        private struct OccludedCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public NativeList<Entity> occluded;
            public NativeList<QuadTreeBoundsXZ> shadowBoxes;
            public NativeList<float> casterDistances;
            public float3 cameraPosition;
            private readonly QuadTreeBoundsXZ searchBounds;

            public OccludedCollector(NativeList<Entity> occluded, NativeList<QuadTreeBoundsXZ> boxes, NativeList<float> distances, float3 camPos, float maxDist)
            {
                this.occluded = occluded;
                shadowBoxes = boxes;
                casterDistances = distances;
                cameraPosition = camPos;
                searchBounds = new QuadTreeBoundsXZ(new Bounds3(camPos - maxDist, camPos + maxDist), BoundsMask.AllLayers, 0);
            }

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                return bounds.Intersect(searchBounds);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
            {
                if (IsObjectOccluded(entity, bounds, shadowBoxes, casterDistances, cameraPosition))
                {
                    occluded.Add(entity);
                }
            }
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
            public float maxDistance;
            public int maxCount;

            private int count;
            public NativeList<(Entity, QuadTreeBoundsXZ)> m_Bounds;
            private QuadTreeBoundsXZ searchBounds;

            public ShadowCasterCollector(float3 cameraPos, float searchRadius, int maxShadowCasters)
            {
                cameraPosition = cameraPos;
                maxDistance = searchRadius;
                maxCount = maxShadowCasters;
                count = 0;
                m_Bounds = new NativeList<(Entity, QuadTreeBoundsXZ)>(maxShadowCasters, Allocator.Temp);
                searchBounds = new QuadTreeBoundsXZ(new Bounds3(cameraPosition - maxDistance, cameraPosition + maxDistance), BoundsMask.AllLayers, 0);
            }

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                if (count >= maxCount)
                {
                    return false;
                }

                return bounds.Intersect(searchBounds);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
            {
                if (count >= maxCount) return;

                var objectSize = (bounds.m_Bounds.max - bounds.m_Bounds.min);
                var minDimension = math.min(math.min(objectSize.x, objectSize.y), objectSize.z);
                var maxDimension = math.max(math.max(objectSize.x, objectSize.y), objectSize.z);

                if(maxDimension > 5f && maxDimension > 15f)
                {
                    m_Bounds.Add((item, bounds));
                    count++;
                }
            }
        }
    }

}
