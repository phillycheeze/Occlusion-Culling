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

            // Debug-only: bypass search tree and directly set culling flag based on simple distance
            if (DEBUG_SIMPLE_CULLING)
            {

                var searchSystem2 = World.GetExistingSystemManaged<Game.Objects.SearchSystem>();
                JobHandle treeDeps2;

                var staticTree = searchSystem2.GetStaticSearchTree(readOnly: false, out treeDeps2);
                Dependency = JobHandle.CombineDependencies(Dependency, treeDeps2);
                Dependency.Complete();

                var cullingInfoRO = GetComponentLookup<CullingInfo>(true);

                int processed = 0;
                int setZero = 0;
                int setOne = 0;
                int wasSuccess = 0;

                // Run immediately so we can log reliable counts in the same frame
                Entities
                    .WithAll<Game.Objects.Static>()
                    .WithName("DebugSimpleCulling")
                    .ForEach((Entity e, in Game.Objects.Transform transform) =>
                    {
                        processed++;
                        float distance = math.distance(transform.m_Position, camPos);
                        byte newVal = (byte)(distance < 50f ? 0 : 1);
                        if (newVal == 0 && cullingInfoRO.HasComponent(e))
                        {
                            setZero++;
                            var ci = cullingInfoRO[e];
                            var bounds = ci.m_Bounds;
                            var success = staticTree.TryUpdate(
                                e,
                                new QuadTreeBoundsXZ(
                                    bounds,
                                    ci.m_Mask,
                                    byte.MaxValue
                                )
                            );
                            if (success) wasSuccess++;
                        }
                        else
                        {
                            setOne++;
                        }
                        
                    })
                    .Run();

                s_log.Info($"DebugSimpleCulling: success={wasSuccess}, processed={processed}, setZero={setZero}, setOne={setOne}, cam=({camPos.x:F1},{camPos.y:F1},{camPos.z:F1})");

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
