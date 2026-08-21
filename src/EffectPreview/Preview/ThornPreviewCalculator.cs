using Peak.Afflictions;

namespace EffectPreview.Preview
{
    internal static class ThornPreviewCalculator
    {
        // mirrors CharacterAfflictions.UpdateWeight's per-frame Thorns/Arrow recompute, see RESEARCH.md
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

            // ThornOnMe.OnPulledOut's own side effect (Arrow's live config: Injury +0.125), see RESEARCH.md
            if (thorn.addStatusOnRemove && thorn.statusToAddOnRemoveAmt != 0f)
            {
                preview.AddStatus(thorn.statusToAddOnRemove, thorn.statusToAddOnRemoveAmt);
            }

            return preview;
        }
    }
}
