using HarmonyLib;

namespace EffectPreview.Patches
{
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
