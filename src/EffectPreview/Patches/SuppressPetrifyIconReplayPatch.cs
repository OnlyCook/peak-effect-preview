using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EffectPreview.Patches
{
    // BarAffliction.OnEnable() replays a reveal tween on every (re)activation.
    // GhostPetrifyArea forces the petrify badge active every frame while native deactivates it right back at 0 real petrify
    // so that used to replay every frame and blink
    //Debounced instead a genuinely fresh reveal after a real gap still plays normally, RESEARCH.md explains more
    [HarmonyPatch(typeof(BarAffliction), nameof(BarAffliction.OnEnable))]
    internal static class SuppressPetrifyIconReplayPatch
    {
        private const float DebounceSeconds = 0.5f;
        private static readonly Dictionary<BarAffliction, float> LastEnableTime = new Dictionary<BarAffliction, float>();

        private static bool Prefix(BarAffliction __instance)
        {
            float now = Time.unscaledTime;
            if (LastEnableTime.TryGetValue(__instance, out float last) && now - last < DebounceSeconds)
            {
                return false;
            }
            LastEnableTime[__instance] = now;
            return true;
        }
    }
}
