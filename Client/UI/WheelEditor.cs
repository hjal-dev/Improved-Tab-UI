using System.Collections.Generic;
using EFT.UI;
using ImprovedTabUI.Eft;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal sealed class WheelEditor : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private const float WheelHeight = 284f;
        private const float OuterRadius = 118f;
        private const float InnerRadius = 62f;
        private const float TickRadius = 132f;
        private const float IconSize = 30f;
        private const float GapDegrees = 2.5f;
        private const float ArrowOffset = 156f;
        private const float ArrowSize = 24f;
        private const float TrayLabelHeight = 22f;
        private const float TileSize = 36f;
        private const float TileGap = 4f;
        private const float BottomPadding = 10f;
        private const float GhostSize = 34f;
        private const float FlipDelay = 0.45f;
        private const int MaxSlots = Plugin.MaxWheelSlots;

        private static readonly Color SlotFilled = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color SlotEmpty = new Color(1f, 1f, 1f, 0.025f);
        private static readonly Color SlotHover = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color SlotTarget = new Color(0.85f, 0.79f, 0.56f, 0.5f);
        private static readonly Color TrayFill = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color TrayTarget = new Color(0.85f, 0.79f, 0.56f, 0.2f);
        private static readonly Color TileFill = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color TileHover = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color CenterColor = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color Faded = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color Lifted = new Color(1f, 1f, 1f, 0.15f);

        private readonly List<InventoryTab> _slots = new List<InventoryTab>();
        private readonly List<InventoryTab> _off = new List<InventoryTab>();
        private readonly Image[] _segments = new Image[MaxSlots];
        private readonly Image[] _icons = new Image[MaxSlots];
        private readonly TextMeshProUGUI[] _letters = new TextMeshProUGUI[MaxSlots];
        private readonly List<Image> _tiles = new List<Image>();
        private readonly List<Image> _tileIcons = new List<Image>();
        private readonly List<TextMeshProUGUI> _tileLetters = new List<TextMeshProUGUI>();

        private NavigatorWindow _window;
        private TMP_FontAsset _font;
        private RectTransform _rect;
        private RectTransform _wheel;
        private RectTransform _tray;
        private RectTransform _grid;
        private Image _gridBackground;
        private TextMeshProUGUI _trayLabel;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI _page;
        private RectTransform _previous;
        private RectTransform _next;
        private RectTransform _ghost;
        private Image _ghostIcon;
        private TextMeshProUGUI _ghostLetter;
        private Camera _camera;

        private int _perWheel = 4;
        private int _pageIndex;
        private int _pageCount = 1;
        private int _rows = 1;
        private InventoryTab _dragged;
        private int _hoverSlot = -1;
        private int _hoverTile = -1;
        private bool _overTray;
        private int _overArrow;
        private float _arrowSince;

        internal float Height
        {
            get { return WheelHeight + TrayLabelHeight + _rows * (TileSize + TileGap) + BottomPadding; }
        }

        private static int Columns
        {
            get { return Mathf.Max(1, Mathf.FloorToInt((NavigatorWindow.Width - NavigatorWindow.Margin * 2f + TileGap) / (TileSize + TileGap))); }
        }

        internal static WheelEditor Create(RectTransform parent, NavigatorWindow window, TMP_FontAsset font, float top, float bottom)
        {
            RectTransform root = UiHelpers.Rect("WheelEditor", parent);
            UiHelpers.Stretch(root, 0f, bottom, 0f, top);
            UiHelpers.Image(root, Color.clear, true);

            WheelEditor editor = root.gameObject.AddComponent<WheelEditor>();
            editor._window = window;
            editor._font = font;
            editor._rect = root;
            editor.Build();
            root.gameObject.SetActive(false);
            return editor;
        }

        internal void ApplyLanguage()
        {
            _trayLabel.text = UiText.Get(UiText.OffWheel);
        }

        internal void Refresh()
        {
            TabRegistry registry = _window.Registry;
            _perWheel = NavigatorWindow.SlotsPerWheel;
            _slots.Clear();
            _off.Clear();

            foreach (InventoryTab tab in registry.Wheel.Slots)
            {
                _slots.Add(tab != null && tab.Tab != null ? tab : null);
            }

            foreach (InventoryTab tab in registry.Wheel.Off)
            {
                if (tab.Tab != null)
                {
                    _off.Add(tab);
                }
            }

            _rows = Mathf.Max(1, (_off.Count + Columns - 1) / Columns);
            Layout();
        }

        private void OnEnable()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            canvas = canvas != null ? canvas.rootCanvas : null;
            _camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        private void OnDisable()
        {
            _dragged = null;
            _hoverSlot = -1;
            _hoverTile = -1;
            _overTray = false;
            _overArrow = 0;

            if (_ghost != null)
            {
                _ghost.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            Vector2 pointer = Input.mousePosition;
            Track(pointer);

            if (_dragged != null)
            {
                MoveGhost(pointer);

                if (_overArrow != 0 && Time.unscaledTime - _arrowSince >= FlipDelay)
                {
                    _arrowSince = Time.unscaledTime;
                    Flip(_overArrow);
                }
            }

            float scroll = Input.mouseScrollDelta.y;

            if (scroll != 0f && OverWheel(pointer))
            {
                Flip(scroll < 0f ? 1 : -1);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            InventoryTab tab = TabAt(eventData.pressPosition);

            if (tab == null)
            {
                return;
            }

            _dragged = tab;
            _ghostIcon.sprite = tab.Icon;
            _ghostIcon.enabled = tab.Icon != null;
            _ghostLetter.text = tab.Icon != null ? string.Empty : Letter(tab);
            _ghost.SetAsLastSibling();
            _ghost.gameObject.SetActive(true);
            MoveGhost(eventData.position);
            UiSounds.Play(EUISoundType.ButtonClick);
            Layout();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragged != null)
            {
                MoveGhost(eventData.position);
                Track(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragged == null)
            {
                return;
            }

            InventoryTab tab = _dragged;
            _dragged = null;
            _ghost.gameObject.SetActive(false);
            Track(eventData.position);

            TabRegistry registry = _window.Registry;

            if (_hoverSlot >= 0)
            {
                Finish(registry.Wheel.Place(tab, _pageIndex * _perWheel + _hoverSlot), true);
            }
            else if (_overTray && registry.Wheel.Contains(tab))
            {
                Finish(registry.Wheel.Remove(tab), registry.Wheel.Count() > 1);
            }
            else
            {
                Layout();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            InventoryTab tab = TabAt(eventData.position);

            if (tab == null)
            {
                return;
            }

            TabRegistry registry = _window.Registry;
            bool placed = registry.Wheel.Contains(tab);

            if (eventData.button == PointerEventData.InputButton.Middle)
            {
                return;
            }

            if (placed)
            {
                Finish(registry.Wheel.Remove(tab), registry.Wheel.Count() > 1);
            }
            else
            {
                int slot = registry.Wheel.FreeSlot(_pageIndex * _perWheel, _perWheel);
                bool changed = registry.Wheel.Place(tab, slot);
                _pageIndex = slot / _perWheel;
                Finish(changed, true);
            }
        }

        private void Finish(bool changed, bool allowed)
        {
            if (changed)
            {
                UiSounds.Play(EUISoundType.MenuCheckBox);
            }
            else if (!allowed)
            {
                UiSounds.Play(EUISoundType.ErrorMessage);
                _window.ShowKeepOne();
            }

            _window.Refresh();
            Layout();
        }

        private void Flip(int direction)
        {
            int target = Mathf.Clamp(_pageIndex + direction, 0, _pageCount - 1);

            if (target == _pageIndex)
            {
                return;
            }

            _pageIndex = target;
            UiSounds.Play(EUISoundType.MenuDropdownSelect);
            Layout();
        }

        private void Track(Vector2 screen)
        {
            int slot = SlotAt(screen);
            int tile = slot < 0 ? TileAt(screen) : -1;
            bool tray = slot < 0 && RectTransformUtility.RectangleContainsScreenPoint(_tray, screen, _camera);
            int arrow = Over(_previous, screen) ? -1 : Over(_next, screen) ? 1 : 0;

            if (arrow != _overArrow)
            {
                _overArrow = arrow;
                _arrowSince = Time.unscaledTime;
            }

            if (slot == _hoverSlot && tile == _hoverTile && tray == _overTray)
            {
                return;
            }

            if (_dragged != null && slot >= 0 && slot != _hoverSlot)
            {
                UiSounds.Play(EUISoundType.ButtonOver);
            }

            _hoverSlot = slot;
            _hoverTile = tile;
            _overTray = tray;
            Paint();
        }

        private bool Over(RectTransform rect, Vector2 screen)
        {
            return rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, _camera);
        }

        private bool WheelPoint(Vector2 screen, out Vector2 local)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_wheel, screen, _camera, out local))
            {
                return false;
            }

            local -= _wheel.rect.center;
            return true;
        }

        private bool OverWheel(Vector2 screen)
        {
            return WheelPoint(screen, out Vector2 local) && local.magnitude <= TickRadius;
        }

        private int SlotAt(Vector2 screen)
        {
            if (!WheelPoint(screen, out Vector2 local))
            {
                return -1;
            }

            float distance = local.magnitude;

            if (distance < InnerRadius - 6f || distance > OuterRadius + 10f)
            {
                return -1;
            }

            float slice = 360f / _perWheel;
            float angle = Mathf.Repeat(Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg + slice * 0.5f, 360f);
            return Mathf.Min((int)(angle / slice), _perWheel - 1);
        }

        private int TileAt(Vector2 screen)
        {
            for (int i = 0; i < _off.Count && i < _tiles.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_tiles[i].rectTransform, screen, _camera))
                {
                    return i;
                }
            }

            return -1;
        }

        private InventoryTab TabAt(Vector2 screen)
        {
            int slot = SlotAt(screen);

            if (slot >= 0)
            {
                return SlotTab(slot);
            }

            int tile = TileAt(screen);
            return tile >= 0 ? _off[tile] : null;
        }

        private InventoryTab SlotTab(int slot)
        {
            int index = _pageIndex * _perWheel + slot;
            return slot >= 0 && slot < _perWheel && index < _slots.Count ? _slots[index] : null;
        }

        private void MoveGhost(Vector2 screen)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, _camera, out Vector2 local))
            {
                _ghost.localPosition = local;
            }
        }

        private void Layout()
        {
            _pageCount = Mathf.Max(1, (_slots.Count + _perWheel - 1) / _perWheel) + (_dragged != null ? 1 : 0);
            _pageIndex = Mathf.Clamp(_pageIndex, 0, _pageCount - 1);

            float slice = 360f / _perWheel;
            float fill = (slice - GapDegrees) / 360f;
            float iconRadius = (OuterRadius + InnerRadius) * 0.5f;

            for (int i = 0; i < MaxSlots; i++)
            {
                bool used = i < _perWheel;
                _segments[i].gameObject.SetActive(used);
                _icons[i].gameObject.SetActive(false);
                _letters[i].gameObject.SetActive(false);

                if (!used)
                {
                    continue;
                }

                _segments[i].fillAmount = fill;
                _segments[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, -(i * slice - (slice - GapDegrees) * 0.5f));

                InventoryTab tab = SlotTab(i);

                if (tab == null)
                {
                    continue;
                }

                float radians = i * slice * Mathf.Deg2Rad;
                Vector2 position = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * iconRadius;

                if (tab.Icon != null)
                {
                    _icons[i].sprite = tab.Icon;
                    _icons[i].rectTransform.anchoredPosition = position;
                    _icons[i].gameObject.SetActive(true);
                }
                else
                {
                    _letters[i].text = Letter(tab);
                    _letters[i].rectTransform.anchoredPosition = position;
                    _letters[i].gameObject.SetActive(true);
                }
            }

            _previous.gameObject.SetActive(_pageIndex > 0);
            _next.gameObject.SetActive(_pageIndex < _pageCount - 1);
            _page.text = _pageCount > 1 ? (_pageIndex + 1) + " / " + _pageCount : string.Empty;
            LayoutTray();
            Paint();
        }

        private void LayoutTray()
        {
            int columns = Columns;
            float step = TileSize + TileGap;
            float gridHeight = _rows * step - TileGap;

            _tray.offsetMin = new Vector2(NavigatorWindow.Margin, -(WheelHeight + TrayLabelHeight + gridHeight + 4f));
            _tray.offsetMax = new Vector2(-NavigatorWindow.Margin, -WheelHeight);
            _grid.sizeDelta = new Vector2(0f, gridHeight + 4f);

            while (_tiles.Count < _off.Count)
            {
                AddTile();
            }

            for (int i = 0; i < _tiles.Count; i++)
            {
                bool used = i < _off.Count;
                _tiles[i].gameObject.SetActive(used);

                if (!used)
                {
                    continue;
                }

                InventoryTab tab = _off[i];
                _tiles[i].rectTransform.anchoredPosition = new Vector2(2f + i % columns * step, -2f - i / columns * step);
                _tileIcons[i].sprite = tab.Icon;
                _tileIcons[i].enabled = tab.Icon != null;
                _tileLetters[i].text = tab.Icon != null ? string.Empty : Letter(tab);
            }
        }

        private void Paint()
        {
            for (int i = 0; i < _perWheel; i++)
            {
                InventoryTab tab = SlotTab(i);
                bool hovered = i == _hoverSlot;
                _segments[i].color = _dragged != null && hovered ? SlotTarget : hovered && tab != null ? SlotHover : tab != null ? SlotFilled : SlotEmpty;

                Color color = tab == null ? Color.clear : tab == _dragged ? Lifted : tab.Visible.Value ? Color.white : UiHelpers.Dim;
                _icons[i].color = color;
                _letters[i].color = color;
            }

            for (int i = 0; i < _off.Count && i < _tiles.Count; i++)
            {
                InventoryTab tab = _off[i];
                _tiles[i].color = _dragged == null && i == _hoverTile ? TileHover : TileFill;

                Color color = tab == _dragged ? Lifted : tab.Visible.Value ? Color.white : UiHelpers.Dim;
                _tileIcons[i].color = color;
                _tileLetters[i].color = color;
            }

            bool dropping = _dragged != null && _overTray && _slots.Contains(_dragged);
            _gridBackground.color = dropping ? TrayTarget : TrayFill;

            InventoryTab shown = _dragged ?? (_hoverSlot >= 0 ? SlotTab(_hoverSlot) : _hoverTile >= 0 ? _off[_hoverTile] : null);
            string name = shown != null ? shown.DisplayName ?? string.Empty : string.Empty;

            if (_name.text != name)
            {
                _name.text = name;
            }
        }

        private static string Letter(InventoryTab tab)
        {
            return string.IsNullOrEmpty(tab.DisplayName) ? "?" : tab.DisplayName.Substring(0, 1);
        }

        private void Build()
        {
            _wheel = UiHelpers.Pin(UiHelpers.Rect("Wheel", _rect), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(WheelHeight, WheelHeight));
            Vector2 center = new Vector2(0.5f, 0.5f);

            Sprite ring = UiHelpers.Ring(InnerRadius / OuterRadius);

            for (int i = 0; i < MaxSlots; i++)
            {
                RectTransform segment = UiHelpers.Pin(UiHelpers.Rect("Segment", _wheel), center, Vector2.zero,
                    new Vector2(OuterRadius * 2f, OuterRadius * 2f));
                _segments[i] = UiHelpers.Filled(UiHelpers.Image(segment, SlotEmpty, false), ring);
            }

            for (int i = 0; i < MaxSlots; i++)
            {
                RectTransform icon = UiHelpers.Pin(UiHelpers.Rect("Icon", _wheel), center, Vector2.zero, new Vector2(IconSize, IconSize));
                _icons[i] = UiHelpers.Image(icon, Color.white, false);
                _icons[i].preserveAspect = true;

                RectTransform letter = UiHelpers.Pin(UiHelpers.Rect("Letter", _wheel), center, Vector2.zero, new Vector2(IconSize, IconSize));
                _letters[i] = UiHelpers.Label(letter, _font, string.Empty, 18f, Color.white, TextAlignmentOptions.Center);
            }

            float centerSize = (InnerRadius - 8f) * 2f;
            RectTransform disc = UiHelpers.Pin(UiHelpers.Rect("Center", _wheel), center, Vector2.zero, new Vector2(centerSize, centerSize));
            UiHelpers.Image(disc, CenterColor, false).sprite = UiHelpers.Ring(0f);

            RectTransform name = UiHelpers.Pin(UiHelpers.Rect("Name", _wheel), center, new Vector2(0f, 4f), new Vector2(centerSize - 12f, 24f));
            _name = UiHelpers.Label(name, _font, string.Empty, 13f, Color.white, TextAlignmentOptions.Center);
            _name.enableAutoSizing = true;
            _name.fontSizeMin = 8f;
            _name.fontSizeMax = 13f;

            RectTransform page = UiHelpers.Pin(UiHelpers.Rect("Page", _wheel), center, new Vector2(0f, -18f), new Vector2(80f, 18f));
            _page = UiHelpers.Label(page, _font, string.Empty, 11f, Faded, TextAlignmentOptions.Center);

            _previous = PageButton("Previous", -1);
            _next = PageButton("Next", 1);

            _tray = UiHelpers.Rect("Tray", _rect);
            _tray.anchorMin = new Vector2(0f, 1f);
            _tray.anchorMax = new Vector2(1f, 1f);
            _tray.pivot = new Vector2(0.5f, 1f);

            RectTransform label = UiHelpers.Rect("Label", _tray);
            UiHelpers.TopStrip(label, 0f, TrayLabelHeight);
            _trayLabel = UiHelpers.Label(label, _font, UiText.Get(UiText.OffWheel), 11f, UiHelpers.Dim, TextAlignmentOptions.MidlineLeft);

            _grid = UiHelpers.Rect("Grid", _tray);
            UiHelpers.TopStrip(_grid, TrayLabelHeight, 0f);
            _gridBackground = UiHelpers.Image(_grid, TrayFill, false);

            _ghost = UiHelpers.Pin(UiHelpers.Rect("Ghost", _rect), center, Vector2.zero, new Vector2(GhostSize, GhostSize));
            _ghostIcon = UiHelpers.Image(_ghost, Color.white, false);
            _ghostIcon.preserveAspect = true;

            RectTransform ghostLetter = UiHelpers.Rect("Letter", _ghost);
            UiHelpers.Stretch(ghostLetter, 0f, 0f, 0f, 0f);
            _ghostLetter = UiHelpers.Label(ghostLetter, _font, string.Empty, 18f, Color.white, TextAlignmentOptions.Center);
            _ghost.gameObject.SetActive(false);
        }

        private RectTransform PageButton(string name, int direction)
        {
            RectTransform rect = UiHelpers.Pin(UiHelpers.Rect(name, _wheel), new Vector2(0.5f, 0.5f), new Vector2(direction * ArrowOffset, 0f),
                new Vector2(ArrowSize, ArrowSize));
            Image background = UiHelpers.Image(rect, Color.white, true);

            RectTransform icon = UiHelpers.Rect("Icon", rect);
            UiHelpers.Stretch(icon, 7f, 7f, 7f, 7f);
            icon.localEulerAngles = new Vector3(0f, 0f, direction * -90f);
            Image arrow = UiHelpers.Image(icon, Color.white, false);
            arrow.sprite = UiHelpers.Arrow;

            UiButton.Attach(rect.gameObject, background, arrow, delegate { Flip(direction); });
            return rect;
        }

        private void AddTile()
        {
            RectTransform tile = UiHelpers.Pin(UiHelpers.Rect("Tile", _grid), new Vector2(0f, 1f), Vector2.zero, new Vector2(TileSize, TileSize));
            _tiles.Add(UiHelpers.Image(tile, TileFill, false));

            RectTransform icon = UiHelpers.Rect("Icon", tile);
            UiHelpers.Stretch(icon, 6f, 6f, 6f, 6f);
            Image image = UiHelpers.Image(icon, Color.white, false);
            image.preserveAspect = true;
            _tileIcons.Add(image);

            RectTransform letter = UiHelpers.Rect("Letter", tile);
            UiHelpers.Stretch(letter, 0f, 0f, 0f, 0f);
            _tileLetters.Add(UiHelpers.Label(letter, _font, string.Empty, 16f, Color.white, TextAlignmentOptions.Center));
        }
    }
}
