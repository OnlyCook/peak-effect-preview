using Peak.Afflictions;

namespace EffectPreview.Preview
{
    internal static class LuggagePreviewCalculator
    {
        // Ancient Luggage is a LuggageCursed instance under the hood (its GameObject is just named "LuggageAncient") -
        // LuggageCursed.Interact_CastFinished applies its Injury delta directly in code off its own injuryAmt field
        // (halved for a skeleton), so no trap/AOE hookup exists to search for. Its Curse delta is
        // Random.Range(minCurse, maxCurse + 1) * 0.025f - non-deterministic, so it's skipped here the same way
        // ItemPreviewCalculator skips other randomized effects (Action_RandomMushroomEffect etc, see RESEARCH.md)
        internal static ItemPreview Compute(Luggage luggage)
        {
            var preview = new ItemPreview();
            if (!(luggage is LuggageCursed cursed))
            {
                return preview;
            }

            Character character = Character.localCharacter;
            bool isSkeleton = character != null && character.data.isSkeleton;
            float injury = isSkeleton ? cursed.injuryAmt * 0.125f : cursed.injuryAmt;
            if (injury > 0f)
            {
                preview.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, injury);
            }

            return preview;
        }
    }
}
