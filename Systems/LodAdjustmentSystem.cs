using Colossal.Logging;
using Game;
using Game.Common;
using Game.Prefabs;
using PerformanceTweaks.Config;
using System.Collections.Generic;
using Unity.Entities;

namespace PerformanceTweaks.Systems
{
    public partial class LodAdjustmentSystem : GameSystemBase
    {
        private static ILog m_Log = Mod.log;
        private PrefabSystem m_PrefabSystem;

        public bool m_AggressiveCulling = false;

        private int m_AggressiveCullingDelta = 3;
        private bool m_Enabled = true;
        private bool m_PendingApply = false;
        private readonly Dictionary<Entity, int> m_OriginalMinLod = new();
        

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!m_PendingApply) return;

            m_Log.Info($"Applying LOD tweaks (enabled={m_Enabled}, aggressive={m_AggressiveCulling})");

            foreach (LodAdjustEntry entry in LodAdjustConfig.Entries)
            {
                var id = new PrefabID(entry.type, entry.name);

                if (!m_PrefabSystem.TryGetPrefab(id, out PrefabBase prefab))
                    continue;

                if (!m_PrefabSystem.TryGetComponentData<ObjectGeometryData>(prefab, out var geom))
                    continue;

                
                var prefabEntity = m_PrefabSystem.GetEntity(prefab);

                if (!m_OriginalMinLod.ContainsKey(prefabEntity))
                {
                    m_OriginalMinLod[prefabEntity] = geom.m_MinLod;
                }

                int baseMinLod = m_OriginalMinLod[prefabEntity];
                int target = baseMinLod;

                if (m_Enabled)
                {
                    int delta = entry.deltaMinLod + (m_AggressiveCulling ? m_AggressiveCullingDelta : 0);
                    target = baseMinLod + delta;
                }

                if (geom.m_MinLod == target)
                    continue;

                geom.m_MinLod = target;
                EntityManager.SetComponentData(prefabEntity, geom);
                if (!EntityManager.HasComponent<Updated>(prefabEntity))
                {
                    EntityManager.AddComponent<Updated>(prefabEntity);
                }
            }

            m_PendingApply = false;
        }

        public void ApplySettings(bool enabled, bool aggressive)
        {
            m_Enabled = enabled;
            m_AggressiveCulling = aggressive;
            m_PendingApply = true;
        }
    }
}
