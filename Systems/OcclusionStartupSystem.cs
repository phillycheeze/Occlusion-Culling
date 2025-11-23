using Colossal.Logging;
using Game;
using Game.Common;
using Game.Objects;
using Game.Rendering;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PerformanceTweaks
{
    public partial class OcclusionStartupSystem : GameSystemBase
    {
        private static ILog s_log = Mod.log;
        private CameraUpdateSystem m_CameraSystem;
        private EntityCommandBufferSystem m_EntityCommandBufferSystem;
        private EntityCommandBuffer m_EntityCommandBuffer;
        private EntityQuery m_EntityQuery;
        private EntityQueryDesc m_EntityQueryDesc;

        NativeArray<Entity> entities;
        private JobHandle handle = default;
        private int jobPerChunkCount = (int)2048 / 4;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_CameraSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_EntityCommandBufferSystem = World.GetExistingSystemManaged<EntityCommandBufferSystem>();

            m_EntityQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Transform>(),
                    ComponentType.ReadOnly<Static>(),
                    ComponentType.ReadOnly<CullingInfo>()
                },
                None = new ComponentType[] {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Moving>()
                },
                Absent = new ComponentType[]
                {
                    ComponentType.ReadOnly<OcclusionDirtyTag>() // Don't process items that are already tagged
                }
            };

            m_EntityQuery = GetEntityQuery(m_EntityQueryDesc);
        }

        protected override void OnStartRunning()
        {
            s_log.Info(nameof(OnStartRunning));
        }

        protected override void OnDestroy()
        {
            s_log.Info(nameof(OnDestroy));
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!handle.Equals(default(JobHandle)))
            {
                if(handle.IsCompleted)
                {
                    handle.Complete();
                    m_EntityCommandBuffer.Dispose();
                    entities.Dispose();

                    handle = default;
                }
                else
                {
                    return; // Job still running, lets wait
                }
            }

            if(m_EntityQuery.CalculateEntityCount() <= 0)
            {
                return;
            }

            m_EntityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();
            entities = m_EntityQuery.ToEntityArray(Allocator.Persistent);

            var job = new OcclusionStartupTagJob
            {
                candidates = entities,
                buffer = m_EntityCommandBuffer.AsParallelWriter()
            };
            handle = job.ScheduleParallel(entities.Length, (int)(entities.Length / 3), new JobHandle());
        }

        //[BurstCompile]
        public struct OcclusionStartupTagJob : IJobFor
        {
            [ReadOnly] public EntityCommandBuffer.ParallelWriter buffer;
            [ReadOnly] public NativeArray<Entity> candidates;

            public void Execute(int index)
            {
                Entity entity = candidates[index];
                buffer.AddComponent<OcclusionDirtyTag>(index, entity);
                buffer.SetComponentEnabled<OcclusionDirtyTag>(index, entity, false);
            }
        }
    }
}
