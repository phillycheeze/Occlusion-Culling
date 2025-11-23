using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;

using Game.Prefabs;
using PerformanceTweaks.Systems;

namespace PerformanceTweaks
{
    public class Mod : IMod
    {
		public static ILog log = LogManager.GetLogger($"{nameof(PerformanceTweaks)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private Setting m_Setting;
        private Harmony m_Harmony;

        public static LodAdjustmentSystem m_LodAdjustmentSystem {  get; private set; }
        public static OcclusionCullingSystem m_OcclusionSystem { get; private set; }
        public static OcclusionStartupSystem m_StartupSystem { get; private set; }
        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

			m_Setting = new Setting(this);
			// Ensure in-memory defaults are set for first run before any UI is created
			m_Setting.SetDefaults();
			// Load saved settings (overrides defaults if present) BEFORE registering UI
			var defaults = new Setting(this);
			defaults.SetDefaults();
			AssetDatabase.global.LoadSettings(nameof(PerformanceTweaks), m_Setting, defaults);
			// Now register UI and localization
			m_Setting.RegisterInOptionsUI();
			GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));

			m_Harmony = new Harmony($"{nameof(PerformanceTweaks)}.{nameof(Mod)}");
            m_Harmony.PatchAll();

            // Register occlusion culling
            updateSystem.UpdateAt<OcclusionCullingSystem>(SystemUpdatePhase.PreCulling);
            m_OcclusionSystem = updateSystem.World.GetOrCreateSystemManaged<OcclusionCullingSystem>();
            m_OcclusionSystem.Enabled = m_Setting.EnableTerrainCulling;

			// LOD adjustment system (always created; apply settings live)
			updateSystem.UpdateAfter<LodAdjustmentSystem, BuildingInitializeSystem>(SystemUpdatePhase.PrefabUpdate);
			m_LodAdjustmentSystem = updateSystem.World.GetOrCreateSystemManaged<LodAdjustmentSystem>();
			m_LodAdjustmentSystem.ApplySettings(m_Setting.EnableLodAdjustmentSystem, m_Setting.AggressiveCulling);

            // TODO: Determine if this is needed
            //updateSystem.UpdateAfter<OcclusionStartupSystem>(SystemUpdatePhase.Rendering);
            //StartupSystem = updateSystem.World.GetOrCreateSystemManaged<OcclusionStartupSystem>();

        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));

            m_Harmony.UnpatchAll();
            m_Harmony = null;

            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
            m_OcclusionSystem = null;
            m_StartupSystem = null;
        }


    }
}
