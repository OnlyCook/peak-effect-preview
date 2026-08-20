using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // splits a real BarAffliction badge into a shrunk real part plus decrease/increase ghost siblings, shown independently rather than netted together
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
            Strip decreaseGhost = Strip.CloneFrom(realBadge, realBadge, isRemoval: true);
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

            // only the pure removal and the pure increase beyond it need their own separate room
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
        // isRemoval pulses alpha subtly (via the shared, globally-synced RemovalPulse clock) so a decrease reads differently from an increase
        private readonly struct Strip
        {
            internal readonly RectTransform Rtf;
            private readonly Image[] _images;
            private readonly float[] _baseAlphas;
            private readonly bool _isRemoval;

            private Strip(RectTransform rtf, Image[] images, float[] baseAlphas, bool isRemoval)
            {
                Rtf = rtf;
                _images = images;
                _baseAlphas = baseAlphas;
                _isRemoval = isRemoval;
            }

            internal bool IsValid => Rtf != null;

            internal static Strip CloneFrom(RectTransform sourceBadge, Transform insertAfter, bool isRemoval = false)
            {
                GameObject go = Object.Instantiate(sourceBadge.gameObject, sourceBadge.parent);
                go.name = sourceBadge.name + " (EffectPreview Ghost)";

                BarAffliction driver = go.GetComponent<BarAffliction>();
                if (driver != null)
                {
                    if (driver.icon != null)
                    {
                        driver.icon.gameObject.SetActive(false);
                    }
                    Object.Destroy(driver);
                }

                Image[] images = go.GetComponentsInChildren<Image>(includeInactive: true);
                float[] baseAlphas = new float[images.Length];
                for (int i = 0; i < images.Length; i++)
                {
                    Color c = Color.Lerp(images[i].color, Color.white, 0.4f);
                    c.a = 0.65f;
                    images[i].color = c;
                    baseAlphas[i] = c.a;
                }

                go.transform.SetSiblingIndex(insertAfter.GetSiblingIndex() + 1);
                go.SetActive(false);
                return new Strip(go.GetComponent<RectTransform>(), images, baseAlphas, isRemoval);
            }

            internal void Apply(float targetWidth, float lerpStep)
            {
                float current = Rtf.gameObject.activeSelf ? Rtf.sizeDelta.x : 0f;

                // lerp toward 0 before deactivating instead of snapping off instantly; a few px is already invisible
                if (targetWidth <= 0f && current < 4f)
                {
                    Hide();
                    return;
                }

                float newWidth = Mathf.Lerp(current, Mathf.Max(0f, targetWidth), lerpStep);
                Rtf.sizeDelta = new Vector2(newWidth, Rtf.sizeDelta.y);
                Rtf.gameObject.SetActive(true);

                if (_isRemoval)
                {
                    RemovalPulse.Apply(_images, _baseAlphas);
                }
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
