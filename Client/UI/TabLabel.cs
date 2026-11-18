using System;
using System.Collections.Generic;
using ImprovedTabUI.Eft;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal sealed class TabLabel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float BadgeHeight = 18f;
        private const float BadgePadding = 5f;
        private const float BadgeGap = 6f;
        private const float BadgeFontSize = 13f;
        private const float BadgeFill = 0.2f;
        private const float FallbackMargin = 26f;
        private const float RightSlack = 6f;
        private const float EdgeFade = 6f;
        private const float ScrollSpeed = 35f;
        private const float EdgePause = 1.2f;
        private const float HoverLead = 0.8f;

        private readonly List<Part> _parts = new List<Part>();

        private InventoryTab _tab;
        private RectTransform _layer;
        private RectTransform _badge;
        private Image _badgeBack;
        private TextMeshProUGUI _badgeText;
        private string _key = string.Empty;
        private float _width = -1f;
        private float _baseAdjustment = float.NaN;
        private float _shift;
        private float _amount;
        private bool _hovered;
        private bool _scrolling;
        private float _scrollFrom;
        private Part _shownPart;
        private Vector3 _shownAt;

        private sealed class Part
        {
            internal RectTransform Version;
            internal TextMeshProUGUI Label;
            internal IconNudge Icon;
            internal Vector2 BadgeAnchor;
            internal float LabelOrigin;
            internal float WindowLeft;
            internal float WindowRight;
            internal float Overflow;
            internal float MeasuredWidth;
            internal Action<TMP_TextInfo> Handler;
        }

        internal static TabLabel For(InventoryTab tab, RectTransform layer)
        {
            TabLabel label = tab.Tab.GetComponent<TabLabel>() ?? tab.Tab.gameObject.AddComponent<TabLabel>();
            label.Bind(tab, layer);
            return label;
        }

        internal float RequiredWidth
        {
            get
            {
                float required = 0f;

                foreach (Part part in _parts)
                {
                    if (part.MeasuredWidth > 0f)
                    {
                        required = Mathf.Max(required, part.MeasuredWidth + part.Overflow);
                    }
                }

                return required;
            }
        }

        internal void Apply(string key, float baseAdjustment)
        {
            float width = ((RectTransform)transform).rect.width;

            if (_tab == null || (key == _key && Mathf.Approximately(width, _width) && Mathf.Approximately(baseAdjustment, _baseAdjustment)))
            {
                return;
            }

            _key = key;
            _width = width;
            _baseAdjustment = baseAdjustment;
            _shift = 0f;

            if (key.Length > 0)
            {
                _badgeText.text = key;
                float badgeWidth = Mathf.Max(BadgeHeight, _badgeText.GetPreferredValues(key).x + BadgePadding * 2f);
                _badge.sizeDelta = new Vector2(badgeWidth, BadgeHeight);
                _shift = badgeWidth + BadgeGap;
            }

            foreach (Part part in _parts)
            {
                Measure(part);
                part.Icon.Offset = _shift;
                part.Label.havePropertiesChanged = true;
            }

            _shownPart = null;
            PlaceBadge();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        private void Bind(InventoryTab tab, RectTransform layer)
        {
            if (_tab != null)
            {
                return;
            }

            _tab = tab;
            _layer = layer;
            AddPart(tab.Tab._normalVersion);
            AddPart(tab.Tab._selectedVersion);
            CreateBadge(_parts.Count > 0 ? _parts[0].Label.font : null);
        }

        private void AddPart(GameObject version)
        {
            if (version == null)
            {
                return;
            }

            RectTransform icon = version.transform.Find("Icon") as RectTransform;
            RectTransform text = version.transform.Find("Text") as RectTransform;
            TextMeshProUGUI label = text != null ? text.GetComponent<TextMeshProUGUI>() : null;

            if (icon == null || label == null || icon.GetComponent<Graphic>() == null)
            {
                return;
            }

            Part part = new Part
            {
                Version = (RectTransform)version.transform,
                Label = label,
                Icon = icon.GetComponent<IconNudge>() ?? icon.gameObject.AddComponent<IconNudge>()
            };

            part.Handler = info => Clip(part, info);
            label.OnPreRenderText += part.Handler;
            _parts.Add(part);
        }

        private void CreateBadge(TMP_FontAsset font)
        {
            _badge = UiHelpers.Rect("Key " + _tab.Id, _layer);
            _badge.anchorMin = new Vector2(0.5f, 0.5f);
            _badge.anchorMax = new Vector2(0.5f, 0.5f);
            _badge.pivot = new Vector2(0f, 0.5f);
            _badgeBack = UiHelpers.Image(_badge, Color.white, false);

            RectTransform text = UiHelpers.Rect("Label", _badge);
            UiHelpers.Stretch(text, 0f, 0f, 0f, 0f);
            _badgeText = UiHelpers.Label(text, font, string.Empty, BadgeFontSize, Color.white, TextAlignmentOptions.Center);
            _badgeText.overflowMode = TextOverflowModes.Overflow;
            _badge.gameObject.SetActive(false);
        }

        private void Measure(Part part)
        {
            RectTransform version = part.Version;
            RectTransform icon = (RectTransform)part.Icon.transform;
            RectTransform text = part.Label.rectTransform;
            Vector3 iconPivot = version.InverseTransformPoint(icon.position);
            float iconRight = iconPivot.x + icon.rect.xMax;

            part.BadgeAnchor = new Vector2(iconPivot.x + icon.rect.xMin, iconPivot.y + icon.rect.center.y);
            part.LabelOrigin = version.InverseTransformPoint(text.position).x;
            part.WindowLeft = Mathf.Max(part.LabelOrigin + text.rect.xMin, iconRight) + _shift;

            float adjustment = _baseAdjustment;
            float textRest = text.anchoredPosition.x;
            float right = adjustment > textRest ? textRest + _width - adjustment + RightSlack : _width - FallbackMargin;
            part.WindowRight = version.rect.xMin + right;
        }

        private void Clip(Part part, TMP_TextInfo info)
        {
            if (_tab == null || _key.Length == 0 || info == null || info.meshInfo == null)
            {
                part.Overflow = 0f;
                part.MeasuredWidth = 0f;
                return;
            }

            float right = float.MinValue;

            for (int m = 0; m < info.materialCount && m < info.meshInfo.Length; m++)
            {
                Vector3[] vertices = info.meshInfo[m].vertices;

                for (int v = 0; vertices != null && v < info.meshInfo[m].vertexCount && v < vertices.Length; v++)
                {
                    right = Mathf.Max(right, vertices[v].x);
                }
            }

            if (right == float.MinValue)
            {
                part.Overflow = 0f;
                return;
            }

            part.Overflow = Mathf.Max(0f, part.LabelOrigin + right + _shift - part.WindowRight);
            part.MeasuredWidth = _width;
            float move = _shift - _amount * part.Overflow;
            bool fadeLeft = _amount > 0f;
            bool fadeRight = part.Overflow > 0f;

            for (int m = 0; m < info.materialCount && m < info.meshInfo.Length; m++)
            {
                Vector3[] vertices = info.meshInfo[m].vertices;
                Color32[] colors = info.meshInfo[m].colors32;

                for (int v = 0; vertices != null && v < info.meshInfo[m].vertexCount && v < vertices.Length; v++)
                {
                    vertices[v].x += move;

                    if ((!fadeLeft && !fadeRight) || colors == null || v >= colors.Length)
                    {
                        continue;
                    }

                    float x = part.LabelOrigin + vertices[v].x;
                    float fade = (fadeLeft ? Mathf.Clamp01((x - part.WindowLeft) / EdgeFade) : 1f)
                        * (fadeRight ? Mathf.Clamp01((part.WindowRight - x) / EdgeFade) : 1f);

                    if (fade < 1f)
                    {
                        colors[v].a = (byte)(colors[v].a * fade);
                    }
                }
            }
        }

        private Part ShownPart()
        {
            foreach (Part part in _parts)
            {
                if (part.Version.gameObject.activeInHierarchy)
                {
                    return part;
                }
            }

            return null;
        }

        private void PlaceBadge()
        {
            if (_badge == null)
            {
                return;
            }

            Part part = ShownPart();
            bool show = _key.Length > 0 && part != null && isActiveAndEnabled;

            if (_badge.gameObject.activeSelf != show)
            {
                _badge.gameObject.SetActive(show);
            }

            if (!show)
            {
                _shownPart = null;
                return;
            }

            Vector3 at = transform.localPosition;

            if (part == _shownPart && at == _shownAt)
            {
                return;
            }

            _shownPart = part;
            _shownAt = at;
            Color color = part.Label.color;
            _badgeText.color = color;
            _badgeBack.color = new Color(color.r, color.g, color.b, BadgeFill);
            Vector3 local = _layer.InverseTransformPoint(part.Version.TransformPoint(part.BadgeAnchor));
            _badge.localPosition = new Vector3(local.x, local.y, 0f);
        }

        private void Update()
        {
            if (_tab == null)
            {
                return;
            }

            float overflow = 0f;

            foreach (Part part in _parts)
            {
                overflow = Mathf.Max(overflow, part.Overflow);
            }

            bool scroll = overflow > 0.5f
                && (Plugin.ScrollNamesAlways.Value || (_hovered && Plugin.ScrollNamesOnHover.Value));
            float amount = 0f;

            if (scroll)
            {
                float now = Time.unscaledTime;

                if (!_scrolling)
                {
                    _scrolling = true;
                    _scrollFrom = now - (Plugin.ScrollNamesAlways.Value ? 0f : HoverLead);
                }

                float travel = overflow / ScrollSpeed;
                float time = Mathf.Repeat(now - _scrollFrom, (EdgePause + travel) * 2f);

                if (time < EdgePause)
                {
                    amount = 0f;
                }
                else if (time < EdgePause + travel)
                {
                    amount = (time - EdgePause) / travel;
                }
                else if (time < EdgePause * 2f + travel)
                {
                    amount = 1f;
                }
                else
                {
                    amount = 1f - (time - EdgePause * 2f - travel) / travel;
                }

                amount = Mathf.SmoothStep(0f, 1f, amount);
            }
            else
            {
                _scrolling = false;
            }

            if (amount != _amount)
            {
                _amount = amount;
                Part shown = ShownPart();

                if (shown != null)
                {
                    shown.Label.havePropertiesChanged = true;
                }
            }
        }

        private void LateUpdate()
        {
            if (_tab != null)
            {
                PlaceBadge();
            }
        }

        private void OnEnable()
        {
            if (_tab != null)
            {
                _shownPart = null;
                PlaceBadge();
            }
        }

        private void OnDisable()
        {
            _hovered = false;
            _scrolling = false;

            if (_badge != null)
            {
                _badge.gameObject.SetActive(false);
            }

            if (_amount != 0f)
            {
                _amount = 0f;

                foreach (Part part in _parts)
                {
                    part.Label.havePropertiesChanged = true;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (Part part in _parts)
            {
                if (part.Label != null)
                {
                    part.Label.OnPreRenderText -= part.Handler;
                }
            }

            if (_badge != null)
            {
                Destroy(_badge.gameObject);
            }
        }
    }
}
