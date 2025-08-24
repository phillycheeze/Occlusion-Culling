using Colossal.Collections;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Mathematics;
using Game.Citizens;
using Game.Common;
using Game.Modding.Toolchain.Dependencies;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using static OcclusionCulling.SectorOcclusionCulling;

namespace OcclusionCulling
{
    //[UpdateAfter(typeof(Game.Objects.SearchSystem))]
    [UpdateBefore(typeof(PreCullingSystem))]
    //[UpdateBefore(typeof(BatchInstanceSystem))]
    public partial class OcclusionCullingSystem : SystemBase
    {
        private static ILog s_log = Mod.log;
        private CameraUpdateSystem m_CameraSystem;
        private Game.Rendering.PreCullingSystem m_PreCullingSystem;
        private Game.Objects.SearchSystem m_SearchSystem;
        private Game.Simulation.TerrainSystem m_TerrainSystem;
        private EntityCommandBuffer m_ECB;
        private ComponentLookup<OcclusionDirtyTag> m_DirtyComponentLookup;
        private ComponentLookup<CullingInfo> m_CullingInfoLookup;
        private NativeHashMap<Entity, QuadTreeBoundsXZ> m_cachedCulls;
        private NativeHashSet<Entity> m_dirtiedEntities;

        NativeQueue<KeyValuePair<Entity, QuadTreeBoundsXZ>> m_Queue;

        static readonly float kMoveThreshold = 3f;

        private float3 m_LastCameraPos = float3.zero;
        private float3 m_LastCameraDir = float3.zero;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_PreCullingSystem = World.GetExistingSystemManaged<Game.Rendering.PreCullingSystem>();
            m_SearchSystem = World.GetExistingSystemManaged<Game.Objects.SearchSystem>();
            m_TerrainSystem = World.GetExistingSystemManaged<Game.Simulation.TerrainSystem>();
            m_DirtyComponentLookup = GetComponentLookup<OcclusionDirtyTag>(true);
            m_CullingInfoLookup = GetComponentLookup<CullingInfo>(false);
            m_cachedCulls = new NativeHashMap<Entity, QuadTreeBoundsXZ>(10000, Allocator.Persistent);
            m_dirtiedEntities = new NativeHashSet<Entity>(2048, Allocator.Persistent);

            m_Queue = new(Allocator.Persistent);
        }

        protected override void OnStartRunning()
        {
            s_log.Info(nameof(OnStartRunning));
            if (m_cachedCulls.Count > 0) m_cachedCulls.Clear();
            if (m_dirtiedEntities.Count > 0) m_dirtiedEntities.Clear();
            if (m_Queue.Count > 0) m_Queue.Clear();
        }

        protected override void OnDestroy()
        {
            s_log.Info(nameof(OnDestroy));
            if (m_cachedCulls.IsCreated) m_cachedCulls.Dispose();
            if (m_dirtiedEntities.IsCreated) m_dirtiedEntities.Dispose();
            if (m_ECB.IsCreated) m_ECB.Dispose();
            base.OnDestroy();
        }

        protected void markDirty(Entity e)
        {
            if (m_dirtiedEntities.Contains(e)) return;
            m_ECB.AddComponent<OcclusionDirtyTag>(e); // Doesn't assert even if component already exists
            m_ECB.SetComponentEnabled<OcclusionDirtyTag>(e, true);
            m_dirtiedEntities.Add(e);
        }

