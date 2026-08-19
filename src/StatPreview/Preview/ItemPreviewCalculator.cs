namespace StatPreview.Preview
{
    internal static class ItemPreviewCalculator
    {
        internal static ItemPreview Compute(Item item, Character character)
        {
            var preview = new ItemPreview();
            if (item == null || character == null)
            {
                return preview;
            }

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
            }

            return preview;
        }
    }
}
