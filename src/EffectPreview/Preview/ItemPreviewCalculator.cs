using System.Collections.Generic;
using Peak;
using Peak.Afflictions;

namespace EffectPreview.Preview
{
    internal static class ItemPreviewCalculator
    {
        private const float WeightPerCarryUnit = 0.025f;

        // CharacterAfflictions.StatusAffectsSkeleton (Injury/Curse/Petrify/FlyTrap/Web) is what AddStatus gates on, but Weight
        // and Arrow never go through AddStatus at all in the vanilla game - both are driven every frame by a direct SetStatus
        // call off live world state (carried weight, stuck arrow count), so they still affect a skeleton same as anyone else
        private static bool AffectsSkeleton(CharacterAfflictions.STATUSTYPE type)
        {
            return type == CharacterAfflictions.STATUSTYPE.Injury
                || type == CharacterAfflictions.STATUSTYPE.Curse
                || type == CharacterAfflictions.STATUSTYPE.Petrify
                || type == CharacterAfflictions.STATUSTYPE.FlyTrap
                || type == CharacterAfflictions.STATUSTYPE.Web
                || type == CharacterAfflictions.STATUSTYPE.Weight
                || type == CharacterAfflictions.STATUSTYPE.Arrow;
        }

        // mirrors just the ItemUses branch of Item.CanUsePrimary() - Value == -1 is the "unlimited/no cap" sentinel
        private static bool HasUsesRemaining(Item item)
        {
            if (!item.HasData(DataEntryKey.ItemUses))
            {
                return true;
            }
            OptionableIntItemData usesData = item.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
            if (!usesData.HasData || usesData.Value == -1)
            {
                return true;
            }
            return usesData.Value > 0;
        }

        private static void AddStatus(ItemPreview preview, bool simulatedSkeleton, CharacterAfflictions.STATUSTYPE type, float amount)
        {
            if (simulatedSkeleton && !AffectsSkeleton(type))
            {
                return;
            }
            preview.AddStatus(type, amount);
        }

