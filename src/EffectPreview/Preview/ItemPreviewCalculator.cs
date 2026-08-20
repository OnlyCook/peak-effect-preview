using Peak;
using Peak.Afflictions;

namespace EffectPreview.Preview
{
    internal static class ItemPreviewCalculator
    {
        private const float WeightPerCarryUnit = 0.025f;

        internal static ItemPreview Compute(Item item, Character character)
        {
            var preview = new ItemPreview();
            if (item == null || character == null)
            {
                return preview;
            }

            bool consumed = false;
            var actions = item.GetComponents<ItemAction>();
            foreach (var action in actions)
            {
                // GetComponents returns disabled components too - raw-vs-cooked bonuses sit disabled until cooked
                if (!action.enabled || !action.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // preview only what pressing (primary) use does, not secondary/alt-use-only effects
                bool secondaryOnly = (action.OnSecondaryCastFinished || action.OnSecondaryPressed || action.OnSecondaryHeld || action.OnSecondaryCancelled)
                    && !(action.OnPressed || action.OnHeld || action.OnCastFinished || action.OnCancelled);
                if (secondaryOnly)
                {
                    continue;
                }

                if (action is Action_ModifyStatus modifyStatus)
                {
                    if (modifyStatus.ifSkeleton && !character.data.isSkeleton)
                    {
                        continue;
                    }
                    // Petrify is routed to data.SetPetrify/AddPetrify, not the status array - goes on PetrifyDelta
                    if (modifyStatus.statusType == CharacterAfflictions.STATUSTYPE.Petrify)
                    {
                        preview.PetrifyDelta += modifyStatus.changeAmount;
                        continue;
                    }
                    preview.AddStatus(modifyStatus.statusType, modifyStatus.changeAmount);
                }
                else if (action is Action_GiveExtraStamina giveExtraStamina)
                {
                    preview.ExtraStaminaDelta += giveExtraStamina.amount;
                }
                else if (action is Action_RestoreHunger restoreHunger)
                {
                    preview.AddStatus(CharacterAfflictions.STATUSTYPE.Hunger, -restoreHunger.restorationAmount);
                }
                else if (action is Action_Consume || action is Action_ConsumeAndSpawn)
                {
                    consumed = true;
                }
                else if (action is Action_InflictPoison inflictPoison)
                {
                    preview.AddStatus(CharacterAfflictions.STATUSTYPE.Poison, inflictPoison.inflictionTime * inflictPoison.poisonPerSecond);
                }
                // must come before Action_ApplyAffliction below since it derives from it
                else if (action is Action_SuperJumpAmulet superJumpAmulet)
                {
                    preview.PetrifyDelta += superJumpAmulet.petrifyPerUse;
                    AddAffliction(preview, superJumpAmulet.affliction);
                    if (superJumpAmulet.extraAfflictions != null)
                    {
                        foreach (Affliction extra in superJumpAmulet.extraAfflictions)
                        {
                            AddAffliction(preview, extra);
                        }
                    }
                }
                else if (action is Action_ApplyAffliction applyAffliction)
                {
                    AddAffliction(preview, applyAffliction.affliction);
                    if (applyAffliction.extraAfflictions != null)
                    {
                        foreach (Affliction extra in applyAffliction.extraAfflictions)
                        {
                            AddAffliction(preview, extra);
                        }
                    }
                }
                // aim-dependent, cached for DynamicPetrifyPreview to recompute every frame
                else if (action is Action_CloneSelectedItem cloneSelectedItem)
                {
                    preview.CloneItemAction = cloneSelectedItem;
                }
                else if (action is Action_HealingGem healingGem)
                {
                    preview.HealingGemAction = healingGem;
                    AddAffliction(preview, healingGem.invincibilityAffliction);
                }
                else if (action is Action_ApplyInfiniteStamina)
                {
                    preview.GrantsInfiniteStaminaOnUse = true;
                }
                // GoToVoidRoutine(clearStatus: true): clears every curable status but Curse, and only knocks Petrify down 20 rather than clearing it. The item itself is also deleted outright by DeleteScoutsHonorFromLocalCharacter (a tag-based removal, not an Action_Consume component), so it costs the item's carry weight same as a normal consumable
                else if (action is Action_WarpToBiome warpToBiome && warpToBiome.segmentToWarpTo == Segment.Void)
                {
                    preview.ClearsCurableStatusOnUse = true;
                    preview.PetrifyReductionOnUse += 0.2f;
                    consumed = true;
                }
            }

            if (consumed)
            {
                preview.AddStatus(CharacterAfflictions.STATUSTYPE.Weight, -WeightPerCarryUnit * item.CarryWeight);
            }

            return preview;
        }

        // covers afflictions whose effect only lands once their buff wears off (energy drink's crash, etc)
        private static void AddAffliction(ItemPreview preview, Affliction affliction)
        {
            if (affliction is Affliction_FasterBoi fasterBoi)
            {
                // Affliction_FasterBoi.OnApplied() hardcodes -0.5 Drowsy immediately, separate from drowsyOnEnd later
                preview.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, -0.5f);
                preview.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, fasterBoi.drowsyOnEnd);
            }
            else if (affliction is Affliction_InfiniteStamina infiniteStamina)
            {
                // Big Lollypop grants this through Action_ApplyAffliction rather than the dedicated Action_ApplyInfiniteStamina action
                preview.GrantsInfiniteStaminaOnUse = true;
                if (infiniteStamina.drowsyAffliction != null)
                {
                    AddAffliction(preview, infiniteStamina.drowsyAffliction);
                }
            }
            // Scout's Ambition applies this wrapper instead of Affliction_InfiniteStamina directly - it periodically re-grants real Affliction_InfiniteStamina to everyone (including the wearer) within radius, every 0.5s, for as long as it's active
            else if (affliction is Affliction_RadiateInfiniteStam)
            {
                preview.GrantsInfiniteStaminaOnUse = true;
            }
            else if (affliction is Affliction_Invincibility)
            {
                preview.GrantsInvincibilityOnUse = true;
            }
            else if (affliction is Affliction_AdjustDrowsyOverTime drowsyOverTime)
            {
                preview.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, drowsyOverTime.statusPerSecond * drowsyOverTime.totalTime);
            }
        }
    }
}
