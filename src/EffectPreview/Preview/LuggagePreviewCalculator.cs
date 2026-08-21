using Peak.Afflictions;

namespace EffectPreview.Preview
{
    internal static class LuggagePreviewCalculator
    {
        // Ancient Luggage is a LuggageCursed under the hood; its random Curse delta is intentionally not previewed, see RESEARCH.md
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
