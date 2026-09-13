using UnityEngine;

namespace EffectPreview.Preview
{
    // combines Scout's Ambition's two native timers into one smooth countdown
    internal class InfiniteStaminaUnifiedTimer
    {
        // RadiateInfiniteStam always re-grants a fixed Affliction_InfiniteStamina(1f) (only a floor)
        private const float TrailingGrantSeconds = 1f;

        private bool _everSawRadiate;
        private bool _inTail;
        private float _tailRemaining;
        private float _lastKnownTotal;

        internal float TotalDuration => _lastKnownTotal;

        // returns -1 if never Radiate-driven this session (caller should read the direct affliction instead)
        internal float Tick(bool radiateActive, float radiateTotalTime, float radiateTimeElapsed, bool directActive, float directRemaining)
        {
            float directOrZero = directActive ? Mathf.Max(0f, directRemaining) : 0f;
            float trailing = Mathf.Max(TrailingGrantSeconds, directOrZero);

            if (radiateActive)
            {
                _everSawRadiate = true;
                _inTail = false;
                float radiateRemaining = Mathf.Max(0f, radiateTotalTime - radiateTimeElapsed);
                _lastKnownTotal = radiateTotalTime + trailing;
                return radiateRemaining + trailing;
            }

            if (!_everSawRadiate)
            {
                return -1f;
            }

            if (!_inTail)
            {
                _inTail = true;
                _tailRemaining = TrailingGrantSeconds;
            }

            _tailRemaining = Mathf.Max(0f, _tailRemaining - Time.deltaTime);
            float shown = Mathf.Max(_tailRemaining, directOrZero);
            _lastKnownTotal = Mathf.Max(_lastKnownTotal, shown);
            return shown;
        }

        internal void Reset()
        {
            _everSawRadiate = false;
            _inTail = false;
            _tailRemaining = 0f;
            _lastKnownTotal = 0f;
        }
    }
}
