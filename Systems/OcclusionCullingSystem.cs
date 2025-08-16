using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Rendering;
using Unity.Jobs;
using Colossal.Collections;
using Game.Common;
using Colossal.Logging;
using Game.Simulation;
using System;

namespace OcclusionCulling
{
    [UpdateBefore(typeof(Game.Rendering.PreCullingSystem))]
    public partial class OcclusionCullingSystem : SystemBase
    {
        private static ILog s_log = Mod.log;
        private CameraUpdateSystem m_CameraSystem;
        private Game.Rendering.PreCullingSystem m_PreCullingSystem;
        private Game.Objects.SearchSystem m_SearchSystem;
        private Game.Simulation.TerrainSystem m_TerrainSystem;
        private NativeParallelHashSet<Entity> m_EnforcedEntities;
        private NativeParallelHashMap<Entity, QuadTreeBoundsXZ> m_OriginalByEntity;

        static int occlusionResumeIndex = 0;
        static readonly float kMoveThreshold = 3f;
        static readonly int kMaxObjectsPerFrame = 256; //low value for testing

        private float3 m_LastCameraPos = float3.zero;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_PreCullingSystem = World.GetExistingSystemManaged<Game.Rendering.PreCullingSystem>();
            m_SearchSystem = World.GetExistingSystemManaged<Game.Objects.SearchSystem>();
            m_TerrainSystem = World.GetExistingSystemManaged<Game.Simulation.TerrainSystem>();
            m_EnforcedEntities = new NativeParallelHashSet<Entity>(1024, Allocator.Persistent);
            m_OriginalByEntity = new NativeParallelHashMap<Entity, QuadTreeBoundsXZ>(1024, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (m_EnforcedEntities.IsCreated) m_EnforcedEntities.Dispose();
            if (m_OriginalByEntity.IsCreated) m_OriginalByEntity.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            // Bail if the camera system isn't ready
            if (!m_CameraSystem.TryGetLODParameters(out var lodParams))
            {
                return;
            }

            float3 camPos = lodParams.cameraPosition;
            float3 camDir = m_CameraSystem.activeViewer.forward;

            // Bail out if looking straight down, or if camera hasn't moved and there are no pending hide/unhide operations
            bool lookingDown = camDir.y < -0.9f;
            float2 deltaXZ = new float2(camPos.x - m_LastCameraPos.x, camPos.z - m_LastCameraPos.z);
            bool moved = math.lengthsq(deltaXZ) > (kMoveThreshold * kMoveThreshold);
            bool hasPending = m_EnforcedEntities.Count() > 0 || m_OriginalByEntity.Count() > 0;
            if (lookingDown || (!moved && !hasPending))
            {
                // Nothing to do, update last camera state and return
                m_LastCameraPos = camPos;
                return;
            }
            if (moved)
            {
                // reset resume index when camera moves
                occlusionResumeIndex = 0;
            }
            
            var msTimer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            s_log.Info($"tm({msTimer}) Starting culling system: camPos({camPos}), camDir({camDir})");

            JobHandle readDeps;
            var staticTreeRO = m_SearchSystem.GetStaticSearchTree(readOnly: true, out readDeps);
            Dependency = JobHandle.CombineDependencies(Dependency, readDeps);

            var terrainData = m_TerrainSystem.GetHeightData();
            s_log.Info($"tm({DateTimeOffset.Now.ToUnixTimeMilliseconds() - msTimer}) Starting culling system: camPos({camPos}), camDir({camDir})");

            int nextIndex;
            var occluded = OcclusionUtilities.FindTerrainOccludedEntities(
                staticTreeRO,
                terrainData,
                camPos,
                camDir,
                out nextIndex
            );

            s_log.Info($"tm({DateTimeOffset.Now.ToUnixTimeMilliseconds() - msTimer}) Utility finished.");

            // Tech: let the apply-delta job do both revert and enforce in one pass
            // User: we’ll handle hiding and revealing all objects in the job itself
            // Schedule writer job to apply delta and update caches
            //var cullingData = m_PreCullingSystem.GetCullingData(readOnly: false, out var writeDeps);
            //Dependency = JobHandle.CombineDependencies(Dependency, writeDeps);

            var deltaJob = new ApplyOcclusionDeltaJob
            {
                //cullingData   = cullingData,
                occludedList  = occluded,
                enforced      = m_EnforcedEntities,
                originals     = m_OriginalByEntity,
                cullingInfo   = GetComponentLookup<CullingInfo>(false)
            };
            
            var handle = deltaJob.Schedule(Dependency);
            s_log.Info($"tm({DateTimeOffset.Now.ToUnixTimeMilliseconds() - msTimer}) Delta job scheduled.");
            var disposeHandle = occluded.Dispose(handle);

            occlusionResumeIndex = nextIndex;
            
            //m_PreCullingSystem.AddCullingDataWriter(disposeHandle);
            // ensure PreCullingSystem waits on our delta job before next read
            //m_PreCullingSystem.AddCullingDataReader(disposeHandle);
            Dependency = disposeHandle;

            m_PreCullingSystem.ResetCulling();

            //m_PreCullingSystem.ResetCulling();
            m_LastCameraPos = camPos;
            return;
        }

