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

        private readonly TMP_FontAsset _font;
        private readonly Material _fontMaterial;

        private BarLabel(TextMeshProUGUI text, RectTransform shadow, UnityEngine.UI.Image shadowImage, TMP_FontAsset font, Material fontMaterial)
        {
            _font = font;
            _fontMaterial = fontMaterial;
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
        internal void BringToFront() => _text.transform.SetAsLastSibling();

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
            return new BarLabel(text, shadowRtf, shadowImage, font, fontMaterial);
        }

        // the decimal is its own smaller text on the integer's baseline, so it can carry a thicker outline without touching the integer's
        private const float DecimalScale = 0.66f;
        private const float DecimalOutlineWidth = OutlineWidth / DecimalScale;

        // grows/shrinks the decimal instead of popping it in and out when the room for it changes
        private const float DecimalGrowPerSecond = 6f;

        // the integer+decimal group has to stay at least this far (text-local units) inside the bar's own edges
        private const float DecimalFitMargin = 1f;

        private const float DecimalMeasureFontSize = 100f;

        private bool _sourceIsTransition;
        private int _sourceBefore = int.MinValue;
        private int _sourceAfter = int.MinValue;
        private string _sourceInteger;
        private int _sourceBeforeLength;

        private float _decimalShown;
        private string _spacedText;
        private int _spacedPercent = -1;
        private string _layoutText;
        private float _layoutBoxWidth = -1f;
        private float _layoutFontSize = -1f;

        private float _lastTargetWidth = -1f;

        private readonly TextMeshProUGUI[] _decimals = new TextMeshProUGUI[2];
        private readonly int[] _decimalDigits = { -1, -1 };
        private readonly float[] _digitEmWidths = new float[10];

        private string _measuredContent;
        private float _measuredPreferred;

        // tenths of the same 0-100 "count" scale the mod's status fractions represent (0.105 -> 105 -> "10.5")
        private static int ToTenths(float fraction)
        {
            return Mathf.Max(0, Mathf.RoundToInt(fraction * 1000f));
        }

        private static string IntegerText(int tenths)
        {
            return (tenths / 10).ToString();
        }

        private void SetSource(bool transition, int before, int after)
        {
            if (transition == _sourceIsTransition && before == _sourceBefore && after == _sourceAfter && _sourceInteger != null)
            {
                return;
            }
            _sourceIsTransition = transition;
            _sourceBefore = before;
            _sourceAfter = after;
            string beforeText = IntegerText(before);
            _sourceBeforeLength = beforeText.Length;
            _sourceInteger = transition ? beforeText + " " + ArrowGlyph + " " + IntegerText(after) : beforeText;
            _spacedPercent = -1;
        }

        private int DecimalCount => _sourceIsTransition ? 2 : 1;

        private int DecimalDigit(int index) => (index == 0 ? _sourceBefore : _sourceAfter) % 10;

        // width of ".d" in ems of its own font size, measured once per digit
        private float DigitEmWidth(int digit)
        {
            if (_digitEmWidths[digit] <= 0f)
            {
                TextMeshProUGUI measurer = GetDecimal(0);
                measurer.fontSize = DecimalMeasureFontSize;
                _digitEmWidths[digit] = Mathf.Max(0.01f, measurer.GetPreferredValues("." + digit).x / DecimalMeasureFontSize);
                _decimalDigits[0] = -1;
            }
            return _digitEmWidths[digit];
        }

        // em widths of the gaps left after each integer, relative to the integer's own font size
        private float GapEm(int index, float shown)
        {
            return DigitEmWidth(DecimalDigit(index)) * DecimalScale * shown;
        }

        private static string Space(float em)
        {
            return "<space=" + em.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "em>";
        }

        private string SpacedText(int percent)
        {
            if (percent == _spacedPercent && _spacedText != null)
            {
                return _spacedText;
            }
            _spacedPercent = percent;
            float shown = percent / 100f;
            string beforeText = IntegerText(_sourceBefore) + Space(GapEm(0, shown));
            _spacedText = _sourceIsTransition ? beforeText + " " + ArrowGlyph + " " + IntegerText(_sourceAfter) + Space(GapEm(1, shown)) : beforeText;
            return _spacedText;
        }

        private TextMeshProUGUI GetDecimal(int index)
        {
            if (_decimals[index] != null)
            {
                return _decimals[index];
            }
            GameObject go = new GameObject("EffectPreview BarLabelDecimal", typeof(RectTransform));
            RectTransform rtf = (RectTransform)go.transform;
            rtf.SetParent(_text.transform, worldPositionStays: false);
            rtf.anchorMin = rtf.anchorMax = new Vector2(0.5f, 0.5f);
            rtf.pivot = new Vector2(0f, 0.5f);
            rtf.sizeDelta = Vector2.zero;
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                text.font = _font;
            }
            if (_fontMaterial != null)
            {
                text.fontSharedMaterial = _fontMaterial;
            }
            text.alignment = TextAlignmentOptions.BaselineLeft;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.outlineWidth = DecimalOutlineWidth;
            Common.GhostOwnershipTag.Attach(go);
            go.SetActive(false);
            _decimals[index] = text;
            return text;
        }

        private void HideDecimals()
        {
            for (int i = 0; i < _decimals.Length; i++)
            {
                if (_decimals[i] != null && _decimals[i].gameObject.activeSelf)
                {
                    _decimals[i].gameObject.SetActive(false);
                }
            }
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

            if (afterFraction >= beforeFraction - 0.0005f)
            {
                SetSource(false, ToTenths(beforeFraction), 0);
                Apply(target, _sourceInteger, foreground, outlineColor, scaleMultiplier, decimalSource: true, settledWorldWidth: fitWidthOverride);
                return;
            }

            SetSource(true, ToTenths(beforeFraction), ToTenths(afterFraction));

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
            float requiredWidth = _sourceInteger.Length * EstimatedCharWidthFactor * MinReadableFontSizeForFit;

            if (width >= requiredWidth)
            {
                Apply(target, _sourceInteger, foreground, outlineColor, scaleMultiplier, decimalSource: true, settledWorldWidth: fitWidthOverride);
            }
            else
            {
                SetSource(false, ToTenths(beforeFraction), 0);
                Apply(target, _sourceInteger, foreground, outlineColor, scaleMultiplier, decimalSource: true, settledWorldWidth: fitWidthOverride);
            }
        }

        // settledLocalWidth: the width (target's own local units) the target bar is animating toward, so the decimal can start growing/shrinking
        // together with it instead of only once it got there, -1 when the caller doesn't know it
        internal void ApplyCount(RectTransform target, float fraction, Color foreground, Color outlineColor, float scaleMultiplier, float settledLocalWidth = -1f)
        {
            SetSource(false, ToTenths(fraction), 0);
            Apply(target, _sourceInteger, foreground, outlineColor, scaleMultiplier, decimalSource: true,
                  settledWorldWidth: settledLocalWidth >= 0f && target != null ? settledLocalWidth * target.lossyScale.x : -1f);
        }

        internal void Apply(RectTransform target, string content, Color foreground, Color outlineColor, float scaleMultiplier, float extraVerticalOffset = 0f, bool bottomAnchored = false, float minBoxWidth = 0f, bool decimalSource = false, string narrowContent = null, float settledWorldWidth = -1f)
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
            float boxWidth = Mathf.Max(1f, width - WidthPadding, minBoxWidth);
            _text.rectTransform.sizeDelta = new Vector2(boxWidth, BoxHeight);
            _text.transform.localScale = Vector3.one * Mathf.Max(0.01f, scaleMultiplier);

            Vector3 pos = _text.rectTransform.position;
            pos.x = center.x;
            pos.y = (bottomAnchored ? _cornerBuffer[0].y : center.y + VerticalOffset * _text.transform.localScale.y) + extraVerticalOffset * _text.transform.parent.lossyScale.y;
            _text.rectTransform.position = pos;

            if (Plugin.Instance.Cfg.PlainBarCounts.Value)
            {
                foreground = new Color(1f, 1f, 1f, foreground.a);
                outlineColor = PlainOutline;
            }

            string shown = content;
            bool measured = false;
            float decimalShown = 0f;
            if (decimalSource)
            {
                // the integer keeps exactly the size it'd get alone, the decimal only grows in if the whole group then still fits inside the bar itself
                measured = MeasurePreferred(content);
                float integerFont = MaxFontSize * Mathf.Min(1f, boxWidth / Mathf.Max(0.01f, _measuredPreferred));
                bool fits = false;
                float localPerWorld = 1f / Mathf.Max(0.0001f, _text.transform.lossyScale.x);
                float currentRoom = width * localPerWorld - DecimalFitMargin;
                float integerEm = _measuredPreferred / MaxFontSize;
                float fullGapsEm = GapEm(0, 1f) + (_sourceIsTransition ? GapEm(1, 1f) : 0f);
                float finalFont = integerFont;
                if (Plugin.Instance.Cfg.DecimalBarCounts.Value)
                {
                    float settledWidth = settledWorldWidth >= 0f ? settledWorldWidth : LookaheadWidth(width);
                    // the integer's own size once the bar has settled, same box rule as the current one
                    finalFont = MaxFontSize * Mathf.Min(1f, Mathf.Max(1f, settledWidth - WidthPadding, minBoxWidth) / Mathf.Max(0.01f, _measuredPreferred));
                    fits = (integerEm + fullGapsEm) * finalFont <= settledWidth * localPerWorld - 2f * DecimalFitMargin;
                }
                _decimalShown = Mathf.MoveTowards(_decimalShown, fits ? 1f : 0f, Time.unscaledDeltaTime * DecimalGrowPerSecond);
                decimalShown = _decimalShown;
                int percent = Mathf.RoundToInt(100f * decimalShown);
                float fontSize = integerFont;
                if (fits)
                {
                    // the settled "5.0" layout scaled down to the bar's current width: depends on that width alone, so it only ever grows/shrinks
                    // with the bar, integer and decimal together, and can't overshoot its final size and snap back
                    fontSize = finalFont * Mathf.Min(1f, currentRoom / Mathf.Max(0.01f, (integerEm + fullGapsEm) * finalFont));
                }
                if (percent > 0)
                {
                    shown = SpacedText(percent);
                    float groupEmWidth = integerEm + GapEm(0, percent / 100f) + (_sourceIsTransition ? GapEm(1, percent / 100f) : 0f);
                    if (!fits)
                    {
                        // shrinking out: stay inside the bar's current edges while the decimal leaves
                        fontSize = Mathf.Min(fontSize, currentRoom / groupEmWidth);
                    }
                    boxWidth = Mathf.Max(boxWidth, groupEmWidth * fontSize + 1f);
                    _text.rectTransform.sizeDelta = new Vector2(boxWidth, BoxHeight);
                }
                else
                {
                    decimalShown = 0f;
                }
                // sized directly from the measured width instead of TMP's auto-size, whose stepped search lands on slightly different sizes
                // frame to frame while the bar resizes, visibly wobbling the number
                if (_text.enableAutoSizing)
                {
                    _text.enableAutoSizing = false;
                }
                _text.fontSize = Mathf.Max(MinFontSize, fontSize);
            }
            else if (!_text.enableAutoSizing)
            {
                _text.enableAutoSizing = true;
            }
            if (!decimalSource && narrowContent != null)
            {
                // compared against the target's own width, not the label box, which may be wider than the bar it sits under
                measured = MeasurePreferred(content);
                shown = Mathf.Min(_measuredPreferred, boxWidth) <= width / Mathf.Max(0.0001f, _text.transform.lossyScale.x) ? content : narrowContent;
            }

            // GetPreferredValues overwrites TMP's internal text buffer, so the real text has to be pushed back unconditionally after measuring
            if (measured)
            {
                _text.SetText(shown);
            }
            else
            {
                _text.text = shown;
            }
            _text.color = foreground;
            _text.outlineColor = outlineColor;

            if (decimalShown > 0f)
            {
                ApplyDecimals(shown, boxWidth, _spacedPercent / 100f, foreground, outlineColor);
            }
            else
            {
                HideDecimals();
            }

            if (_shadow != null)
            {
                _shadow.gameObject.SetActive(true);
                float shadowScale = _text.transform.localScale.x;
                _shadow.sizeDelta = new Vector2(_text.renderedWidth + ShadowWidthPadding, ShadowHeight) * shadowScale;
                _shadowImage.pixelsPerUnitMultiplier = Mathf.Max(0.01f, ShadowBorder / (ShadowHeight * shadowScale * 0.5f));
                _shadow.position = _text.rectTransform.position;
            }
        }

        // narrowContent: shorter form used instead whenever content would be wider than the target itself
        internal void ApplyCountdown(RectTransform target, string content, Color foreground, Color outlineColor, float extraOffset = 0f, string narrowContent = null)
        {
            Apply(target, content, foreground, outlineColor, Plugin.Instance.Cfg.AfflictionCountdownFontScale.Value * CountdownScale, CountdownVerticalOffset + extraOffset, bottomAnchored: true, minBoxWidth: CountdownMinBoxWidth, narrowContent: narrowContent);
        }

        // bars ease out a fixed share of the remaining gap per frame, so extending the last frame's resize a few frames ahead starts the decimal
        // a bit earlier while staying short of where the bar settles at any frame rate (no overshoot, nothing to pop back), see RESEARCH.md
        private const float DecimalLookaheadFrames = 3f;

        private float LookaheadWidth(float currentWidth)
        {
            float delta = _lastTargetWidth >= 0f ? currentWidth - _lastTargetWidth : 0f;
            _lastTargetWidth = currentWidth;
            return currentWidth + delta * DecimalLookaheadFrames;
        }

        // returns true when it actually ran a measurement this call (and so clobbered TMP's text buffer)
        private bool MeasurePreferred(string content)
        {
            if (content == _measuredContent)
            {
                return false;
            }
            _measuredContent = content;
            // normalized to fontSizeMax, GetPreferredValues measures at the current fontSize once auto-size is off
            float measureSize = _text.enableAutoSizing ? _text.fontSizeMax : _text.fontSize;
            _measuredPreferred = _text.GetPreferredValues(content).x * MaxFontSize / Mathf.Max(0.01f, measureSize);
            return true;
        }

        // each decimal sits in the gap after its integer, read off the integer's own laid-out glyphs so it shares its baseline
        private void ApplyDecimals(string shown, float boxWidth, float decimalShown, Color foreground, Color outlineColor)
        {
            if (!ReferenceEquals(shown, _layoutText) || Mathf.Abs(boxWidth - _layoutBoxWidth) > 0.01f || !Mathf.Approximately(_text.fontSize, _layoutFontSize))
            {
                _layoutFontSize = _text.fontSize;
                // an ancestor can be transiently inactive here, which ForceMeshUpdate would otherwise silently skip
                _text.ForceMeshUpdate(ignoreActiveState: true);
                bool laidOut = _text.textInfo.characterCount > 0;
                _layoutText = laidOut ? shown : null;
                _layoutBoxWidth = boxWidth;
            }

            // TMP centers on the last visible glyph, so the trailing gap (no glyph after it) is left out and has to be re-centered by hand
            float trailingGap = GapEm(DecimalCount - 1, decimalShown) * _text.fontSize;
            Vector3 pos = _text.rectTransform.position;
            pos.x -= trailingGap * 0.5f * _text.transform.lossyScale.x;
            _text.rectTransform.position = pos;

            TMP_TextInfo info = _text.textInfo;
            for (int i = 0; i < _decimals.Length; i++)
            {
                int charIndex = i == 0 ? _sourceBeforeLength - 1 : info.characterCount - 1;
                if (i >= DecimalCount || charIndex < 0 || charIndex >= info.characterCount)
                {
                    if (_decimals[i] != null && _decimals[i].gameObject.activeSelf)
                    {
                        _decimals[i].gameObject.SetActive(false);
                    }
                    continue;
                }

                TextMeshProUGUI dec = GetDecimal(i);
                int digit = DecimalDigit(i);
                if (digit != _decimalDigits[i])
                {
                    _decimalDigits[i] = digit;
                    dec.SetText("." + digit);
                }
                if (!dec.gameObject.activeSelf)
                {
                    dec.gameObject.SetActive(true);
                }
                dec.fontSize = _text.fontSize * DecimalScale * decimalShown;
                dec.color = foreground;
                dec.outlineColor = outlineColor;
                TMP_CharacterInfo character = info.characterInfo[charIndex];
                dec.rectTransform.anchoredPosition = new Vector2(character.xAdvance, character.baseLine);
            }
        }

        internal void Hide()
        {
            if (_text != null)
            {
                HideDecimals();
                _lastTargetWidth = -1f;
                _text.gameObject.SetActive(false);
            }
            if (_shadow != null)
            {
                _shadow.gameObject.SetActive(false);
            }
        }
    }
}
