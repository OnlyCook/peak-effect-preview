using Peak;
using Peak.Afflictions;
using UnityEngine;

namespace StatPreview.Preview
{
    // Action_CloneSelectedItem/Action_HealingGem costs depend on live aim state that changes every frame without the held item changing, so this recomputes every frame instead of going through ItemPreviewCalculator's once-per-item-change pipeline
    internal static class DynamicPetrifyPreview
    {
        internal static float Compute(ItemPreview preview, Character localCharacter)
        {
            float delta = 0f;

            if (preview.CloneItemAction != null)
            {
                delta += ComputeClone(preview.CloneItemAction);
            }

            if (preview.HealingGemAction != null && localCharacter != null)
            {
                delta += ComputeHealingGem(preview.HealingGemAction, localCharacter);
            }

            return delta;
        }

        private static float ComputeClone(Action_CloneSelectedItem action)
        {
            if (Interaction.instance == null)
            {
                return 0f;
            }

            Interaction.instance.DoInteractableRaycasts(out IInteractible result, action.range);
            if (!(result is Item targetItem))
            {
                return 0f;
            }

            if (targetItem.itemTags.HasFlag(Item.ItemTags.ScoutAmulet) || targetItem.itemTags.HasFlag(Item.ItemTags.NonCloneable))
            {
                return 0f;
            }

            int points = targetItem.itemTags.HasFlag(Item.ItemTags.Mystical) ? action.petrifyMystical : action.petrify;
            return points / 100f;
        }

        private static float ComputeHealingGem(Action_HealingGem action, Character localCharacter)
        {
            if (action.healingAffliction == null)
            {
                return 0f;
            }

            Character target = localCharacter;
            if (Interaction.instance != null && Interaction.instance.hasValidTargetCharacter)
            {
                Interaction.instance.DoInteractableRaycasts(out IInteractible result);
                if (result is CharacterInteractible characterInteractible && characterInteractible.character != null && characterInteractible.character != localCharacter)
                {
                    target = characterInteractible.character;
                }
            }

            float totalHeal = Affliction_HealAll.GetTotalPotentialHeal(target, action.healingAffliction.maxHealing);
            float value = totalHeal * action.healingToPetrifyRatio;
            return Mathf.Clamp(value, action.minPetrify, action.maxPetrify);
        }
    }
}
