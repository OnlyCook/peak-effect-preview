using System.Collections.Generic;

namespace StatPreview.Preview
{
    internal class ItemPreview
    {
        internal readonly Dictionary<CharacterAfflictions.STATUSTYPE, float> StatusDeltas = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();

        internal float ExtraStaminaDelta;

        internal bool IsEmpty => StatusDeltas.Count == 0 && ExtraStaminaDelta == 0f;

        internal void AddStatus(CharacterAfflictions.STATUSTYPE type, float amount)
        {
            StatusDeltas.TryGetValue(type, out float existing);
            StatusDeltas[type] = existing + amount;
        }
    }
}
