using System.Collections.Generic;

namespace StatPreview.Preview
{
    internal class ItemPreview
    {
        // increases and decreases on the same status are kept separate, not netted,
        // so e.g. "clears all current drowsy" and "adds 25% drowsy later" both show
        internal readonly Dictionary<CharacterAfflictions.STATUSTYPE, float> StatusIncreases = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();
        internal readonly Dictionary<CharacterAfflictions.STATUSTYPE, float> StatusDecreases = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();

        internal float ExtraStaminaDelta;

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
