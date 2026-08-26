using Peak;

namespace EffectPreview.Preview
{
    internal static class RitualDaggerPreviewCalculator
    {
        // mirrors Item.StartUseSecondary/FinishCastSecondary's own gate for actually firing FeedItem on a target
        internal static bool IsUsable(Item item)
        {
            if (item == null || !item.canUseOnFriend || !Interaction.instance.hasValidTargetCharacter)
            {
                return false;
            }
            return item.GetComponentInChildren<RitualDaggerFeedBehavior>() != null;
        }

        internal static void Compute(Item item, ItemPreview preview)
        {
            RitualDaggerFeedBehavior feedBehavior = item.GetComponentInChildren<RitualDaggerFeedBehavior>();
            if (feedBehavior == null)
            {
                return;
            }

            preview.ClearsCurableStatusOnUse = true;
            preview.ExtraStaminaDelta += feedBehavior.bonusStamina;
            if (feedBehavior.infiniteStaminaTime > 0f)
            {
                preview.GrantsInfiniteStaminaOnUse = true;
            }
        }
    }
}
