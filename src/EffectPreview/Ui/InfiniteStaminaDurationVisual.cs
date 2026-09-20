using UnityEngine;

namespace EffectPreview.Ui
{
    // shrinks the real StaminaBar.rainbowStamina by its stretch anchor to visualize remaining buff duration
    internal class InfiniteStaminaDurationVisual
    {
        private readonly RectTransform _rainbow;

        internal InfiniteStaminaDurationVisual(RectTransform rainbowStamina)
        {
            _rainbow = rainbowStamina;
        }

        internal bool IsValid => _rainbow != null;

        internal void Apply(float remainingFraction, float visibleWidth)
        {
            if (_rainbow == null)
            {
                return;
            }

            float parentWidth = _rainbow.parent is RectTransform parent ? parent.rect.width : 0f;
            if (parentWidth <= 0.01f)
            {
                return;
            }

            // parent (staminaBar) runs away past the visible bar while infiniteStam is active
            float shownWidth = Mathf.Min(parentWidth, visibleWidth);
            Vector2 anchorMax = _rainbow.anchorMax;
            anchorMax.x = Mathf.Clamp01(remainingFraction * shownWidth / parentWidth);
            _rainbow.anchorMax = anchorMax;
        }

        internal void Hide()
        {
            if (_rainbow == null)
            {
                return;
            }

            Vector2 anchorMax = _rainbow.anchorMax;
            if (anchorMax.x < 0.9995f)
            {
                anchorMax.x = 1f;
                _rainbow.anchorMax = anchorMax;
            }
        }
    }
}
