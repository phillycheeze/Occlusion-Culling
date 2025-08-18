using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using Game.UI;
using System.Collections.Generic;

namespace OcclusionCulling
{
    [FileLocation("OcclusionCullingMod")]
    [SettingsUIGroupOrder(kMainGroup, kAdvGroup)]
    [SettingsUIShowGroupName(kMainGroup, kAdvGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kAdvSection = "Advanced";

        public const string kMainGroup = "Main";
        public const string kAdvGroup = "Advanced";

        public Setting(IMod mod) : base(mod)
        {

        }

        [SettingsUISlider(min = 200, max = 3000, step = 10, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kMainGroup)]
        public int MaxDistanceSlider { get; set; }

        [SettingsUISlider(min = 24, max = 196, step = 2, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kAdvSection, kAdvGroup)]
        public int SectorsSlider { get; set; }

        [SettingsUISlider(min = 24, max = 256, step = 2, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kAdvSection, kAdvGroup)]
        public int BinsSlider { get; set; }

        [SettingsUISlider(min = 100, max = 2500, step = 10, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kAdvSection, kAdvGroup)]
        public int ObjectOcclusionDistanceSlider { get; set; }

        [SettingsUISlider(min = 50, max = 2500, step = 10, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kAdvSection, kAdvGroup)]
        public int BatchSizeSlider { get; set; }


        public override void SetDefaults()
        {
            this.MaxDistanceSlider = 2000;
            this.SectorsSlider = 96;
            this.BinsSlider = 156;
            this.ObjectOcclusionDistanceSlider = 400;
            this.BatchSizeSlider = 1000;
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
                { m_Setting.GetSettingsLocaleID(), "Better Culling (Alpha)" },

                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },
                { m_Setting.GetOptionTabLocaleID(Setting.kAdvSection), "Advanced" },

                //{ m_Setting.GetBindingMapLocaleID(), "Mod settings sample" },
            };
        }

        public void Unload()
        {

        }
    }
}
