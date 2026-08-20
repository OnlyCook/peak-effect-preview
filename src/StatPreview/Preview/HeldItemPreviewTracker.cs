using UnityEngine;

namespace StatPreview.Preview
{
    internal class HeldItemPreviewTracker : MonoBehaviour
    {
        internal static HeldItemPreviewTracker Instance { get; private set; }

        internal ItemPreview Current { get; private set; } = new ItemPreview();

        private Item _lastItem;
        private int _lastCookedAmount;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!Plugin.Instance.Cfg.EnablePreview.Value)
            {
                return;
            }

            Character character = Character.localCharacter;
            Item item = character != null ? character.data.currentItem : null;

            // item.consuming flips true before the reference actually changes; see RESEARCH.md
            if (item != null && item.consuming)
            {
                Current = new ItemPreview();
                _lastItem = item;
                _lastCookedAmount = 0;
                return;
            }

            // cooking mutates the same Item instance in place, so also recompute when CookedAmount changes
            int cookedAmount = item != null && item.data != null && item.data.TryGetDataEntry<IntItemData>(DataEntryKey.CookedAmount, out var cookedData)
                ? cookedData.Value
                : 0;

            // ReferenceEquals, not ==: Unity's == treats a destroyed object as null, which would mask an item->null transition
            if (ReferenceEquals(item, _lastItem) && cookedAmount == _lastCookedAmount)
            {
                return;
            }

            _lastItem = item;
            _lastCookedAmount = cookedAmount;
            Common.Safe.Run("HeldItemPreviewTracker.Compute", () => Current = ItemPreviewCalculator.Compute(item, character));
        }
    }
}
