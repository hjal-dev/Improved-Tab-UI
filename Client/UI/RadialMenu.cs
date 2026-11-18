using System.Collections.Generic;
using System.Text;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using ImprovedTabUI.Eft;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal sealed class RadialMenu : MonoBehaviour
    {
        private const float OuterRadius = 230f;
        private const float InnerRadius = 124f;
        private const float DotSize = 10f;
        private const float DotSpacing = 18f;
        private const float IconSize = 44f;
        private const float DeadZone = 18f;
        private const float Reach = 120f;
        private const float Hysteresis = 6f;
        private const float PointerFollow = 30f;
        private const float GapDegrees = 2.2f;
        private const float MouseScale = 10f;
        private const int MaxSegments = 12;

        private const float OpenTime = 0.22f;
        private const float SegmentTime = 0.09f;
        private const float StaggerTime = 0.11f;
        private const float ScrambleTime = 0.16f;
        private const string Glyphs = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789#/<>";

        private static readonly Color SegmentIdle = new Color(0f, 0f, 0f, 0.78f);
        private static readonly Color CenterColor = new Color(0f, 0f, 0f, 0.82f);
        private static readonly Color SegmentEmpty = new Color(0f, 0f, 0f, 0.42f);
        private static readonly Color Faded = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color Unavailable = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color HairlineColor = new Color(0.85f, 0.79f, 0.56f, 0.35f);

        private readonly List<InventoryTab> _tabs = new List<InventoryTab>();
        private readonly Image[] _segments = new Image[MaxSegments];
        private readonly Image[] _icons = new Image[MaxSegments];
        private readonly TextMeshProUGUI[] _letters = new TextMeshProUGUI[MaxSegments];
        private readonly StringBuilder _scrambled = new StringBuilder();

        private GameObject _content;
        private RectTransform _ring;
        private CanvasGroup _group;
        private Image _highlight;
        private RectTransform _pointer;
        private Image _pointerImage;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI _mod;
        private RectTransform _dots;
        private readonly List<Image> _dotImages = new List<Image>();

        private int _perPage;
        private int _pageIndex;
        private int _pageCount;
        private int _onPage;
        private int _selected = -1;
        private int _aimed = -1;
        private float _fill;
        private Vector2 _cursor;
        private float _highlightAngle;
        private float _pointerAngle;
        private float _punch;
        private float _openedAt;
        private float _segmentsAt;
        private float _scrambleAt = -1f;
        private string _scrambleTarget = string.Empty;

        private CursorLockMode _savedLock;
        private bool _savedVisible;
        private bool _hasPoint;
        private UiInput.CursorPoint _savedPoint;
        private int _restoreFrames;
        private ECursorType _savedCursor;

        internal static readonly MethodInfo CursorHandler =
            AccessTools.Method("EFT.ClientApplicationInitOperation:CursorVisibilityChangedHandler", new[] { typeof(bool) });

        private static bool _holdingCursor;
        private static bool? _heldVisibility;

        internal bool IsOpen
        {
            get { return _content.activeSelf; }
        }

        internal static bool HoldCursor(bool visible)
        {
            if (!_holdingCursor)
            {
                return false;
            }

            _heldVisibility = visible;
            return true;
        }

        internal static RadialMenu Create(TMP_FontAsset font)
        {
            GameObject overlay = new GameObject("ImprovedTabUIRadial", typeof(RectTransform));
            overlay.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;
            DontDestroyOnLoad(overlay);

            Canvas canvas = overlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            CanvasScaler scaler = overlay.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            overlay.AddComponent<GraphicRaycaster>();

            RectTransform content = UiHelpers.Rect("Content", overlay.transform);
            UiHelpers.Stretch(content, 0f, 0f, 0f, 0f);
            UiHelpers.Image(content, Color.clear, true);

            RectTransform root = UiHelpers.Pin(UiHelpers.Rect("Wheel", content), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(OuterRadius * 2f, OuterRadius * 2f));

            RadialMenu menu = overlay.AddComponent<RadialMenu>();
            menu._content = content.gameObject;
            menu._ring = root;
            menu._group = root.gameObject.AddComponent<CanvasGroup>();
            menu._group.blocksRaycasts = false;
            menu.Build(font);
            content.gameObject.SetActive(false);
            return menu;
        }

        internal void Show(List<InventoryTab> tabs, Tab current)
        {
            _tabs.Clear();
            _tabs.AddRange(tabs);
            _perPage = Mathf.Clamp(NavigatorWindow.SlotsPerWheel, 1, MaxSegments);
            _pageCount = Mathf.Max(1, (_tabs.Count + _perPage - 1) / _perPage);
            _pageIndex = 0;

            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i] != null && _tabs[i].Tab == current)
                {
                    _pageIndex = i / _perPage;
                }
            }

            _cursor = Vector2.zero;
            _selected = -1;
            _aimed = -1;
            _punch = 0f;
            _scrambleAt = -1f;
            _openedAt = Time.unscaledTime;
            _segmentsAt = _openedAt;
            _restoreFrames = 0;
            SaveCursor();
            LayoutPage();
            _content.SetActive(true);
            Animate();
        }

        internal InventoryTab Close()
        {
            InventoryTab chosen = _selected >= 0 ? TabAt(_selected) : null;
            _content.SetActive(false);
            RestoreCursor();
            return chosen;
        }

        internal void Tick()
        {
            float deltaTime = Time.unscaledDeltaTime;
            float sensitivity = Plugin.RadialSensitivity.Value * MouseScale;
            _cursor += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * sensitivity;

            if (_cursor.sqrMagnitude > Reach * Reach)
            {
                _cursor = _cursor.normalized * Reach;
            }

            float wheel = Input.mouseScrollDelta.y;
            int target = wheel != 0f ? Mathf.Clamp(_pageIndex + (wheel < 0f ? 1 : -1), 0, _pageCount - 1) : _pageIndex;

            if (target != _pageIndex)
            {
                _pageIndex = target;
                _selected = -1;
                _aimed = -1;
                _segmentsAt = Time.unscaledTime;
                UiSounds.Play(EUISoundType.MenuDropdownSelect);
                LayoutPage();
            }

            float magnitude = _cursor.magnitude;
            float angle = Mathf.Atan2(_cursor.x, _cursor.y) * Mathf.Rad2Deg;

            if (magnitude > DeadZone)
            {
                float slice = 360f / _onPage;
                int index = Mathf.FloorToInt(Mathf.Repeat(angle + slice / 2f, 360f) / slice) % _onPage;

                if (_aimed >= 0 && index != _aimed && Mathf.Abs(Mathf.DeltaAngle(angle, _aimed * slice)) < slice * 0.5f + Hysteresis)
                {
                    index = _aimed;
                }

                if (index != _aimed)
                {
                    _aimed = index;
                    InventoryTab aimed = TabAt(index);
                    int pick = aimed != null && aimed.CanOpen ? index : -1;

                    if (pick != _selected)
                    {
                        bool first = _selected < 0;
                        _selected = pick;

                        if (pick >= 0)
                        {
                            _punch = 1f;
                            UiSounds.Play(EUISoundType.ButtonOver);
                        }

                        ApplySelection(first);
                    }
                }
            }

            bool pointing = magnitude > DeadZone * 0.5f;

            if (pointing && !_pointerImage.enabled)
            {
                _pointerAngle = angle;
            }

            _pointerImage.enabled = pointing;
            _pointerAngle = Mathf.LerpAngle(_pointerAngle, angle, 1f - Mathf.Exp(-PointerFollow * deltaTime));
            _pointer.localEulerAngles = new Vector3(0f, 0f, -_pointerAngle);

            if (_selected >= 0)
            {
                float follow = 1f - Mathf.Exp(-Plugin.RadialSmoothing.Value * deltaTime);
                float targetAngle = _selected * 360f / _onPage;
                _highlightAngle = Mathf.LerpAngle(_highlightAngle, targetAngle, Mathf.Min(1f, follow * 1.4f));
                RotateSegment(_highlight.rectTransform, _highlightAngle);
            }

            _punch = Mathf.Max(0f, _punch - deltaTime * 7f);
            _highlight.color = Color.Lerp(UiHelpers.Accent, Color.white, _punch * 0.65f);
            _highlight.rectTransform.localScale = Vector3.one * (1f + _punch * 0.025f);

            Animate();
            HideCursor();
        }

        private void Update()
        {
            if (_restoreFrames > 0 && !IsOpen)
            {
                _restoreFrames--;
                UiInput.PlaceCursor(_savedPoint);
            }
        }

        private void LateUpdate()
        {
            if (IsOpen)
            {
                HideCursor();
            }
        }

        private void SaveCursor()
        {
            _savedLock = Cursor.lockState;
            _savedVisible = Cursor.visible;
            _savedCursor = CursorSwitcher.PreviousType;
            _hasPoint = _savedLock != CursorLockMode.Locked && UiInput.TryGetCursor(out _savedPoint);
            _holdingCursor = true;
            _heldVisibility = null;
            HideCursor();
        }

        private static void HideCursor()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (Cursor.visible)
            {
                Cursor.visible = false;
            }

            if (CursorSwitcher._currentCursor != ECursorType.Invisible)
            {
                CursorSwitcher.SetCursor(ECursorType.Invisible);
            }
        }

        private void RestoreCursor()
        {
            _holdingCursor = false;
            Cursor.lockState = _savedLock;
            Cursor.visible = _savedVisible;
            CursorSwitcher.SetCursor(_savedCursor);

            if (_heldVisibility.HasValue)
            {
                bool visible = _heldVisibility.Value;
                _heldVisibility = null;
                CursorHandler?.Invoke(null, new object[] { visible });
            }

            if (_hasPoint)
            {
                UiInput.PlaceCursor(_savedPoint);
                _restoreFrames = 2;
            }
        }

        private void Animate()
        {
            float now = Time.unscaledTime;
            float open = Ease(Mathf.Clamp01((now - _openedAt) / OpenTime));

            _group.alpha = Mathf.Clamp01((now - _openedAt) / (OpenTime * 0.45f));
            _ring.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, open);

            float stagger = _onPage > 0 ? StaggerTime / _onPage : 0f;

            for (int i = 0; i < _onPage; i++)
            {
                float local = Ease(Mathf.Clamp01((now - _segmentsAt - i * stagger) / SegmentTime));
                _segments[i].fillAmount = _fill * local;
                Vector3 scale = Vector3.one * local;
                _icons[i].rectTransform.localScale = scale;
                _letters[i].rectTransform.localScale = scale;
            }

            Scramble(now);
        }

        private void Scramble(float now)
        {
            if (_scrambleAt < 0f)
            {
                return;
            }

            float progress = (now - _scrambleAt) / ScrambleTime;

            if (progress >= 1f)
            {
                _scrambleAt = -1f;
                _name.text = _scrambleTarget;
                return;
            }

            int revealed = Mathf.FloorToInt(progress * _scrambleTarget.Length);
            _scrambled.Length = 0;

            for (int i = 0; i < _scrambleTarget.Length; i++)
            {
                char letter = _scrambleTarget[i];
                _scrambled.Append(i < revealed || char.IsWhiteSpace(letter) ? letter : Glyphs[Random.Range(0, Glyphs.Length)]);
            }

            _name.text = _scrambled.ToString();
        }

        private static float Ease(float t)
        {
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }

        private InventoryTab TabAt(int slot)
        {
            int index = _pageIndex * _perPage + slot;
            return slot >= 0 && slot < _onPage && index < _tabs.Count ? _tabs[index] : null;
        }

        private void LayoutPage()
        {
            int start = _pageIndex * _perPage;
            _onPage = _perPage;
            float slice = 360f / _onPage;
            _fill = (slice - GapDegrees) / 360f;
            float iconRadius = (OuterRadius + InnerRadius) * 0.5f;

            for (int i = 0; i < MaxSegments; i++)
            {
                bool used = i < _onPage;
                _segments[i].gameObject.SetActive(used);
                _icons[i].gameObject.SetActive(false);
                _letters[i].gameObject.SetActive(false);

                if (!used)
                {
                    continue;
                }

                _segments[i].fillAmount = 0f;
                RotateSegment(_segments[i].rectTransform, i * slice);

                float radians = i * slice * Mathf.Deg2Rad;
                Vector2 position = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * iconRadius;
                InventoryTab tab = start + i < _tabs.Count ? _tabs[start + i] : null;
                _segments[i].color = tab != null ? SegmentIdle : SegmentEmpty;

                if (tab == null)
                {
                    continue;
                }

                if (tab.Icon != null)
                {
                    _icons[i].sprite = tab.Icon;
                    _icons[i].rectTransform.anchoredPosition = position;
                    _icons[i].gameObject.SetActive(true);
                }
                else
                {
                    _letters[i].text = string.IsNullOrEmpty(tab.DisplayName) ? "?" : tab.DisplayName.Substring(0, 1);
                    _letters[i].rectTransform.anchoredPosition = position;
                    _letters[i].gameObject.SetActive(true);
                }
            }

            _highlight.fillAmount = _fill;
            LayoutDots();
            ApplySelection(true);
        }

        private void ApplySelection(bool snap)
        {
            bool selected = _selected >= 0;
            _highlight.gameObject.SetActive(selected);

            for (int i = 0; i < _onPage; i++)
            {
                InventoryTab slot = TabAt(i);
                Color color = slot == null || !slot.CanOpen ? Unavailable : i == _selected ? Color.black : selected ? Faded : Color.white;
                _icons[i].color = color;
                _letters[i].color = color;
            }

            if (!selected)
            {
                _scrambleAt = -1f;
                _name.text = string.Empty;
                _mod.text = string.Empty;
                return;
            }

            InventoryTab tab = TabAt(_selected);
            _scrambleTarget = tab.DisplayName ?? string.Empty;
            _scrambleAt = Time.unscaledTime;
            _mod.text = tab.IsVanilla ? string.Empty : !string.IsNullOrEmpty(tab.ModName) ? tab.ModName : UiText.Get(UiText.Mod);
            Scramble(_scrambleAt);

            if (snap)
            {
                _highlightAngle = _selected * 360f / _onPage;
                RotateSegment(_highlight.rectTransform, _highlightAngle);
            }
        }

        private void RotateSegment(RectTransform segment, float centerAngle)
        {
            float half = (360f / _onPage - GapDegrees) * 0.5f;
            segment.localEulerAngles = new Vector3(0f, 0f, -(centerAngle - half));
        }

        private void Build(TMP_FontAsset font)
        {
            Sprite ring = UiHelpers.Ring(InnerRadius / OuterRadius);

            for (int i = 0; i < MaxSegments; i++)
            {
                _segments[i] = Segment("Segment", ring, SegmentIdle);
            }

            _highlight = Segment("Highlight", ring, UiHelpers.Accent);

            for (int i = 0; i < MaxSegments; i++)
            {
                RectTransform icon = UiHelpers.Pin(UiHelpers.Rect("Icon", _ring), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(IconSize, IconSize));
                _icons[i] = UiHelpers.Image(icon, Color.white, false);
                _icons[i].preserveAspect = true;

                RectTransform letter = UiHelpers.Pin(UiHelpers.Rect("Letter", _ring), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(IconSize, IconSize));
                _letters[i] = UiHelpers.Label(letter, font, string.Empty, 26f, Color.white, TextAlignmentOptions.Center);
            }

            float centerSize = (InnerRadius - 10f) * 2f;
            RectTransform center = UiHelpers.Pin(UiHelpers.Rect("Center", _ring), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(centerSize, centerSize));
            Image disc = UiHelpers.Image(center, CenterColor, false);
            disc.sprite = UiHelpers.Ring(0f);

            RectTransform hairline = UiHelpers.Pin(UiHelpers.Rect("Hairline", _ring), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(centerSize - 8f, centerSize - 8f));
            Image line = UiHelpers.Image(hairline, HairlineColor, false);
            line.sprite = UiHelpers.Ring(0.985f);

            _pointer = UiHelpers.Pin(UiHelpers.Rect("Pointer", _ring), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1f, 1f));
            RectTransform tip = UiHelpers.Pin(UiHelpers.Rect("Tip", _pointer), new Vector2(0.5f, 0.5f), new Vector2(0f, InnerRadius - 4f),
                new Vector2(16f, 12f));
            _pointerImage = UiHelpers.Image(tip, UiHelpers.Accent, false);
            _pointerImage.sprite = UiHelpers.Arrow;

            RectTransform name = UiHelpers.Pin(UiHelpers.Rect("Name", _ring), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(InnerRadius * 1.7f, 40f));
            _name = UiHelpers.Label(name, font, string.Empty, 20f, Color.white, TextAlignmentOptions.Center);

            RectTransform mod = UiHelpers.Pin(UiHelpers.Rect("Mod", _ring), new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(InnerRadius * 1.6f, 20f));
            _mod = UiHelpers.Label(mod, font, string.Empty, 12f, UiHelpers.Accent, TextAlignmentOptions.Center);

            _dots = UiHelpers.Pin(UiHelpers.Rect("Dots", _ring), new Vector2(0.5f, 0.5f), new Vector2(0f, -OuterRadius - 24f),
                new Vector2(0f, DotSize));

        }

        private void LayoutDots()
        {
            while (_dotImages.Count < _pageCount)
            {
                RectTransform dot = UiHelpers.Pin(UiHelpers.Rect("Dot", _dots), new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(DotSize, DotSize));
                _dotImages.Add(UiHelpers.Image(dot, Color.white, false));
            }

            float start = -(_pageCount - 1) * DotSpacing * 0.5f;

            for (int i = 0; i < _dotImages.Count; i++)
            {
                Image dot = _dotImages[i];
                bool used = _pageCount > 1 && i < _pageCount;
                dot.gameObject.SetActive(used);

                if (!used)
                {
                    continue;
                }

                bool current = i == _pageIndex;
                dot.sprite = current ? UiHelpers.Dot : UiHelpers.HollowDot;
                dot.color = current ? Color.white : Faded;
                dot.rectTransform.anchoredPosition = new Vector2(start + i * DotSpacing, 0f);
            }
        }

        private Image Segment(string name, Sprite ring, Color color)
        {
            RectTransform rect = UiHelpers.Pin(UiHelpers.Rect(name, _ring), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(OuterRadius * 2f, OuterRadius * 2f));
            return UiHelpers.Filled(UiHelpers.Image(rect, color, false), ring);
        }
    }
}
