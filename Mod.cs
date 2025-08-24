using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Input;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;
using UnityEngine.InputSystem;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using Game.Prefabs;

namespace OcclusionCulling
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(OcclusionCulling)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private Setting m_Setting;
        private Harmony m_Harmony;

        public static ProxyAction m_ButtonAction;
        public const string kButtonActionName = "ButtonBinding";

        public static OcclusionCullingSystem OcclusionSystem { get; private set; }
        public static OcclusionStartupSystem StartupSystem { get; private set; }
        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
            m_Setting.RegisterKeyBindings();

            m_ButtonAction = m_Setting.GetAction(kButtonActionName);
            m_ButtonAction.shouldBeEnabled = true;
            m_ButtonAction.onInteraction += (_, phase) =>
            {
                if(phase == InputActionPhase.Performed && UnityEngine.Mathf.Approximately( m_ButtonAction.ReadValue<float>(), 1.0f))
                    OcclusionSystem.Enabled = !OcclusionSystem.Enabled;
                log.Info($"[{m_ButtonAction.name}] On{phase} {m_ButtonAction.ReadValue<float>()}");
            };
            AssetDatabase.global.LoadSettings(nameof(OcclusionCulling), m_Setting, new Setting(this));

            m_Harmony = new Harmony($"{nameof(OcclusionCulling)}.{nameof(Mod)}");
            m_Harmony.PatchAll();

            // Register occlusion culling
            updateSystem.UpdateBefore<OcclusionCullingSystem>(SystemUpdatePhase.PreCulling);
            OcclusionSystem = updateSystem.World.GetOrCreateSystemManaged<OcclusionCullingSystem>();

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
            OcclusionSystem = null;
            StartupSystem = null;
        }


    }
}
