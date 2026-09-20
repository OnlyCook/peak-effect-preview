using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // crops the real invincibility border to its left remainingFraction, revealing the plain border underneath
    internal class InvincibilityBorderVisual
    {
        private readonly Image _borderImage;
        private readonly RectTransform _borderRtf;
        private readonly Transform _originalParent;
        private readonly int _originalSiblingIndex;

        private RectTransform _wrapperRtf;
        private float _wrapperFullWidth;
        private bool _wrapped;

        private readonly Vector3[] _corners = new Vector3[4];

        internal InvincibilityBorderVisual(RectTransform shieldTransform)
        {
            if (shieldTransform == null)
            {
                return;
            }

            foreach (Image img in shieldTransform.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject == shieldTransform.gameObject)
                {
                    continue;
                }
                _borderImage = img;
                _borderRtf = img.rectTransform;
                break;
            }

            if (_borderRtf == null)
            {
                return;
            }

            _originalParent = _borderRtf.parent;
            _originalSiblingIndex = _borderRtf.GetSiblingIndex();
        }

        internal bool IsValid => _borderRtf == null || (_borderRtf != null && (!_wrapped || _wrapperRtf != null));

        internal void Apply(float remainingFraction)
        {
            if (_borderRtf == null)
            {
                return;
            }

            if (remainingFraction >= 0.9995f)
            {
                Hide();
                return;
            }

            EnsureWrapped();
            _wrapperRtf.sizeDelta = new Vector2(_wrapperFullWidth * Mathf.Clamp01(remainingFraction), _wrapperRtf.sizeDelta.y);
        }

        // lazy: shield sits inactive whenever not invincible, and adding components under an inactive hierarchy defers Awake()
        private void EnsureWrapped()
        {
            if (_wrapped)
            {
                return;
            }

            _borderRtf.GetWorldCorners(_corners);
            Vector3 leftCenterWorld = new Vector3(_corners[0].x, (_corners[0].y + _corners[1].y) * 0.5f, _corners[0].z);
            Vector3 bottomLeft = _originalParent.InverseTransformPoint(_corners[0]);
            Vector3 topRight = _originalParent.InverseTransformPoint(_corners[2]);
            float width = topRight.x - bottomLeft.x;
            float height = topRight.y - bottomLeft.y;
            Vector2 ownSize = _borderRtf.rect.size;

            GameObject wrapperGo = new GameObject(_borderRtf.name + " (EffectPreview Reveal Mask)", typeof(RectTransform));
            _wrapperRtf = (RectTransform)wrapperGo.transform;
            _wrapperRtf.SetParent(_originalParent, worldPositionStays: false);
            _wrapperRtf.SetSiblingIndex(_originalSiblingIndex);
            _wrapperRtf.anchorMin = (_wrapperRtf.anchorMax = new Vector2(0f, 0.5f));
            _wrapperRtf.pivot = new Vector2(0f, 0.5f);
            _wrapperRtf.position = leftCenterWorld;
            _wrapperRtf.sizeDelta = new Vector2(width, height);
            wrapperGo.AddComponent<RectMask2D>();
            Common.GhostOwnershipTag.Attach(wrapperGo);

            _wrapperFullWidth = width;

            _borderRtf.SetParent(_wrapperRtf, worldPositionStays: false);
            _borderRtf.anchorMin = (_borderRtf.anchorMax = new Vector2(0f, 0.5f));
            _borderRtf.pivot = new Vector2(0f, 0.5f);
            _borderRtf.anchoredPosition = Vector2.zero;
            _borderRtf.sizeDelta = ownSize;

            _wrapped = true;
        }

        internal void Hide()
        {
            if (_borderRtf == null || !_wrapped)
            {
                return;
            }

            _borderRtf.SetParent(_originalParent, worldPositionStays: true);
            _borderRtf.SetSiblingIndex(_originalSiblingIndex);

            if (_wrapperRtf != null)
            {
                Object.Destroy(_wrapperRtf.gameObject);
            }
            _wrapperRtf = null;
            _wrapped = false;
        }
    }
}
