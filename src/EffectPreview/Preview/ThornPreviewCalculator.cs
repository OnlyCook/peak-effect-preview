using Peak.Afflictions;

namespace EffectPreview.Preview
{
    internal static class ThornPreviewCalculator
    {
        // Thorns/Arrow aren't driven through AddStatus/SubtractStatus at all - CharacterAfflictions.UpdateWeight()
        // recomputes them from scratch every frame as 0.025f * sum(GetThornDamage() over every still-stuck physical
        // thorn/arrow of that type) and SetStatus()s the result directly (see RESEARCH.md/ItemPreviewCalculator's
        // AffectsSkeleton comment). So pulling one out just drops its own GetThornDamage() contribution out of that sum
        internal static ItemPreview Compute(ThornOnMe thorn)
        {
            var preview = new ItemPreview();
            if (thorn == null || !thorn.stuckIn)
            {
                return preview;
            }

            float delta = 0.025f * thorn.GetThornDamage();
            if (delta > 0f)
            {
                CharacterAfflictions.STATUSTYPE type = thorn.isArrow ? CharacterAfflictions.STATUSTYPE.Arrow : CharacterAfflictions.STATUSTYPE.Thorns;
                preview.AddStatus(type, -delta);
            }

            // ThornOnMe.OnPulledOut's own side effect (Arrow's live config: addStatusOnRemove=true, Injury, 0.125) -
            // base.OnPulledOut only gates this on character.IsLocal, not removedByPlayer, so it fires the same way for
            // a player-pulled removal as an automatic pop-out
            if (thorn.addStatusOnRemove && thorn.statusToAddOnRemoveAmt != 0f)
            {
                preview.AddStatus(thorn.statusToAddOnRemove, thorn.statusToAddOnRemoveAmt);
            }

            return preview;
        }
    }
}
