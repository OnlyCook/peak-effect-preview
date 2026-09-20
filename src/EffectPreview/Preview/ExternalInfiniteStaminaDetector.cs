using UnityEngine;

namespace EffectPreview.Preview
{
    // flags infinite stamina that keeps getting re-granted by another player (Scout's Ambition aura, Bugle of Friendship)
    internal class ExternalInfiniteStaminaDetector
    {
        private const int ResetsToFlag = 2;

        private float _lastElapsed = -1f;
        private int _resets;

        internal bool IsExternal => _resets >= ResetsToFlag;

        internal void Tick(bool directActive, float timeElapsed)
        {
            if (!directActive)
            {
                _lastElapsed = -1f;
                return;
            }

            if (_lastElapsed >= 0f && timeElapsed < _lastElapsed - 0.0005f)
            {
                _resets++;
            }
            _lastElapsed = timeElapsed;
        }

        internal void Reset()
        {
            _lastElapsed = -1f;
            _resets = 0;
        }
    }
}
