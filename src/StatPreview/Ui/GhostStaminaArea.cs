using UnityEngine;

namespace StatPreview.Ui
{
    // shrinks maxStaminaBar/staminaBar to make room for previewed status increases (poison etc), mirroring GetMaxStamina() = 1 - statusSum
    internal class GhostStaminaArea
    {
        private const float LerpRate = 10f;
        private const float MaxLerpStep = 0.1f;

        private readonly RectTransform _maxRtf;
        private readonly RectTransform _curRtf;
        private bool _shrinking;
        private float _maxDisplayed;
        private float _curDisplayed;

        internal GhostStaminaArea(RectTransform maxStaminaBar, RectTransform staminaBar)
        {
            _maxRtf = maxStaminaBar;
            _curRtf = staminaBar;
        }

        // Unity-null: maxStaminaBar/staminaBar can get destroyed and recreated by the game (observed on item pickup)
        internal bool IsValid => _maxRtf != null && _curRtf != null;

        internal void Apply(float fullLocalWidth, float trueMaxStamina, float trueCurrentStamina, float totalPositiveDelta)
        {
            if (totalPositiveDelta <= 0f)
            {
                Release();
                return;
            }

            float lerpStep = Mathf.Min(Time.deltaTime * LerpRate, MaxLerpStep);
            float targetMax = Mathf.Max(0f, trueMaxStamina - totalPositiveDelta) * fullLocalWidth;
            float targetCur = Mathf.Min(trueCurrentStamina * fullLocalWidth, targetMax);

            if (!_shrinking)
            {
                _maxDisplayed = _maxRtf.sizeDelta.x;
                _curDisplayed = _curRtf.sizeDelta.x;
            }
            _shrinking = true;

            _maxDisplayed = Mathf.Lerp(_maxDisplayed, targetMax, lerpStep);
            _curDisplayed = Mathf.Lerp(_curDisplayed, targetCur, lerpStep);
            _maxRtf.sizeDelta = new Vector2(_maxDisplayed, _maxRtf.sizeDelta.y);
            _curRtf.sizeDelta = new Vector2(_curDisplayed, _curRtf.sizeDelta.y);
        }

        internal void Release()
        {
            _shrinking = false;
        }
    }
}
