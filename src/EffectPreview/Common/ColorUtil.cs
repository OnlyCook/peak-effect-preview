using UnityEngine;

namespace EffectPreview.Common
{
    internal static class ColorUtil
    {
        internal static float Luminance(Color color)
        {
            Color linear = color.linear;
            return 0.2126f * linear.r + 0.7152f * linear.g + 0.0722f * linear.b;
        }

        internal static float ContrastRatio(Color a, Color b)
        {
            float la = Luminance(a);
            float lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }
    }
}
