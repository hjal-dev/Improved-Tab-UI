using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using ImprovedTabUI.Eft;
using UnityEngine;

namespace ImprovedTabUI.Config
{
    internal static class TabSettings
    {
        private static readonly Dictionary<string, ConfigEntry<bool>> VisibleEntries = new Dictionary<string, ConfigEntry<bool>>();
        private static readonly Dictionary<string, ConfigEntry<KeyboardShortcut>> KeyEntries = new Dictionary<string, ConfigEntry<KeyboardShortcut>>();

        private static ConfigFile _config;
        private static string _visibilitySection;
        private static string _keysSection;

        internal static event Action VisibilityChanged;
        internal static event Action KeysChanged;

        internal static void Initialize(ConfigFile config, string visibilitySection, string keysSection)
        {
            _config = config;
            _visibilitySection = visibilitySection;
            _keysSection = keysSection;
        }

        internal static ConfigEntry<bool> Visible(string id, string name, int order)
        {
            if (!VisibleEntries.TryGetValue(id, out ConfigEntry<bool> entry))
            {
                entry = _config.Bind(_visibilitySection, id, true, Named("Shows this tab on the tab bar.", id, name, order));
                entry.SettingChanged += delegate { VisibilityChanged?.Invoke(); };
                VisibleEntries.Add(id, entry);
            }

            return entry;
        }

        internal static ConfigEntry<KeyboardShortcut> Key(string id, string name, int order)
        {
            if (!KeyEntries.TryGetValue(id, out ConfigEntry<KeyboardShortcut> entry))
            {
                entry = _config.Bind(_keysSection, id, KeyboardShortcut.Empty, Named("Opens this tab. Empty = no key.", id, name, order));
                entry.SettingChanged += delegate { KeysChanged?.Invoke(); };
                KeyEntries.Add(id, entry);
            }

            return entry;
        }

        internal static void ClearKeys()
        {
            foreach (ConfigEntry<KeyboardShortcut> entry in KeyEntries.Values)
            {
                if (entry.Value.MainKey != KeyCode.None)
                {
                    entry.Value = KeyboardShortcut.Empty;
                }
            }
        }

        internal static void Rename(string id, string name)
        {
            if (VisibleEntries.TryGetValue(id, out ConfigEntry<bool> visible))
            {
                SetName(visible, id, name);
            }

            if (KeyEntries.TryGetValue(id, out ConfigEntry<KeyboardShortcut> key))
            {
                SetName(key, id, name);
            }
        }

        private static ConfigDescription Named(string text, string id, string name, int order)
        {
            ConfigDescription description = Plugin.Describe(text, 1000 - order);
            ((ConfigurationManagerAttributes)description.Tags[0]).DispName = Label(id, name);
            return description;
        }

        private static void SetName(ConfigEntryBase entry, string id, string name)
        {
            ((ConfigurationManagerAttributes)entry.Description.Tags[0]).DispName = Label(id, name);
        }

        private static string Label(string id, string name)
        {
            return InventoryTabs.IsVanilla(id) ? name : name + " (mod)";
        }
    }
}
