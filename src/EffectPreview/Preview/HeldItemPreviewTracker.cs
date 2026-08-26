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
        private bool _lastFlareActive;
        private int _lastFuelBucket;
        private bool _lastCookingPreviewActive;
        private bool _lastPitonPlaceable;
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
                _lastFlareActive = false;
                _lastFuelBucket = 0;
                _lastCookingPreviewActive = false;
                _lastPitonPlaceable = false;
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

            bool cookingPreviewActive = Plugin.Instance.Cfg.EnableCookingPreview.Value
                && Input.GetKey(Plugin.Instance.Cfg.CookingPreviewKey.Value)
                && IsAbleToCookHeldItem(item, character);

            bool pitonPlaceable = Plugin.Instance.Cfg.EnableWeightPreview.Value && IsPitonPlaceable(item, character);

            // Lantern, Faerie Lantern, Candlestick toggle this on activate/deactivate
            bool flareActive = item.HasData(DataEntryKey.FlareActive) && item.GetData<BoolItemData>(DataEntryKey.FlareActive).Value;

            // fuel keeps draining every frame while lit, so the removal-cap preview needs to keep shrinking too
            // instead of freezing at whatever it was when the item was first picked up/lit (but bucketed so it doesn't update every frame)
            int fuelBucket = item.HasData(DataEntryKey.Fuel) ? Mathf.RoundToInt(item.GetData<FloatItemData>(DataEntryKey.Fuel).Value * 10f) : 0;

            // ReferenceEquals, not ==: Unity's == treats a destroyed object as null, which would mask an item->null transition
            if (ReferenceEquals(item, _lastItem) && cookedAmount == _lastCookedAmount && uses == _lastUses
                && cookingPreviewActive == _lastCookingPreviewActive && pitonPlaceable == _lastPitonPlaceable
                && flareActive == _lastFlareActive && fuelBucket == _lastFuelBucket)
            {
                return;
            }

            _lastItem = item;
            _lastCookedAmount = cookedAmount;
            _lastUses = uses;
            _lastCookingPreviewActive = cookingPreviewActive;
            _lastPitonPlaceable = pitonPlaceable;
            _lastFlareActive = flareActive;
            _lastFuelBucket = fuelBucket;
            Common.Safe.Run("HeldItemPreviewTracker.Compute", () =>
            {
                ItemPreview preview = cookingPreviewActive
                    ? CookingPreviewCalculator.Compute(item, character)
                    : ItemPreviewCalculator.Compute(item, character, isActionActive: null, pitonPlaceable);
                if (Plugin.Instance.Cfg.EnableTimedUsagePreview.Value)
                {
                    TimedUsagePreviewCalculator.Compute(item, preview);
                }
                Current = preview;
            });
        }

        // mirrors Player.RaycastClimbingSpikeStart and the climbingSpikeCount gate updateClimbingSpikeUse checks before it'll even try hammering one in
        private static bool IsPitonPlaceable(Item item, Character character)
        {
            if (character.data.climbingSpikeCount <= 0)
            {
                return false;
            }
            ClimbingSpikeComponent spike = item.GetComponent<ClimbingSpikeComponent>();
            if (spike == null || MainCamera.instance == null)
            {
                return false;
            }
            float maxDistance = character.data.isClimbingAnything
                ? spike.climbingSpikeStartDistance
                : spike.climbingSpikeStartDistanceGrounded;
            Transform cameraTransform = MainCamera.instance.transform;
            return Physics.Raycast(cameraTransform.position, cameraTransform.forward, out _, maxDistance, HelperFunctions.GetMask(HelperFunctions.LayerType.TerrainMap));
        }

        // holding the cooking preview key while looking at a lit campfire you're able to cook this item on right now,
        // mirrors Campfire.IsInteractible/IsConstantlyInteractable's own gate, see RESEARCH.md
        private bool IsAbleToCookHeldItem(Item item, Character character)
        {
            if (!CookingPreviewCalculator.CanPreviewNextStage(item))
            {
                return false;
            }

            Interaction interaction = Interaction.instance;
            IInteractible hovered = interaction != null ? interaction.currentHovered : null;

            return hovered is Campfire campfire && campfire.Lit;
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
