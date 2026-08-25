using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    internal class GhostStatusCapIcon
    {
        // gap between the two icons (as a fraction of one icon's side length) when showing both
        private const float GapRatio = 0.15f;
        private const float ThicknessRatio = 0.16f;

        private readonly RectTransform _realIconRtf;
        private readonly Image _realIconImage;
        private readonly Vector2 _realIconRestPosition;
        private readonly RectTransform _ghostRtf;
        private readonly RectTransform _ghostStrikethroughRtf;

        private GhostStatusCapIcon(RectTransform realIconRtf, Image realIconImage, RectTransform ghostRtf, RectTransform ghostStrikethroughRtf)
        {
            _realIconRtf = realIconRtf;
            _realIconImage = realIconImage;
            _realIconRestPosition = realIconRtf.anchoredPosition;
            _ghostRtf = ghostRtf;
            _ghostStrikethroughRtf = ghostStrikethroughRtf;
        }

        internal bool IsValid => _realIconRtf != null && _ghostRtf != null;

        internal static GhostStatusCapIcon Create(Image realIcon)
        {
            if (realIcon == null)
            {
                return null;
            }

            GameObject go = Object.Instantiate(realIcon.gameObject, realIcon.transform.parent);
            go.name = realIcon.name + " (EffectPreview CapGhost)";
            Image ghostImage = go.GetComponent<Image>();

            // reuses the icon's own sprite/color instead of a generic marker
            Color tint = Color.Lerp(ghostImage.color, Color.white, 0.4f);
            tint.a *= 0.65f;
            ghostImage.color = tint;

            RectTransform ghostRtf = go.GetComponent<RectTransform>();
            RectTransform strikethroughRtf = CreateStrikethrough(ghostRtf, ghostImage.color);

            go.transform.localScale = Vector3.one;
            go.SetActive(false);

            return new GhostStatusCapIcon(realIcon.rectTransform, realIcon, ghostRtf, strikethroughRtf);
        }

        // fullyRemovable: live status <= this item's removal cap - hide the real icon, show only the struck-through
        // ghost at the icon's natural position. Otherwise (partial): show both, centered as a pair on that position
        internal void Apply(bool fullyRemovable)
        {
            float side = Mathf.Min(_realIconRtf.rect.width, _realIconRtf.rect.height);
            if (side <= 0f)
            {
                return;
            }
            if (_ghostStrikethroughRtf != null)
            {
                _ghostStrikethroughRtf.sizeDelta = new Vector2(side * 1.41421356f, side * ThicknessRatio);
            }

            _ghostRtf.gameObject.SetActive(true);
            if (fullyRemovable)
            {
                if (_realIconImage != null)
                {
                    _realIconImage.enabled = false;
                }
                _realIconRtf.anchoredPosition = _realIconRestPosition;
                _ghostRtf.anchoredPosition = _realIconRestPosition;
            }
            else
            {
                float offset = side * (0.5f + GapRatio * 0.5f);
                if (_realIconImage != null)
                {
                    _realIconImage.enabled = true;
                }
                _realIconRtf.anchoredPosition = _realIconRestPosition - new Vector2(offset, 0f);
                _ghostRtf.anchoredPosition = _realIconRestPosition + new Vector2(offset, 0f);
            }
        }

        internal void Hide()
        {
            if (_realIconImage != null)
            {
                _realIconImage.enabled = true;
            }
            if (_realIconRtf != null)
            {
                _realIconRtf.anchoredPosition = _realIconRestPosition;
            }
            if (_ghostRtf != null)
            {
                _ghostRtf.gameObject.SetActive(false);
            }
        }

        // parented under the ghost icon itself so it always shares its position/scale, sized fresh every Apply
        private static RectTransform CreateStrikethrough(RectTransform iconRtf, Color tintColor)
        {
            GameObject go = new GameObject("GhostStrikethrough", typeof(RectTransform), typeof(Image));
            RectTransform rtf = (RectTransform)go.transform;
            rtf.SetParent(iconRtf, worldPositionStays: false);
            rtf.anchorMin = (rtf.anchorMax = (rtf.pivot = new Vector2(0.5f, 0.5f)));
            rtf.anchoredPosition = Vector2.zero;
            rtf.localRotation = Quaternion.Euler(0f, 0f, 45f);

            Image image = go.GetComponent<Image>();
            image.color = tintColor;
            image.raycastTarget = false;
            return rtf;
        }
    }
}
