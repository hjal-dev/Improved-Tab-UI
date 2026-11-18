using EFT.UI;
using ImprovedTabUI.Eft;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal sealed class NavigatorRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal const float Height = 30f;
        internal const float ButtonGap = 4f;
        internal const float KeyWidth = 44f;

        private const float BoxSize = 14f;
        private const float ModIconSize = 16f;
        private const float OriginGap = 8f;
        private const float NameLeft = NavigatorWindow.Margin + BoxSize + 10f;

        private static readonly Color HoverFill = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color BoxBorder = new Color(1f, 1f, 1f, 0.7f);

        private NavigatorWindow _window;
        private CanvasGroup _group;
        private Image _background;
        private Image _check;
        private TextMeshProUGUI _name;
        private Image _modIcon;
        private bool _tooltipShown;
        private UiButton _keyButton;
        private TextMeshProUGUI _keyLabel;
        private UiButton _up;
        private UiButton _down;
        private InventoryTab _tab;

        private int _index;
        private bool _canMoveUp;
        private bool _canMoveDown;
        private bool _hovered;
        private bool _capturing;
        private bool _hasState;
        private bool _shownVisible;
        private bool _shownCurrent;

        internal InventoryTab Tab
        {
            get { return _tab; }
        }

        internal int Index
        {
            get { return _index; }
        }

        internal static NavigatorRow Create(RectTransform parent, NavigatorWindow window, TMP_FontAsset font)
        {
            RectTransform root = UiHelpers.Rect("Row", parent);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = Height;
            layout.preferredHeight = Height;

            NavigatorRow row = root.gameObject.AddComponent<NavigatorRow>();
            row._window = window;
            row._group = root.gameObject.AddComponent<CanvasGroup>();
            row._background = UiHelpers.Image(root, Color.clear, true);

            float right = NavigatorWindow.Margin - NavigatorWindow.RowInset;
            float keyRight = NavigatorWindow.KeyColumn - NavigatorWindow.RowInset;
            float originRight = NavigatorWindow.OriginColumn - NavigatorWindow.RowInset;
            float drop = NavigatorWindow.BaselineDrop;

            RectTransform box = UiHelpers.Pin(UiHelpers.Rect("Box", root), new Vector2(0f, 0.5f), new Vector2(NavigatorWindow.Margin, 0f),
                new Vector2(BoxSize, BoxSize));
            UiHelpers.Image(box, BoxBorder, false);

            RectTransform inner = UiHelpers.Rect("Inner", box);
            UiHelpers.Stretch(inner, 1f, 1f, 1f, 1f);
            UiHelpers.Image(inner, UiHelpers.Dark, false);

            RectTransform check = UiHelpers.Rect("Check", inner);
            UiHelpers.Stretch(check, 2f, 2f, 2f, 2f);
            row._check = UiHelpers.Image(check, Color.white, false);

            RectTransform name = UiHelpers.Rect("Name", root);
            UiHelpers.Stretch(name, NameLeft, -drop, originRight + ModIconSize + OriginGap, drop);
            row._name = UiHelpers.Label(name, font, string.Empty, 14f, UiHelpers.Text, TextAlignmentOptions.BaselineLeft);

            RectTransform modIcon = UiHelpers.Pin(UiHelpers.Rect("ModIcon", root), new Vector2(1f, 0.5f), new Vector2(-originRight, 0f),
                new Vector2(ModIconSize, ModIconSize));
            row._modIcon = UiHelpers.Image(modIcon, UiHelpers.Accent, false);
            row._modIcon.sprite = UiHelpers.Wrench;
            row._modIcon.preserveAspect = true;

            RectTransform key = UiHelpers.Pin(UiHelpers.Rect("Key", root), new Vector2(1f, 0.5f), new Vector2(-keyRight, 0f),
                new Vector2(KeyWidth, UiHelpers.ButtonSize));
            Image keyBackground = UiHelpers.Image(key, Color.white, true);
            RectTransform keyText = UiHelpers.Rect("Text", key);
            UiHelpers.Stretch(keyText, 2f, 0f, 2f, 0f);
            row._keyLabel = UiHelpers.Label(keyText, font, string.Empty, 10f, Color.white, TextAlignmentOptions.Center);
            row._keyButton = UiButton.Attach(key.gameObject, keyBackground, row._keyLabel, row.OnKeyClicked);

            row._down = UiHelpers.ArrowButton(root, "MoveRight", right, true, row.OnMoveDown);
            row._up = UiHelpers.ArrowButton(root, "MoveLeft", right + UiHelpers.ButtonSize + ButtonGap, false, row.OnMoveUp);

            row.ApplyHover();
            return row;
        }

        internal void Bind(InventoryTab tab, int index, int count)
        {
            _tab = tab;
            _index = index;
            _hasState = false;
            _canMoveUp = index > 0;
            _canMoveDown = index < count - 1;
            RefreshKey();
            ApplyHover();
            Sync();
        }

        internal void Sync()
        {
            if (_tab == null)
            {
                return;
            }

            if (_name.text != _tab.DisplayName)
            {
                _name.text = _tab.DisplayName;
            }

            bool visible = _tab.Visible.Value;
            bool current = _tab.Tab != null && _tab.Tab == _window.CurrentTab;

            if (_hasState && visible == _shownVisible && current == _shownCurrent)
            {
                return;
            }

            _hasState = true;
            _shownVisible = visible;
            _shownCurrent = current;
            _check.enabled = visible;
            _name.color = !visible ? UiHelpers.Dim : current ? UiHelpers.Accent : UiHelpers.Text;
            ApplyHover();
        }

        internal void SetCapturing(bool capturing)
        {
            _capturing = capturing;
            RefreshKey();
            ApplyHover();
        }

        internal void SetDragging(bool dragging)
        {
            _group.alpha = dragging ? 0.35f : 1f;
        }

        internal void RefreshKey()
        {
            if (_tab == null)
            {
                return;
            }

            string text = _capturing ? "?" : UiInput.KeyText(_tab.Key.Value);

            if (text.Length == 0)
            {
                text = "+";
            }

            if (_keyLabel.text != text)
            {
                _keyLabel.text = text;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            ApplyHover();
            UiSounds.Play(EUISoundType.ButtonOver);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            ApplyHover();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && _tab != null)
            {
                bool changed = _window.ToggleRow(_tab);
                UiSounds.Play(changed ? EUISoundType.MenuCheckBox : EUISoundType.ErrorMessage);
                Sync();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && _tab != null)
            {
                _window.BeginRowDrag(this);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            _window.DragRow(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _window.EndRowDrag(this);
        }

        private void ApplyHover()
        {
            _background.color = _hovered ? HoverFill : Color.clear;
            SetShown(_up, _hovered && _canMoveUp);
            SetShown(_down, _hovered && _canMoveDown);
            SetShown(_keyButton, _hovered || _capturing || (_tab != null && _tab.Key.Value.MainKey != KeyCode.None));
            bool modded = _tab != null && !_tab.IsVanilla;

            if (_modIcon.enabled != modded)
            {
                _modIcon.enabled = modded;
            }

            ApplyTooltip(_hovered && modded);
        }

        private void ApplyTooltip(bool show)
        {
            if (show == _tooltipShown)
            {
                return;
            }

            _tooltipShown = show;
            UiHelpers.Tooltip(show ? (!string.IsNullOrEmpty(_tab.ModName) ? _tab.ModName : UiText.Get(UiText.Mod)) : null);
        }

        private static void SetShown(Component button, bool shown)
        {
            if (button.gameObject.activeSelf != shown)
            {
                button.gameObject.SetActive(shown);
            }
        }

        private void OnKeyClicked()
        {
            if (_tab != null)
            {
                _window.BeginKeyCapture(this);
            }
        }

        private void OnMoveUp()
        {
            if (_tab != null)
            {
                _window.Move(_tab, -1);
            }
        }

        private void OnMoveDown()
        {
            if (_tab != null)
            {
                _window.Move(_tab, 1);
            }
        }

        private void OnDisable()
        {
            if (_hovered)
            {
                _hovered = false;
                ApplyHover();
            }
        }
    }
}
