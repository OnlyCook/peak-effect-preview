using UnityEngine;
using UnityEngine.UI;

namespace StatPreview.Ui
{
    // splits a real BarAffliction badge into a shrunk real part plus a ghost
    // sibling in the same layout group, own state tracked privately since
    // AddStamina's per-tick ChangeBar() keeps stomping BarAffliction.size
    internal class GhostBadge
    {
        private const float LerpRate = 10f;
        private const float MaxLerpStep = 0.1f;

        private readonly RectTransform _realRtf;
        private readonly GameObject _realIcon;
        private readonly RectTransform _ghostRtf;
        private bool _realShrinking;
        private float _realDisplayedWidth;
        private bool _ghostWasVisible;

        private GhostBadge(RectTransform realRtf, GameObject realIcon, RectTransform ghostRtf)
        {
            _realRtf = realRtf;
            _realIcon = realIcon;
            _ghostRtf = ghostRtf;
        }

        internal bool IsValid => _realRtf != null && _ghostRtf != null;

        internal static GhostBadge Create(BarAffliction realAffliction)
        {
            RectTransform realBadge = realAffliction.rtf;
            GameObject go = Object.Instantiate(realBadge.gameObject, realBadge.parent);
            go.name = realBadge.name + " (StatPreview Ghost)";

            BarAffliction driver = go.GetComponent<BarAffliction>();
            if (driver != null)
            {
                if (driver.icon != null)
                {
                    driver.icon.gameObject.SetActive(false);
                }
                Object.Destroy(driver);
            }

            foreach (Image image in go.GetComponentsInChildren<Image>(includeInactive: true))
            {
                Color c = Color.Lerp(image.color, Color.white, 0.4f);
                c.a = 0.65f;
                image.color = c;
            }

            go.transform.SetSiblingIndex(realBadge.GetSiblingIndex() + 1);
            go.SetActive(false);

            GameObject realIcon = realAffliction.icon != null ? realAffliction.icon.gameObject : null;
            return new GhostBadge(realBadge, realIcon, go.GetComponent<RectTransform>());
        }

        internal void Apply(float fullLocalWidth, float liveValue, float delta)
        {
            float ghostMagnitude = delta < 0f ? Mathf.Min(-delta, liveValue) : delta;
            if (ghostMagnitude <= 0f)
            {
                Hide();
                return;
            }

            float lerpStep = Mathf.Min(Time.deltaTime * LerpRate, MaxLerpStep);

            if (delta < 0f)
            {
                float remaining = Mathf.Max(0f, liveValue - ghostMagnitude);
                float targetWidth = fullLocalWidth * remaining;

                if (!_realShrinking)
                {
                    _realDisplayedWidth = _realRtf.sizeDelta.x;
                }
                _realShrinking = true;

                _realDisplayedWidth = Mathf.Lerp(_realDisplayedWidth, targetWidth, lerpStep);
                _realRtf.sizeDelta = new Vector2(_realDisplayedWidth, _realRtf.sizeDelta.y);

                if (_realIcon != null)
                {
                    _realIcon.SetActive(remaining > 0.001f);
                }
            }

            if (!_ghostWasVisible)
            {
                _ghostRtf.sizeDelta = new Vector2(0f, _ghostRtf.sizeDelta.y);
            }
            _ghostWasVisible = true;

            float ghostTargetWidth = fullLocalWidth * ghostMagnitude;
            float ghostCurrentWidth = _ghostRtf.sizeDelta.x;
            _ghostRtf.transform.SetSiblingIndex(_realRtf.GetSiblingIndex() + 1);
            _ghostRtf.sizeDelta = new Vector2(Mathf.Lerp(ghostCurrentWidth, ghostTargetWidth, lerpStep), _ghostRtf.sizeDelta.y);
            _ghostRtf.gameObject.SetActive(true);

            RectTransform parent = _realRtf.parent as RectTransform;
            if (parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }
        }

        internal void Hide()
        {
            _realShrinking = false;
            _ghostWasVisible = false;
            if (_ghostRtf != null)
            {
                _ghostRtf.gameObject.SetActive(false);
            }
            if (_realIcon != null)
            {
                _realIcon.SetActive(true);
            }
        }
    }
}
