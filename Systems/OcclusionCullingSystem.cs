using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Rendering;
using Unity.Jobs;
using Game.Common;
using Colossal.Logging;
using System;

namespace OcclusionCulling
{
    [UpdateAfter(typeof(Game.Objects.SearchSystem))]
    [UpdateBefore(typeof(Game.Rendering.PreCullingSystem))]
    public partial class OcclusionCullingSystem : SystemBase
    {
        private static ILog s_log = Mod.log;
        private CameraUpdateSystem m_CameraSystem;
        private Game.Rendering.PreCullingSystem m_PreCullingSystem;
        private Game.Objects.SearchSystem m_SearchSystem;
        private Game.Simulation.TerrainSystem m_TerrainSystem;
        private OverlayRenderSystem m_OverlayRenderSystem;
        private NativeHashMap<Entity, QuadTreeBoundsXZ> m_cachedCulls;
        private NativeHashSet<Entity> m_dirtiedEntities;
        private NativeHashSet<float3> m_DebugLines;

        static int occlusionResumeIndex = 0;
        static readonly float kMoveThreshold = 3f;
        static readonly int kMaxObjectsPerFrame = 256; //low value for testing

        private float3 m_LastCameraPos = float3.zero;
        private float3 m_LastCameraDir = float3.zero;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_PreCullingSystem = World.GetExistingSystemManaged<Game.Rendering.PreCullingSystem>();
            m_SearchSystem = World.GetExistingSystemManaged<Game.Objects.SearchSystem>();
            m_TerrainSystem = World.GetExistingSystemManaged<Game.Simulation.TerrainSystem>();
            m_OverlayRenderSystem = World.GetExistingSystemManaged<OverlayRenderSystem>();
            m_cachedCulls = new NativeHashMap<Entity, QuadTreeBoundsXZ>(1024, Allocator.Persistent);
            m_dirtiedEntities = new NativeHashSet<Entity>(1024, Allocator.Persistent);
            m_DebugLines = new NativeHashSet<float3>(1024, Allocator.Persistent);
        }

        protected override void OnStartRunning()
        {
            s_log.Info(nameof(OnStartRunning));
            if (m_cachedCulls.Count > 0) m_cachedCulls.Clear();
            if (m_dirtiedEntities.Count > 0) m_dirtiedEntities.Clear();
            if (m_DebugLines.Count > 0) m_DebugLines.Clear();
            base.OnStartRunning();
        }

        protected override void OnDestroy()
        {
            if (m_cachedCulls.IsCreated) m_cachedCulls.Dispose();
            if (m_dirtiedEntities.IsCreated) m_dirtiedEntities.Dispose();
            s_log.Info("OnDestroy()");
            base.OnDestroy();
        }

        protected void markDirty(Entity e)
        {
            if (m_dirtiedEntities.Contains(e)) return;
            if (!EntityManager.HasComponent<OcclusionDirtyTag>(e))
                EntityManager.AddComponent<OcclusionDirtyTag>(e);
            EntityManager.SetComponentEnabled<OcclusionDirtyTag>(e, true);
            m_dirtiedEntities.Add(e);
        }

