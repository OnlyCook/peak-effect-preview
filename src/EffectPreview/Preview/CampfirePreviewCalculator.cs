using Peak.Afflictions;

namespace EffectPreview.Preview
{
    internal static class CampfirePreviewCalculator
    {
        // mirrors the one-time reward Campfire.Update() grants (MoraleBoost.SpawnMoraleBoost -> Character.MoraleBoost's
        // AddExtraStamina, plus the two hardcoded AdjustStatus calls right below it) the instant an unlit campfire becomes
        // lit, for whichever player is within moraleBoostRadius - see RESEARCH.md. Injury/Petrify are hardcoded -0.2f in
        // that code, not read off the campfire's own (unused) injuryReduction field, so mirrored as a literal here too
        private const float InjuryReduction = 0.2f;
        private const float PetrifyReduction = 0.2f;

        internal static ItemPreview Compute(Campfire campfire)
        {
            var preview = new ItemPreview();
            if (campfire == null)
            {
                return preview;
            }

            preview.ExtraStaminaDelta += campfire.moraleBoostBaseline;
            preview.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, -InjuryReduction);
            preview.PetrifyReductionOnUse += PetrifyReduction;
            return preview;
        }
    }
}
