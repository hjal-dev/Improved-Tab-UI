using BepInEx.Configuration;
using UnityEngine;

namespace ImprovedTabUI.Eft
{
    internal sealed class InventoryTab
    {
        internal readonly Tab Tab;
        internal readonly TabElement Element;
        internal readonly string Id;
        internal readonly bool IsVanilla;
        internal readonly int Sequence;
        internal readonly ConfigEntry<bool> Visible;
        internal readonly ConfigEntry<KeyboardShortcut> Key;

        internal string DisplayName;
        internal Sprite Icon;

        internal bool HiddenByUs;
        internal string ModName;
        internal int OwnerAttempts;

        internal InventoryTab(Tab tab, string id, string displayName, int sequence, ConfigEntry<bool> visible, ConfigEntry<KeyboardShortcut> key)
        {
            Tab = tab;
            Element = tab.GetComponent<TabElement>();
            Id = id;
            IsVanilla = InventoryTabs.IsVanilla(id);
            DisplayName = displayName;
            Sequence = sequence;
            Visible = visible;
            Key = key;
            Icon = InventoryTabs.IconOf(tab);
        }

        internal bool IsShown
        {
            get { return Tab != null && Tab.gameObject.activeSelf; }
        }

        internal bool CanOpen
        {
            get { return Tab != null && Visible.Value && Tab.gameObject.activeSelf && Tab.Interactable; }
        }
    }
}
