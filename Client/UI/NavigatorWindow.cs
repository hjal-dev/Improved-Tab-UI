using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using EFT.UI;
using ImprovedTabUI.Config;
using ImprovedTabUI.Eft;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal sealed class NavigatorWindow : MonoBehaviour
    {
        internal const float Margin = 14f;
        internal const float KeyColumn = Margin + UiHelpers.ButtonSize * 2f + NavigatorRow.ButtonGap + 8f;
        internal const float OriginColumn = KeyColumn + NavigatorRow.KeyWidth + 8f;
        internal const float RowInset = 6f;
        internal const float BaselineDrop = 5f;

        internal const float Width = 380f;
        private const float HeaderHeight = 36f;
        private const float FooterHeight = 34f;
        private const float OptionHeight = 30f;
        private const float ListGap = 6f;
        private const int MaxVisibleRows = 12;
        private const string FadedColor = "<color=#D9C98F80>";
        private const float ModeLeft = 158f;
        private const float BarButtonWidth = 50f;
        private const float WheelButtonWidth = 58f;
        private const float SlotsValueWidth = 28f;

        private static readonly Color Border = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color Background = new Color(0f, 0f, 0f, 0.97f);
        private static readonly Color HeaderColor = Color.black;
        private static readonly Color Separator = new Color(1f, 1f, 1f, 0.1f);

        private readonly List<NavigatorRow> _rows = new List<NavigatorRow>();

        private TabBar _bar;
        private TMP_FontAsset _font;
        private RectTransform _root;
        private RectTransform _content;
        private TextMeshProUGUI _count;
        private int _shownVersion = -1;
        private int _shownVisible = -1;
        private int _shownTotal = -1;
        private float _shownHeight = -1f;
        private bool _languageDirty;

        private TextMeshProUGUI _resetLabel;
        private RectTransform _dropMarker;
        private NavigatorRow _dragRow;
        private int _dropIndex = -1;
        private NavigatorRow _capturing;

        private bool _wheelMode;
        private int _shownWheelVersion = -1;
        private int _shownSlots = -1;
        private UiButton _barButton;
        private UiButton _wheelButton;
        private TextMeshProUGUI _barLabel;
        private TextMeshProUGUI _wheelLabel;
        private TextMeshProUGUI _slotsLabel;
        private TextMeshProUGUI _slotsValue;
        private UiButton _moreSlots;
        private UiButton _lessSlots;
        private UiButton _resetKeysButton;
        private TextMeshProUGUI _resetKeysLabel;
        private OptionToggle _raidKeys;
        private GameObject _stepper;
        private GameObject _viewport;
        private GameObject _scrollbar;
        private WheelEditor _editor;

        private static KeyCode[] _captureKeys;

        internal bool IsOpen
        {
            get { return gameObject.activeSelf; }
        }

        internal TabRegistry Registry
        {
            get { return _bar.Registry; }
        }

        internal static int SlotsPerWheel
        {
            get { return Mathf.Clamp(Plugin.WheelSlots.Value, Plugin.MinWheelSlots, Plugin.MaxWheelSlots); }
        }

        internal Tab CurrentTab
        {
            get { return _bar.SelectedTab; }
        }

        internal static NavigatorWindow Create(Transform screen, TabBar bar, TMP_FontAsset font)
        {
            RectTransform root = UiHelpers.Pin(UiHelpers.Rect("TabNavigatorWindow", screen), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(Width, 400f));
            UiHelpers.Image(root, Border, true);

            RectTransform body = UiHelpers.Rect("Body", root);
            UiHelpers.Stretch(body, 1f, 1f, 1f, 1f);
            UiHelpers.Image(body, Background, true);

            NavigatorWindow window = root.gameObject.AddComponent<NavigatorWindow>();
            window._bar = bar;
            window._font = font;
            window._root = root;
            window.BuildHeader(body);
            window.BuildList(body);
            window.BuildFooter(body);
            window._editor = WheelEditor.Create(body, window, font, HeaderHeight, FooterHeight);
            QuickAccess.LanguageChanged += window.OnLanguageChanged;
            Plugin.RadialEnabled.SettingChanged += window.OnRadialSwitched;
            root.gameObject.SetActive(false);
            return window;
        }

        private void OnDestroy()
        {
            QuickAccess.LanguageChanged -= OnLanguageChanged;
            Plugin.RadialEnabled.SettingChanged -= OnRadialSwitched;
        }

        private void OnRadialSwitched(object sender, EventArgs args)
        {
            ApplyRadialSwitch();
        }

        private void ApplyRadialSwitch()
        {
            bool radial = Plugin.RadialEnabled.Value;
            _barButton.gameObject.SetActive(radial);
            _wheelButton.gameObject.SetActive(radial);

            if (!radial && _wheelMode)
            {
                SetMode(false);
            }
        }

        private void OnLanguageChanged()
        {
            _languageDirty = true;

            if (IsOpen)
            {
                ApplyLanguage();
                Refresh();
            }
        }

        private void ApplyLanguage()
        {
            if (!_languageDirty)
            {
                return;
            }

            _languageDirty = false;
            _shownVersion = -1;
            _shownVisible = -1;
            _resetLabel.text = UiText.Get(UiText.ResetToDefault);
            _resetKeysLabel.text = UiText.Get(UiText.ResetKeys);
            _raidKeys.ApplyLanguage();
            _barLabel.text = UiText.Get(UiText.Bar);
            _wheelLabel.text = UiText.Get(UiText.Wheel);
            _slotsLabel.text = UiText.Get(UiText.Slots);
            _editor.ApplyLanguage();
        }

        private void SetMode(bool wheel)
        {
            EndKeyCapture();
            _wheelMode = wheel;
            _barButton.SetSelected(!wheel);
            _wheelButton.SetSelected(wheel);
            _stepper.SetActive(wheel);
            _resetKeysButton.gameObject.SetActive(!wheel);
            _raidKeys.gameObject.SetActive(!wheel);
            _viewport.SetActive(!wheel);
            _scrollbar.SetActive(!wheel);
            _editor.gameObject.SetActive(wheel);
            _shownVersion = -1;
            _shownVisible = -1;
            Refresh();
        }

        private void ChangeSlots(int direction)
        {
            int slots = Mathf.Clamp(SlotsPerWheel + direction, Plugin.MinWheelSlots, Plugin.MaxWheelSlots);

            if (slots != Plugin.WheelSlots.Value)
            {
                Plugin.WheelSlots.Value = slots;
            }

            Refresh();
        }

        internal void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        internal void Open()
        {
            _bar.Registry.RefreshNames();
            _bar.Registry.ResolveOwners();
            _languageDirty = true;
            ApplyLanguage();
            ApplyRadialSwitch();
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Refresh();
        }

        internal void Close()
        {
            if (IsOpen)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            EndKeyCapture();

            if (_dragRow != null)
            {
                _dragRow.SetDragging(false);
                _dragRow = null;
            }

            if (_dropMarker != null)
            {
                _dropMarker.gameObject.SetActive(false);
            }
        }

        internal void BeginRowDrag(NavigatorRow row)
        {
            EndKeyCapture();
            _dragRow = row;
            _dropIndex = row.Index;
            row.SetDragging(true);
            _dropMarker.SetAsLastSibling();
            PlaceMarker(_dropIndex);
            _dropMarker.gameObject.SetActive(true);
        }

        internal void DragRow(NavigatorRow row, PointerEventData eventData)
        {
            if (_dragRow != row)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_content, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }

            int gap = Mathf.Clamp(Mathf.RoundToInt(-local.y / NavigatorRow.Height), 0, _bar.Registry.Tabs.Count);

            if (gap != _dropIndex)
            {
                _dropIndex = gap;
                PlaceMarker(gap);
            }
        }

        internal void EndRowDrag(NavigatorRow row)
        {
            if (_dragRow != row)
            {
                return;
            }

            _dragRow = null;
            row.SetDragging(false);
            _dropMarker.gameObject.SetActive(false);

            int from = row.Index;
            int target = _dropIndex > from ? _dropIndex - 1 : _dropIndex;

            if (row.Tab != null && target != from && target >= 0)
            {
                UiSounds.Play(EUISoundType.ButtonClick);
                _bar.Registry.MoveTo(row.Tab, target);
            }
        }

        private void PlaceMarker(int gap)
        {
            _dropMarker.anchoredPosition = new Vector2(0f, -gap * NavigatorRow.Height);
        }

        internal void BeginKeyCapture(NavigatorRow row)
        {
            EndKeyCapture();
            _capturing = row;
            row.SetCapturing(true);
            QuickAccess.CapturingKey = true;
        }

        private void EndKeyCapture()
        {
            if (_capturing != null)
            {
                _capturing.SetCapturing(false);
                _capturing = null;
            }

            QuickAccess.CapturingKey = false;
        }

        private void Update()
        {
            if (_capturing == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EndKeyCapture();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
            {
                SetKey(KeyboardShortcut.Empty);
                return;
            }

            foreach (KeyCode key in CaptureKeys())
            {
                if (Input.GetKeyDown(key))
                {
                    SetKey(new KeyboardShortcut(key, HeldModifiers()));
                    return;
                }
            }
        }

        private void SetKey(KeyboardShortcut shortcut)
        {
            InventoryTab tab = _capturing.Tab;
            EndKeyCapture();

            if (tab != null)
            {
                tab.Key.Value = shortcut;
                UiSounds.Play(EUISoundType.MenuCheckBox);
            }

            Refresh();
        }

        private static KeyCode[] HeldModifiers()
        {
            List<KeyCode> held = new List<KeyCode>();
            KeyCode[] modifiers = { KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftShift, KeyCode.RightShift };

            foreach (KeyCode modifier in modifiers)
            {
                if (Input.GetKey(modifier))
                {
                    held.Add(modifier);
                }
            }

            return held.ToArray();
        }

        private static KeyCode[] CaptureKeys()
        {
            if (_captureKeys != null)
            {
                return _captureKeys;
            }

            List<KeyCode> keys = new List<KeyCode>();

            foreach (KeyCode key in (KeyCode[])Enum.GetValues(typeof(KeyCode)))
            {
                bool skip = key == KeyCode.None || key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Escape
                    || key == KeyCode.Backspace || key == KeyCode.Delete
                    || key == KeyCode.LeftControl || key == KeyCode.RightControl || key == KeyCode.LeftAlt || key == KeyCode.RightAlt
                    || key == KeyCode.LeftShift || key == KeyCode.RightShift || key >= KeyCode.JoystickButton0;

                if (!skip && !keys.Contains(key))
                {
                    keys.Add(key);
                }
            }

            _captureKeys = keys.ToArray();
            return _captureKeys;
        }

        internal void OnTabsChanged()
        {
            if (IsOpen)
            {
                Refresh();
            }
        }

        internal void OnSelectionChanged()
        {
            if (!IsOpen)
            {
                return;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].gameObject.activeSelf)
                {
                    _rows[i].Sync();
                }
            }
        }

        internal bool ToggleRow(InventoryTab tab)
        {
            if (!_bar.TryChangeVisibility(tab, !tab.Visible.Value))
            {
                ShowKeepOne();
                return false;
            }

            UpdateCount();
            return true;
        }

        internal void ShowKeepOne()
        {
            _shownVisible = -1;
            _count.color = UiHelpers.OffText;
            _count.text = UiText.Get(UiText.KeepOneVisible);
        }

        internal void Move(InventoryTab tab, int direction)
        {
            _bar.Registry.Move(tab, direction);
        }

        private void ResetKeys()
        {
            EndKeyCapture();
            TabSettings.ClearKeys();
            Refresh();
        }

        private void ResetToDefault()
        {
            if (_wheelMode)
            {
                _bar.Registry.Wheel.Reset();
                Refresh();
                return;
            }

            _bar.Registry.ResetToDefault();
        }

        internal void Refresh()
        {
            if (_wheelMode)
            {
                RefreshWheel();
                return;
            }

            IReadOnlyList<InventoryTab> tabs = _bar.Registry.Tabs;

            if (_shownVersion != _bar.Registry.Version)
            {
                _shownVersion = _bar.Registry.Version;
                _shownVisible = -1;
                _bar.Registry.ResolveOwners();

                while (_rows.Count < tabs.Count)
                {
                    _rows.Add(NavigatorRow.Create(_content, this, _font));
                }

                for (int i = 0; i < _rows.Count; i++)
                {
                    bool used = i < tabs.Count;

                    if (used)
                    {
                        _rows[i].Bind(tabs[i], i, tabs.Count);
                    }

                    if (_rows[i].gameObject.activeSelf != used)
                    {
                        _rows[i].gameObject.SetActive(used);
                    }
                }

                FitHeight(ListGap * 2f + Mathf.Clamp(tabs.Count, 1, MaxVisibleRows) * NavigatorRow.Height + OptionHeight);
            }
            else
            {
                for (int i = 0; i < tabs.Count && i < _rows.Count; i++)
                {
                    _rows[i].Sync();
                    _rows[i].RefreshKey();
                }
            }

            UpdateCount();
        }

        private void RefreshWheel()
        {
            int slots = SlotsPerWheel;
            int wheelVersion = _bar.Registry.Wheel.Version;
            _slotsValue.text = slots.ToString();
            _moreSlots.gameObject.SetActive(slots < Plugin.MaxWheelSlots);
            _lessSlots.gameObject.SetActive(slots > Plugin.MinWheelSlots);

            if (_shownVersion != _bar.Registry.Version || _shownWheelVersion != wheelVersion || _shownSlots != slots)
            {
                _shownVersion = _bar.Registry.Version;
                _shownWheelVersion = wheelVersion;
                _shownSlots = slots;
                _shownVisible = -1;
                _editor.Refresh();
                FitHeight(_editor.Height);
            }

            UpdateCount();
        }

        private void FitHeight(float content)
        {
            if (Mathf.Approximately(content, _shownHeight))
            {
                return;
            }

            _shownHeight = content;
            _root.sizeDelta = new Vector2(Width, 2f + HeaderHeight + content + FooterHeight);
        }

        private void UpdateCount()
        {
            int visible = _wheelMode ? _bar.Registry.Wheel.Count() : _bar.Registry.CountVisible();
            int total = _bar.Registry.Tabs.Count;

            if (visible == _shownVisible && total == _shownTotal)
            {
                return;
            }

            _shownVisible = visible;
            _shownTotal = total;
            _count.color = UiHelpers.Accent;
            _count.text = visible + FadedColor + " / " + total + "</color>";
        }

        private void BuildHeader(RectTransform body)
        {
            RectTransform header = UiHelpers.Rect("Header", body);
            UiHelpers.TopStrip(header, 0f, HeaderHeight);
            UiHelpers.Image(header, HeaderColor, true);
            header.gameObject.AddComponent<WindowDragHandle>().Window = _root;

            RectTransform separator = UiHelpers.Rect("Separator", header);
            separator.anchorMin = new Vector2(0f, 0f);
            separator.anchorMax = new Vector2(1f, 0f);
            separator.offsetMin = Vector2.zero;
            separator.offsetMax = new Vector2(0f, 1f);
            UiHelpers.Image(separator, Separator, false);

            RectTransform title = UiHelpers.Rect("Title", header);
            UiHelpers.Stretch(title, Margin, -BaselineDrop, Width - ModeLeft, BaselineDrop);
            UiHelpers.Label(title, _font, "TAB NAVIGATOR", 15f, UiHelpers.Text, TextAlignmentOptions.BaselineLeft);

            _barButton = TextButton(header, "Bar", new Vector2(0f, 0.5f), new Vector2(ModeLeft, 0f), BarButtonWidth, UiText.Get(UiText.Bar),
                out _barLabel, delegate { SetMode(false); }).WithTooltip(() => UiText.Get(UiText.BarTip));
            _wheelButton = TextButton(header, "Wheel", new Vector2(0f, 0.5f), new Vector2(ModeLeft + BarButtonWidth + 2f, 0f), WheelButtonWidth,
                UiText.Get(UiText.Wheel), out _wheelLabel, delegate { SetMode(true); }).WithTooltip(() => UiText.Get(UiText.WheelTip));
            _barButton.SetSelected(true);

            RectTransform count = UiHelpers.Pin(UiHelpers.Rect("Count", header), new Vector2(1f, 0.5f), new Vector2(-KeyColumn, -BaselineDrop),
                new Vector2(120f, HeaderHeight));
            _count = UiHelpers.Label(count, _font, string.Empty, 12f, UiHelpers.Text, TextAlignmentOptions.BaselineRight);

            RectTransform close = UiHelpers.Pin(UiHelpers.Rect("Close", header), new Vector2(1f, 0.5f), new Vector2(-Margin, 0f),
                new Vector2(UiHelpers.ButtonSize, UiHelpers.ButtonSize));
            Image closeBackground = UiHelpers.Image(close, Color.white, true);

            RectTransform cross = UiHelpers.Rect("X", close);
            UiHelpers.Stretch(cross, 0f, 0f, 0f, 0f);
            TextMeshProUGUI crossLabel = UiHelpers.Label(cross, _font, "X", 12f, Color.white, TextAlignmentOptions.Center);

            UiButton.Attach(close.gameObject, closeBackground, crossLabel, Close).WithHoverFill(UiHelpers.Accent);
        }

        private void BuildList(RectTransform body)
        {
            float top = HeaderHeight + ListGap;
            float bottom = FooterHeight + OptionHeight;

            RectTransform viewport = UiHelpers.Rect("Viewport", body);
            _viewport = viewport.gameObject;
            UiHelpers.Stretch(viewport, 0f, bottom, RowInset, top);
            UiHelpers.Image(viewport, Color.clear, true);
            viewport.gameObject.AddComponent<RectMask2D>();

            _content = UiHelpers.Rect("Content", viewport);
            UiHelpers.TopStrip(_content, 0f, 0f);

            VerticalLayoutGroup layout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _dropMarker = UiHelpers.Rect("DropMarker", _content);
            _dropMarker.anchorMin = new Vector2(0f, 1f);
            _dropMarker.anchorMax = new Vector2(1f, 1f);
            _dropMarker.pivot = new Vector2(0.5f, 0.5f);
            _dropMarker.sizeDelta = new Vector2(-Margin * 2f, 2f);
            _dropMarker.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiHelpers.Image(_dropMarker, UiHelpers.Accent, false);
            _dropMarker.gameObject.SetActive(false);

            RectTransform track = UiHelpers.Rect("Scrollbar", body);
            _scrollbar = track.gameObject;
            track.anchorMin = new Vector2(1f, 0f);
            track.anchorMax = new Vector2(1f, 1f);
            track.pivot = new Vector2(1f, 0.5f);
            track.offsetMin = new Vector2(-6f, bottom);
            track.offsetMax = new Vector2(-4f, -top);
            UiHelpers.Image(track, new Color(1f, 1f, 1f, 0.04f), true);

            RectTransform area = UiHelpers.Rect("Area", track);
            UiHelpers.Stretch(area, 0f, 0f, 0f, 0f);

            RectTransform handle = UiHelpers.Rect("Handle", area);
            UiHelpers.Stretch(handle, 0f, 0f, 0f, 0f);
            Image handleImage = UiHelpers.Image(handle, UiHelpers.Dim, true);

            Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.transition = Selectable.Transition.None;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = _content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = NavigatorRow.Height;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private void BuildFooter(RectTransform body)
        {
            RectTransform reset = UiHelpers.Pin(UiHelpers.Rect("ResetToDefault", body), new Vector2(0f, 0f), new Vector2(Margin, 7f),
                new Vector2(128f, UiHelpers.ButtonSize + 2f));
            Image background = UiHelpers.Image(reset, Color.white, true);

            RectTransform text = UiHelpers.Rect("Text", reset);
            UiHelpers.Stretch(text, 0f, 0f, 0f, 0f);
            TextMeshProUGUI label = UiHelpers.Label(text, _font, UiText.Get(UiText.ResetToDefault), 11f, Color.white, TextAlignmentOptions.Center);
            _resetLabel = label;

            UiButton.Attach(reset.gameObject, background, label, ResetToDefault)
                .WithTooltip(() => UiText.Get(_wheelMode ? UiText.ResetWheelTip : UiText.ResetBarTip));

            _raidKeys = OptionToggle.Create(body, _font, Plugin.KeysInRaid, FooterHeight, OptionHeight,
                () => UiText.Get(UiText.KeysInRaid), () => UiText.Get(UiText.KeysInRaidTip));

            _resetKeysButton = TextButton(body, "ResetKeybinds", new Vector2(1f, 0f), new Vector2(-Margin, 7f), 128f, UiText.Get(UiText.ResetKeys),
                out _resetKeysLabel, ResetKeys).WithTooltip(() => UiText.Get(UiText.ResetKeysTip));

            RectTransform stepper = UiHelpers.Rect("Slots", body);
            UiHelpers.Stretch(stepper, 0f, 0f, 0f, 0f);
            _stepper = stepper.gameObject;

            float size = UiHelpers.ButtonSize + 2f;
            _moreSlots = TextButton(stepper, "More", new Vector2(1f, 0f), new Vector2(-Margin, 7f), size, "+", out TextMeshProUGUI _, delegate { ChangeSlots(1); });

            RectTransform value = UiHelpers.Pin(UiHelpers.Rect("Value", stepper), new Vector2(1f, 0f), new Vector2(-Margin - size, 7f),
                new Vector2(SlotsValueWidth, size));
            _slotsValue = UiHelpers.Label(value, _font, string.Empty, 13f, UiHelpers.Accent, TextAlignmentOptions.Center);

            _lessSlots = TextButton(stepper, "Less", new Vector2(1f, 0f), new Vector2(-Margin - size - SlotsValueWidth, 7f), size, "-", out TextMeshProUGUI _,
                delegate { ChangeSlots(-1); });

            RectTransform caption = UiHelpers.Pin(UiHelpers.Rect("Caption", stepper), new Vector2(1f, 0f),
                new Vector2(-Margin - size * 2f - SlotsValueWidth - 8f, 7f), new Vector2(80f, size));
            _slotsLabel = UiHelpers.Label(caption, _font, UiText.Get(UiText.Slots), 11f, UiHelpers.Dim, TextAlignmentOptions.Right);

            _stepper.SetActive(false);
        }

        private UiButton TextButton(RectTransform parent, string name, Vector2 anchor, Vector2 position, float width, string text,
            out TextMeshProUGUI label, Action clicked)
        {
            RectTransform rect = UiHelpers.Pin(UiHelpers.Rect(name, parent), anchor, position, new Vector2(width, UiHelpers.ButtonSize + 2f));
            Image background = UiHelpers.Image(rect, Color.white, true);

            RectTransform textRect = UiHelpers.Rect("Text", rect);
            UiHelpers.Stretch(textRect, 2f, 0f, 2f, 0f);
            label = UiHelpers.Label(textRect, _font, text, 11f, Color.white, TextAlignmentOptions.Center);

            return UiButton.Attach(rect.gameObject, background, label, clicked);
        }
    }
}
