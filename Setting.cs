using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using System.Collections.Generic;
using PerformanceTweaks.Patches;

namespace PerformanceTweaks
{
	[FileLocation("ModsSettings/PerformanceTweaks/PerformanceTweaks")]
	[SettingsUIGroupOrder(kGpuGroup)]
	[SettingsUIShowGroupName(kGpuGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

		public const string kGpuGroup = "GPU";

        public Setting(IMod mod) : base(mod)
        {

        }

		private bool m_EnableTerrainCulling;

		[SettingsUISection(kSection, kGpuGroup)]
		public bool EnableTerrainCulling
		{
			get => m_EnableTerrainCulling;
			set
			{
				m_EnableTerrainCulling = value;
				if (Mod.m_OcclusionSystem != null)
				{
					Mod.m_OcclusionSystem.Enabled = value;
				}
			}
		}

		private bool m_EnableLodAdjustmentSystem;

		[SettingsUISection(kSection, kGpuGroup)]
		public bool EnableLodAdjustmentSystem
		{
			get => m_EnableLodAdjustmentSystem;
			set
			{
				m_EnableLodAdjustmentSystem = value;
				if (Mod.m_LodAdjustmentSystem != null)
				{
					Mod.m_LodAdjustmentSystem.ApplySettings(m_EnableLodAdjustmentSystem, m_AggressiveCulling);
				}
			}
		}

		private bool m_AggressiveCulling;

		[SettingsUISection(kSection, kGpuGroup)]
		public bool AggressiveCulling
		{
			get => m_AggressiveCulling;
			set
			{
				m_AggressiveCulling = value;
				if (Mod.m_LodAdjustmentSystem != null)
					Mod.m_LodAdjustmentSystem.ApplySettings(m_EnableLodAdjustmentSystem, m_AggressiveCulling);
			}
		}

		[SettingsUISection(kSection, kGpuGroup)]
		public bool EnableShadowCullingPatch
		{
			get => Patch_GetShadowCullingData.Enabled;
			set => Patch_GetShadowCullingData.Enabled = value;
		}

		[SettingsUISection(kSection, kGpuGroup)]
		public bool DoubleShadowCullingExtents
		{
			get => Patch_GetShadowCullingData.DoubleYZ;
			set => Patch_GetShadowCullingData.DoubleYZ = value;
		}


        public override void SetDefaults()
        {
			this.EnableTerrainCulling = true;
			m_EnableLodAdjustmentSystem = true;
			m_AggressiveCulling = false;
			// Defer applying to Mod.OnLoad where system exists
			this.EnableShadowCullingPatch = true;
			this.DoubleShadowCullingExtents = false;
        }
        
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Performance Tweaks" },

                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },
				{ m_Setting.GetOptionGroupLocaleID(Setting.kGpuGroup), "GPU" },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableTerrainCulling)), "Enable Terrain Culling" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableTerrainCulling)), "Will remove objects from rendering pipeline if visibility is blocked by terrain. Increases CPU usage." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableLodAdjustmentSystem)), "Enable LOD Adjustment System" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableLodAdjustmentSystem)), "Applies LOD distance adjustements based on manually-reviewed objects." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.AggressiveCulling)), "Aggressive culling (extra LOD bias)" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.AggressiveCulling)), "Increase minimum LOD by an additional bias for even more performance." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableShadowCullingPatch)), "Enable Shadow Culling Patch" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableShadowCullingPatch)), "Toggle an additional shadow rendering bug fix to properly respect your Settings > Graphics > Shadows selection." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.DoubleShadowCullingExtents)), "Increased Shadow Threshold" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.DoubleShadowCullingExtents)), "Multiplies the threshold for shadow sizes that can pass through the rendering pipeline for more performance." },
            };
        }

        public void Unload()
        {

        }
    }
}
