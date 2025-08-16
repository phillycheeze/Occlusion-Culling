using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;

namespace OcclusionCulling
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(OcclusionCulling)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private Setting m_Setting;
        private Harmony m_Harmony;

        public static OcclusionCullingSystem OcclusionSystem { get; private set; }
        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
            AssetDatabase.global.LoadSettings(nameof(OcclusionCulling), m_Setting, new Setting(this));

            m_Harmony = new Harmony($"{nameof(OcclusionCulling)}.{nameof(Mod)}");
            m_Harmony.PatchAll();

            // Register occlusion culling
            updateSystem.UpdateAt<OcclusionCullingSystem>(SystemUpdatePhase.PreCulling);
            OcclusionSystem = updateSystem.World.GetOrCreateSystemManaged<OcclusionCullingSystem>();
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
            
            OcclusionSystem = null;
        }


    }
}
