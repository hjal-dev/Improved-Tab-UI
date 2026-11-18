using System;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal sealed class TabKeyButton
    {
        private const float MinimumWidth = 46f;
        private const float Padding = 14f;
        private const float FontSize = 18f;

        private readonly ConfigEntry<KeyboardShortcut> _key;
        private readonly RectTransform _rect;
        private readonly TextMeshProUGUI _label;

        private TabKeyButton(ConfigEntry<KeyboardShortcut> key, RectTransform rect, TextMeshProUGUI label)
        {
            _key = key;
            _rect = rect;
            _label = label;
        }

        internal float Width { get; private set; }

        internal bool Shown { get; private set; }

        internal static TabKeyButton Create(RectTransform parent, string name, TMP_FontAsset font, ConfigEntry<KeyboardShortcut> key, Action clicked)
        {
            RectTransform rect = UiHelpers.Rect(name, parent);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = Vector2.zero;
            Image background = UiHelpers.Image(rect, Color.white, true);

            RectTransform text = UiHelpers.Rect("Label", rect);
            UiHelpers.Stretch(text, 0f, 0f, 0f, 0f);
            TextMeshProUGUI label = UiHelpers.Label(text, font, string.Empty, FontSize, Color.white, TextAlignmentOptions.Center);
            label.overflowMode = TextOverflowModes.Overflow;

            UiButton.Attach(rect.gameObject, background, label, clicked);

            TabKeyButton button = new TabKeyButton(key, rect, label);
            button.ApplyKey();
            return button;
        }

        internal void ApplyKey()
        {
            string text = UiInput.KeyText(_key.Value);
            Shown = text.Length > 0 && Plugin.ShowKeyButtons.Value;
            _label.text = text;
            Width = Shown ? Mathf.Max(MinimumWidth, _label.GetPreferredValues(text).x + Padding * 2f) : 0f;
            _rect.gameObject.SetActive(Shown);
        }

        internal void Place(Vector2 position, float height)
        {
            if (_rect.anchoredPosition != position)
            {
                _rect.anchoredPosition = position;
            }

            Vector2 size = new Vector2(Width, height);

            if (_rect.sizeDelta != size)
            {
                _rect.sizeDelta = size;
            }
        }

        internal void Destroy()
        {
            if (_rect != null)
            {
                UnityEngine.Object.Destroy(_rect.gameObject);
            }
        }
    }
}
