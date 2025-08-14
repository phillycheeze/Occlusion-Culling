using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Rendering;
using Unity.Jobs;
using Colossal.Collections;
using Game.Common;
using Colossal.Logging;

namespace OcclusionCulling
{
    //[UpdateAfter(typeof(Game.Objects.SearchSystem))]
    [UpdateBefore(typeof(Game.Rendering.PreCullingSystem))]
    public partial class OcclusionCullingSystem : SystemBase
    {
        private static ILog s_log = Mod.log;
        static readonly float kMoveThresholdSq = 4f; // 2 m
        static readonly float kCosRotThreshold = math.cos(math.radians(1f));
        private CameraUpdateSystem m_CameraSystem;
        private Game.Rendering.PreCullingSystem m_PreCullingSystem;
        private Game.Objects.SearchSystem m_SearchSystem;
        private float3 m_LastCameraPos;
        private float3 m_LastDirXZ; // keep normalized XZ direction
        private const bool DEBUG_SIMPLE_CULLING = true;
        // Cache of entities enforced and their original bounds for restore
        private NativeParallelHashSet<Entity> m_EnforcedEntities;
        private NativeParallelHashMap<Entity, QuadTreeBoundsXZ> m_OriginalByEntity;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_PreCullingSystem = World.GetExistingSystemManaged<Game.Rendering.PreCullingSystem>();
            m_SearchSystem = World.GetExistingSystemManaged<Game.Objects.SearchSystem>();
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
            float2 dXZ = new float2(camPos.x - m_LastCameraPos.x, camPos.z - m_LastCameraPos.z);
            bool moved = math.lengthsq(dXZ) > kMoveThresholdSq;

            if (!moved)
            {
                float3 dirXZ = math.normalizesafe(new float3(camDir.x, 0f, camDir.z));
                // If first run (uninitialized), treat as moved
                bool uninit = math.lengthsq(new float2(m_LastDirXZ.x, m_LastDirXZ.z)) < 1e-6f;
                bool rotated = uninit || math.dot(dirXZ, m_LastDirXZ) < kCosRotThreshold;
                if (!rotated) return;
            }

            // TODO: move to parallel job, add caching struct, and other optimizations
            // TODO: only have utility find XX max culling entities this frame
            if (DEBUG_SIMPLE_CULLING)
            {
                JobHandle readDeps;
                var staticTreeRO = m_SearchSystem.GetStaticSearchTree(readOnly: true, out readDeps);
                Dependency = JobHandle.CombineDependencies(Dependency, readDeps);

                var occluded = OcclusionUtilities.FindOccludedEntities(staticTreeRO, camPos, camDir, 1000f, Allocator.TempJob);

                // Build delta sets on main thread
                var currentOccluded = new NativeParallelHashSet<Entity>(occluded.Length, Allocator.TempJob);
                for (int i = 0; i < occluded.Length; i++)
                {
                    currentOccluded.Add(occluded[i].entity);
                }

                var toEnforce = new NativeList<(Entity, QuadTreeBoundsXZ)>(Allocator.TempJob);
                for (int i = 0; i < occluded.Length; i++)
                {
                    var (e,b) = occluded[i];
                    if (!m_EnforcedEntities.Contains(e))
                    {
                        toEnforce.Add((e, b));
                    }
                }

                var toRevert = new NativeList<Entity>(Allocator.TempJob);
                var enforcedKeys = m_EnforcedEntities.GetKeyArray(Allocator.Temp);
                for (int i = 0; i < enforcedKeys.Length; i++)
                {
                    var e = enforcedKeys[i];
                    if (!currentOccluded.Contains(e))
                    {
                        toRevert.Add(e);
                    }
                }

                currentOccluded.Dispose();

                // Schedule writer job to apply delta and update caches
                JobHandle writeDeps;
                var staticTree = m_SearchSystem.GetStaticSearchTree(readOnly: false, out writeDeps);
                var deltaJob = new ApplyOcclusionDeltaJob
                {
                    tree = staticTree,
                    toEnforce = toEnforce,
                    toRevert = toRevert,
                    enforced = m_EnforcedEntities,
                    originals = m_OriginalByEntity
                };
                var combined = JobHandle.CombineDependencies(Dependency, writeDeps);
                var handle = deltaJob.Schedule(combined);

                var disposeHandle = occluded.Dispose(handle);
                disposeHandle = toEnforce.Dispose(disposeHandle);
                disposeHandle = toRevert.Dispose(disposeHandle);

                m_SearchSystem.AddStaticSearchTreeWriter(disposeHandle);
                Dependency = disposeHandle;

                // Only needed if running after PreCullingSystem, I think
                //m_PreCullingSystem.ResetCulling();

                m_LastCameraPos = camPos;
                m_LastDirXZ = math.normalizesafe(new float3(camDir.x, 0f, camDir.z));
                return;
            }

            return;
        }

        private struct ApplyOcclusionDeltaJob : IJob
        {
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> tree;
            [ReadOnly] public NativeList<(Entity, QuadTreeBoundsXZ)> toEnforce;
            [ReadOnly] public NativeList<Entity> toRevert;
            public NativeParallelHashSet<Entity> enforced;
            public NativeParallelHashMap<Entity, QuadTreeBoundsXZ> originals;

            public void Execute()
            {
                // Revert entities no longer occluded
                for (int i = 0; i < toRevert.Length; i++)
                {
                    var e = toRevert[i];
                    if (originals.TryGetValue(e, out var original))
                    {
                        tree.TryUpdate(e, original);
                        originals.Remove(e);
                    }
                    enforced.Remove(e);
                }

                // Enforce newly occluded
                for (int i = 0; i < toEnforce.Length; i++)
                {
                    var pair = toEnforce[i];
                    var e = pair.Item1;
                    var b = pair.Item2;
                    originals.TryAdd(e, b);
                    tree.TryUpdate(e, new QuadTreeBoundsXZ(b.m_Bounds, b.m_Mask, byte.MaxValue));
                    enforced.Add(e);
                }
            }
        }
    }
}
