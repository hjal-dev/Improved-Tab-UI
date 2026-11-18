using System;
using EFT.UI;
using ImprovedTabUI.Logging;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal sealed class UiButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const float MinimumIdleAlpha = 0.12f;

        private static bool _styled;
        private static Sprite _sprite;
        private static Image.Type _imageType = Image.Type.Simple;
        private static float _pixelsPerUnit = 1f;
        private static Color _idleBackground = new Color(1f, 1f, 1f, 0.07f);
        private static Color _hoverBackground = new Color(1f, 1f, 1f, 0.16f);
        private static Color _idleForeground = Color.white;
        private static Color _hoverForeground = new Color(0.85f, 0.79f, 0.56f, 1f);

        private Image _background;
        private Graphic _foreground;
        private Action _clicked;
        private bool _hovered;
        private Color? _hoverFill;
        private bool _selected;
        private Func<string> _tooltip;
        private bool _tooltipShown;

        internal UiButton WithTooltip(Func<string> tooltip)
        {
            _tooltip = tooltip;
            return this;
        }

        internal UiButton WithHoverFill(Color color)
        {
            _hoverFill = color;
            Apply();
            return this;
        }

        internal void SetSelected(bool selected)
        {
            if (_selected != selected)
            {
                _selected = selected;
                Apply();
            }
        }

        internal static void UseGameStyle(DefaultUIButton reference)
        {
            if (_styled)
            {
                return;
            }

            _styled = true;
            DefaultUIButtonAnimation animation = reference != null ? reference.GetComponent<DefaultUIButtonAnimation>() : null;

            if (animation == null || animation.Image == null || animation.Image.sprite == null)
            {
                TabLog.Warn("The game's button style was not found, using plain buttons.");
                return;
            }

            Image image = animation.Image;
            _sprite = image.sprite;
            _imageType = image.type;
            _pixelsPerUnit = image.pixelsPerUnitMultiplier;
            _idleBackground = new Color(1f, 1f, 1f, Mathf.Max(animation._backgorundNormalStateAplha, MinimumIdleAlpha));
            _hoverBackground = Color.white;
            _hoverForeground = animation._highlightedLabelColor;
        }

        internal static UiButton Attach(GameObject host, Image background, Graphic foreground, Action clicked)
        {
            if (_sprite != null)
            {
                background.sprite = _sprite;
                background.type = _imageType;
                background.pixelsPerUnitMultiplier = _pixelsPerUnit;
            }

            UiButton button = host.AddComponent<UiButton>();
            button._background = background;
            button._foreground = foreground;
            button._clicked = clicked;
            button.Apply();
            return button;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Apply();
            UiSounds.Play(EUISoundType.ButtonOver);
            ShowTooltip(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Apply();
            ShowTooltip(false);
        }

        private void ShowTooltip(bool show)
        {
            if (_tooltip == null || show == _tooltipShown)
            {
                return;
            }

            _tooltipShown = show;
            UiHelpers.Tooltip(show ? _tooltip() : null);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            UiSounds.Play(EUISoundType.ButtonClick);
            _clicked?.Invoke();
        }

        private void Apply()
        {
            if (_selected)
            {
                _background.color = UiHelpers.Accent;
                _foreground.color = Color.black;
                return;
            }

            _background.color = _hovered ? _hoverFill ?? _hoverBackground : _idleBackground;
            _foreground.color = _hovered ? _hoverForeground : _idleForeground;
        }

        private void OnDisable()
        {
            ShowTooltip(false);

            if (_hovered)
            {
                _hovered = false;
                Apply();
            }
        }
    }
}
