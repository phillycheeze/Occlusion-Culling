using Unity.Collections;
using Unity.Entities;
using Colossal.Collections;
using Unity.Mathematics;
using Game.Common;
using Colossal.Mathematics;
using Game.Rendering;
using Colossal.Logging;
using Game.Simulation;
using Game.Objects;
using Game.Prefabs;

namespace OcclusionCulling
{
    public static class OcclusionUtilities
    {
        private static ILog s_log = Mod.log;
        public static EntityManager entityManager;

        private static QuadTreeBoundsXZ GetTrueGeometryBounds(Entity e, QuadTreeBoundsXZ bounds)
        {
            if(entityManager.HasComponent<PrefabRef>(e))
            {
                PrefabRef pr = entityManager.GetComponentData<PrefabRef>(e);
                if (entityManager.HasComponent<ObjectGeometryData>(e))
                {
                    ObjectGeometryData geo = entityManager.GetComponentData<ObjectGeometryData>(e);
                    Transform t = entityManager.GetComponentData<Transform>(e);
                    var realBoundary = ObjectUtils.CalculateBounds(t.m_Position, t.m_Rotation, geo);
                    return new QuadTreeBoundsXZ(realBoundary, BoundsMask.AllLayers, bounds.m_MinLod);
                }
            }
            return bounds;
        }

        public struct CandidateCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public float3 cameraPosition;
            public float3 cameraDirectionXZ;
            public float maxDistance;

            public NativeList<SectorOcclusionCulling.CullingCandidate> candidates;
            private QuadTreeBoundsXZ searchBounds;

            public CandidateCollector(float3 cameraPos, float3 cameraDir, float searchRadius)
            {
                cameraPosition = cameraPos;
                cameraDirectionXZ = math.normalizesafe(new float3(cameraDir.x, 0f, cameraDir.z), new float3(0f, 0f, 1f));
                maxDistance = searchRadius;
                candidates = new NativeList<SectorOcclusionCulling.CullingCandidate>(2000, Allocator.Temp);

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
                // Possible already culled items here, could check mask too
                // if (bounds.m_MinLod >= byte.MaxValue) return;
                var can = new SectorOcclusionCulling.CullingCandidate();
                can.entity = item;
                can.bounds = bounds;
                candidates.Add(can);
            }

            public void Dispose()
            {
                if (candidates.IsCreated) candidates.Dispose();
            }
        }

        public struct RegionQueryCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public QuadTreeBoundsXZ searchRegion;
            public NativeList<SectorOcclusionCulling.CullingCandidate> results;
            public int maxCount;

            private int count;

            public bool Intersect(QuadTreeBoundsXZ nodeBounds)
            {
                return nodeBounds.Intersect(searchRegion);
            }

            public void Iterate(QuadTreeBoundsXZ largeBounds, Entity item)
            {
                if (count >= maxCount) return;
                
                // Use large, more efficient bounds in the Intersection above, 
                // now narrow down to true occluder geometry size
                var bounds = GetTrueGeometryBounds(item, largeBounds);
                bool passedTight = bounds.Intersect(searchRegion);
                if (!passedTight)
                {
                    // Didn't succeed in tighter filtering checking of true geometry
                    return;
                }
                var objectSize = (bounds.m_Bounds.max - bounds.m_Bounds.min);
                var minDimension = math.min(math.min(objectSize.x, objectSize.y), objectSize.z);
                var maxDimension = math.max(math.max(objectSize.x, objectSize.y), objectSize.z);

                if (minDimension > 8f && maxDimension > 15f)
                {
                    var can = new SectorOcclusionCulling.CullingCandidate();
                    can.entity = item;
                    can.bounds = bounds;
                    results.Add(can);
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


