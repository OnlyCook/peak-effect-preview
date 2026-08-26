using Peak.Afflictions;

namespace EffectPreview.Preview
{
    // CharacterInteractible.GetEaten(): SubtractStatus(Hunger, 1f) + AddStatus(Curse, 0.1f), both fixed magnitudes
    // baked directly in code rather than read off any item/component
    internal static class CannibalismPreviewCalculator
    {
        private const float HungerRestored = 1f;
        private const float CurseGained = 0.1f;

        internal static ItemPreview Compute()
        {
            var preview = new ItemPreview();
            preview.AddStatus(CharacterAfflictions.STATUSTYPE.Hunger, -HungerRestored);
            preview.AddStatus(CharacterAfflictions.STATUSTYPE.Curse, CurseGained);
            return preview;
        }
    }
}
