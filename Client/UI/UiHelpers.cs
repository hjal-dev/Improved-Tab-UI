using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal static class UiHelpers
    {
        internal static readonly Color Text = Color.white;
        internal static readonly Color Dim = new Color(1f, 1f, 1f, 0.38f);
        internal static readonly Color Accent = new Color(0.85f, 0.79f, 0.56f, 1f);
        internal static readonly Color Dark = Color.black;
        internal static readonly Color OffText = new Color(0.93f, 0.62f, 0.55f, 1f);

        private static Sprite _arrow;
        private static readonly System.Collections.Generic.Dictionary<int, Sprite> Discs = new System.Collections.Generic.Dictionary<int, Sprite>();

        internal static Sprite Ring(float innerRatio)
        {
            return Disc(512, innerRatio);
        }

        internal static Sprite Dot
        {
            get { return Disc(64, 0f); }
        }

        internal static Sprite HollowDot
        {
            get { return Disc(64, 0.6f); }
        }

        private static Sprite Disc(int size, float innerRatio)
        {
            int key = size * 10000 + Mathf.RoundToInt(innerRatio * 1000f);

            if (Discs.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            float outer = size * 0.5f;
            float inner = outer * innerRatio;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - outer;
                    float dy = y + 0.5f - outer;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(outer - distance) * (inner <= 0f ? 1f : Mathf.Clamp01(distance - inner));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Trilinear;
            texture.Apply(true, true);
            Sprite sprite = Sprite.Create(texture, new UnityEngine.Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            Discs[key] = sprite;
            return sprite;
        }

        private static Sprite _wrench;

        internal static Sprite Wrench
        {
            get
            {
                if (_wrench != null)
                {
                    return _wrench;
                }

                const int size = 128;
                const int samples = 4;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
                Color32[] pixels = new Color32[size * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int hits = 0;

                        for (int sy = 0; sy < samples; sy++)
                        {
                            for (int sx = 0; sx < samples; sx++)
                            {
                                float px = (x + (sx + 0.5f) / samples) / size * 2f - 1f;
                                float py = (y + (sy + 0.5f) / samples) / size * 2f - 1f;

                                if (InWrench(px, py))
                                {
                                    hits++;
                                }
                            }
                        }

                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / (samples * samples)));
                    }
                }

                texture.SetPixels32(pixels);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Trilinear;
                texture.Apply(true, true);
                _wrench = Sprite.Create(texture, new UnityEngine.Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
                return _wrench;
            }
        }

        private static bool InWrench(float x, float y)
        {
            const float head = 0.8f;
            const float shaft = 0.16f;

            float u = (x - y) * 0.70710678f;
            float v = (x + y) * 0.70710678f;

            if (Mathf.Abs(v) <= shaft && Mathf.Abs(u) <= head)
            {
                return true;
            }

            return InWrenchHead(u, v, 1f) || InWrenchHead(u, v, -1f);
        }

        private static bool InWrenchHead(float u, float v, float side)
        {
            const float head = 0.8f;
            const float radius = 0.36f;
            const float jaw = 0.15f;
            const float tilt = 20f * Mathf.Deg2Rad;

            float du = (u - side * head) * side;
            float dv = v * side;

            if (du * du + dv * dv > radius * radius)
            {
                return false;
            }

            float ju = du * Mathf.Cos(tilt) + dv * Mathf.Sin(tilt);
            float jv = -du * Mathf.Sin(tilt) + dv * Mathf.Cos(tilt);
            return ju < -0.02f || Mathf.Abs(jv) > jaw;
        }

        internal static Sprite Arrow
        {
            get
            {
                if (_arrow != null)
                {
                    return _arrow;
                }

                const int size = 32;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color32[] pixels = new Color32[size * size];
                Color32 white = new Color32(255, 255, 255, 255);
                Color32 clear = new Color32(255, 255, 255, 0);

                for (int y = 0; y < size; y++)
                {
                    float half = (25 - y) * 0.6f;

                    for (int x = 0; x < size; x++)
                    {
                        bool inside = y >= 6 && y <= 25 && Mathf.Abs(x + 0.5f - size / 2f) <= half;
                        pixels[y * size + x] = inside ? white : clear;
                    }
                }

                texture.SetPixels32(pixels);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.Apply(false, true);
                _arrow = Sprite.Create(texture, new UnityEngine.Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
                return _arrow;
            }
        }

        internal const float ButtonSize = 20f;

        internal static UiButton ArrowButton(RectTransform parent, string name, float right, bool down, System.Action onClick)
        {
            RectTransform rect = Pin(Rect(name, parent), new Vector2(1f, 0.5f), new Vector2(-right, 0f), new Vector2(ButtonSize, ButtonSize));
            Image background = Image(rect, Color.white, true);

            RectTransform icon = Rect("Icon", rect);
            Stretch(icon, 6f, 6f, 6f, 6f);
            Image arrow = Image(icon, Color.white, false);
            arrow.sprite = Arrow;

            if (down)
            {
                icon.localEulerAngles = new Vector3(0f, 0f, 180f);
            }

            return UiButton.Attach(rect.gameObject, background, arrow, onClick);
        }

        internal static void Tooltip(string text)
        {
            try
            {
                EFT.UI.SimpleTooltip tooltip = EFT.UI.ItemUiContext.Instance != null ? EFT.UI.ItemUiContext.Instance.Tooltip : null;

                if (tooltip == null)
                {
                    return;
                }

                if (text != null)
                {
                    tooltip.Show(text);
                }
                else
                {
                    tooltip.Close();
                }
            }
            catch (System.Exception)
            {
            }
        }

        internal static RectTransform Pin(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        internal static RectTransform Rect(string name, Transform parent)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            host.layer = parent.gameObject.layer;
            return host.GetComponent<RectTransform>();
        }

        internal static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        internal static void TopStrip(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -top - height);
            rect.offsetMax = new Vector2(0f, -top);
        }

        internal static Image Image(RectTransform rect, Color color, bool raycast)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        internal static Image Filled(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = UnityEngine.UI.Image.Type.Filled;
            image.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            image.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            image.fillClockwise = true;
            return image;
        }

        internal static TextMeshProUGUI Label(RectTransform rect, TMP_FontAsset font, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            GameObject host = rect.gameObject;
            bool wasActive = host.activeSelf;

            if (wasActive)
            {
                host.SetActive(false);
            }

            TextMeshProUGUI label = host.AddComponent<TextMeshProUGUI>();

            if (font != null)
            {
                label.font = font;
            }

            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.text = text;

            if (wasActive)
            {
                host.SetActive(true);
            }

            return label;
        }
    }
}
