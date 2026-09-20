using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // clone of the bonus stamina border squashed flat below its slot, its fill shrinking right to left with the speed boost's remaining time
    internal class SpeedBoostDurationLine
    {
        private const float LineHeight = 17f;
        private const float LineGap = 8f; // visible edge 2px off; so LineGap+2 = actual gap
        private const float FillInsetX = 7f;
        private const float FillInsetY = 5f;
        private const float FillAlpha = 0.9f;
        private const float LabelGap = 10f;
        private const float LabelWidth = 56f;

        private const int FillSpriteSize = 12;
        private const float FillCornerRadius = 4f;

        private static Sprite _fillSprite;

        private static readonly Color LabelOutline = new Color(0.2f, 0.2f, 0.2f, 1f);

        private readonly RectTransform _widthSource;
        private readonly RectTransform _slotSource;
        private readonly RectTransform _line;
        private readonly RectTransform _fill;
        private readonly RectTransform _labelAnchor;
        private readonly BarLabel _label;
        private readonly Vector3[] _corners = new Vector3[4];

        private SpeedBoostDurationLine(RectTransform widthSource, RectTransform slotSource, RectTransform line, RectTransform fill, RectTransform labelAnchor, BarLabel label)
        {
            _fill = fill;
            _widthSource = widthSource;
            _slotSource = slotSource;
            _line = line;
            _labelAnchor = labelAnchor;
            _label = label;
        }

        internal bool IsValid => _widthSource != null && _slotSource != null && _line != null && _fill != null && _labelAnchor != null && _label != null && _label.IsValid;

        internal static SpeedBoostDurationLine Create(RectTransform widthSource, RectTransform bonusOutline, Transform parent, TMPro.TMP_FontAsset font, Material fontMaterial)
        {
            if (widthSource == null || bonusOutline == null || parent == null || font == null)
            {
                return null;
            }

            GameObject lineGo = Object.Instantiate(bonusOutline.gameObject, parent, worldPositionStays: false);
            lineGo.name = "EffectPreview SpeedBoost Line";
            RectTransform line = (RectTransform)lineGo.transform;
            line.anchorMin = (line.anchorMax = new Vector2(0.5f, 0.5f));
            line.pivot = new Vector2(0f, 1f);
            LayoutElement lineLayout = lineGo.GetComponent<LayoutElement>();
            if (lineLayout == null)
            {
                lineLayout = lineGo.AddComponent<LayoutElement>();
            }
            lineLayout.ignoreLayout = true;
            foreach (Graphic graphic in lineGo.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
            Image borderImage = lineGo.GetComponent<Image>();
            Color fillColor = borderImage != null ? borderImage.color : Color.white;
            fillColor.a = FillAlpha;
            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform fill = (RectTransform)fillGo.transform;
            fill.SetParent(line, worldPositionStays: false);
            fill.SetAsFirstSibling();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = new Vector2(0f, -2f * FillInsetY);
            Image fillImage = fillGo.GetComponent<Image>();
            fillImage.sprite = GetFillSprite();
            fillImage.type = Image.Type.Sliced;
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
            Common.GhostOwnershipTag.Attach(fillGo);
            Common.GhostOwnershipTag.Attach(lineGo);
            lineGo.SetActive(false);

            GameObject anchorGo = new GameObject("EffectPreview SpeedBoost Label Anchor", typeof(RectTransform), typeof(LayoutElement));
            RectTransform anchor = (RectTransform)anchorGo.transform;
            anchor.SetParent(parent, worldPositionStays: false);
            anchor.anchorMin = (anchor.anchorMax = new Vector2(0.5f, 0.5f));
            anchor.pivot = new Vector2(0f, 1f);
            anchor.sizeDelta = new Vector2(LabelWidth, LineHeight);
            anchorGo.GetComponent<LayoutElement>().ignoreLayout = true;
            Common.GhostOwnershipTag.Attach(anchorGo);
            anchorGo.SetActive(false);

            BarLabel label = BarLabel.Create(parent, font, fontMaterial);
            return new SpeedBoostDurationLine(widthSource, bonusOutline, line, fill, anchor, label);
        }

        private static Sprite GetFillSprite()
        {
            if (_fillSprite != null)
            {
                return _fillSprite;
            }

            Texture2D tex = new Texture2D(FillSpriteSize, FillSpriteSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float half = FillSpriteSize * 0.5f;
            for (int y = 0; y < FillSpriteSize; y++)
            {
                for (int x = 0; x < FillSpriteSize; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - FillCornerRadius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - FillCornerRadius), 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(FillCornerRadius - distance + 0.5f)));
                }
            }
            tex.Apply();
            _fillSprite = Sprite.Create(tex, new Rect(0, 0, FillSpriteSize, FillSpriteSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(FillCornerRadius, FillCornerRadius, FillCornerRadius, FillCornerRadius));
            return _fillSprite;
        }

        internal void Apply(float remainingFraction, string labelText, bool showLine, bool showLabel, float fontScale)
        {
            if (_widthSource == null || _slotSource == null || _line == null)
            {
                return;
            }

            Transform parent = _line.parent;
            _slotSource.GetWorldCorners(_corners);
            float top = parent.InverseTransformPoint(_corners[0]).y - LineGap;
            _widthSource.GetWorldCorners(_corners);
            Vector3 topLeft = parent.InverseTransformPoint(_corners[1]);
            Vector3 topRight = parent.InverseTransformPoint(_corners[2]);
            float width = topRight.x - topLeft.x;

            bool lineVisible = showLine && width > 0.5f;
            _line.gameObject.SetActive(lineVisible);
            if (lineVisible)
            {
                _line.localPosition = new Vector3(topLeft.x, top, 0f);
                _line.sizeDelta = new Vector2(width, LineHeight);
                _fill.anchoredPosition = new Vector2(FillInsetX, 0f);
                _fill.sizeDelta = new Vector2(Mathf.Max(0f, (width - 2f * FillInsetX) * Mathf.Clamp01(remainingFraction)), -2f * FillInsetY);
                _line.SetAsLastSibling();
            }

            if (showLabel && !string.IsNullOrEmpty(labelText))
            {
                _labelAnchor.gameObject.SetActive(true);
                _labelAnchor.localPosition = new Vector3(topRight.x + LabelGap, top, 0f);
                _label.Apply(_labelAnchor, labelText, Color.white, LabelOutline, fontScale);
            }
            else
            {
                _labelAnchor.gameObject.SetActive(false);
                _label.Hide();
            }
        }

        internal void Hide()
        {
            if (_line != null)
            {
                _line.gameObject.SetActive(false);
            }
            if (_labelAnchor != null)
            {
                _labelAnchor.gameObject.SetActive(false);
            }
            _label?.Hide();
        }
    }
}
