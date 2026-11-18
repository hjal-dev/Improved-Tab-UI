using System.Collections.Generic;
using EFT.UI;
using ImprovedTabUI.Config;
using UnityEngine;

namespace ImprovedTabUI.Eft
{
    internal sealed class TabRegistry
    {
        private const int MaxOwnerAttempts = 3;

        private static readonly System.Comparison<InventoryTab> BySequence = (a, b) => a.Sequence.CompareTo(b.Sequence);

        private readonly List<InventoryTab> _tabs = new List<InventoryTab>();
        private readonly Dictionary<Tab, InventoryTab> _known = new Dictionary<Tab, InventoryTab>();
        private readonly HashSet<string> _usedIds = new HashSet<string>();
        private readonly List<InventoryTab> _found = new List<InventoryTab>();
        private readonly List<InventoryTab> _extras = new List<InventoryTab>();
        private readonly HashSet<Tab> _seen = new HashSet<Tab>();
        private readonly List<InventoryTab> _ordered = new List<InventoryTab>();
        private readonly List<string> _savedOrder = new List<string>();
        private string _savedOrderText;
        private int _nextSequence;


        internal IReadOnlyList<InventoryTab> Tabs
        {
            get { return _tabs; }
        }

        internal WheelLayout Wheel { get; }

        internal TabRegistry()
        {
            Wheel = new WheelLayout(this);
        }

        internal int Version { get; private set; }

        internal bool Refresh(InventoryScreen screen, Transform container)
        {
            _found.Clear();
            _extras.Clear();
            _seen.Clear();

            IReadOnlyDictionary<EInventoryTab, Tab> dictionary = screen._tabDictionary;

            if (dictionary != null)
            {
                foreach (KeyValuePair<EInventoryTab, Tab> pair in dictionary)
                {
                    if (pair.Value != null && _seen.Add(pair.Value))
                    {
                        _found.Add(GetOrCreate(pair.Value, pair.Key));
                    }
                }
            }

            for (int i = 0; i < container.childCount; i++)
            {
                Tab tab = container.GetChild(i).GetComponent<Tab>();

                if (tab != null && _seen.Add(tab))
                {
                    _extras.Add(GetOrCreate(tab, null));
                }
            }

            _extras.Sort(BySequence);
            _found.AddRange(_extras);
            ApplySavedOrder();

            if (SameAsCurrent())
            {
                return false;
            }

            _tabs.Clear();
            _tabs.AddRange(_ordered);
            ForgetDestroyedTabs();
            Version++;
            return true;
        }

        internal void RefreshNames()
        {
            foreach (InventoryTab tab in _tabs)
            {
                if (tab.Tab != null)
                {
                    tab.DisplayName = InventoryTabs.DisplayName(tab.Tab, tab.Id);
                    tab.Icon = InventoryTabs.IconOf(tab.Tab) ?? tab.Icon;
                    TabSettings.Rename(tab.Id, tab.DisplayName);
                }
            }
        }

        internal void ResolveOwners()
        {
            HashSet<string> shared = null;

            foreach (InventoryTab tab in _tabs)
            {
                if (tab.IsVanilla || tab.ModName != null || tab.OwnerAttempts >= MaxOwnerAttempts || tab.Tab == null)
                {
                    continue;
                }

                if (shared == null)
                {
                    shared = SharedOwners();
                }

                string owner = TabOwners.Resolve(tab.Tab, shared);

                if (owner == null && !TabOwners.HasController(tab.Tab))
                {
                    continue;
                }

                tab.OwnerAttempts++;
                tab.ModName = owner;
            }
        }

        private HashSet<string> SharedOwners()
        {
            HashSet<string> shared = new HashSet<string>();

            foreach (InventoryTab tab in _tabs)
            {
                if (tab.IsVanilla)
                {
                    TabOwners.CollectShared(tab.Tab, shared);
                }
            }

            return shared;
        }

        internal int CountShowable()
        {
            int count = 0;

            foreach (InventoryTab tab in _tabs)
            {
                if (tab.Visible.Value && (tab.IsShown || tab.HiddenByUs))
                {
                    count++;
                }
            }

            return count;
        }

