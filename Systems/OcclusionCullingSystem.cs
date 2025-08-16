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
using Colossal.Entities;

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

            var staticTreeRW = m_SearchSystem.GetStaticSearchTree(readOnly: false, out var readDeps);
            Dependency = JobHandle.CombineDependencies(Dependency, readDeps);

            var terrainData = m_TerrainSystem.GetHeightData();

            int nextIndex;
            var occluded = OcclusionUtilities.FindTerrainOccludedEntities(
                staticTreeRW,
                terrainData,
                camPos,
                camDir,
                out nextIndex
            );

            //var cullingData = m_PreCullingSystem.GetCullingData(readOnly: false, out var writeDeps);
            //Dependency = JobHandle.CombineDependencies(Dependency, writeDeps);            

            occlusionResumeIndex = nextIndex;

            //m_PreCullingSystem.ResetCulling();
            m_LastCameraPos = camPos;
            return;
        }

    }
}


