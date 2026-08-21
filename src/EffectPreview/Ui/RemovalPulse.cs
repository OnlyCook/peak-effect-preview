using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // subtle, 'non-threatening' alpha pulse distinguishing a removal ghost bar
    //driven by global timer so its unified
    internal static class RemovalPulse
    {
        private const float MinAlpha = 0.2f;

        private const float HoldAtFullDuration = 1.6f;
        private const float TransitionDuration = 0.3f;
        private const float HoldAtFloorDuration = 0.2f;
        private const float CycleDuration = HoldAtFullDuration + TransitionDuration + HoldAtFloorDuration + TransitionDuration;

        internal static void Apply(Image[] images, float[] baseAlphas)
        {
            // disabled: hold at fully visible instead of pulsing, rather than skip the call entirely, so a mid-pulse toggle doesn't freeze at a dim frame
            float factor = Plugin.Instance.Cfg.EnableRemovalBlink.Value ? Factor(Time.time % CycleDuration) : 1f;

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null)
                {
                    continue;
                }
                float floor = Mathf.Min(MinAlpha, baseAlphas[i]);
                Color c = images[i].color;
                c.a = Mathf.Lerp(floor, baseAlphas[i], factor);
                images[i].color = c;
            }
        }

        // 1 = fully visible; 0 = at the floor
        private static float Factor(float t)
        {
            if (t < HoldAtFullDuration)
            {
                return 1f;
            }
            t -= HoldAtFullDuration;
            if (t < TransitionDuration)
            {
                return 1f - Smooth(t / TransitionDuration);
            }
            t -= TransitionDuration;
            if (t < HoldAtFloorDuration)
            {
                return 0f;
            }
            t -= HoldAtFloorDuration;
            return Smooth(t / TransitionDuration);
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