        internal static ItemPreview Compute(Item item, Character character)
        {
            var preview = new ItemPreview();
            if (item == null || character == null)
            {
                return preview;
            }

            // an emptied charge item (Book of Bones with 0 uses left) still carries the same live, enabled ItemAction
            // components, they just can no longer fire - Item.CanUsePrimary() covers this exact ItemUses check but also
            // folds in UIData.hasMainInteract, which is really about cooking readiness (false until an uncooked item has
            // been cooked at least once, see ItemCooking) and has nothing to do with charges, so replicate just the
            // uses-remaining half rather than calling CanUsePrimary() itself
            if (!HasUsesRemaining(item))
            {
                return preview;
            }

            // toggled by Action_BecomeSkeleton as the loop below encounters it, so any Action_ModifyStatus(ifSkeleton=true)
            // later in the same component order sees the post-toggle state, matching real RunAction execution order
            bool simulatedSkeleton = character.data.isSkeleton;
            bool consumed = false;
            bool consumesOnFinalUse = false;

            // Action_ModifyStatus entries are netted here (not sent to preview.AddStatus one at a time) because multiple of
            // them can fire on the very same status within a single RunAction batch and really represent one coherent
            // change (Book of Bones: a +50 and a -25 Curse action both fire on the same press) - unlike an Affliction-driven
            // effect that applies part of its change now and the rest later (Energy Drink's Drowsy), which stays unnetted
            var immediateStatusDelta = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();
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

                if (action is Action_BecomeSkeleton)
                {
                    simulatedSkeleton = !simulatedSkeleton;
                }
                else if (action is Action_ClearAllStatus)
                {
                    // Book of Bones is the only user of this today; assumes the default excludeCurse=true/no extra exclusions,
                    // which is the same set GoToVoidRoutine already previews via ClearsCurableStatusOnUse below
                    preview.ClearsCurableStatusOnUse = true;
                }
                else if (action is Action_ModifyStatus modifyStatus)
                {
                    if (modifyStatus.ifSkeleton && !simulatedSkeleton)
                    {
                        continue;
                    }
                    // Petrify is routed to data.SetPetrify/AddPetrify, not the status array - goes on PetrifyDelta
                    if (modifyStatus.statusType == CharacterAfflictions.STATUSTYPE.Petrify)
                    {
                        preview.PetrifyDelta += modifyStatus.changeAmount;
                        continue;
                    }
                    if (simulatedSkeleton && !AffectsSkeleton(modifyStatus.statusType))
                    {
                        continue;
                    }
                    immediateStatusDelta.TryGetValue(modifyStatus.statusType, out float existingDelta);
                    immediateStatusDelta[modifyStatus.statusType] = existingDelta + modifyStatus.changeAmount;
                }
                else if (action is Action_GiveExtraStamina giveExtraStamina)
                {
                    preview.ExtraStaminaDelta += giveExtraStamina.amount;
                }
                else if (action is Action_RestoreHunger restoreHunger)
                {
                    AddStatus(preview, simulatedSkeleton, CharacterAfflictions.STATUSTYPE.Hunger, -restoreHunger.restorationAmount);
                }
                else if (action is Action_Consume || action is Action_ConsumeAndSpawn)
                {
                    consumed = true;
                }
                else if (action is Action_ReduceUses reduceUses)
                {
                    consumesOnFinalUse = reduceUses.consumeOnFullyUsed;
                }
                else if (action is Action_InflictPoison inflictPoison)
                {
                    AddStatus(preview, simulatedSkeleton, CharacterAfflictions.STATUSTYPE.Poison, inflictPoison.inflictionTime * inflictPoison.poisonPerSecond);
                }
                // must come before Action_ApplyAffliction below since it derives from it
                else if (action is Action_SuperJumpAmulet superJumpAmulet)
                {
                    preview.PetrifyDelta += superJumpAmulet.petrifyPerUse;
                    AddAffliction(preview, simulatedSkeleton, superJumpAmulet.affliction);
                    if (superJumpAmulet.extraAfflictions != null)
                    {
                        foreach (Affliction extra in superJumpAmulet.extraAfflictions)
                        {
                            AddAffliction(preview, simulatedSkeleton, extra);
                        }
                    }
                }
                else if (action is Action_ApplyAffliction applyAffliction)
                {
                    AddAffliction(preview, simulatedSkeleton, applyAffliction.affliction);
                    if (applyAffliction.extraAfflictions != null)
                    {
                        foreach (Affliction extra in applyAffliction.extraAfflictions)
                        {
                            AddAffliction(preview, simulatedSkeleton, extra);
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
                    AddAffliction(preview, simulatedSkeleton, healingGem.invincibilityAffliction);
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

            foreach (KeyValuePair<CharacterAfflictions.STATUSTYPE, float> entry in immediateStatusDelta)
            {
                preview.AddStatus(entry.Key, entry.Value);
            }

            // Action_ReduceUses(consumeOnFullyUsed=true) auto-consumes the item once its last charge is spent (see
            // Action_ReduceUses.RunAction in RESEARCH.md) - only preview the weight loss on that final press, not every
            // press, and only for items that actually vanish this way (Book of Bones sets consumeOnFullyUsed=false, it
            // survives at 0 uses as an inert item, so it correctly never hits this path)
            if (!consumed && consumesOnFinalUse && item.HasData(DataEntryKey.ItemUses))
            {
                OptionableIntItemData usesData = item.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
                if (usesData.HasData && usesData.Value == 1)
                {
                    consumed = true;
                }
            }

            if (consumed)
            {
                AddStatus(preview, simulatedSkeleton, CharacterAfflictions.STATUSTYPE.Weight, -WeightPerCarryUnit * item.CarryWeight);
            }

            return preview;
        }

        // covers afflictions whose effect only lands once their buff wears off (energy drink's crash, etc)
        private static void AddAffliction(ItemPreview preview, bool simulatedSkeleton, Affliction affliction)
        {
            if (affliction is Affliction_FasterBoi fasterBoi)
            {
                // Affliction_FasterBoi.OnApplied() hardcodes -0.5 Drowsy immediately, separate from drowsyOnEnd later
                AddStatus(preview, simulatedSkeleton, CharacterAfflictions.STATUSTYPE.Drowsy, -0.5f);
                AddStatus(preview, simulatedSkeleton, CharacterAfflictions.STATUSTYPE.Drowsy, fasterBoi.drowsyOnEnd);
            }
            else if (affliction is Affliction_InfiniteStamina infiniteStamina)
            {
                // Big Lollypop grants this through Action_ApplyAffliction rather than the dedicated Action_ApplyInfiniteStamina action
                preview.GrantsInfiniteStaminaOnUse = true;
                if (infiniteStamina.drowsyAffliction != null)
                {
                    AddAffliction(preview, simulatedSkeleton, infiniteStamina.drowsyAffliction);
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
                AddStatus(preview, simulatedSkeleton, CharacterAfflictions.STATUSTYPE.Drowsy, drowsyOverTime.statusPerSecond * drowsyOverTime.totalTime);
            }
        }
    }
}