        protected void removeDirty(Entity e)
        {
            if (m_DirtyComponentLookup.HasComponent(e) && m_DirtyComponentLookup.HasEnabledComponent(e))
                m_ECB.SetComponentEnabled<OcclusionDirtyTag>(e, false);
        }
        protected override void OnUpdate()
        {
            // Bail if the camera system isn't ready
            if (m_CameraSystem == null ||  !m_CameraSystem.TryGetLODParameters(out var lodParams))
            {
                return;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            float3 camPos = m_LastCameraPos;
            float3 camDir = m_LastCameraDir;
            camPos = lodParams.cameraPosition;
            camDir = m_CameraSystem.activeViewer.forward;
            
            bool lookingDown = camDir.y < -0.9f;
            float2 deltaXZ = new float2(camPos.x - m_LastCameraPos.x, camPos.z - m_LastCameraPos.z);
            bool moved = math.lengthsq(deltaXZ) > (kMoveThreshold * kMoveThreshold);

            //Bail if camera hasn't moved or looking downward, but keep culling unless looking down
            if (lookingDown || !moved)
            {
                return;
            }

            m_ECB = new EntityCommandBuffer(Allocator.Temp);
            var staticTreeRO = m_SearchSystem.GetStaticSearchTree(readOnly: false, out var readDeps);
            var terrainData = m_TerrainSystem.GetHeightData();
            NativeQuadTree<Entity, QuadTreeBoundsXZ> candidates = new(20000, Allocator.TempJob); // Must put here because there is no Dispose(handle) function
            m_Queue.Clear();

            // TODO:
            // 1. Make radial map into burst job, fetch dependency
            // 2. Fetch candidates
            // 3. Call culling job, passing dependency from before (return this dependency instead of occluders)
            // 4. Make culling/unculling below into job, using returned dependency
            // 5. Swap out toUnCull.Add() and toTriggerCull.Add() into function calls to cleanup hashset setting, for loops, etc

            // Maybe? Move dirty/undirty logic to be separate from occlusion logic (it keeps entities dirty even when camera isn't moving)
            // .. lets just undirty them at the top of the OnUpdate (in a job), since marking dirty for an entity two frames in a row is rare
            // .. We don't need to maintain cache anymore, just use EntityQuery to find any enabled and swap them back (complete dependency before step 5 job runs above)
            var soc = new SectorOcclusionCulling(camPos, camDir);
            soc.m_PrefabRefLookup = GetComponentLookup<PrefabRef>(true);
            soc.m_MeshDataLookup = GetComponentLookup<MeshData>(true);
            soc.m_TransformLookup = GetComponentLookup<Game.Objects.Transform>(true);
            soc.m_SubMeshLookup = GetBufferLookup<SubMesh>(true);

            readDeps.Complete();

            var timerBeforeJob = stopwatch.ElapsedMilliseconds;
            JobHandle queueHandle = soc.CullByRadialMap(
                staticTreeRO,
                candidates,
                terrainData,
                m_Queue,
                out int nextIndex
            );

            // TEMP FOR TESTING until bottom moved to job
            queueHandle.Complete();
            var timerForJob = stopwatch.ElapsedMilliseconds;

            // Build a map of this frame's cull candidates
            var currentMap = new NativeHashMap<Entity, QuadTreeBoundsXZ>(m_Queue.Count, Allocator.Temp);
            var reader = m_Queue.AsReadOnly().GetEnumerator();
            while (reader.MoveNext())
            {
                var kvp = reader.Current;
                currentMap.Add(kvp.Key, kvp.Value);
            }

            // Extract keys for set operations
            var currentKeys = currentMap.GetKeyArray(Allocator.Temp);
            var previousKeys = m_cachedCulls.GetKeyArray(Allocator.Temp);
            int size = math.max(currentKeys.Length, previousKeys.Length);

            // Entities to newly cull (in current but not in cache)
            var cullTemp = new NativeHashSet<Entity>(size, Allocator.Temp);
            cullTemp.UnionWith(currentKeys);
            cullTemp.ExceptWith(previousKeys);                

            // Apply culling changes
            foreach (var e in cullTemp)
            {
                if (EntityManager.Exists(e))
                {
                    markDirty(e);
                    ref var ci = ref m_CullingInfoLookup.GetRefRW(e).ValueRW;
                    ci.m_Mask = 0;
                    m_cachedCulls.Add(e, currentMap[e]);
                }
            }

            cullTemp.Clear();
            cullTemp.UnionWith(previousKeys);
            cullTemp.ExceptWith(currentKeys);
            foreach (var e in cullTemp)
            {
                if (EntityManager.Exists(e) && m_cachedCulls.TryGetValue(e, out var bounds))
                {
                    markDirty(e);
                    ref var ci = ref m_CullingInfoLookup.GetRefRW(e).ValueRW;
                    ci.m_Mask = bounds.m_Mask;
                    m_cachedCulls.Remove(e);
                }
            }

            // Reset any dirty tags for entities no longer changing
            foreach (var e in m_dirtiedEntities)
            {
                if (!currentMap.ContainsKey(e) && !cullTemp.Contains(e))
                {
                    removeDirty(e);
                }
            }

            if (m_ECB.ShouldPlayback)
                m_ECB.Playback(EntityManager);

            stopwatch.Stop();
            // Sample logs
            if ((m_dirtiedEntities.Count > 0 || m_cachedCulls.Count > 0))
            {
                s_log.Info($"OnUpdate: dirtied: {m_dirtiedEntities.Count}, cached: {m_cachedCulls.Count}, timeInMs:{stopwatch.ElapsedMilliseconds}, timeJobOnly:{timerForJob}, timeBeforeJob:{timerBeforeJob}");
            }

            m_LastCameraPos = camPos;
            m_LastCameraDir = camDir;

            // Clean up temporaries
            m_dirtiedEntities.Clear();
            m_ECB.Dispose();
            currentKeys.Dispose();
            previousKeys.Dispose();
            cullTemp.Dispose();
            currentMap.Dispose();
            candidates.Dispose();
            return;
            
        }
    }
}


