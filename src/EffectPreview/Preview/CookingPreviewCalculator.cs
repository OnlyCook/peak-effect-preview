using System.Collections.Generic;
using Peak.Afflictions;
using UnityEngine;

namespace EffectPreview.Preview
{
    // mirrors ChangeStatsCooked and AdditionalCookingBehavior's next-cook effects, see RESEARCH.md
    internal static class CookingPreviewCalculator
    {
        private const float PoisonPerCookAtStage4Plus = 0.1f;
        private const float HungerRestorationDecayPastStage2 = 0.05f;
        private const float MinExtraStaminaAtStage1 = 0.1f;

        internal static bool CanPreviewNextStage(Item item)
        {
            ItemCooking cooking = item != null ? item.cooking : null;
            if (cooking == null || !cooking.canBeCooked || cooking.wreckWhenCooked)
            {
                return false;
            }
            return cooking.timesCookedLocal < ItemCooking.COOKING_MAX;
        }

        internal static ItemPreview Compute(Item item, Character character)
        {
            if (item == null || character == null || !CanPreviewNextStage(item))
            {
                return new ItemPreview();
            }

            ItemCooking cooking = item.cooking;
            int nextCookedAmount = cooking.timesCookedLocal + 1;
            bool isSkeleton = character.data.isSkeleton;

            var activeOverride = BuildNextStageActiveOverride(cooking, nextCookedAmount);
            var preview = ItemPreviewCalculator.Compute(item, character, activeOverride);

            // Bandage-type items opt out of ChangeStatsCooked entirely, see RESEARCH.md
            if (cooking.ignoreDefaultCookBehavior)
            {
                return preview;
            }

            // Candlestick-type items are never actually consumed, see RESEARCH.md
            if (!ItemPreviewCalculator.WouldConsumeItem(item, activeOverride))
            {
                return preview;
            }

            var restoreHunger = item.GetComponent<Action_RestoreHunger>();
            if (restoreHunger != null && !isSkeleton)
            {
                float newAmount = restoreHunger.restorationAmount;
                if (nextCookedAmount < 2)
                {
                    newAmount *= 2f;
                }
                else if (nextCookedAmount > 2)
                {
                    newAmount = Mathf.Max(newAmount - HungerRestorationDecayPastStage2, 0f);
                }
                AdjustStatusDecrease(preview, CharacterAfflictions.STATUSTYPE.Hunger, newAmount - restoreHunger.restorationAmount);
            }

            var extraStamina = item.GetComponent<Action_GiveExtraStamina>();
            float currentExtraStamina = extraStamina != null ? extraStamina.amount : 0f;
            float newExtraStamina = currentExtraStamina;
            if (nextCookedAmount < 2)
            {
                newExtraStamina = Mathf.Max(MinExtraStaminaAtStage1, currentExtraStamina * 1.5f);
            }
            else if (nextCookedAmount > 2)
            {
                newExtraStamina = 0f;
            }
            preview.ExtraStaminaDelta += newExtraStamina - currentExtraStamina;

            if (nextCookedAmount >= 4 && !cooking.ignoreDefaultPoisonBehavior && !isSkeleton)
            {
                preview.AddStatus(CharacterAfflictions.STATUSTYPE.Poison, PoisonPerCookAtStage4Plus);
            }

            return preview;
        }

        // null when nothing would newly toggle on the next cook
        private static System.Func<ItemAction, bool> BuildNextStageActiveOverride(ItemCooking cooking, int nextCookedAmount)
        {
            if (cooking.additionalCookingBehaviors == null || cooking.additionalCookingBehaviors.Length == 0)
            {
                return null;
            }

            HashSet<ItemAction> forceDisabled = null;
            HashSet<ItemAction> forceEnabled = null;

            foreach (AdditionalCookingBehavior behavior in cooking.additionalCookingBehaviors)
            {
                // would this newly cross its trigger threshold on the next cook, see RESEARCH.md
                if (behavior == null || cooking.timesCookedLocal >= behavior.cookedAmountToTrigger || nextCookedAmount < behavior.cookedAmountToTrigger)
                {
                    continue;
                }

                if (behavior is CookingBehavior_DisableScripts disable && disable.scriptsToDisable != null)
                {
                    foreach (MonoBehaviour script in disable.scriptsToDisable)
                    {
                        if (script is ItemAction action)
                        {
                            (forceDisabled ??= new HashSet<ItemAction>()).Add(action);
                        }
                    }
                }
                else if (behavior is CookingBehavior_EnableScripts enable && enable.scriptsToEnable != null)
                {
                    foreach (MonoBehaviour script in enable.scriptsToEnable)
                    {
                        if (script is ItemAction action)
                        {
                            (forceEnabled ??= new HashSet<ItemAction>()).Add(action);
                        }
                    }
                }
            }

            if (forceDisabled == null && forceEnabled == null)
            {
                return null;
            }

            return action =>
            {
                if (forceDisabled != null && forceDisabled.Contains(action))
                {
                    return false;
                }
                if (forceEnabled != null && forceEnabled.Contains(action))
                {
                    return true;
                }
                return action.enabled && action.gameObject.activeInHierarchy;
            };
        }

        // adjusts (never flips the sign of) a decrease the base preview already recorded
        private static void AdjustStatusDecrease(ItemPreview preview, CharacterAfflictions.STATUSTYPE type, float delta)
        {
            if (delta == 0f)
            {
                return;
            }
            preview.StatusDecreases.TryGetValue(type, out float existing);
            float updated = existing + delta;
            if (updated <= 0f)
            {
                preview.StatusDecreases.Remove(type);
            }
            else
            {
                preview.StatusDecreases[type] = updated;
            }
        }
    }
}
