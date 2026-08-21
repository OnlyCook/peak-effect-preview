using Peak;
using UnityEngine;

namespace EffectPreview.Preview
{
    internal static class JetpackFuelPreviewCalculator
    {
        // mirrors Backpack.AddFuel's num computation, see RESEARCH.md
        internal static int ComputeAddedFuel(Item item)
        {
            int num = 25;
            if (item == null)
            {
                return num;
            }
            if (item.TryGetComponent<RopeSpool>(out var rope))
            {
                num = Mathf.CeilToInt(rope.RopeFuel);
            }
            else if (item.HasData(DataEntryKey.Fuel))
            {
                num = item.overrideJetpackFuelAmount + Mathf.RoundToInt(item.GetData<FloatItemData>(DataEntryKey.Fuel).Value *
                item.overrideJetpackFuelPerFuelMult);
            }
            else if (item is Backpack)
            {
                num = item.overrideJetpackFuelAmount;
            }
            else if (item.overrideJetpackFuel)
            {
                num = item.HasData(DataEntryKey.ItemUses)
                    ? item.overrideJetpackFuelAmount + item.GetData<OptionableIntItemData>(DataEntryKey.ItemUses).Value * item.overrideJetpackFuelPerUse
                    : item.overrideJetpackFuelAmount;
            }
            return num;
        }
    }
}
