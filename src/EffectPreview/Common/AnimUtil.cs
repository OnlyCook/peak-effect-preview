using UnityEngine;

namespace EffectPreview.Common
{
    internal static class AnimUtil
    {
        private const float ReferenceFps = 100f;

        // shared dotween grow/shrink durations so every ghost bar's own-fill animation stays in lockstep, no matter which bar drives it
        internal const float TweenShowDuration = 0.46f;
        internal const float TweenHideDuration = 0.36f;

        // step100Fps: fraction of the remaining gap to close in one frame at 100fps
        // this makes it bound by delta time instead of frames
        internal static float LerpStep(float step100Fps)
        {
            return 1f - Mathf.Pow(1f - step100Fps, Time.deltaTime * ReferenceFps);
        }
    }
}
