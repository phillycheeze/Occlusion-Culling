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

namespace OcclusionCulling
{
    [UpdateAfter(typeof(Game.Rendering.PreCullingSystem))]
    public partial class OcclusionCullingSystem : SystemBase
    {
        private static ILog s_log = Mod.log;
        static readonly float kMoveThreshold= 1f;
        private CameraUpdateSystem m_CameraSystem;
        private Game.Rendering.PreCullingSystem m_PreCullingSystem;
        private Game.Objects.SearchSystem m_SearchSystem;
        private Game.Terrain.TerrainSystem m_TerrainSystem;
        private float3 m_LastCameraPos;
        private float3 m_LastDirXZ;
        private NativeParallelHashSet<Entity> m_EnforcedEntities;
        private NativeParallelHashMap<Entity, QuadTreeBoundsXZ> m_OriginalByEntity;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_PreCullingSystem = World.GetExistingSystemManaged<Game.Rendering.PreCullingSystem>();
            m_SearchSystem = World.GetExistingSystemManaged<Game.Objects.SearchSystem>();
            m_TerrainSystem = World.GetExistingSystemManaged<Game.Terrain.TerrainSystem>();
            m_LastCameraPos = float3.zero;
            m_LastDirXZ = float3.zero;
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
            // Tech: determine if camera moved, turned, or is looking straight down to skip heavy culling
            // User: if you hardly move/turn or look straight down, skip shadow checks
            float2 deltaXZ = new float2(camPos.x - m_LastCameraPos.x, camPos.z - m_LastCameraPos.z);
            bool moved = math.length(deltaXZ) > kMoveThreshold;
            float2 currentDirXZ = math.normalize(new float2(camDir.x, camDir.z));
            bool turned = math.dot(currentDirXZ, m_LastDirXZ) < 0.996f; // threshold ~5° turn
            bool lookingDown = camDir.y < -0.9f;
            if ((!moved && !turned) || lookingDown)
            {
                return;
            }

            // TODO: only have utility find XX max culling entities this frame
            JobHandle readDeps;
            var staticTreeRO = m_SearchSystem.GetStaticSearchTree(readOnly: true, out readDeps);
            Dependency = JobHandle.CombineDependencies(Dependency, readDeps);

            var terrainData = m_TerrainSystem.GetHeightData();

            // Many calculations on the main thread, not ideal
            var occluded = OcclusionUtilities.FindTerrainOccludedEntities(
                staticTreeRO,
                terrainData,
                camPos,
                camDir,
                1000f,           // max distance to check
                12,             // samples per ray
                0.5f,           // clearance meters
                Allocator.TempJob
            );

            // Tech: let the apply-delta job do both revert and enforce in one pass
            // User: we’ll handle hiding and revealing all objects in the job itself

            // Schedule writer job to apply delta and update caches
            var cullingData = m_PreCullingSystem.GetCullingData(readOnly: false, out var writeDeps);
            var deltaJob = new ApplyOcclusionDeltaJob
            {
                cullingData   = cullingData,
                occludedList  = occluded,
                enforced      = m_EnforcedEntities,
                originals     = m_OriginalByEntity
            };
            var combined = JobHandle.CombineDependencies(Dependency, writeDeps);
            var handle = deltaJob.Schedule(combined);
            
            var disposeHandle = occluded.Dispose(handle);
            
            m_PreCullingSystem.AddCullingDataWriter(disposeHandle);
            Dependency = disposeHandle;

            m_LastCameraPos = camPos;
            m_LastDirXZ = math.normalizesafe(new float3(camDir.x, 0f, camDir.z));
            return;
        }


        private struct ApplyOcclusionDeltaJob : IJob
        {
            [NativeDisableParallelForRestriction]
            public NativeList<PreCullingData> cullingData;
            [ReadOnly] public NativeList<(Entity entity, QuadTreeBoundsXZ bounds)> occludedList;
            public NativeParallelHashSet<Entity> enforced;
            public NativeParallelHashMap<Entity, QuadTreeBoundsXZ> originals;

            public void Execute()
            {
                // Tech: Revert entities that were hidden but are no longer in occludedList
                // User: unhide any objects that have come back into view
                var enumOld = enforced.GetEnumerator();
                while (enumOld.MoveNext())
                {
                    var kv = enumOld.Current;
                    var e  = kv.Key;
                    bool stillHidden = false;
                    // check if in occludedList
                    for (int oi = 0; oi < occludedList.Length; oi++)
                    {
                        if (occludedList[oi].entity.Equals(e)) { stillHidden = true; break; }
                    }
                    if (!stillHidden)
                    {
                        if (originals.TryGetValue(e, out var origBounds))
                        {
                            // restore pass flags
                            for (int j = 0; j < cullingData.Length; j++)
                            {
                                if (cullingData[j].m_Entity.Equals(e))
                                {
                                    var data = cullingData[j];
                                    data.m_Flags |= PreCullingFlags.PassedCulling;
                                    data.m_Timer = 0;
                                    cullingData[j] = data;
                                    break;
                                }
                            }
                            originals.Remove(e);
                        }
                        enforced.Remove(e);
                    }
                }
                enumOld.Dispose();

                // Tech: Enforce any new occlusions not already tagged
                // User: hide objects that just got blocked
                for (int oi = 0; oi < occludedList.Length; oi++)
                {
                    var (e,bounds) = occludedList[oi];
                    if (!enforced.Contains(e))
                    {
                        originals.TryAdd(e, bounds);
                        for (int j = 0; j < cullingData.Length; j++)
                        {
                            if (cullingData[j].m_Entity.Equals(e))
                            {
                                var data = cullingData[j];
                                data.m_Flags &= ~PreCullingFlags.PassedCulling;
                                data.m_Timer = byte.MaxValue;
                                cullingData[j] = data;
                                break;
                            }
                        }
                        enforced.Add(e);
                    }
                }
            }
        }
    }
}


