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
            if (DEBUG_SIMPLE_CULLING)
            {
                var searchSystem2 = World.GetExistingSystemManaged<Game.Objects.SearchSystem>();
                JobHandle treeDeps2;
                var staticTree = searchSystem2.GetStaticSearchTree(readOnly: false, out treeDeps2);
                Dependency = JobHandle.CombineDependencies(Dependency, treeDeps2);
                Dependency.Complete();

                // Mark occluded using optimized tree culling (sets m_PassedCulling = 0 for occluded)
                var cullingInfoLookup = GetComponentLookup<CullingInfo>(false);
                OcclusionUtilities.ApplyOcclusionCulling(staticTree, camPos, camDir, cullingInfoLookup, 250f);

                // Enforce occlusion via tree: raise minLod for those flagged as not passed
                int processed = 0;
                int enforced = 0;
                Entities
                    .WithAll<Game.Objects.Static>()
                    .WithName("DebugEnforceOccludedInTree")
                    .ForEach((Entity e, ref CullingInfo info) =>
                    {
                        processed++;
                        if (info.m_PassedCulling == 0)
                        {
                            var success = staticTree.TryUpdate(
                                e,
                                new QuadTreeBoundsXZ(
                                    info.m_Bounds,
                                    info.m_Mask,
                                    byte.MaxValue
                                )
                            );
                            if (success) enforced++;
                        }
                    })
                    .Run();

                s_log.Info($"DebugOcclusionTreeEnforce: processed={processed}, enforcedMinLod={enforced}, cam=({camPos.x:F1},{camPos.y:F1},{camPos.z:F1})");

                m_LastCameraPos = camPos;
                m_LastCameraDir = camDir;
                return;
            }

            // Get the existing static search tree and wait for its build
            var searchSystem = World.GetExistingSystemManaged<Game.Objects.SearchSystem>();
            var tree = searchSystem.GetStaticSearchTree(readOnly: true, out var treeDeps);

            // Chain dependency on the SearchSystem's build job
            Dependency = JobHandle.CombineDependencies(Dependency, treeDeps);

            // Prepare lookup for writes
            var lookup = GetComponentLookup<Game.Rendering.CullingInfo>(false);

            // Schedule an apply job that runs the occlusion applier (math lives in utilities)
            var job = new ApplyOcclusionJob
            {
                tree = tree,
                cameraPosition = camPos,
                cameraDirection = camDir,
                cullingInfoLookup = lookup
            };
            var handle = job.Schedule(Dependency);

            // Register our read with the SearchSystem so future builds wait for us
            searchSystem.AddStaticSearchTreeReader(handle);
            Dependency = handle;

            // Cache camera state until next significant move
            m_LastCameraPos = camPos;
            m_LastCameraDir = camDir;
        }

        [BurstCompile]
        private struct ApplyOcclusionJob : IJob
        {
            [ReadOnly] public NativeQuadTree<Entity, QuadTreeBoundsXZ> tree;
            [ReadOnly] public float3 cameraPosition;
            [ReadOnly] public float3 cameraDirection;
            [NativeDisableParallelForRestriction] public ComponentLookup<Game.Rendering.CullingInfo> cullingInfoLookup;

            public void Execute()
            {
                OcclusionUtilities.ApplyOcclusionCulling(tree, cameraPosition, cameraDirection, cullingInfoLookup, 1000f);
            }
        }
    }
}
