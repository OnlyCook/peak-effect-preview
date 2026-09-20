using Peak.Afflictions;
using UnityEngine;

namespace EffectPreview.Preview
{
    // tracks a climb-grace countdown ourselves since Affliction.bonusTime is protected
    internal class InfiniteStaminaGraceTracker
    {
        private float _trackedOriginalGrace = -1f;
        private float _trackedElapsed;

        // true while timeElapsed is still 0 (grace not yet used up)
        internal bool TryGetRemainingGrace(Affliction affliction, float climbDelay, out float remainingGraceSeconds)
        {
            remainingGraceSeconds = 0f;

            if (affliction == null || affliction.timeElapsed > 0.0005f || climbDelay <= 0.0005f)
            {
                Reset();
                return false;
            }

            if (_trackedOriginalGrace < 0f || !Mathf.Approximately(_trackedOriginalGrace, climbDelay))
            {
                _trackedOriginalGrace = climbDelay;
                _trackedElapsed = 0f;
            }

            _trackedElapsed += Time.deltaTime;
            remainingGraceSeconds = Mathf.Max(0f, _trackedOriginalGrace - _trackedElapsed);
            return true;
        }

        internal void Reset()
        {
            _trackedOriginalGrace = -1f;
            _trackedElapsed = 0f;
        }
    }
}
