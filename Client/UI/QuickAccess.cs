using System;
using System.Collections.Generic;
using EFT.UI;
using ImprovedTabUI.Config;
using ImprovedTabUI.Eft;
using ImprovedTabUI.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal sealed class QuickAccess : MonoBehaviour
    {
        private const int PrepareInterval = 120;

        private readonly List<InventoryTab> _keyed = new List<InventoryTab>();
        private readonly List<InventoryTab> _wheelTabs = new List<InventoryTab>();

        private TabBar _bar;
        private RadialMenu _radial;
        private KeyCode _radialKey;
        private int _keyedVersion = -1;
        private bool _keysDirty = true;
        private Action _languageUnsubscriber;

        internal static bool CapturingKey;
        internal static event Action LanguageChanged;

        private bool _radialWasHeld;

        private void Awake()
        {
            TabSettings.KeysChanged += MarkKeysDirty;
        }

        private void OnDestroy()
        {
            TabSettings.KeysChanged -= MarkKeysDirty;
            _languageUnsubscriber?.Invoke();
        }

        private void MarkKeysDirty()
        {
            _keysDirty = true;
        }

        private void Update()
        {
            bool radialHeld = Plugin.RadialEnabled.Value && UiInput.IsHeld(Plugin.RadialKey.Value, _radial != null && _radial.IsOpen);
            bool radialPressed = radialHeld && !_radialWasHeld;
            _radialWasHeld = radialHeld;

            if (_radial != null && _radial.IsOpen)
            {
                UpdateRadial(radialHeld);
                return;
            }

            if (CapturingKey || !EnsureBar())
            {
                return;
            }

            if (radialPressed && !UiInput.IsTyping())
            {
                OpenRadial();
                return;
            }

            CheckTabKeys();
        }

        private bool EnsureBar()
        {
            if (_bar != null)
            {
                return true;
            }

            if (Time.frameCount % PrepareInterval != 0)
            {
                return false;
            }

            InventoryScreen screen = InventoryOpener.Screen;

            if (screen == null)
            {
                return false;
            }

            _bar = TabBar.Prepare(screen);

            if (_bar != null && _languageUnsubscriber == null)
            {
                _languageUnsubscriber = UiText.OnLanguageChanged(OnLanguageChanged);
            }

            return _bar != null;
        }

        private void OnLanguageChanged()
        {
            if (_bar != null)
            {
                _bar.Registry.RefreshNames();
            }

            LanguageChanged?.Invoke();
        }

        private void CheckTabKeys()
        {
            if (_keysDirty || _keyedVersion != _bar.Registry.Version)
            {
                _keysDirty = false;
                _keyedVersion = _bar.Registry.Version;
                _keyed.Clear();

                foreach (InventoryTab tab in _bar.Registry.Tabs)
                {
                    if (tab.Key.Value.MainKey != KeyCode.None)
                    {
                        _keyed.Add(tab);
                    }
                }
            }

            InventoryTab best = null;
            int bestModifiers = -1;

            for (int i = 0; i < _keyed.Count; i++)
            {
                int modifiers = UiInput.PressedNow(_keyed[i].Key.Value);

                if (modifiers > bestModifiers)
                {
                    best = _keyed[i];
                    bestModifiers = modifiers;
                }
            }

            if (best != null && !UiInput.IsTyping() && (Plugin.KeysInRaid.Value || !InventoryOpener.InRaid || InventoryOpener.InventoryOpen))
            {
                InventoryOpener.Open(best);
            }
        }

        private void OpenRadial()
        {
            InventoryScreen screen = InventoryOpener.Screen;
            TabBar bar = screen != null ? TabBar.Prepare(screen) : null;

            if (bar == null)
            {
                TabLog.WarnOnce("radial-bar", "The radial menu can't open yet, the inventory tab bar isn't ready.");
                return;
            }

            _bar = bar;
            _bar.Registry.RefreshNames();
            _bar.Registry.ResolveOwners();
            _wheelTabs.Clear();

            bool any = false;

            IReadOnlyList<InventoryTab> slots = _bar.Registry.Wheel.Slots;
            int perWheel = NavigatorWindow.SlotsPerWheel;

            for (int start = 0; start < slots.Count; start += perWheel)
            {
                bool used = false;

                for (int i = start; i < start + perWheel && i < slots.Count; i++)
                {
                    used |= slots[i] != null && slots[i].Tab != null;
                }

                if (!used)
                {
                    continue;
                }

                for (int i = start; i < start + perWheel; i++)
                {
                    InventoryTab tab = i < slots.Count && slots[i] != null && slots[i].Tab != null ? slots[i] : null;
                    _wheelTabs.Add(tab);
                    any |= tab != null && tab.CanOpen;
                }
            }

            if (!any)
            {
                TabLog.WarnOnce("radial-empty", "The radial menu has no tab it can open right now.");
                return;
            }

            if (_radial == null)
            {
                _radial = RadialMenu.Create(_bar.Font);
            }

            _radialKey = Plugin.RadialKey.Value.MainKey;
            InventoryOpener.BlockLook = !InventoryOpener.InventoryOpen;
            _radial.Show(_wheelTabs, InventoryOpener.InventoryOpen ? _bar.SelectedTab : null);
        }

        private void UpdateRadial(bool held)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CloseRadial(false);
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                CloseRadial(true);
                return;
            }

            if (!held && !UiInput.KeyHeld(_radialKey, true))
            {
                CloseRadial(true);
                return;
            }

            _radial.Tick();
        }

        private void CloseRadial(bool open)
        {
            InventoryTab chosen = _radial.Close();
            InventoryOpener.BlockLook = false;

            if (open && chosen != null)
            {
                UiSounds.Play(EUISoundType.ButtonClick);
                InventoryOpener.Open(chosen);
            }
        }
    }
}
