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

        // ghost text needs to stay legible over the HUD, so it's tinted lighter but far less transparent than the ghost bars themselves
        private const float GhostAlpha = 0.85f;

        // "before -> after" is only shown if it would still fit at a legible size - below that, the arrow/after half is dropped rather than shrunk further
        private const float MinReadableFontSizeForFit = 13f;
        private const float EstimatedCharWidthFactor = 0.55f;

        // game doesnt carry any arrow symbol directly, but TMP somehow resolves it so we don't touch it
        private const string ArrowGlyph = "→";

        private readonly TextMeshProUGUI _text;
        private readonly Vector3[] _cornerBuffer = new Vector3[4];

        private BarLabel(TextMeshProUGUI text)
        {
            _text = text;
        }

        internal bool IsValid => _text != null;

        // font/fontMaterial: the game's own TMP font asset/material (e.g. StaminaBar.moraleBoostText's), so this reads as native UI rather than a mod font
        internal static BarLabel Create(Transform parent, TMP_FontAsset font, Material fontMaterial)
        {
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

            go.SetActive(false);
            return new BarLabel(text);
        }

        // truncates to the same 0-100 "count" scale the mod's status fractions represent (0.10 -> "10")
        internal static string FormatCount(float fraction)
        {
            return Mathf.FloorToInt(fraction * 100f).ToString();
        }

        // lightly translucent version of a raw fill color, so a ghost label still reads as "ghost" without losing legibility the way the ghost bars can
        internal static Color GhostTint(Color color)
        {
            Color c = Color.Lerp(color, Color.white, 0.4f);
            c.a = GhostAlpha;
            return c;
        }

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

        internal void Apply(RectTransform target, string content, Color foreground, Color outlineColor, float scaleMultiplier)
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
            _text.rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, width - WidthPadding), BoxHeight);
            _text.transform.localScale = Vector3.one * Mathf.Max(0.01f, scaleMultiplier);

            Vector3 pos = _text.rectTransform.position;
            pos.x = center.x;
            pos.y = center.y + VerticalOffset;
            _text.rectTransform.position = pos;

            _text.text = content;
            _text.color = foreground;
            _text.outlineColor = outlineColor;
        }

        internal void Hide()
        {
            if (_text != null)
            {
                _text.gameObject.SetActive(false);
            }
        }
    }
}
