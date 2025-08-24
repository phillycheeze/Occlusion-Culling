using Unity.Collections;
using Unity.Entities;
using Colossal.Collections;
using Unity.Mathematics;
using Game.Common;
using Colossal.Mathematics;
using Colossal.Logging;
using Game.Objects;
using Game.Prefabs;
using OcclusionCulling.Utilities;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Burst;

namespace OcclusionCulling
{
    public static class OcclusionUtilities
    {
        private static readonly ILog s_log = Mod.log;
        private static readonly LRUCache<int, QuadTreeBoundsXZ> cache = new(4096);

        private static readonly float boundingScaleCorrection = 0.60f;

        // For now, applying fixed scaleCorrection percentage
        // TODO: Either do
        // A) Grab the SubMesh components from the Prefab and use those bounds (usually more accurate, but some things still don't fit well)
        // B) Create and maintain a list of prefab overrides to either ignore them or tweak their bounds
        // C) somehow figure out a better way to get the true mesh bounding box
        // D) combo of A and B
        private static QuadTreeBoundsXZ GetTrueGeometryBounds(
            Entity e,
            QuadTreeBoundsXZ bounds,
            ComponentLookup<PrefabRef> prefabRefLookup,
            ComponentLookup<MeshData> meshDataLookup,
            ComponentLookup<Transform> transformLookup,
            BufferLookup<SubMesh> subMeshLookup
        )
        {
            if(prefabRefLookup.HasComponent(e))
            {
                PrefabRef pr = prefabRefLookup[e];

                // TODO: Can't use managed LRUCache class with burst compiler
                //QuadTreeBoundsXZ cacheHit = cache.Get(pr.m_Prefab.Index);

                // byte.MinValue is checking if returned a default() object
                //if (cacheHit.m_MinLod == byte.MinValue)
                //{
                    if (subMeshLookup.TryGetBuffer(pr.m_Prefab, out var buffer))
                    {
                        if (buffer.IsEmpty)
                        {
                            //cache.Add(pr.m_Prefab.Index, bounds);
                            return bounds;
                        }
                        
                        // Todo, how to handle objects with multiple submeshes (not many)
                        SubMesh first = buffer[0];
                        Entity subMesh = first.m_SubMesh;

                        if (
                            !meshDataLookup.TryGetComponent(subMesh, out MeshData md) ||
                            !transformLookup.TryGetComponent(e, out Transform t)
                        )
                        {
                            //cache.Add(pr.m_Prefab.Index, bounds);
                            return bounds;
                        }

                        md.m_Bounds.min.x *= boundingScaleCorrection;
                        md.m_Bounds.min.z *= boundingScaleCorrection;
                        md.m_Bounds.max.x *= boundingScaleCorrection;
                        md.m_Bounds.max.z *= boundingScaleCorrection;
                        Bounds3 realBoundary = ObjectUtils.CalculateBounds(t.m_Position, t.m_Rotation, md.m_Bounds);
                        QuadTreeBoundsXZ result = new(realBoundary, bounds.m_Mask, bounds.m_MinLod);
                        //cache.Add(pr.m_Prefab.Index, result);
                        return result;
                    }
                    else
                    {
                        //cache.Add(pr.m_Prefab.Index, bounds);
                        return bounds;
                    }
                }

                //return cacheHit;
            //}
            return bounds;
        }


        [BurstCompile]
        public struct TreeFlattenCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public NativeList<KeyValuePair<Entity, QuadTreeBoundsXZ>> results;
            public bool Intersect(QuadTreeBoundsXZ bounds) { return true; }
            public void Iterate(QuadTreeBoundsXZ bounds, Entity item) {
                results.Add(new KeyValuePair<Entity, QuadTreeBoundsXZ>(item, bounds));
            }
        }

        public struct CandidateCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> candidates;
            public QuadTreeBoundsXZ searchBounds;
            public int candidateCount;

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                return bounds.Intersect(searchBounds);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
            {
                if(candidates.TryAdd(item, bounds))
                {
                    candidateCount++;
                }
            }

            public void Dispose()
            {
                if (candidates.IsCreated) candidates.Dispose();
                candidateCount = 0;
            }
        }

        public struct RegionQueryCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            [ReadOnly] public ComponentLookup<PrefabRef> PrefabRefLookup;
            [ReadOnly] public ComponentLookup<MeshData> MeshDataLookup;
            [ReadOnly] public ComponentLookup<Transform> TransformLookup;
            [ReadOnly] public BufferLookup<SubMesh> SubMeshLookup;
            public QuadTreeBoundsXZ searchRegion;
            public NativeList<KeyValuePair<Entity, QuadTreeBoundsXZ>> results;
            public int maxCount;

            private int count;

            public bool Intersect(QuadTreeBoundsXZ nodeBounds)
            {
                return nodeBounds.Intersect(searchRegion);
            }

            public void Iterate(QuadTreeBoundsXZ largeBounds, Entity item)
            {
                if (count >= maxCount) return;
                
                var objectSize = (largeBounds.m_Bounds.max - largeBounds.m_Bounds.min);
                var minDimension = math.min(objectSize.x, objectSize.z);
                var maxDimension = math.max(objectSize.x, objectSize.z);

                // Completely skip thin/flat height objects
                if (objectSize.y < 15f || minDimension < 9f || maxDimension < 15f)
                {
                    return;
                }

                //Use large, more efficient bounds in the Intersection above, 
                // now narrow down to true occluder geometry size
                var bounds = GetTrueGeometryBounds(item, largeBounds, PrefabRefLookup, MeshDataLookup, TransformLookup, SubMeshLookup);
                bool passedTight = bounds.Intersect(searchRegion);
                if (!passedTight)
                {
                    // Didn't succeed in tighter filtering checking of true geometry
                    return;
                }
                results.Add(new KeyValuePair<Entity, QuadTreeBoundsXZ>(item, largeBounds));
                count++;
            }

            public void Dispose()
            {
                if (results.IsCreated) results.Dispose();
            }
        }
    }
}


