using TMPro;
using UnityEngine;

namespace EffectPreview.Ui
{
    // numeric readout centered on a bar (real or ghost), sized to fit whatever width/height that bar currently has
    // pure decoration: takes no layout space of its own, sits above the bar it labels
    internal class BarLabel
    {
        private const float OutlineWidth = 0.08f;
        private const float WidthPadding = 4f;
        private const float MinFontSize = 1f;
        private const float MaxFontSize = 24f;

        // text sizing only scales down for actual width squeeze, not the bar's (often quite short) height
        private const float BoxHeight = 40f;

        // nudges the label up off dead-vertical-center, where it otherwise reads as sitting a bit low
        private const float VerticalOffset = 3f;

        // below this, the bar is too thin for a legible number anyway, and the label would sit right on top of/overlapping whatever's on the neighboring bar
        private const float MinWidthToShow = 10f;

        private const float MinTextToOutlineContrast = 7f;

        private static readonly Color PlainOutline = new Color(0.2f, 0.2f, 0.2f, 1f);

        // ghost text needs to stay legible over the HUD, so it's tinted lighter but far less transparent than the ghost bars themselves
        private const float GhostAlpha = 0.85f;

        // "before -> after" is only shown if it would still fit at a legible size - below that, the arrow/after half is dropped rather than shrunk further
        private const float MinReadableFontSizeForFit = 13f;
        private const float EstimatedCharWidthFactor = 0.55f;

        // game doesnt carry any arrow symbol directly, but TMP somehow resolves it so we don't touch it
        private const string ArrowGlyph = "→";

        private readonly TextMeshProUGUI _text;
        private readonly Vector3[] _cornerBuffer = new Vector3[4];

        private static readonly Color ShadowColor = new Color(1f, 1f, 1f, 0.55f);
        private const float ShadowWidthPadding = 12f;
        private const float ShadowHeight = 22f;
        private const int ShadowTexSize = 32;
        private const int ShadowBorder = 14;
        private const float ShadowCornerRadius = 12f;
        private const float ShadowFade = 8f;

        private static Sprite _shadowSprite;

        private static readonly System.Collections.Generic.Dictionary<Transform, RectTransform> ShadowLayers = new System.Collections.Generic.Dictionary<Transform, RectTransform>();

        private const float CountdownScale = 0.64f;
        private const float CountdownMinBoxWidth = 80f;
        private const float CountdownVerticalOffset = -6f;

        private readonly RectTransform _shadow;
        private readonly UnityEngine.UI.Image _shadowImage;

        private BarLabel(TextMeshProUGUI text, RectTransform shadow, UnityEngine.UI.Image shadowImage)
        {
            _shadowImage = shadowImage;
            _text = text;
            _shadow = shadow;
        }