        // Todo occassionally null reference here (probably from temp, static entities being created in one frame and deleted in a later one)
        protected void removeDirty(Entity e)
        {
            if (!m_dirtiedEntities.Contains(e)) return;
            if (!EntityManager.HasComponent<OcclusionDirtyTag>(e))
                EntityManager.SetComponentEnabled<OcclusionDirtyTag>(e, false);
            m_dirtiedEntities.Remove(e);
        }
        protected override void OnUpdate()
        {
            // Bail if the camera system isn't ready
            if (m_CameraSystem == null ||  !m_CameraSystem.TryGetLODParameters(out var lodParams))
            {
                return;
            }

            float3 camPos = m_LastCameraPos;
            float3 camDir = m_LastCameraDir;
            camPos = lodParams.cameraPosition;
            camDir = m_CameraSystem.activeViewer.forward;
            
            var buffer = m_OverlayRenderSystem.GetBuffer(out var dep);
            //int lc = 0;
            //foreach (float3 point in m_DebugLines)
            //{
            //    lc++;
            //    if (lc % 5 != 0) { continue; }
            //    //var lPad = math.normalize(camDir) * 2f;
            //    //Line3.Segment segment = new Line3.Segment(camPos + lPad, camPos + point);
            //    //buffer.DrawLine(UnityEngine.Color.red, segment, 1f);
            //    buffer.DrawCircle(UnityEngine.Color.red, camPos + point, 1f);
            //}

            // Bail out if looking straight down, or if camera hasn't moved and there are no pending hide/unhide operations
            bool lookingDown = camDir.y < -0.9f;
            float2 deltaXZ = new float2(camPos.x - m_LastCameraPos.x, camPos.z - m_LastCameraPos.z);
            bool moved = math.lengthsq(deltaXZ) > (kMoveThreshold * kMoveThreshold);

            // If we aren't in the middle of a batch
            if (occlusionResumeIndex == 0)
            {
                //Bail if camera hasn't moved or looking downward
                if(lookingDown || !moved)
                {
                    return;
                }
            }
            else
            {
                // We are in the middle of a batch, but reset batch if we moved
                if (moved)
                {
                    occlusionResumeIndex = 0;
                }
            }

            long timer = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var staticTreeRO = m_SearchSystem.GetStaticSearchTree(readOnly: true, out var readDeps);
            Dependency = JobHandle.CombineDependencies(Dependency, readDeps);
            var terrainData = m_TerrainSystem.GetHeightData();

            var occluded = SectorOcclusionCulling.CullByRadialMap(
                staticTreeRO,
                this.EntityManager,
                terrainData,
                camPos,
                camDir,
                out var nextIndex,
                out var points
            );

            //m_DebugLines.Clear();
            //foreach (var p in points)
            //{
            //    m_DebugLines.Add(p);
            //}

            s_log.Info($"Found {occluded.Length} objects to occlude");

            NativeHashSet<Entity> toUnCull = new NativeHashSet<Entity>(occluded.Length, Allocator.Temp);
            NativeHashSet<Entity> toTriggerCull = new NativeHashSet<Entity>(occluded.Length, Allocator.Temp);
            NativeHashSet<Entity> toUndirty = new NativeHashSet<Entity>(m_dirtiedEntities.Count, Allocator.Temp);

            // Copy dirtyEntities to reset later
            foreach(Entity e in m_dirtiedEntities)
            {
                toUndirty.Add(e);
            }
            m_dirtiedEntities.Clear();

            // Un-cull anything that isn't present this frame but was last frame
            foreach (var item in m_cachedCulls)
            {
                Entity e = item.Key;
                bool culledAgain = false;
                for (int j = 0; j < occluded.Length; j++)
                {
                    if (occluded[j].entity.Equals(e))
                    {
                        culledAgain = true;
                        break;
                    }
                }

                if (!culledAgain)
                {
                    toUnCull.Add(e);
                }
            }

            // Trigger new cull unless was culled in previous actionable frame
            foreach (var item in occluded)
            {
                Entity e = item.entity;
                QuadTreeBoundsXZ b = item.bounds;
                if (m_cachedCulls.ContainsKey(e))
                {
                    continue;
                }
                toTriggerCull.Add(e);
                m_cachedCulls.TryAdd(e, b);
            }

            // Perform culling
            int enforcedCount = 0;
            foreach (var e in toTriggerCull) 
            {
                if (!EntityManager.Exists(e))
                {
                    s_log.Info($"Error: toCull entity no longer exists: {e.Index}");
                    continue;
                }
                markDirty(e);
                var ci = EntityManager.GetComponentData<CullingInfo>(e);
                ci.m_Mask = 0;
                EntityManager.SetComponentData(e, ci);
                enforcedCount++;
            }

            // Perform unculling
            int revertedCount = 0;
            foreach (Entity e in toUnCull)
            {
                if (m_cachedCulls.TryGetValue(e, out var bounds))
                {
                    if(!EntityManager.Exists(e))
                    {
                        s_log.Info($"Error: unculling entity no longer exists: {e.Index}");
                        continue;
                    }
                    markDirty(e);
                    var ci = EntityManager.GetComponentData<CullingInfo>(e);
                    ci.m_Mask = bounds.m_Mask;

                    EntityManager.SetComponentData(e, ci);
                    revertedCount++;
                    m_cachedCulls.Remove(e);
                }
            }

            // Resync dirtiedEntities from last actionable frame
            foreach (Entity e in toUndirty)
            {
                if(m_dirtiedEntities.Contains(e))
                {
                    continue;
                }
                // Previous dirtied is no longer dirty, so let's remove it
                removeDirty(e);
            }


            if (enforcedCount > 0 || revertedCount > 0)
            {
                s_log.Info($"OnUpdate: enforcedCulls:{enforcedCount}, revertedCulls:{revertedCount}, totalFound:{occluded.Length}, previousBatchIndex:{occlusionResumeIndex}, timeInMs:{DateTimeOffset.Now.ToUnixTimeMilliseconds() - timer}");
            }

            occluded.Dispose();         

            occlusionResumeIndex = nextIndex;
            m_LastCameraPos = camPos;
            m_LastCameraDir = camDir;
            return;
        }

    }
}


