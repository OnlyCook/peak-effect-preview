using UnityEngine;

namespace EffectPreview.Common
{
    internal static class ColorUtil
    {
        // ripped from SoD
        internal static Color Darken(Color color, float amount = 0.55f)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color darkened = Color.HSVToRGB(h, s, v * (1f - amount));
            darkened.a = color.a;
            return darkened;
        }
    }
}
