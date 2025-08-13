using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Rendering;
using Unity.Jobs;
using Colossal.Collections;
using Game.Common;
using Colossal.Logging;
using Game.Vehicles;

namespace OcclusionCulling
{
    //[UpdateAfter(typeof(Game.Objects.SearchSystem))]
    [UpdateBefore(typeof(Game.Rendering.PreCullingSystem))]
    public partial class OcclusionCullingSystem : SystemBase
    {
        private static ILog s_log = Mod.log;
        private CameraUpdateSystem m_CameraSystem;
        private float3 m_LastCameraPos;
        private float3 m_LastCameraDir;
        private const bool DEBUG_SIMPLE_CULLING = true; // Set true to bypass tree and directly toggle PassedCulling

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_LastCameraPos = float3.zero;
            m_LastCameraDir = float3.zero;
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

            // Movement/rotation thresholds
            float moveDist = math.distance(camPos, m_LastCameraPos);
            float dot = math.clamp(math.dot(math.normalize(new float3(camDir.x, 0f, camDir.z)), math.normalize(new float3(m_LastCameraDir.x, 0f, m_LastCameraDir.z))), -1f, 1f);
            float rotAngle = math.degrees(math.acos(dot));
            bool camMoved = moveDist > 2f || rotAngle > 1f || m_LastCameraDir.Equals(float3.zero);
            if (!camMoved)
            {
                return;
            }

            // Debug-only: use shadow-box occlusion, then enforce via tree minLod so PreCulling hides it
            // TODO: move to parallel job, add caching struct, and other optimizations
            if (DEBUG_SIMPLE_CULLING)
            {
                var searchSystem2 = World.GetExistingSystemManaged<Game.Objects.SearchSystem>();
                JobHandle treeDeps2;
                var staticTree = searchSystem2.GetStaticSearchTree(readOnly: false, out treeDeps2);
                Dependency = JobHandle.CombineDependencies(Dependency, treeDeps2);
                Dependency.Complete();

                // Find occluded entities only (no side-effects), including their tree bounds/mask
                var occluded = OcclusionUtilities.FindOccludedEntities(staticTree, camPos, camDir, 250f, Allocator.TempJob);

                if (occluded.Length > 2)
                {
                    s_log.Info($"Finished occluding calc; sample1:({occluded[0].entity}, {occluded[0].bounds}); sample2:({occluded[1].entity}, {occluded[1].bounds})");
                }

                // Enforce occlusion via tree only for those entities, preserving original bounds/mask
                int enforced = 0;
                for (int i = 0; i < occluded.Length; i++)
                {
                    var pair = occluded[i];
                    var e = pair.entity;
                    var b = pair.bounds;
                    // Optional safety filter: only trees, skip lots/zones
                    // if ((b.m_Mask & BoundsMask.IsTree) == 0 || (b.m_Mask & (BoundsMask.HasLot | BoundsMask.OccupyZone)) != 0) continue;
                    var success = staticTree.TryUpdate(
                        e,
                        new QuadTreeBoundsXZ(
                            b.m_Bounds,
                            b.m_Mask,
                            byte.MaxValue
                        )
                    );
                    if (success) enforced++;
                }
                occluded.Dispose();

                s_log.Info($"DebugOcclusionTreeEnforce: enforcedMinLod={enforced}, cam=({camPos.x:F1},{camPos.y:F1},{camPos.z:F1})");

                m_LastCameraPos = camPos;
                m_LastCameraDir = camDir;
                return;
            }
        }
    }
}
