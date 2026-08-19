using Peak.Afflictions;

namespace StatPreview.Preview
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
                if (action is Action_ModifyStatus modifyStatus)
                {
                    if (modifyStatus.ifSkeleton && !character.data.isSkeleton)
                    {
                        continue;
                    }
                    // Petrify runs off petrifyAmount, not the status array, skip it here
                    if (modifyStatus.statusType == CharacterAfflictions.STATUSTYPE.Petrify)
                    {
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
            }

            if (consumed)
            {
                preview.AddStatus(CharacterAfflictions.STATUSTYPE.Weight, -WeightPerCarryUnit * item.CarryWeight);
            }

            return preview;
        }

        // covers the eventual effect of afflictions that only apply once their
        // buff wears off (energy drink's crash, infinite stamina's drowsiness)
        private static void AddAffliction(ItemPreview preview, Affliction affliction)
        {
            if (affliction is Affliction_FasterBoi fasterBoi)
            {
                // Affliction_FasterBoi.OnApplied() hardcodes -0.5 Drowsy immediately, separate from drowsyOnEnd later
                preview.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, -0.5f);
                preview.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, fasterBoi.drowsyOnEnd);
            }
            else if (affliction is Affliction_InfiniteStamina infiniteStamina && infiniteStamina.drowsyAffliction != null)
            {
                AddAffliction(preview, infiniteStamina.drowsyAffliction);
            }
            else if (affliction is Affliction_AdjustDrowsyOverTime drowsyOverTime)
            {
                preview.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, drowsyOverTime.statusPerSecond * drowsyOverTime.totalTime);
            }
        }
    }
}
