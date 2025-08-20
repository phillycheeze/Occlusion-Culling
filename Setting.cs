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
    [SettingsUIGroupOrder(kMainGroup, kKeybindingGroup, kAdvGroup)]
    [SettingsUIShowGroupName(kMainGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        //public const string kAdvSection = "Advanced";

        public const string kMainGroup = "Main";
        public const string kAdvGroup = "Advanced";

        public const string kKeybindingGroup = "KeyBinding";

        public Setting(IMod mod) : base(mod)
        {

        }

        [SettingsUIKeyboardBinding(BindingKeyboard.M, Mod.kButtonActionName, ctrl: true)]
        [SettingsUISection(kSection, kKeybindingGroup)]
        public ProxyBinding KeyboardBinding { get; set; }

        [SettingsUISection(kSection, kKeybindingGroup)]
        public bool ResetBindings
        {
            set
            {
                Mod.log.Info("Reset key bindings");
                ResetKeyBindings();
            }
        }

        [SettingsUISlider(min = 200, max = 3000, step = 10, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kMainGroup)]
        public int MaxDistanceSlider { get; set; }

        [SettingsUISlider(min = 24, max = 196, step = 2, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kAdvGroup)]
        public int SectorsSlider { get; set; }

        [SettingsUISlider(min = 24, max = 256, step = 2, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kAdvGroup)]
        public int BinsSlider { get; set; }

        [SettingsUISlider(min = 100, max = 2500, step = 10, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kAdvGroup)]
        public int ObjectOcclusionDistanceSlider { get; set; }

        [SettingsUISlider(min = 50, max = 2500, step = 10, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kAdvGroup)]
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
                { m_Setting.GetOptionGroupLocaleID(Setting.kKeybindingGroup), "Key bindings" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.KeyboardBinding)), "Enable/Disable system" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.KeyboardBinding)), $"Keyboard binding of Button input action" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetBindings)), "Reset key bindings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetBindings)), $"Reset all key bindings of the mod" },

                { m_Setting.GetBindingKeyLocaleID(Mod.kButtonActionName), "Button key" },
                //{ m_Setting.GetOptionTabLocaleID(Setting.kAdvSection), "Advanced" },

                //{ m_Setting.GetBindingMapLocaleID(), "Mod settings sample" },
            };
        }

        public void Unload()
        {

        }
    }
}
