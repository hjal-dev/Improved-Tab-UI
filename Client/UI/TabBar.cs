using System;
using System.Collections.Generic;
using EFT.UI;
using ImprovedTabUI.Config;
using ImprovedTabUI.Eft;
using ImprovedTabUI.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal sealed class TabBar : MonoBehaviour
    {
        private const float FallbackGap = -26f;
        private const float FallbackRightReserve = 260f;
        private const float BackButtonMargin = 12f;
        private const float VerticalBleed = 24f;
        private const float DecelerationRate = 0.135f;
        private const float KeyGap = 8f;
        private const float MaxExtraWidth = 120f;

        private static readonly List<TabBar> Bars = new List<TabBar>();
        private static readonly Vector3[] Corners = new Vector3[4];

        private readonly TabRegistry _registry = new TabRegistry();

        private InventoryScreen _screen;
        private RectTransform _container;
        private RectTransform _bar;
        private TabSizeController _sizeController;
        private RectMask2D _mask;
        private TabGroup _group;
        private NavigatorWindow _window;
        private RectTransform _badgeLayer;
        private readonly List<TabLabel> _labels = new List<TabLabel>();
        private readonly Dictionary<TabElement, float> _baseAdjustments = new Dictionary<TabElement, float>();
        private float _extraWidth;
        private TabKeyButton _previousKey;
        private TabKeyButton _nextKey;

        private float _restX;
        private float _step;
        private float _contentWidth;
        private float _viewportWidth;
        private float _offset;
        private float _inset;
        private float _restWidth;
        private float _keysLeft;
        private float _keysRight;
        private readonly List<Transform> _drawOrder = new List<Transform>();

        private bool _dirty;
        private bool _checkSelection;
        private bool _showingMap;
        private bool _groupShown;
        private int _settleFrame = -1;
        private float _clipScale = 1f;
        private bool _dragging;
        private bool _gliding;
        private float _velocity;
        private float _lastDragOffset;

        internal TabRegistry Registry
        {
            get { return _registry; }
        }

        internal static void Attach(InventoryScreen screen)
        {
            RectTransform container = FindContainer(screen);

            if (container == null)
            {
                TabLog.WarnOnce("no-container", "The inventory tab bar was not found, so it is left as it is.");
                return;
            }

            TabBar bar = container.GetComponent<TabBar>() ?? container.gameObject.AddComponent<TabBar>();
            bar.Bind(screen, container);
        }

        internal static void BeforeGroupShow(TabGroup group, ref Tab tab)
        {
            TabBar bar = For(group);

            if (bar == null)
            {
                return;
            }

            bar._groupShown = true;
            bar.Rebuild();

            if (bar._showingMap)
            {
                return;
            }

            Tab pending = InventoryOpener.TakePendingTab();

            if (pending != null)
            {
                tab = pending;
                return;
            }

            Tab target = tab != null ? tab : group._selectedTab;

            if (target != null && !target.gameObject.activeSelf)
            {
                Tab fallback = bar.FallbackTab();

                if (fallback != null)
                {
                    tab = fallback;
                }
            }
        }

        internal static TabBar Prepare(InventoryScreen screen)
        {
            RectTransform container = screen != null ? FindContainer(screen) : null;

            if (container == null)
            {
                return null;
            }

            TabBar bar = container.GetComponent<TabBar>();

            if (bar == null)
            {
                Attach(screen);
                return container.GetComponent<TabBar>();
            }

            if (bar._registry.Refresh(screen, container))
            {
                bar.ApplyLayout();
            }

            return bar;
        }

        internal static void SelectOnScreen(InventoryScreen screen, Tab tab)
        {
            foreach (TabBar bar in Bars)
            {
                if (bar._screen == screen)
                {
                    bar.Select(tab);
                }
            }
        }

        internal void Select(Tab tab)
        {
            if (tab == null || _group == null || !_groupShown || _group._isChangingSelection || tab == _group._selectedTab)
            {
                return;
            }

            _group.SelectionChangedHandler(tab, true);
        }

        internal TMP_FontAsset Font
        {
            get { return InventoryTabs.FontOf(_registry.Tabs.Count > 0 ? _registry.Tabs[0].Tab : null); }
        }

        internal static void AfterGroupSelect(TabGroup group, Tab tab)
        {
            TabBar bar = For(group);

            if (bar != null && tab != null)
            {
                bar.ApplyDrawOrder();
                bar.ScrollIntoView(tab);

                if (bar._window != null)
                {
                    bar._window.OnSelectionChanged();
                }
            }
        }

        internal Tab SelectedTab
        {
            get { return _group != null && _groupShown ? _group._selectedTab : null; }
        }

        internal static void SetShowingMap(InventoryScreen screen, bool showingMap)
        {
            foreach (TabBar bar in Bars)
            {
                if (bar._screen == screen)
                {
                    bar._showingMap = showingMap;
                }
            }
        }

        internal void MarkDirty()
        {
            _dirty = true;
        }

        internal void Scroll(Vector2 wheel)
        {
            float notches = wheel.y != 0f ? wheel.y : -wheel.x;

            if (notches != 0f && _contentWidth > _viewportWidth)
            {
                _gliding = false;
                _velocity = 0f;
                SetOffset(_offset - notches * Plugin.ScrollSpeed.Value);
            }
        }

        internal void BeginDrag()
        {
            _dragging = true;
            _gliding = false;
            _velocity = 0f;
            _lastDragOffset = _offset;
        }

        internal void Drag(float distance)
        {
            if (distance != 0f && _contentWidth > _viewportWidth)
            {
                SetOffset(_offset - distance);
            }
        }

        internal void EndDrag()
        {
            _dragging = false;
            _gliding = Mathf.Abs(_velocity) > 1f;
        }

        private void UpdateMotion()
        {
            float deltaTime = Time.unscaledDeltaTime;

            if (deltaTime <= 0f)
            {
                return;
            }

            if (_dragging)
            {
                float frameVelocity = (_offset - _lastDragOffset) / deltaTime;
                _velocity = Mathf.Lerp(_velocity, frameVelocity, deltaTime * 10f);
                _lastDragOffset = _offset;
                return;
            }

            _velocity *= Mathf.Pow(DecelerationRate, deltaTime);
            float before = _offset;
            SetOffset(_offset + _velocity * deltaTime);

            if (Mathf.Abs(_velocity) < 1f || _offset == before)
            {
                _gliding = false;
                _velocity = 0f;
            }
        }

        internal bool TryChangeVisibility(InventoryTab tab, bool visible)
        {
            if (!visible && tab.Visible.Value && tab.IsShown && _registry.CountShowable() <= 1)
            {
                return false;
            }

            tab.Visible.Value = visible;
            return true;
        }

        private static RectTransform FindContainer(InventoryScreen screen)
        {
            if (screen._tabDictionary == null)
            {
                return null;
            }

            foreach (KeyValuePair<EInventoryTab, Tab> pair in screen._tabDictionary)
            {
                if (pair.Value != null)
                {
                    return pair.Value.transform.parent as RectTransform;
                }
            }

            return null;
        }

        private static TabBar For(TabGroup group)
        {
            if (group == null)
            {
                return null;
            }

            foreach (TabBar bar in Bars)
            {
                if (bar._group == group)
                {
                    return bar;
                }
            }

            return null;
        }

        private void Bind(InventoryScreen screen, RectTransform container)
        {
            if (_screen == null)
            {
                _screen = screen;
                _container = container;
                _bar = container.parent as RectTransform;
                _restX = container.anchoredPosition.x;
                _restWidth = container.rect.width;
                _badgeLayer = UiHelpers.Rect("ImprovedTabUI Keys", container);
                UiHelpers.Stretch(_badgeLayer, 0f, 0f, 0f, 0f);
                _badgeLayer.pivot = new Vector2(0.5f, 0.5f);
                _sizeController = container.GetComponent<TabSizeController>();

                _mask = container.GetComponent<RectMask2D>() ?? container.gameObject.AddComponent<RectMask2D>();

                if (_bar != null)
                {
                    TabBarScrollCatcher catcher = _bar.GetComponent<TabBarScrollCatcher>() ?? _bar.gameObject.AddComponent<TabBarScrollCatcher>();
                    catcher.Bar = this;
                    CreateKeys(screen, container);
                }

                Bars.Add(this);
                TabSettings.VisibilityChanged += OnVisibilityChanged;
                Plugin.TabOrder.SettingChanged += OnSettingChanged;
                TabSettings.KeysChanged += MarkDirty;
                Plugin.ShowTabKeys.SettingChanged += OnSettingChanged;
                Plugin.FitNames.SettingChanged += OnSettingChanged;
            }

            _group = InventoryTabs.GroupOf(screen);
            _groupShown = false;
            _checkSelection = false;
            _offset = 0f;
            _gliding = false;
            _velocity = 0f;
            Rebuild();
        }

        private void Rebuild()
        {
            _dirty = false;

            if (_screen == null)
            {
                return;
            }

            bool tabsChanged = _registry.Refresh(_screen, _container);
            _registry.RefreshNames();
            ApplyLayout();

            if (_checkSelection)
            {
                _checkSelection = false;
                SwitchAwayFromHiddenTab();
            }

            if (_window != null && (tabsChanged || _window.IsOpen))
            {
                _window.OnTabsChanged();
            }
        }

        private void ApplyLayout()
        {
            IReadOnlyList<InventoryTab> tabs = _registry.Tabs;
            bool anyChosen = _registry.CountShowable() > 0;
            bool switched = false;

            foreach (InventoryTab tab in tabs)
            {
                bool wanted = tab.Visible.Value || !anyChosen;
                GameObject tabObject = tab.Tab.gameObject;

                if (!wanted)
                {
                    if (tabObject.activeSelf)
                    {
                        tabObject.SetActive(false);
                        tab.HiddenByUs = true;
                        switched = true;
                    }
                }
                else if (tab.HiddenByUs)
                {
                    tab.HiddenByUs = false;
                    tabObject.SetActive(true);
                    switched = true;
                }
            }

            if (switched)
            {
                _settleFrame = Time.frameCount + 1;
            }

            float width = 0f;

            foreach (InventoryTab tab in tabs)
            {
                if (tab.IsShown)
                {
                    width = Mathf.Max(width, ((RectTransform)tab.Tab.transform).rect.width);
                }
            }

            float gap = _sizeController != null ? _sizeController._gap : FallbackGap;
            _step = width + gap > 0f ? width + gap : width;

            int shown = 0;

            foreach (InventoryTab tab in tabs)
            {
                if (tab.IsShown)
                {
                    shown++;
                }
            }

            _contentWidth = shown == 0 ? 0f : (shown - 1) * _step + width;
            UpdateViewport();
            PlaceKeys();
            ApplyClip();
            SetOffset(_offset, true);
            RefreshLabels();
            ApplyDrawOrder();
        }

        private bool TabsMoved()
        {
            int slot = 0;

            foreach (InventoryTab tab in _registry.Tabs)
            {
                if (!tab.IsShown)
                {
                    continue;
                }

                if (tab.Tab.transform.localPosition.x != _inset + slot * _step - _offset)
                {
                    return true;
                }

                slot++;
            }

            return false;
        }

        private void ApplyDrawOrder()
        {
            _drawOrder.Clear();
            IReadOnlyList<InventoryTab> tabs = _registry.Tabs;
            Tab selected = SelectedTab;

            for (int i = tabs.Count - 1; i >= 0; i--)
            {
                Tab tab = tabs[i].Tab;

                if (tab != null && tab != selected && tab.transform.parent == _container)
                {
                    _drawOrder.Add(tab.transform);
                }
            }

            if (selected != null && selected.transform.parent == _container)
            {
                _drawOrder.Add(selected.transform);
            }

            int next = 0;

            for (int i = 0; i < _container.childCount && next < _drawOrder.Count; i++)
            {
                Transform child = _container.GetChild(i);

                if (!_drawOrder.Contains(child))
                {
                    continue;
                }

                if (child != _drawOrder[next])
                {
                    foreach (Transform tab in _drawOrder)
                    {
                        tab.SetAsLastSibling();
                    }

                    break;
                }

                next++;
            }

            if (_badgeLayer != null && _badgeLayer.GetSiblingIndex() != _container.childCount - 1)
            {
                _badgeLayer.SetAsLastSibling();
            }
        }

        private void RefreshLabels()
        {
            bool show = Plugin.ShowTabKeys.Value;
            _labels.Clear();

            foreach (InventoryTab tab in _registry.Tabs)
            {
                if (tab.Tab == null)
                {
                    continue;
                }

                float baseAdjustment = BaseAdjustment(tab.Element);
                string key = show && tab.Key != null ? UiInput.KeyText(tab.Key.Value) : string.Empty;

                if (key.Length == 0 && tab.Tab.GetComponent<TabLabel>() == null)
                {
                    continue;
                }

                TabLabel label = TabLabel.For(tab, _badgeLayer);
                label.Apply(key, baseAdjustment);
                _labels.Add(label);
            }
        }

        private float BaseAdjustment(TabElement element)
        {
            if (element == null)
            {
                return 0f;
            }

            if (!_baseAdjustments.TryGetValue(element, out float value))
            {
                value = element._widthAdjustment;
                _baseAdjustments[element] = value;
            }

            if (element._widthAdjustment != value + _extraWidth)
            {
                element._widthAdjustment = value + _extraWidth;
            }

            return value;
        }

        private void FitNames()
        {
            float extra = 0f;

            if (Plugin.FitNames.Value && _labels.Count > 0)
            {
                float widest = 0f;

                foreach (InventoryTab tab in _registry.Tabs)
                {
                    if (tab.IsShown && tab.Element != null)
                    {
                        widest = Mathf.Max(widest, tab.Element.Width);
                    }
                }

                float required = 0f;

                foreach (TabLabel label in _labels)
                {
                    if (label != null)
                    {
                        required = Mathf.Max(required, label.RequiredWidth);
                    }
                }

                if (widest > 0f && required > 0f)
                {
                    extra = Mathf.Clamp(Mathf.Ceil(required - (widest - _extraWidth)), 0f, MaxExtraWidth);
                }
            }

            if (Mathf.Abs(extra - _extraWidth) < 1f)
            {
                return;
            }

            _extraWidth = extra;

            foreach (KeyValuePair<TabElement, float> pair in _baseAdjustments)
            {
                if (pair.Key != null)
                {
                    pair.Key._widthAdjustment = pair.Value + _extraWidth;
                }
            }
        }

        private void OnSettingChanged(object sender, EventArgs args)
        {
            _dirty = true;
        }

        private void UpdateViewport()
        {
            _clipScale = ClipScale();

            if (_bar == null)
            {
                _inset = 0f;
                _viewportWidth = Mathf.Max(_step, _contentWidth);
                return;
            }

            Rect barRect = _bar.rect;
            float right = barRect.xMax - FallbackRightReserve;
            DefaultUIButton backButton = _screen._backButton;

            if (backButton != null && backButton.gameObject.activeInHierarchy)
            {
                ((RectTransform)backButton.transform).GetWorldCorners(Corners);
                float backLeft = _bar.InverseTransformPoint(Corners[0]).x;

                if (backLeft > barRect.xMin)
                {
                    right = backLeft - BackButtonMargin;
                }
            }

            float left = barRect.xMin + barRect.width * _container.anchorMin.x + _restX;
            _keysLeft = left;
            _keysRight = right;
            _inset = _previousKey.Shown ? _previousKey.Width + KeyGap : 0f;
            float end = _nextKey.Shown ? right - _nextKey.Width - KeyGap : right;
            float width = end - left - _inset;
            _viewportWidth = IsUsable(width) ? Mathf.Max(_step, width) : Mathf.Max(_step, _contentWidth);
        }

        private float ClipScale()
        {
            Canvas canvas = _container.GetComponentInParent<Canvas>();
            Transform root = canvas != null ? canvas.rootCanvas.transform : null;

            if (root == null || root.lossyScale.x == 0f)
            {
                return 1f;
            }

            float scale = _container.lossyScale.x / root.lossyScale.x;
            return IsUsable(scale) && scale > 0f ? scale : 1f;
        }

        private static bool IsUsable(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void SetOffset(float offset, bool force = false)
        {
            float max = Mathf.Max(0f, _contentWidth - _viewportWidth);
            offset = IsUsable(offset) && IsUsable(max) ? Mathf.Clamp(offset, 0f, max) : 0f;

            if (!force && offset == _offset)
            {
                return;
            }

            _offset = offset;
            PlaceTabs();
        }

        private void PlaceTabs()
        {
            int slot = 0;

            foreach (InventoryTab tab in _registry.Tabs)
            {
                if (!tab.IsShown)
                {
                    continue;
                }

                Transform tabTransform = tab.Tab.transform;
                Vector3 position = tabTransform.localPosition;
                float x = _inset + slot * _step - _offset;

                if (position.x != x)
                {
                    tabTransform.localPosition = new Vector3(x, position.y, position.z);
                }

                if (tab.Element != null)
                {
                    tab.Element.OrderNumber = slot;
                }

                slot++;
            }
        }

        private void ApplyClip()
        {
            if (_container.anchoredPosition.x != _restX)
            {
                _container.anchoredPosition = new Vector2(_restX, _container.anchoredPosition.y);
            }

            float reach = Mathf.Max(_restWidth, (_inset + 1f) / Mathf.Max(0.1f, 1f - _container.pivot.x));

            if (!Mathf.Approximately(_container.rect.width, reach))
            {
                _container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, reach);
            }

            Rect rect = _container.rect;
            float scale = _clipScale;
            Vector4 padding = new Vector4(
                (_inset - rect.xMin) * scale,
                -VerticalBleed * scale,
                (rect.xMax - _inset - _viewportWidth) * scale,
                -VerticalBleed * scale);

            if (_mask.padding != padding)
            {
                _mask.padding = padding;
            }
        }

        private void ScrollIntoView(Tab tab)
        {
            if (!tab.gameObject.activeSelf || _contentWidth <= _viewportWidth)
            {
                return;
            }

            RectTransform rect = (RectTransform)tab.transform;
            float left = rect.localPosition.x - _inset + _offset;
            float right = left + rect.rect.width;

            if (left < _offset)
            {
                SetOffset(left);
            }
            else if (right > _offset + _viewportWidth)
            {
                SetOffset(right - _viewportWidth);
            }
        }

        private Tab FallbackTab()
        {
            Tab first = null;

            foreach (InventoryTab tab in _registry.Tabs)
            {
                if (!tab.IsShown || !tab.Tab.Interactable)
                {
                    continue;
                }

                if (tab.Id == nameof(EInventoryTab.Gear))
                {
                    return tab.Tab;
                }

                if (first == null)
                {
                    first = tab.Tab;
                }
            }

            return first;
        }

        private void SwitchAwayFromHiddenTab()
        {
            if (_group == null || !_groupShown || _group._isChangingSelection || !isActiveAndEnabled)
            {
                return;
            }

            Tab selected = _group._selectedTab;

            if (selected == null || selected.gameObject.activeSelf)
            {
                return;
            }

            Tab fallback = FallbackTab();

            if (fallback != null)
            {
                _group.SelectionChangedHandler(fallback, true);
            }
        }

        private void OnVisibilityChanged()
        {
            _dirty = true;
            _checkSelection = true;
        }

        private void Update()
        {
            if (QuickAccess.CapturingKey)
            {
                return;
            }

            if (Plugin.WindowKey.Value.IsDown() && !UiInput.IsTyping())
            {
                ToggleWindow();
            }
            else if (Plugin.PreviousTabKey.Value.IsDown() && !UiInput.IsTyping())
            {
                Cycle(-1);
            }
            else if (Plugin.NextTabKey.Value.IsDown() && !UiInput.IsTyping())
            {
                Cycle(1);
            }
        }

        private void Cycle(int direction)
        {
            IReadOnlyList<InventoryTab> tabs = _registry.Tabs;

            if (_group == null || !_groupShown || _group._isChangingSelection || tabs.Count == 0)
            {
                return;
            }

            int start = -1;

            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].Tab == _group._selectedTab)
                {
                    start = i;
                    break;
                }
            }

            if (start < 0)
            {
                start = direction > 0 ? -1 : tabs.Count;
            }

            for (int step = 1; step <= tabs.Count; step++)
            {
                int index = ((start + direction * step) % tabs.Count + tabs.Count) % tabs.Count;
                InventoryTab candidate = tabs[index];

                if (candidate.IsShown && candidate.Tab.Interactable)
                {
                    Select(candidate.Tab);

                    return;
                }
            }
        }

        private void LateUpdate()
        {
            if (_settleFrame >= 0 && Time.frameCount >= _settleFrame)
            {
                _settleFrame = -1;
                _dirty = true;
            }

            if (_dirty)
            {
                Rebuild();
            }
            else if (TabsMoved())
            {
                ApplyLayout();
            }

            if (_dragging || _gliding)
            {
                UpdateMotion();
            }

            FitNames();
        }

        private void ToggleWindow()
        {
            if (_window == null)
            {
                Tab anyTab = _registry.Tabs.Count > 0 ? _registry.Tabs[0].Tab : null;
                UiButton.UseGameStyle(_screen._backButton);
                _window = NavigatorWindow.Create(_screen.transform, this, InventoryTabs.FontOf(anyTab));
            }

            _window.Toggle();
        }

        private void OnTransformChildrenChanged()
        {
            _dirty = true;
        }

        private void OnDisable()
        {
            _dragging = false;
            _gliding = false;
            _velocity = 0f;

            foreach (InventoryTab tab in _registry.Tabs)
            {
                if (tab.Tab != null)
                {
                    tab.Tab.Hover(false);
                }
            }

            if (_window != null)
            {
                _window.Close();
            }
        }

        private void OnDestroy()
        {
            Bars.Remove(this);
            TabSettings.VisibilityChanged -= OnVisibilityChanged;
            Plugin.TabOrder.SettingChanged -= OnSettingChanged;
            TabSettings.KeysChanged -= MarkDirty;
            Plugin.ShowTabKeys.SettingChanged -= OnSettingChanged;
            Plugin.FitNames.SettingChanged -= OnSettingChanged;

            if (_previousKey != null)
            {
                Plugin.PreviousTabKey.SettingChanged -= OnKeyChanged;
                Plugin.NextTabKey.SettingChanged -= OnKeyChanged;
                Plugin.ShowKeyButtons.SettingChanged -= OnKeyChanged;
                _previousKey.Destroy();
                _nextKey.Destroy();
            }
        }

        private void CreateKeys(InventoryScreen screen, RectTransform container)
        {
            UiButton.UseGameStyle(screen._backButton);
            TMP_FontAsset font = InventoryTabs.FontOf(container.GetComponentInChildren<Tab>(true));
            _previousKey = TabKeyButton.Create(_bar, "Previous Tab Key", font, Plugin.PreviousTabKey, () => Cycle(-1));
            _nextKey = TabKeyButton.Create(_bar, "Next Tab Key", font, Plugin.NextTabKey, () => Cycle(1));
            Plugin.PreviousTabKey.SettingChanged += OnKeyChanged;
            Plugin.NextTabKey.SettingChanged += OnKeyChanged;
            Plugin.ShowKeyButtons.SettingChanged += OnKeyChanged;
        }

        private void OnKeyChanged(object sender, EventArgs args)
        {
            _previousKey.ApplyKey();
            _nextKey.ApplyKey();
            _dirty = true;
        }

        private void PlaceKeys()
        {
            if (_bar == null)
            {
                return;
            }

            RectTransform first = null;

            foreach (InventoryTab tab in _registry.Tabs)
            {
                if (tab.IsShown)
                {
                    first = (RectTransform)tab.Tab.transform;
                    break;
                }
            }

            if (first == null)
            {
                return;
            }

            first.GetWorldCorners(Corners);
            float bottom = _bar.InverseTransformPoint(Corners[0]).y;
            float height = _bar.InverseTransformPoint(Corners[1]).y - bottom;
            Vector2 center = _bar.rect.center;
            _previousKey.Place(new Vector2(_keysLeft - center.x, bottom - center.y), height);
            _nextKey.Place(new Vector2(_keysRight - _nextKey.Width - center.x, bottom - center.y), height);
        }

    }
}
