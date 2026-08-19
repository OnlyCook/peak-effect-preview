using UnityEngine;
using UnityEngine.UI;

namespace StatPreview.Ui
{
    // splits a real BarAffliction badge into a shrunk real part plus up to two
    // ghost siblings in the same layout group (one for a decrease, one for an
    // increase, shown independently rather than netted against each other),
    // own state tracked privately since AddStamina's per-tick ChangeBar() keeps
    // stomping BarAffliction.size
    internal class GhostBadge
    {
        private const float LerpRate = 10f;
        private const float MaxLerpStep = 0.1f;

        private readonly RectTransform _realRtf;
        private readonly GameObject _realIcon;
        private readonly Strip _decreaseGhost;
        private readonly Strip _increaseGhost;
        private bool _realShrinking;
        private float _realDisplayedWidth;

        private GhostBadge(RectTransform realRtf, GameObject realIcon, Strip decreaseGhost, Strip increaseGhost)
        {
            _realRtf = realRtf;
            _realIcon = realIcon;
            _decreaseGhost = decreaseGhost;
            _increaseGhost = increaseGhost;
        }

        internal bool IsValid => _realRtf != null && _decreaseGhost.IsValid && _increaseGhost.IsValid;

        internal static GhostBadge Create(BarAffliction realAffliction)
        {
            RectTransform realBadge = realAffliction.rtf;
            Strip decreaseGhost = Strip.CloneFrom(realBadge, realBadge);
            Strip increaseGhost = Strip.CloneFrom(realBadge, decreaseGhost.Rtf);

            GameObject realIcon = realAffliction.icon != null ? realAffliction.icon.gameObject : null;
            return new GhostBadge(realBadge, realIcon, decreaseGhost, increaseGhost);
        }

        internal void Apply(float fullLocalWidth, float liveValue, float decreaseAmount, float increaseAmount)
        {
            float lerpStep = Mathf.Min(Time.deltaTime * LerpRate, MaxLerpStep);
            float shrinkMagnitude = Mathf.Min(decreaseAmount, liveValue);

            if (shrinkMagnitude > 0f)
            {
                float remaining = Mathf.Max(0f, liveValue - shrinkMagnitude);
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
            else if (_realShrinking)
            {
                _realShrinking = false;
                if (_realIcon != null)
                {
                    _realIcon.SetActive(true);
                }
            }

            // the part of the increase that lands within the space the decrease already vacated doesn't need its own extra room
            // only the pure removal (what's gone for good) and the pure increase (what grows beyond the current width)
            // actually need separate space
            float overlap = Mathf.Min(shrinkMagnitude, increaseAmount);
            _decreaseGhost.Apply(fullLocalWidth * (shrinkMagnitude - overlap), lerpStep);
            _increaseGhost.Apply(fullLocalWidth * increaseAmount, lerpStep);

            RectTransform parent = _realRtf.parent as RectTransform;
            if (parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }
        }

        internal void Hide()
        {
            _realShrinking = false;
            _decreaseGhost.Hide();
            _increaseGhost.Hide();
            if (_realIcon != null)
            {
                _realIcon.SetActive(true);
            }
        }

        // one cloned, tinted badge sibling whose width is driven independently
        private readonly struct Strip
        {
            internal readonly RectTransform Rtf;

            private Strip(RectTransform rtf)
            {
                Rtf = rtf;
            }

            internal bool IsValid => Rtf != null;

            internal static Strip CloneFrom(RectTransform sourceBadge, Transform insertAfter)
            {
                GameObject go = Object.Instantiate(sourceBadge.gameObject, sourceBadge.parent);
                go.name = sourceBadge.name + " (StatPreview Ghost)";

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

                go.transform.SetSiblingIndex(insertAfter.GetSiblingIndex() + 1);
                go.SetActive(false);
                return new Strip(go.GetComponent<RectTransform>());
            }

            internal void Apply(float targetWidth, float lerpStep)
            {
                if (targetWidth <= 0f)
                {
                    Hide();
                    return;
                }

                float current = Rtf.gameObject.activeSelf ? Rtf.sizeDelta.x : 0f;
                Rtf.sizeDelta = new Vector2(Mathf.Lerp(current, targetWidth, lerpStep), Rtf.sizeDelta.y);
                Rtf.gameObject.SetActive(true);
            }

            internal void Hide()
            {
                if (Rtf != null)
                {
                    Rtf.gameObject.SetActive(false);
                }
            }
        }
    }
}
