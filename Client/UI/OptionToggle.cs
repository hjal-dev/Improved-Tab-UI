using System;
using BepInEx.Configuration;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal sealed class OptionToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const float BoxSize = 14f;
        private const float LabelSize = 12f;

        private static readonly Color BoxBorder = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color HoverFill = new Color(1f, 1f, 1f, 0.06f);

        private ConfigEntry<bool> _entry;
        private Func<string> _text;
        private Func<string> _tooltip;
        private Image _background;
        private Image _check;
        private TextMeshProUGUI _label;
        private bool _hovered;
        private bool _tooltipShown;

        internal static OptionToggle Create(RectTransform parent, TMP_FontAsset font, ConfigEntry<bool> entry, float bottom, float height,
            Func<string> text, Func<string> tooltip)
        {
            RectTransform root = UiHelpers.Rect("Option", parent);
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.offsetMin = new Vector2(0f, bottom);
            root.offsetMax = new Vector2(0f, bottom + height);

            OptionToggle toggle = root.gameObject.AddComponent<OptionToggle>();
            toggle._entry = entry;
            toggle._text = text;
            toggle._tooltip = tooltip;
            toggle._background = UiHelpers.Image(root, Color.clear, true);

            RectTransform line = UiHelpers.Rect("Line", root);
            line.anchorMin = new Vector2(0f, 1f);
            line.anchorMax = new Vector2(1f, 1f);
            line.offsetMin = new Vector2(NavigatorWindow.Margin, -1f);
            line.offsetMax = new Vector2(-NavigatorWindow.Margin, 0f);
            UiHelpers.Image(line, new Color(1f, 1f, 1f, 0.1f), false);

            RectTransform box = UiHelpers.Pin(UiHelpers.Rect("Box", root), new Vector2(0f, 0.5f), new Vector2(NavigatorWindow.Margin, 0f),
                new Vector2(BoxSize, BoxSize));
            UiHelpers.Image(box, BoxBorder, false);

            RectTransform inner = UiHelpers.Rect("Inner", box);
            UiHelpers.Stretch(inner, 1f, 1f, 1f, 1f);
            UiHelpers.Image(inner, UiHelpers.Dark, false);

            RectTransform check = UiHelpers.Rect("Check", inner);
            UiHelpers.Stretch(check, 2f, 2f, 2f, 2f);
            toggle._check = UiHelpers.Image(check, Color.white, false);

            RectTransform label = UiHelpers.Rect("Label", root);
            UiHelpers.Stretch(label, NavigatorWindow.Margin + BoxSize + 10f, 0f, NavigatorWindow.Margin, 0f);
            toggle._label = UiHelpers.Label(label, font, text(), LabelSize, UiHelpers.Text, TextAlignmentOptions.Left);

            entry.SettingChanged += toggle.OnSettingChanged;
            toggle.Sync();
            return toggle;
        }

        internal void ApplyLanguage()
        {
            _label.text = _text();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Sync();
            UiSounds.Play(EUISoundType.ButtonOver);
            ShowTooltip(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Sync();
            ShowTooltip(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _entry.Value = !_entry.Value;
            UiSounds.Play(EUISoundType.MenuCheckBox);
            Sync();
        }

        private void OnSettingChanged(object sender, EventArgs args)
        {
            Sync();
        }

        private void Sync()
        {
            _check.enabled = _entry.Value;
            _background.color = _hovered ? HoverFill : Color.clear;
            _label.color = _hovered ? UiHelpers.Accent : UiHelpers.Text;
        }

        private void ShowTooltip(bool show)
        {
            if (show == _tooltipShown)
            {
                return;
            }

            _tooltipShown = show;
            UiHelpers.Tooltip(show ? _tooltip() : null);
        }

        private void OnDisable()
        {
            _hovered = false;
            ShowTooltip(false);

            if (_check != null)
            {
                Sync();
            }
        }

        private void OnDestroy()
        {
            if (_entry != null)
            {
                _entry.SettingChanged -= OnSettingChanged;
            }
        }
    }
}
