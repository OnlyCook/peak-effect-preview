using Peak;
using Peak.Afflictions;
using UnityEngine;

namespace EffectPreview.Preview
{
    internal static class TimedUsagePreviewCalculator
    {
        internal static void Compute(Item item, ItemPreview preview)
        {
            if (item == null)
            {
                return;
            }

            Lantern lantern = item.GetComponentInChildren<Lantern>(true);
            Candle candle = item.GetComponentInChildren<Candle>(true);
            if (lantern != null || candle != null)
            {
                ComputeFuelBasedCaps(item, lantern, candle, preview);
                return;
            }

            ShelfShroom shelfShroom = item.GetComponentInChildren<ShelfShroom>(true);
            if (shelfShroom != null && shelfShroom.instantiateOnBreak != null)
            {
                ComputeCloudCaps(shelfShroom.instantiateOnBreak, preview);
            }
        }

        private static void ComputeFuelBasedCaps(Item item, Lantern lantern, Candle candle, ItemPreview preview)
        {
            float remainingFuel = item.HasData(DataEntryKey.Fuel)
                ? item.GetData<FloatItemData>(DataEntryKey.Fuel).Value
                : (lantern != null ? lantern.startingFuel : candle.startingFuel);
            if (remainingFuel <= 0f)
            {
                return;
            }

            foreach (StatusFieldBase field in item.GetComponentsInChildren<StatusFieldBase>(true))
            {
                // StatusFieldBase.IncreaseStatus applies the same amt to statusType and every entry in additionalStatuses 
                // each StatusFieldStatus's own statusAmountPerSecond is never read by the
                // game, so the cap for every one of them has to use the field's rate, not its own
                float perSecond = Mathf.Abs(field.statusAmountPerSecond);
                AddCap(preview, field.statusType, perSecond * remainingFuel);
                if (field.additionalStatuses == null)
                {
                    continue;
                }
                foreach (StatusFieldBase.StatusFieldStatus extra in field.additionalStatuses)
                {
                    AddCap(preview, extra.statusType, perSecond * remainingFuel);
                }
            }
        }

        // this here is awful but i can't manage to figure out where the game computes the Remedy Fugus' values
        // so these are hardcoded
        // game removes 17.5 Injury immediately and then 35 Injury/Poison/Spores over 14 seconds in 1 second ticks
        private const float RepeatTickAmount = 0.025f;
        private const int RepeatTickCount = 14;
        private const float OneShotBurstAmount = 0.175f;

        private static void ComputeCloudCaps(GameObject cloudPrefab, ItemPreview preview)
        {
            foreach (AOE aoe in cloudPrefab.GetComponentsInChildren<AOE>(true))
            {
                if (Mathf.Abs(aoe.statusAmount) <= 0f || !aoe.auto)
                {
                    continue;
                }

                TimeEvent timeEvent = aoe.GetComponent<TimeEvent>();
                bool repeats = timeEvent != null && timeEvent.repeating;
                float amount = repeats ? RepeatTickAmount * RepeatTickCount : OneShotBurstAmount;

                AddCap(preview, aoe.statusType, amount);
                if (aoe.addtlStatus == null)
                {
                    continue;
                }
                foreach (CharacterAfflictions.STATUSTYPE extraType in aoe.addtlStatus)
                {
                    AddCap(preview, extraType, amount);
                }
            }
        }

        private static void AddCap(ItemPreview preview, CharacterAfflictions.STATUSTYPE type, float amount)
        {
            if (amount <= 0f)
            {
                return;
            }
            preview.StatusRemovalCaps.TryGetValue(type, out float existing);
            preview.StatusRemovalCaps[type] = existing + amount;

            // mirrors CharacterAfflictions.SubtractStatus's Poison->Spores rule for the local character
            if (type == CharacterAfflictions.STATUSTYPE.Poison)
            {
                preview.StatusRemovalCaps.TryGetValue(CharacterAfflictions.STATUSTYPE.Spores, out float existingSpores);
                preview.StatusRemovalCaps[CharacterAfflictions.STATUSTYPE.Spores] = existingSpores + amount;
            }
        }
    }
}
