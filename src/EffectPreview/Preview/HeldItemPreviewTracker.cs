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
        private Object _lastEmptyHandedSource;
        // disambiguates "no source yet computed" from "computed, found nothing" - null is valid for both
        private bool _lastEmptyHandedSourceValid;

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
                _lastEmptyHandedSource = null;
                _lastEmptyHandedSourceValid = false;
                return;
            }

            if (item == null)
            {
                UpdateEmptyHandedPreview(character);
                return;
            }

            _lastEmptyHandedSource = null;
            _lastEmptyHandedSourceValid = false;

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

        // empty-handed: preview what interacting with whatever the player is currently looking at would grant right now
        // (an unlit campfire to light, an ancient luggage to open, ...), see CampfirePreviewCalculator/LuggagePreviewCalculator
        private void UpdateEmptyHandedPreview(Character character)
        {
            _lastItem = null;
            _lastCookedAmount = 0;
            _lastUses = 0;

            Object source = TryGetEmptyHandedPreviewSource(character);
            if (_lastEmptyHandedSourceValid && ReferenceEquals(source, _lastEmptyHandedSource))
            {
                return;
            }

            _lastEmptyHandedSource = source;
            _lastEmptyHandedSourceValid = true;
            Common.Safe.Run("HeldItemPreviewTracker.ComputeEmptyHanded", () =>
            {
                if (source is Campfire campfire)
                {
                    Current = CampfirePreviewCalculator.Compute(campfire);
                }
                else if (source is Luggage luggage)
                {
                    Current = LuggagePreviewCalculator.Compute(luggage);
                }
                else if (source is ThornOnMe thorn)
                {
                    Current = ThornPreviewCalculator.Compute(thorn);
                }
                else
                {
                    Current = new ItemPreview();
                }
            });
        }

        private static Object TryGetEmptyHandedPreviewSource(Character character)
        {
            if (character == null)
            {
                return null;
            }
            Interaction interaction = Interaction.instance;
            IInteractible hovered = interaction != null ? interaction.currentHovered : null;

            if (Plugin.Instance.Cfg.EnableWorldObjectPreviews.Value)
            {
                if (hovered is Campfire campfire && IsLightableCampfire(campfire))
                {
                    return campfire;
                }
                if (hovered is Luggage luggage && IsOpenableLuggage(luggage))
                {
                    return luggage;
                }
            }
            if (Plugin.Instance.Cfg.EnablePlayerEntityPreviews.Value && hovered is ThornOnMe thorn && IsRemovableOwnThorn(thorn, character))
            {
                return thorn;
            }
            return null;
        }

        private static bool IsLightableCampfire(Campfire campfire)
        {
            if (campfire.state != Campfire.FireState.Off)
            {
                return false;
            }
            // mirrors the same gate Campfire.Interact_CastFinished uses before it will actually RPC Light_Rpc - if this
            // is false, holding interact on it does nothing, so no preview should show either
            return campfire.EveryoneInRange();
        }

        private static bool IsOpenableLuggage(Luggage luggage)
        {
            // mirrors Luggage.Interact_CastFinished's own gate before it will actually RPC OpenLuggageRPC
            return luggage.IsInteractible(Character.localCharacter);
        }

        // only the local player's own body, not an ally's - removing someone else's thorn/arrow doesn't touch your bars
        private static bool IsRemovableOwnThorn(ThornOnMe thorn, Character character)
        {
            if (!thorn.stuckIn || thorn.character != character)
            {
                return false;
            }
            return thorn.IsInteractible(character);
        }
    }
}
