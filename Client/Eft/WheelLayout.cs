using System.Collections.Generic;

namespace ImprovedTabUI.Eft
{
    internal sealed class WheelLayout
    {
        private const string EmptySlot = "-";

        private readonly TabRegistry _registry;
        private readonly List<InventoryTab> _slots = new List<InventoryTab>();
        private readonly List<string> _slotIds = new List<string>();
        private readonly List<InventoryTab> _off = new List<InventoryTab>();
        private readonly List<string> _offIds = new List<string>();
        private string _text;
        private int _builtFor = -1;
        private int _version;

        internal WheelLayout(TabRegistry registry)
        {
            _registry = registry;
        }

        internal IReadOnlyList<InventoryTab> Slots
        {
            get
            {
                Rebuild();
                return _slots;
            }
        }

        internal IReadOnlyList<InventoryTab> Off
        {
            get
            {
                Rebuild();
                return _off;
            }
        }

        internal int Version
        {
            get
            {
                Rebuild();
                return _version;
            }
        }

        internal bool Contains(InventoryTab tab)
        {
            Rebuild();
            return _slots.Contains(tab);
        }

        internal int Count()
        {
            Rebuild();
            int count = 0;

            foreach (InventoryTab tab in _slots)
            {
                if (tab != null)
                {
                    count++;
                }
            }

            return count;
        }

        internal int FreeSlot(int start, int count)
        {
            Rebuild();

            for (int slot = start; slot < start + count; slot++)
            {
                if (slot >= _slots.Count || _slots[slot] == null)
                {
                    return slot;
                }
            }

            int index = _slots.IndexOf(null);
            return index >= 0 ? index : _slots.Count;
        }

        internal bool Remove(InventoryTab tab)
        {
            Rebuild();
            int index = _slots.IndexOf(tab);

            if (index < 0 || Count() <= 1)
            {
                return false;
            }

            _slotIds[index] = null;
            _offIds.Insert(0, tab.Id);
            Save();
            return true;
        }

        internal bool Place(InventoryTab tab, int slot)
        {
            Rebuild();
            int from = _slots.IndexOf(tab);

            if (slot < 0 || slot == from)
            {
                return false;
            }

            while (_slotIds.Count <= slot)
            {
                _slotIds.Add(null);
            }

            string occupant = _slotIds[slot];
            _slotIds[slot] = tab.Id;

            if (from >= 0)
            {
                _slotIds[from] = occupant;
            }
            else
            {
                _offIds.Remove(tab.Id);

                if (occupant != null)
                {
                    _offIds.Insert(0, occupant);
                }
            }

            Save();
            return true;
        }

        internal void Reset()
        {
            Plugin.WheelLayout.Value = string.Empty;
        }

        private void Rebuild()
        {
            string text = Plugin.WheelLayout.Value ?? string.Empty;

            if (ReferenceEquals(text, _text) && _builtFor == _registry.Version)
            {
                return;
            }

            _text = text;
            _builtFor = _registry.Version;
            _version++;
            _slotIds.Clear();
            _offIds.Clear();
            HashSet<string> seen = new HashSet<string>();

            foreach (string part in text.Split(','))
            {
                string entry = part.Trim();

                if (entry == EmptySlot)
                {
                    _slotIds.Add(null);
                    continue;
                }

                bool off = entry.StartsWith("!");
                string id = off ? entry.Substring(1).Trim() : entry;

                if (id.Length == 0 || !seen.Add(id))
                {
                    continue;
                }

                if (off)
                {
                    _offIds.Add(id);
                }
                else
                {
                    _slotIds.Add(id);
                }
            }

            while (_slotIds.Count > 0 && _slotIds[_slotIds.Count - 1] == null)
            {
                _slotIds.RemoveAt(_slotIds.Count - 1);
            }

            foreach (InventoryTab tab in _registry.Tabs)
            {
                if (seen.Add(tab.Id))
                {
                    _slotIds.Add(tab.Id);
                }
            }

            _slots.Clear();
            _off.Clear();

            foreach (string id in _slotIds)
            {
                _slots.Add(id != null ? _registry.Find(id) : null);
            }

            foreach (string id in _offIds)
            {
                InventoryTab tab = _registry.Find(id);

                if (tab != null)
                {
                    _off.Add(tab);
                }
            }
        }

        private void Save()
        {
            List<string> entries = new List<string>();
            int last = _slotIds.Count - 1;

            while (last >= 0 && _slotIds[last] == null)
            {
                last--;
            }

            for (int i = 0; i <= last; i++)
            {
                entries.Add(_slotIds[i] ?? EmptySlot);
            }

            foreach (string id in _offIds)
            {
                entries.Add("!" + id);
            }

            Plugin.WheelLayout.Value = string.Join(",", entries);
        }
    }
}
