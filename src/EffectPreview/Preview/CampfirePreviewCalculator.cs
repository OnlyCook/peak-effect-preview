using Peak.Afflictions;

namespace EffectPreview.Preview
{
    internal static class CampfirePreviewCalculator
    {
        // mirrors Campfire.Update()'s one-time lighting reward, see RESEARCH.md
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
