using System;
using System.Collections.Generic;
using Peak;
using Peak.Afflictions;
using UnityEngine;

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
            return Compute(item, character, isActionActive: null);
        }

        // isActionActive: lets CookingPreviewCalculator simulate a toggled ItemAction without mutating it, see RESEARCH.md
        // pitonPlaceable: HeldItemPreviewTracker's live raycast says this ClimbingSpikeComponent item could be hammered in right now
        internal static ItemPreview Compute(Item item, Character character, Func<ItemAction, bool> isActionActive, bool pitonPlaceable = false)
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

            var actions = item.GetComponentsInChildren<ItemAction>();
            bool wouldConsume = WouldConsumeItem(item, actions, isActionActive, requireLastUse: true);

            // Action_ModifyStatus entries are netted here (not sent to preview.AddStatus one at a time) because multiple of
            // them can fire on the very same status within a single RunAction batch and really represent one coherent
            // change (Book of Bones: a +50 and a -25 Curse action both fire on the same press) - unlike an Affliction-driven
            // effect that applies part of its change now and the rest later (Energy Drink's Drowsy), which stays unnetted
            var immediateStatusDelta = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();
            foreach (var action in actions)
            {
                // GetComponents returns disabled components too - raw-vs-cooked bonuses sit disabled until cooked
                bool active = isActionActive != null ? isActionActive(action) : (action.enabled && action.gameObject.activeInHierarchy);
                if (!active)
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

                // an OnConsumed-only action never fires unless the item is actually consumed see RESEARCH.md
                bool onConsumedOnly = action.OnConsumed && !action.OnPressed && !action.OnHeld && !action.OnCastFinished && !action.OnCancelled;
                if (onConsumedOnly && !wouldConsume)
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

                    // SubtractStatus mirrors any non-natural Poison decrease onto Spores by the same amount, see RESEARCH.md
                    if (modifyStatus.statusType == CharacterAfflictions.STATUSTYPE.Poison && modifyStatus.changeAmount < 0f)
                    {
                        immediateStatusDelta.TryGetValue(CharacterAfflictions.STATUSTYPE.Spores, out float existingSpores);
                        immediateStatusDelta[CharacterAfflictions.STATUSTYPE.Spores] = existingSpores + modifyStatus.changeAmount;
                    }
                }
                else if (action is Action_GiveExtraStamina giveExtraStamina)
                {
                    preview.ExtraStaminaDelta += giveExtraStamina.amount;
                }
                else if (action is Action_RestoreHunger restoreHunger)
                {
                    AddStatus(preview, simulatedSkeleton, CharacterAfflictions.STATUSTYPE.Hunger, -restoreHunger.restorationAmount);
                }
                else if (action is Action_InflictPoison inflictPoison)
                {
                    AddStatus(preview, simulatedSkeleton, CharacterAfflictions.STATUSTYPE.Poison, inflictPoison.inflictionTime * inflictPoison.poisonPerSecond);
                }
                else if (action is Action_AddOrRemoveThorns addOrRemoveThorns)
                {
                    float thornsDelta = ComputeThornsDelta(addOrRemoveThorns, character);
                    if (thornsDelta != 0f)
                    {
                        AddStatus(preview, simulatedSkeleton, CharacterAfflictions.STATUSTYPE.Thorns, thornsDelta);
                    }
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
                // must come before Action_ApplyAffliction below since it derives from it
                //Bugle of Friendship's massAffliction has ignoreCaster=true, TryAddAfflictionToLocalCharacter skips the holder entirely
                else if (action is Action_ApplyMassAffliction massAffliction)
                {
                    if (!massAffliction.ignoreCaster)
                    {
                        AddAffliction(preview, simulatedSkeleton, massAffliction.affliction);
                        if (massAffliction.extraAfflictions != null)
                        {
                            foreach (Affliction extra in massAffliction.extraAfflictions)
                            {
                                AddAffliction(preview, simulatedSkeleton, extra);
                            }
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
                }
            }

            foreach (KeyValuePair<CharacterAfflictions.STATUSTYPE, float> entry in immediateStatusDelta)
            {
                preview.AddStatus(entry.Key, entry.Value);
            }

            if ((wouldConsume || pitonPlaceable) && Plugin.Instance.Cfg.EnableWeightPreview.Value)
            {
                AddStatus(preview, simulatedSkeleton, CharacterAfflictions.STATUSTYPE.Weight, WeightDeltaOnConsume(item, character));
            }

            return preview;
        }

        // AddThorn/RemoveRandomThornLinq pick uniformly among matching physical thorns, so this previews the expected total damage of 'count' of them rather than a specific pick
        private static float ComputeThornsDelta(Action_AddOrRemoveThorns action, Character character)
        {
            if (action.thornCount == 0 || character?.refs?.afflictions?.physicalThorns == null)
            {
                return 0f;
            }

            List<ThornOnMe> physicalThorns = character.refs.afflictions.physicalThorns;
            if (action.thornCount > 0)
            {
                return ExpectedThornDamage(physicalThorns, stuckIn: false, action.thornCount) * 0.025f;
            }
            return -ExpectedThornDamage(physicalThorns, stuckIn: true, -action.thornCount) * 0.025f;
        }

        private static float ExpectedThornDamage(List<ThornOnMe> physicalThorns, bool stuckIn, int count)
        {
            int matchCount = 0;
            int totalDamage = 0;
            foreach (ThornOnMe thorn in physicalThorns)
            {
                if (thorn != null && thorn.isThorn && thorn.stuckIn == stuckIn)
                {
                    matchCount++;
                    totalDamage += thorn.GetThornDamage();
                }
            }
            if (matchCount == 0)
            {
                return 0f;
            }
            int picked = Mathf.Min(count, matchCount);
            return totalDamage * (picked / (float)matchCount);
        }

        // Weight is SetStatus()'d fresh every frame off a clamped live sum, not incrementally added - see RESEARCH.md
        private static float WeightDeltaOnConsume(Item item, Character character)
        {
            float cap = character.refs.afflictions.GetStatusCap(CharacterAfflictions.STATUSTYPE.Weight);
            int rawSum = RawCarryWeightSum(character);
            float liveStatus = Mathf.Clamp(WeightPerCarryUnit * rawSum, 0f, cap);
            float projectedStatus = Mathf.Clamp(WeightPerCarryUnit * (rawSum - item.CarryWeight), 0f, cap);
            return projectedStatus - liveStatus;
        }

        // mirrors CharacterAfflictions.UpdateWeight's item-weight sum, see RESEARCH.md
        private static int RawCarryWeightSum(Character character)
        {
            int sum = 0;
            ItemSlot[] itemSlots = character.player.itemSlots;
            for (int i = 0; i < itemSlots.Length; i++)
            {
                if (itemSlots[i].prefab != null)
                {
                    sum += itemSlots[i].prefab.CarryWeight;
                }
            }
            BackpackSlot backpackSlot = character.player.backpackSlot;
            if (!backpackSlot.IsEmpty() && backpackSlot.data.TryGetDataEntry<BackpackData>(DataEntryKey.BackpackData, out var backpackData))
            {
                for (int i = 0; i < backpackData.itemSlots.Length; i++)
                {
                    ItemSlot slot = backpackData.itemSlots[i];
                    if (!slot.IsEmpty())
                    {
                        sum += slot.prefab.CarryWeight;
                    }
                }
                if (ItemDatabase.TryGetItem(backpackSlot.GetPrefabName(), out Item backpackItem))
                {
                    sum += backpackItem.CarryWeight;
                }
            }
            ItemSlot trinketSlot = character.player.GetItemSlot(250);
            if (!trinketSlot.IsEmpty())
            {
                sum += trinketSlot.prefab.CarryWeight;
            }
            if (character.data.carriedPlayer != null)
            {
                sum += 8;
            }
            foreach (StickyItemComponent stuckItem in StickyItemComponent.ALL_STUCK_ITEMS)
            {
                if (stuckItem.stuckToCharacter == character)
                {
                    sum += stuckItem.addWeightToStuckPlayer;
                }
            }
            return sum;
        }

        // exposed for CookingPreviewCalculator's own hunger/extra-stamina/poison magnitude math, see RESEARCH.md
        internal static bool WouldConsumeItem(Item item, Func<ItemAction, bool> isActionActive = null)
        {
            return item != null && WouldConsumeItem(item, item.GetComponentsInChildren<ItemAction>(), isActionActive, requireLastUse: true);
        }

        // like WouldConsumeItem but ignores remaining uses - "does eating this ever consume it", see RESEARCH.md
        internal static bool CanEverConsumeItem(Item item, Func<ItemAction, bool> isActionActive = null)
        {
            return item != null && WouldConsumeItem(item, item.GetComponentsInChildren<ItemAction>(), isActionActive, requireLastUse: false);
        }

        // whether pressing (primary) use on this item ever actually calls Item.Consume() see RESEARCH.md
        private static bool WouldConsumeItem(Item item, ItemAction[] actions, Func<ItemAction, bool> isActionActive, bool requireLastUse)
        {
            foreach (var action in actions)
            {
                bool active = isActionActive != null ? isActionActive(action) : (action.enabled && action.gameObject.activeInHierarchy);
                if (!active)
                {
                    continue;
                }

                bool secondaryOnly = (action.OnSecondaryCastFinished || action.OnSecondaryPressed || action.OnSecondaryHeld || action.OnSecondaryCancelled)
                    && !(action.OnPressed || action.OnHeld || action.OnCastFinished || action.OnCancelled);
                if (secondaryOnly)
                {
                    continue;
                }

                if (action is Action_Consume || action is Action_ConsumeAndSpawn || action is Action_SpawnGuidebookPage)
                {
                    return true;
                }
                if (action is Action_WarpToBiome warpToBiome && warpToBiome.segmentToWarpTo == Segment.Void)
                {
                    return true;
                }
                if (action is Action_ReduceUses reduceUses && reduceUses.consumeOnFullyUsed)
                {
                    if (!requireLastUse)
                    {
                        return true;
                    }
                    if (item.HasData(DataEntryKey.ItemUses))
                    {
                        OptionableIntItemData usesData = item.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
                        if (usesData.HasData && usesData.Value == 1)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
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
            else if (affliction is Affliction_AdjustColdOverTime coldOverTime)
            {
                AddStatus(preview, simulatedSkeleton, CharacterAfflictions.STATUSTYPE.Cold, coldOverTime.statusPerSecond * coldOverTime.totalTime);
            }
        }
    }
}
