using HarmonyLib;

namespace EffectPreview.Patches
{
    // keeps a held Thorn/Arrow removal (and its hover highlight) locked on target while interact is held, see RESEARCH.md
    [HarmonyPatch(typeof(Interaction), nameof(Interaction.DoInteractableRaycasts))]
    internal static class StickyThornRemovalPatch
    {
        private static void Postfix(Interaction __instance, ref IInteractible interactableResult)
        {
            if (!Plugin.Instance.Cfg.StickyThornRemoval.Value)
            {
                return;
            }
            if (!(__instance.currentHeldInteractible is ThornOnMe))
            {
                return;
            }

            Character character = Character.localCharacter;
            if (character == null || !character.input.interactIsPressed)
            {
                return;
            }
            interactableResult = __instance.currentHeldInteractible;
        }
    }
}
