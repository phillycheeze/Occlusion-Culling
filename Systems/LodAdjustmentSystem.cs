using Colossal.Json;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game;
using Game.Common;
using Game.Prefabs;
using OcclusionCulling.Config;
using System;
using System.Collections.Generic;
using System.IO;

namespace OcclusionCulling.Systems
{
    public partial class LodAdjustmentSystem : GameSystemBase
    {
        private static ILog m_Log = Mod.log;
        private PrefabSystem m_PrefabSystem;

        private bool m_Finished = false;

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
            if (m_Finished) return;

            m_Log.Info($"Starting up LodAdjustmentSystem");

            foreach (LodAdjustEntry entry in LodAdjustConfig.Entries)
            {
                var id = new PrefabID(entry.type, entry.name);

                m_Log.Info($"Entry: {id.GetName()}");

                if (!m_PrefabSystem.TryGetPrefab(id, out PrefabBase prefab))
                    continue;

                if (!m_PrefabSystem.TryGetComponentData<ObjectGeometryData>(prefab, out var geom))
                    continue;

                geom.m_MinLod += entry.deltaMinLod;
                var prefabEntity = m_PrefabSystem.GetEntity(prefab);
                EntityManager.SetComponentData(prefabEntity, geom);
                m_Log.Info($"...set to minLod of {geom.m_MinLod}");

                if (!EntityManager.HasComponent<Updated>(prefabEntity))
                {
                    EntityManager.AddComponent<Updated>(prefabEntity);
                    m_Log.Info($"...and added Updated");
                }

            }
            m_Finished = true;
        }

    }
}
