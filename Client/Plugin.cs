using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ImprovedTabUI.Config;
using ImprovedTabUI.Patches;
using ImprovedTabUI.UI;
using UnityEngine;

namespace ImprovedTabUI
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("EscapeFromTarkov.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.hj.improvedtabui";
        public const string PluginName = "Hj's Improved Tab UI";
        public const string PluginVersion = "1.0.0";

        public const int MinWheelSlots = 2;
        public const int MaxWheelSlots = 12;

        internal static ManualLogSource Log;

        private const string GENERAL_SECTION = "1. General";
        private const string VISIBILITY_SECTION = "2. Tab Visibility";
        private const string KEYS_SECTION = "3. Tab Keys";
        private const string RADIAL_SECTION = "4. Radial Menu";

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<KeyboardShortcut> WindowKey;
        public static ConfigEntry<KeyboardShortcut> PreviousTabKey;
        public static ConfigEntry<KeyboardShortcut> NextTabKey;
        public static ConfigEntry<bool> ShowKeyButtons;
        public static ConfigEntry<bool> ShowTabKeys;
        public static ConfigEntry<bool> FitNames;
        public static ConfigEntry<bool> ScrollNamesAlways;
        public static ConfigEntry<bool> ScrollNamesOnHover;
        public static ConfigEntry<float> ScrollSpeed;

        public static ConfigEntry<string> TabOrder;
        public static ConfigEntry<bool> KeysInRaid;

        public static ConfigEntry<bool> RadialEnabled;
        public static ConfigEntry<KeyboardShortcut> RadialKey;
        public static ConfigEntry<int> WheelSlots;
        public static ConfigEntry<string> WheelLayout;
        public static ConfigEntry<float> RadialSensitivity;
        public static ConfigEntry<float> RadialSmoothing;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind(GENERAL_SECTION, "Enabled", true, Describe(
                "Turns the mod on or off. Needs a game restart.", 30));

            WindowKey = Config.Bind(GENERAL_SECTION, "Open Tab Navigator", new KeyboardShortcut(KeyCode.N), Describe(
                "Opens the Tab Navigator window on the inventory screen.", 25));

            PreviousTabKey = Config.Bind(GENERAL_SECTION, "Previous Tab", new KeyboardShortcut(KeyCode.Q), Describe(
                "Goes to the tab on the left. Clear it to turn it off.", 22));

            NextTabKey = Config.Bind(GENERAL_SECTION, "Next Tab", new KeyboardShortcut(KeyCode.E), Describe(
                "Goes to the tab on the right. Clear it to turn it off.", 21));

            ShowKeyButtons = Config.Bind(GENERAL_SECTION, "Show Key Buttons", true, Describe(
                "Shows the Previous / Next Tab keys as buttons at the ends of the tab bar.", 20));

            ShowTabKeys = Config.Bind(GENERAL_SECTION, "Show Keys On Tabs", true, Describe(
                "Shows each tab's key in front of its icon.", 19));

            FitNames = Config.Bind(GENERAL_SECTION, "Fit Names Next To Keys", true, Describe(
                "Makes the tabs a bit wider so every name fits next to its key.", 18));

            ScrollNamesAlways = Config.Bind(GENERAL_SECTION, "Scroll Long Names", false, Describe(
                "Names that don't fit keep scrolling back and forth.", 17));

            ScrollNamesOnHover = Config.Bind(GENERAL_SECTION, "Scroll Long Names On Hover", true, Describe(
                "Names that don't fit scroll while you hover the tab.", 16));

            ScrollSpeed = Config.Bind(GENERAL_SECTION, "Scroll Speed", 30f, Describe(
                "How far one mouse wheel notch scrolls the tab bar. 30 = Trader Scrolling.", 10, new AcceptableValueRange<float>(5f, 100f)));

            TabOrder = Config.Bind(VISIBILITY_SECTION, "Tab Order", string.Empty, Describe(
                "The tab order. Set it in the Tab Navigator window. Empty = the game's order.", 2000));

            KeysInRaid = Config.Bind(KEYS_SECTION, "Tab Keys Work In Raid", false, Describe(
                "Lets the tab keys open the inventory in raid. They always work while it is open.", 2000));

            RadialEnabled = Config.Bind(RADIAL_SECTION, "Enable Radial Menu", false, Describe(
                "Hold a key to pick a tab from a wheel.", 50));

            RadialKey = Config.Bind(RADIAL_SECTION, "Hold Key", new KeyboardShortcut(KeyCode.Mouse3), Describe(
                "Hold to show the wheel, let go to open the tab you point at. Mouse3 = back side button.", 40));

            WheelSlots = Config.Bind(RADIAL_SECTION, "Slots Per Wheel", 4, Describe(
                "How many tabs fit on one wheel.", 30, new AcceptableValueRange<int>(MinWheelSlots, MaxWheelSlots)));

            WheelLayout = Config.Bind(RADIAL_SECTION, "Wheel Layout", string.Empty, Describe(
                "The wheel layout. Set it in the WHEEL page of the window. Empty = every tab in bar order.", 25));

            RadialSensitivity = Config.Bind(RADIAL_SECTION, "Mouse Sensitivity", 1f, Describe(
                "How far the mouse has to move to pick a tab.", 20, new AcceptableValueRange<float>(0.2f, 4f)));

            RadialSmoothing = Config.Bind(RADIAL_SECTION, "Smoothing", 14f, Describe(
                "How fast the highlight follows the mouse.", 10, new AcceptableValueRange<float>(4f, 40f)));

            TabSettings.Initialize(Config, VISIBILITY_SECTION, KEYS_SECTION);

            if (!Enabled.Value)
            {
                Log.LogInfo("[ImprovedTabUI] Switched off in the config");
                return;
            }

            TryEnable("tab bar", delegate { new InventoryScreenShowPatch().Enable(); });
            TryEnable("map tab", delegate { new InventoryScreenShowMapPatch().Enable(); });
            TryEnable("opening tab", delegate { new TabGroupShowPatch().Enable(); });
            TryEnable("selected tab", delegate { new TabGroupSelectTabPatch().Enable(); });
            TryEnable("tab sizes", delegate { new TabSizeControllerUpdateLayoutPatch().Enable(); });
            TryEnable("tab keys", delegate { new InputTreeTranslatePatch().Enable(); });
            TryEnable("radial cursor", delegate { new CursorVisibilityPatch().Enable(); });

            gameObject.AddComponent<QuickAccess>();
            Log.LogInfo(PluginName + " is ready (SPT 4.1)");
        }

        private void Start()
        {
            if (!Enabled.Value)
            {
                return;
            }

            TryEnableCompat("WristAccessories.AccessoriesTab+Host", "Arrange", "KeepPlace");
            TryEnableCompat("FemReshapeEverything.ReshapeTab+TabPin", "Place");
        }

        internal static ConfigDescription Describe(string text, int order, AcceptableValueBase range = null)
        {
            ConfigurationManagerAttributes attributes = new ConfigurationManagerAttributes();
            attributes.Order = order;
            return new ConfigDescription(text, range, attributes);
        }

        private void TryEnableCompat(string type, params string[] methods)
        {
            if (AccessTools.TypeByName(type) == null)
            {
                return;
            }

            foreach (string method in methods)
            {
                TryEnable(type + "." + method, delegate { new ForeignTabPatch(type + ":" + method).Enable(); });
            }
        }

        private void TryEnable(string what, Action enable)
        {
            try
            {
                enable();
            }
            catch (Exception error)
            {
                Log.LogWarning("[ImprovedTabUI] '" + what + "' patch failed, that part is off: " + error.Message);
            }
        }
    }
}