        private static Sprite GetShadowSprite()
        {
            if (_shadowSprite == null)
            {
                Texture2D tex = new Texture2D(ShadowTexSize, ShadowTexSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < ShadowTexSize; y++)
                {
                    for (int x = 0; x < ShadowTexSize; x++)
                    {
                        float half = ShadowTexSize * 0.5f;
                        float qx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - ShadowCornerRadius), 0f);
                        float qy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - ShadowCornerRadius), 0f);
                        float distance = Mathf.Sqrt(qx * qx + qy * qy) - ShadowCornerRadius;
                        float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-distance / ShadowFade));
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                    }
                }
                tex.Apply();
                _shadowSprite = Sprite.Create(tex, new Rect(0f, 0f, ShadowTexSize, ShadowTexSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(ShadowBorder, ShadowBorder, ShadowBorder, ShadowBorder));
            }
            return _shadowSprite;
        }

        // one shared layer per row so every shadow sits below every shadowed label, never covering a neighbor's text
        private static RectTransform GetShadowLayer(Transform parent)
        {
            if (!ShadowLayers.TryGetValue(parent, out RectTransform layerRtf) || layerRtf == null)
            {
                GameObject layer = new GameObject("EffectPreview ShadowLayer", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
                layerRtf = (RectTransform)layer.transform;
                layerRtf.SetParent(parent, worldPositionStays: false);
                layerRtf.anchorMin = (layerRtf.anchorMax = (layerRtf.pivot = new Vector2(0.5f, 0.5f)));
                layer.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
                Common.GhostOwnershipTag.Attach(layer);
                ShadowLayers[parent] = layerRtf;
            }
            return layerRtf;
        }

        internal bool IsValid => _text != null;

        // font/fontMaterial: the game's own TMP font asset/material (e.g. StaminaBar.moraleBoostText's), so this reads as native UI rather than a mod font
        internal static BarLabel Create(Transform parent, TMP_FontAsset font, Material fontMaterial, bool shadow = false)
        {
            RectTransform shadowRtf = null;
            UnityEngine.UI.Image shadowImage = null;
            if (shadow)
            {
                RectTransform layer = GetShadowLayer(parent);
                GameObject shadowGo = new GameObject("EffectPreview LabelShadow", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                shadowRtf = (RectTransform)shadowGo.transform;
                shadowRtf.SetParent(layer, worldPositionStays: false);
                shadowRtf.anchorMin = (shadowRtf.anchorMax = (shadowRtf.pivot = new Vector2(0.5f, 0.5f)));
                UnityEngine.UI.Image image = shadowImage = shadowGo.GetComponent<UnityEngine.UI.Image>();
                image.sprite = GetShadowSprite();
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.color = ShadowColor;
                image.raycastTarget = false;
                Common.GhostOwnershipTag.Attach(shadowGo);
                shadowGo.SetActive(false);
            }

            GameObject go = new GameObject("EffectPreview BarLabel", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
            RectTransform rtf = (RectTransform)go.transform;
            rtf.SetParent(parent, worldPositionStays: false);
            rtf.anchorMin = (rtf.anchorMax = (rtf.pivot = new Vector2(0.5f, 0.5f)));

            go.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }
            if (fontMaterial != null)
            {
                text.fontSharedMaterial = fontMaterial;
            }
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = MinFontSize;
            text.fontSizeMax = MaxFontSize;
            text.outlineWidth = OutlineWidth;

            Common.GhostOwnershipTag.Attach(go);
            go.SetActive(false);
            return new BarLabel(text, shadowRtf, shadowImage);
        }

        // truncates to the same 0-100 "count" scale the mod's status fractions represent (0.10 -> "10")
        // epsilon guards against native petrifyAmount landing just under a whole number (e.g. 9.999999 from float accumulation), which would otherwise floor to one less than the real value
        private const float FormatCountEpsilon = 1e-4f;

        internal static string FormatCount(float fraction)
        {
            return Mathf.FloorToInt(fraction * 100f + FormatCountEpsilon).ToString();
        }

        // light hue-tinted text with a dark hue-tinted outline, outline pushed darker until the ratio holds so it reads over any stripe or ghost behind it
        // striped (blocked) statuses hand us their translucent stripe color, so alpha is dropped and only the hue is trusted
        internal static void CountColors(Color barColor, bool ghost, out Color foreground, out Color outlineColor)
        {
            barColor.a = 1f;
            Color.RGBToHSV(barColor, out float h, out float s, out float v);
            float textSaturation = Mathf.Min(s, 0.7f);
            Color fg = Color.HSVToRGB(h, textSaturation, 1f);
            float outlineSaturation = s;
            float outlineValue = 0.22f;
            Color outline = Color.HSVToRGB(h, outlineSaturation, outlineValue);
            for (int i = 0; i < 8 && Common.ColorUtil.ContrastRatio(fg, outline) < MinTextToOutlineContrast; i++)
            {
                outlineValue -= 0.03f;
                outline = Color.HSVToRGB(h, outlineSaturation, Mathf.Max(0f, outlineValue));
            }

            foreground = fg;
            outlineColor = outline;
            if (ghost)
            {
                foreground.a = GhostAlpha;
            }
        }

        internal static void PaletteCountColors(Color fg, Color outline, bool ghost, out Color foreground, out Color outlineColor)
        {
            foreground = fg;
            outlineColor = outline;
            if (ghost)
            {
                foreground.a = GhostAlpha;
            }
        }

        internal static readonly Color CurseText = new Color(0.60f, 0.54f, 0.75f);
        internal static readonly Color CurseOutline = new Color(0.05f, 0.03f, 0.10f);
        internal static readonly Color PetrifyText = new Color(0.72f, 0.75f, 0.85f);
        internal static readonly Color PetrifyOutline = new Color(0.07f, 0.08f, 0.13f);

        // shows "before -> after" only when the item would actually bring the value DOWN and there's room for the full form; otherwise falls
        // back to just "before" - an increase is already visualized by this area's own ghost bar, so the transition only needs to cover the
        // direction that bar can't show (e.g. bonus stamina being knocked down by a previewed petrify gain)
        //
        // fitWidthOverride: world-space fit width to use instead of target's own (possibly still mid-lerp) width, see RESEARCH.md
        internal void ApplyTransition(RectTransform target, float beforeFraction, float afterFraction, Color foreground, Color outlineColor, float scaleMultiplier, float fitWidthOverride = -1f)
        {
            // activeSelf not activeInHierarchy - an ancestor can be transiently inactive mid-frame and still get forced open before render
            if (target == null || !target.gameObject.activeSelf)
            {
                Hide();
                return;
            }

            string beforeText = FormatCount(beforeFraction);
            if (afterFraction >= beforeFraction - 0.0005f)
            {
                Apply(target, beforeText, foreground, outlineColor, scaleMultiplier);
                return;
            }

            string fullText = beforeText + " " + ArrowGlyph + " " + FormatCount(afterFraction);

            float width;
            if (fitWidthOverride >= 0f)
            {
                width = fitWidthOverride;
            }
            else
            {
                target.GetWorldCorners(_cornerBuffer);
                width = _cornerBuffer[2].x - _cornerBuffer[0].x;
            }
            float requiredWidth = fullText.Length * EstimatedCharWidthFactor * MinReadableFontSizeForFit;

            Apply(target, width >= requiredWidth ? fullText : beforeText, foreground, outlineColor, scaleMultiplier);
        }

        internal void Apply(RectTransform target, string content, Color foreground, Color outlineColor, float scaleMultiplier, float extraVerticalOffset = 0f, bool bottomAnchored = false, float minBoxWidth = 0f)
        {
            if (target == null || !target.gameObject.activeSelf || string.IsNullOrEmpty(content))
            {
                Hide();
                return;
            }

            target.GetWorldCorners(_cornerBuffer);
            float width = _cornerBuffer[2].x - _cornerBuffer[0].x;
            float height = _cornerBuffer[1].y - _cornerBuffer[0].y;

            if (width < MinWidthToShow || height <= 1f)
            {
                Hide();
                return;
            }

            Vector3 center = (_cornerBuffer[0] + _cornerBuffer[2]) * 0.5f;

            _text.gameObject.SetActive(true);
            _text.transform.SetAsLastSibling();
            _text.rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, width - WidthPadding, minBoxWidth), BoxHeight);
            _text.transform.localScale = Vector3.one * Mathf.Max(0.01f, scaleMultiplier);

            Vector3 pos = _text.rectTransform.position;
            pos.x = center.x;
            pos.y = (bottomAnchored ? _cornerBuffer[0].y : center.y + VerticalOffset) + extraVerticalOffset * _text.transform.parent.lossyScale.y;
            _text.rectTransform.position = pos;

            if (Plugin.Instance.Cfg.PlainBarCounts.Value)
            {
                foreground = new Color(1f, 1f, 1f, foreground.a);
                outlineColor = PlainOutline;
            }

            _text.text = content;
            _text.color = foreground;
            _text.outlineColor = outlineColor;

            if (_shadow != null)
            {
                _shadow.gameObject.SetActive(true);
                float shadowScale = _text.transform.localScale.x;
                _shadow.sizeDelta = new Vector2(_text.renderedWidth + ShadowWidthPadding, ShadowHeight) * shadowScale;
                _shadowImage.pixelsPerUnitMultiplier = Mathf.Max(0.01f, ShadowBorder / (ShadowHeight * shadowScale * 0.5f));
                _shadow.position = _text.rectTransform.position;
            }
        }

        internal void ApplyCountdown(RectTransform target, string content, Color foreground, Color outlineColor, float extraOffset = 0f)
        {
            Apply(target, content, foreground, outlineColor, Plugin.Instance.Cfg.AfflictionCountdownFontScale.Value * CountdownScale, CountdownVerticalOffset + extraOffset, bottomAnchored: true, minBoxWidth: CountdownMinBoxWidth);
        }

        internal void Hide()
        {
            if (_text != null)
            {
                _text.gameObject.SetActive(false);
            }
            if (_shadow != null)
            {
                _shadow.gameObject.SetActive(false);
            }
        }
    }
}
