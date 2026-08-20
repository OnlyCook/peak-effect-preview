using System.Collections.Generic;
using Peak;

namespace StatPreview.Preview
{
    internal class ItemPreview
    {
        // increases and decreases on the same status are kept separate, not netted, so both show
        internal readonly Dictionary<CharacterAfflictions.STATUSTYPE, float> StatusIncreases = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();
        internal readonly Dictionary<CharacterAfflictions.STATUSTYPE, float> StatusDecreases = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();

        internal float ExtraStaminaDelta;

        // flat petrify gain (Action_ModifyStatus/Action_SuperJumpAmulet); the aim-dependent actions below are just cached refs for DynamicPetrifyPreview to recompute each frame
        internal float PetrifyDelta;
        internal Action_CloneSelectedItem CloneItemAction;
        internal Action_HealingGem HealingGemAction;

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
