using System;
using System.Text;
using System.Text.RegularExpressions;
using EFT;
using EFT.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ImprovedTabUI.Eft
{
    internal static class InventoryTabs
    {
        private const string ModPrefix = "Mod: ";

        private static readonly Regex Markup = new Regex("<[^>]*>", RegexOptions.Compiled);

        private static readonly AccessTools.FieldRef<InventoryScreen, TabGroup> TabGroupRef =
            AccessTools.FieldRefAccess<InventoryScreen, TabGroup>("_tabs");

        internal static TabGroup GroupOf(InventoryScreen screen)
        {
            return screen == null ? null : TabGroupRef(screen);
        }

        internal static string StableId(EInventoryTab? key, Tab tab)
        {
            if (key.HasValue && key.Value != EInventoryTab.Unchanged && Enum.IsDefined(typeof(EInventoryTab), key.Value))
            {
                return key.Value == EInventoryTab.Notes ? "Tasks" : key.Value.ToString();
            }

            return ModPrefix + CleanKey(tab.gameObject.name);
        }

        internal static bool IsVanilla(string id)
        {
            switch (id)
            {
                case "Overall":
                case "Gear":
                case "Health":
                case "Skills":
                case "Map":
                case "Tasks":
                case "Achievements":
                case "Prestige":
                    return true;
                default:
                    return false;
            }
        }

        internal static string DisplayName(Tab tab, string fallback)
        {
            LocalizedText localized = tab.GetComponent<LocalizedText>();

            if (localized != null && localized.enabled && !string.IsNullOrEmpty(localized.localizationKey))
            {
                string translated = localized.localizationKey.Localized(localized._stringCase);

                string clean = Clean(translated);

                if (clean.Length > 0 && translated != localized.localizationKey)
                {
                    return clean;
                }
            }

            TextMeshProUGUI label = tab._normalVersion != null
                ? tab._normalVersion.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;

            if (label == null)
            {
                label = tab.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            string text = Clean(label != null ? label.text : null);
            return text.Length == 0 ? fallback : text;
        }

        private static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return Markup.Replace(text, string.Empty).Replace("\u25BE", string.Empty).Replace("\u25BC", string.Empty).Trim();
        }

        internal static Sprite IconOf(Tab tab)
        {
            Transform root = tab._normalVersion != null ? tab._normalVersion.transform : tab.transform;
            Sprite fallback = null;

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null)
                {
                    continue;
                }

                if (image.name == "Icon")
                {
                    return image.sprite;
                }

                if (fallback == null && image.name != "Filling" && image.name != "FillingMask" && image.name != "Outline" && image.name != "Shadow")
                {
                    fallback = image.sprite;
                }
            }

            return fallback;
        }

        internal static TMP_FontAsset FontOf(Tab tab)
        {
            TextMeshProUGUI label = tab != null ? tab.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            return label != null ? label.font : null;
        }

        private static string CleanKey(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Unnamed";
            }

            StringBuilder clean = new StringBuilder(name.Length);

            foreach (char c in name)
            {
                bool invalid = c == '=' || c == '\n' || c == '\r' || c == '\t' || c == '\\' || c == '"' || c == '\'' || c == '[' || c == ']' || c == ',';
                clean.Append(invalid ? '_' : c);
            }

            return clean.ToString().Trim();
        }
    }
}
