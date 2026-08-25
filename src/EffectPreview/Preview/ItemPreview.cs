using System.Collections.Generic;
using Peak;

namespace EffectPreview.Preview
{
    internal class ItemPreview
    {
        // increases and decreases on the same status are kept separate, not netted, so both show - some items genuinely
        // apply both to the same status as two distinct, temporally separate effects (Energy Drink: -50 Drowsy immediately,
        // then +25 Drowsy once the speed buff wears off later). ItemPreviewCalculator nets same-instant contributions
        // (e.g. Book of Bones' two simultaneous Action_ModifyStatus on Curse) into a single call before they ever reach here
        internal readonly Dictionary<CharacterAfflictions.STATUSTYPE, float> StatusIncreases = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();
        internal readonly Dictionary<CharacterAfflictions.STATUSTYPE, float> StatusDecreases = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();

        internal float ExtraStaminaDelta;

        // flat petrify gain (Action_ModifyStatus/Action_SuperJumpAmulet); the aim-dependent actions below are just cached refs for DynamicPetrifyPreview to recompute each frame
        internal float PetrifyDelta;
        internal Action_CloneSelectedItem CloneItemAction;
        internal Action_HealingGem HealingGemAction;

        // Action_WarpToBiome(segmentToWarpTo=Void) - MapHandler.GoToVoidRoutine clears all curable statuses (not Curse/Petrify) and knocks Petrify down by a flat amount, live status values decide the actual widths so this is just intent + the fixed reduction
        internal bool ClearsCurableStatusOnUse;
        internal float PetrifyReductionOnUse;

        // Action_ApplyInfiniteStamina (Scout's Ambition, Big Lollypop) - just an on/off, no magnitude to compute
        internal bool GrantsInfiniteStaminaOnUse;

        // Affliction_Invincibility (Fortified Milk, Scout's Tenacity) - just an on/off, no magnitude to compute
        internal bool GrantsInvincibilityOnUse;

        // Lantern / Faerie Lantern / Candlestick / Remedy Fungus / Heat Pack remove status effect per second while active
        internal readonly Dictionary<CharacterAfflictions.STATUSTYPE, float> StatusRemovalCaps = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();

        internal void AddStatus(CharacterAfflictions.STATUSTYPE type, float amount)
        {
            if (amount > 0f)
            {
                StatusIncreases.TryGetValue(type, out float existing);
                StatusIncreases[type] = existing + amount;
            }
            else if (amount < 0f)
            {
                StatusDecreases.TryGetValue(type, out float existing);
                StatusDecreases[type] = existing - amount;
            }
        }
    }
}