        internal int CountVisible()
        {
            int count = 0;

            foreach (InventoryTab tab in _tabs)
            {
                if (tab.Visible.Value)
                {
                    count++;
                }
            }

            return count;
        }

        internal void ResetToDefault()
        {
            foreach (InventoryTab tab in _tabs)
            {
                if (!tab.Visible.Value)
                {
                    tab.Visible.Value = true;
                }
            }

            ResetOrder();
        }

        private InventoryTab GetOrCreate(Tab tab, EInventoryTab? key)
        {
            if (_known.TryGetValue(tab, out InventoryTab existing))
            {
                return existing;
            }

            string id = InventoryTabs.StableId(key, tab);
            string baseId = id;

            for (int copy = 2; !_usedIds.Add(id); copy++)
            {
                id = baseId + " #" + copy;
            }

            string name = InventoryTabs.DisplayName(tab, id);
            int sequence = _nextSequence++;
            InventoryTab created = new InventoryTab(tab, id, name, sequence,
                TabSettings.Visible(id, name, sequence), TabSettings.Key(id, name, sequence));
            _known.Add(tab, created);
            return created;
        }

        internal void Move(InventoryTab tab, int direction)
        {
            MoveTo(tab, _tabs.IndexOf(tab) + direction);
        }

        internal void MoveTo(InventoryTab tab, int target)
        {
            int from = _tabs.IndexOf(tab);

            if (from < 0 || target < 0 || target >= _tabs.Count || target == from)
            {
                return;
            }

            List<string> order = new List<string>(_tabs.Count);

            foreach (InventoryTab known in _tabs)
            {
                order.Add(known.Id);
            }

            order.RemoveAt(from);
            order.Insert(target, tab.Id);

            List<string> saved = SavedOrder();

            for (int i = 0; i < saved.Count; i++)
            {
                if (order.Contains(saved[i]))
                {
                    continue;
                }

                int after = i > 0 ? order.IndexOf(saved[i - 1]) : -1;
                order.Insert(after + 1, saved[i]);
            }

            Plugin.TabOrder.Value = string.Join(",", order);
        }

        internal InventoryTab Find(string id)
        {
            foreach (InventoryTab tab in _tabs)
            {
                if (tab.Id == id)
                {
                    return tab;
                }
            }

            return null;
        }

        internal void ResetOrder()
        {
            Plugin.TabOrder.Value = string.Empty;
        }

        private void ApplySavedOrder()
        {
            _ordered.Clear();
            List<string> saved = SavedOrder();

            foreach (string id in saved)
            {
                foreach (InventoryTab tab in _found)
                {
                    if (tab.Id == id)
                    {
                        _ordered.Add(tab);
                        break;
                    }
                }
            }

            for (int i = 0; i < _found.Count; i++)
            {
                InventoryTab tab = _found[i];

                if (_ordered.Contains(tab))
                {
                    continue;
                }

                int at = 0;

                for (int j = i - 1; j >= 0; j--)
                {
                    int index = _ordered.IndexOf(_found[j]);

                    if (index >= 0)
                    {
                        at = index + 1;
                        break;
                    }
                }

                _ordered.Insert(at, tab);
            }
        }

        private List<string> SavedOrder()
        {
            string text = Plugin.TabOrder.Value ?? string.Empty;

            if (!ReferenceEquals(text, _savedOrderText))
            {
                _savedOrderText = text;
                _savedOrder.Clear();

                foreach (string part in text.Split(','))
                {
                    string id = part.Trim();

                    if (id.Length > 0 && !_savedOrder.Contains(id))
                    {
                        _savedOrder.Add(id);
                    }
                }
            }

            return _savedOrder;
        }

        private bool SameAsCurrent()
        {
            if (_ordered.Count != _tabs.Count)
            {
                return false;
            }

            for (int i = 0; i < _ordered.Count; i++)
            {
                if (_ordered[i] != _tabs[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void ForgetDestroyedTabs()
        {
            List<Tab> dead = null;

            foreach (Tab tab in _known.Keys)
            {
                if (tab == null)
                {
                    (dead ?? (dead = new List<Tab>())).Add(tab);
                }
            }

            if (dead == null)
            {
                return;
            }

            foreach (Tab tab in dead)
            {
                _usedIds.Remove(_known[tab].Id);
                _known.Remove(tab);
            }
        }

    }
}