        // TODO: Maybe just use nativearrays for tracking entity cache, then doing a simple intersection call to generate diffs
        private struct ApplyOcclusionDeltaJob : IJob
        {
            //[NativeDisableParallelForRestriction]
            //public NativeList<PreCullingData> cullingData;
            [ReadOnly] public NativeList<(Entity entity, QuadTreeBoundsXZ bounds)> occludedList;
            public NativeParallelHashSet<Entity> enforced;
            public NativeParallelHashMap<Entity, QuadTreeBoundsXZ> originals;
            public ComponentLookup<CullingInfo> cullingInfo;

            public void Execute()
            {
                // Tech: Revert entities that were hidden but are no longer in occludedList
                // User: unhide any objects that have come back into view
                //s_log.Info($"Inside Job.Execute: occludedListCount({occludedList.Length}, enforcedCount({enforced.Count()}, originalsCount({originals.Count()})");
                // First pass: collect entities to unhide without mutating the set during iteration
                var toUnhide = new NativeList<Entity>(Allocator.TempJob);
                var enumOld = enforced.GetEnumerator();
                while (enumOld.MoveNext())
                {
                    Entity e = enumOld.Current;
                    bool stillHidden = false;
                    // check if still occluded
                    for (int oi = 0; oi < occludedList.Length; oi++)
                    {
                        if (occludedList[oi].entity.Equals(e)) { stillHidden = true; break; }
                    }
                    if (!stillHidden)
                    {
                        toUnhide.Add(e);
                    }
                }
                enumOld.Dispose();
                
                // Perform unhide operations
                for (int ui = 0; ui < toUnhide.Length; ui++)
                {
                    var e = toUnhide[ui];
                    if (originals.TryGetValue(e, out var origBounds))
                    {

                        //for (int j = 0; j < cullingData.Length; j++)
                        //{
                        //    if (cullingData[j].m_Entity.Equals(e))
                        //    {
                        //        var data = cullingData[j];
                        //        data.m_Flags |= PreCullingFlags.PassedCulling;
                        //        data.m_Flags |= PreCullingFlags.Updated;
                        //        data.m_Flags |= PreCullingFlags.NearCameraUpdated;
                        //        data.m_Timer = 0;
                        //        cullingData[j] = data;
                        //        break;
                        //    }
                        //}
                        ref var ci = ref cullingInfo.GetRefRWOptional(e).ValueRW;
                        ci.m_Mask |= BoundsMask.NormalLayers;
                        ci.m_Mask |= BoundsMask.Debug;
                        ci.m_MinLod = 110;
                        //var newData = new PreCullingData
                        //{
                        //    m_Entity = e,
                        //    m_Flags = PreCullingFlags.PassedCulling | PreCullingFlags.Updated | PreCullingFlags.NearCameraUpdated,
                        //    m_Timer = 0,
                        //    m_UpdateFrame = -1
                        //};
                        //cullingData.Add(newData);

                        originals.Remove(e);
                    }
                    enforced.Remove(e);
                }
                toUnhide.Dispose();

                // Tech: Enforce any new occlusions not already tagged
                // User: hide objects that just got blocked
                for (int oi = 0; oi < occludedList.Length; oi++)
                {
                    var (e,bounds) = occludedList[oi];
                    if (!enforced.Contains(e))
                    {
                        ref var ci = ref cullingInfo.GetRefRWOptional(e).ValueRW;
                        ci.m_MinLod = byte.MaxValue;
                        ci.m_Mask &= ~(BoundsMask.NormalLayers | BoundsMask.Debug);

                        s_log.Info($"Attempted to cull e({e.Index}) with m_Mask({ci.m_Mask})");

                        originals.TryAdd(e, bounds);
                        //for (int j = 0; j < cullingData.Length; j++)
                        //{
                        //    if (cullingData[j].m_Entity.Equals(e))
                        //    {
                        //        var data = cullingData[j];
                        //        data.m_Flags &= ~PreCullingFlags.PassedCulling;
                        //        data.m_Timer = byte.MaxValue;
                        //        cullingData[j] = data;
                        //        break;
                        //    }
                        //}
                        enforced.Add(e);
                    }
                }
            }
        }
    }
}


