using UnityEngine;

namespace EffectPreview.Preview
{
    internal class HeldItemPreviewTracker : MonoBehaviour
    {
        internal static HeldItemPreviewTracker Instance { get; private set; }

        internal ItemPreview Current { get; private set; } = new ItemPreview();

        private Item _lastItem;
        private int _lastCookedAmount;
        private int _lastUses;
        private Campfire _lastCampfire;
        // null is a legitimate "no lightable campfire nearby" result, so a plain ReferenceEquals against _lastCampfire
        // can't tell that state apart from "campfire branch hasn't run yet this empty-handed streak" (e.g. right after
        // dropping an item) - this flag disambiguates the two so the empty-handed/no-campfire case still forces one
        // recompute (clearing Current back to empty) instead of leaving the previous item's ghost bars stuck on screen
        private bool _lastCampfireValid;

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
                _lastUses = 0;
                _lastCampfire = null;
                _lastCampfireValid = false;
                return;
            }

            if (item == null)
            {
                UpdateCampfirePreview(character);
                return;
            }

            _lastCampfire = null;
            _lastCampfireValid = false;

            // cooking mutates the same Item instance in place, so also recompute when CookedAmount changes
            int cookedAmount = item.data != null && item.data.TryGetDataEntry<IntItemData>(DataEntryKey.CookedAmount, out var cookedData)
                ? cookedData.Value
                : 0;

            // a charge item (Book of Bones etc) also mutates the same Item instance in place when used - it isn't consumed/
            // reference-swapped, only its remaining ItemUses drops, so that needs its own recompute trigger too
            int uses = item.data != null && item.data.TryGetDataEntry<OptionableIntItemData>(DataEntryKey.ItemUses, out var usesData)
                ? usesData.Value
                : 0;

            // ReferenceEquals, not ==: Unity's == treats a destroyed object as null, which would mask an item->null transition
            if (ReferenceEquals(item, _lastItem) && cookedAmount == _lastCookedAmount && uses == _lastUses)
            {
                return;
            }

            _lastItem = item;
            _lastCookedAmount = cookedAmount;
            _lastUses = uses;
            Common.Safe.Run("HeldItemPreviewTracker.Compute", () => Current = ItemPreviewCalculator.Compute(item, character));
        }

        // empty-handed: preview what walking up to and lighting a currently-unlit campfire would grant, see CampfirePreviewCalculator
        private void UpdateCampfirePreview(Character character)
        {
            _lastItem = null;
            _lastCookedAmount = 0;
            _lastUses = 0;

            Campfire campfire = TryGetLightableCampfire(character);
            if (_lastCampfireValid && ReferenceEquals(campfire, _lastCampfire))
            {
                return;
            }

            _lastCampfire = campfire;
            _lastCampfireValid = true;
            Common.Safe.Run("HeldItemPreviewTracker.ComputeCampfire", () => Current = CampfirePreviewCalculator.Compute(campfire));
        }

        private static Campfire TryGetLightableCampfire(Character character)
        {
            if (character == null)
            {
                return null;
            }
            Interaction interaction = Interaction.instance;
            if (interaction == null || !(interaction.currentHovered is Campfire campfire))
            {
                return null;
            }
            if (campfire.state != Campfire.FireState.Off)
            {
                return null;
            }
            // mirrors the same gate Campfire.Interact_CastFinished uses before it will actually RPC Light_Rpc - if this
            // is false, holding interact on it does nothing, so no preview should show either
            if (!campfire.EveryoneInRange())
            {
                return null;
            }
            return campfire;
        }
    }
}
