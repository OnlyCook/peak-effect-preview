using HarmonyLib;

namespace EffectPreview.Patches
{
    // skips the native rainbow fade-out
    // Ui/InfiniteStaminaDurationVisual already shrinks it to 0
    [HarmonyPatch(typeof(StaminaBar), nameof(StaminaBar.RemoveRainbow))]
    internal static class SuppressRainbowFadeOutPatch
    {
        private static bool Prefix(StaminaBar __instance)
        {
            if (!Plugin.Instance.Cfg.ShowSpecialStatusDurationVisual.Value)
            {
                return true;
            }

            if (__instance.rainbowStamina != null)
            {
                __instance.rainbowStamina.enabled = false;
            }
            return false;
        }
    }
}
