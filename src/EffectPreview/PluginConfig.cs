using BepInEx.Configuration;
using UnityEngine;

namespace EffectPreview
{
    internal class PluginConfig
    {
        internal ConfigEntry<bool> EnablePreview;
        internal ConfigEntry<bool> EnableWasteIndicator;
        internal ConfigEntry<bool> EnableRemovalBlink;
        internal ConfigEntry<bool> ShowGhostBarCounts;
        internal ConfigEntry<bool> ShowVanillaBarCounts;
        internal ConfigEntry<float> BarCountFontScale;
        internal ConfigEntry<bool> StickyThornRemoval;
        internal ConfigEntry<bool> EnableWorldObjectPreviews;
        internal ConfigEntry<bool> EnablePlayerEntityPreviews;
        internal ConfigEntry<bool> EnableCookingPreview;
        internal ConfigEntry<KeyCode> CookingPreviewKey;

        internal PluginConfig(ConfigFile config)
        {
            EnablePreview = config.Bind("General", "enable-preview", true,
                                        "Show a ghost preview of status effect changes on both stamina bars while holding an item that would cause them.");
            EnableWorldObjectPreviews = config.Bind("General", "enable-world-object-previews", true,
                                                    "Show a preview when empty-handed and able to interact with a world object that changes your status effects (unlit campfires, ancient luggage).");
            EnablePlayerEntityPreviews = config.Bind("General", "enable-player-entity-previews", true,
                                                     "Show a preview when empty-handed and able to interact with a physical Thorn/Arrow stuck on your own body.");
            EnableCookingPreview = config.Bind("General", "enable-cooking-preview", true,
                                               "While holding an item you're able to cook (near a lit campfire or portable stove), hold the cooking preview key to preview what its next cook stage would do instead of its current stage.");
            CookingPreviewKey = config.Bind("General", "cooking-preview-key", KeyCode.C,
                                            "Key to hold to preview a held item's next cook stage instead of its current one.");
            StickyThornRemoval = config.Bind("General", "sticky-thorn-removal", true,
                                             "Keep removing a physical Thorn/Arrow you're holding interact on even if your aim drifts off it, instead of vanilla's behavior of cancelling the moment you're not looking straight at it.");
            EnableWasteIndicator = config.Bind("General", "enable-waste-indicator", false,
                                               "Show a marker on the ghost preview when the held item's effect would only be partially applied (some of it wasted).");
            EnableRemovalBlink = config.Bind("General", "enable-removal-blink", true,
                                             "Pulse the ghost bar for anything the held item would remove, instead of showing it as a steady translucent bar.");
            ShowGhostBarCounts = config.Bind("General", "show-ghost-bar-counts", false,
                                             "Show a number on each ghost preview bar (this mod's own) for how much it would add or remove.");
            ShowVanillaBarCounts = config.Bind("General", "show-vanilla-bar-counts", false,
                                               "Show a number on the game's own (non-ghost) bars for their current amount.");
            BarCountFontScale = config.Bind("General", "bar-count-font-scale", 1f,
                                            new ConfigDescription("Multiplier applied on top of the automatic size-to-fit bar scaling used by both ghost/vanilla bar count numbers.",
                                            new AcceptableValueRange<float>(0.5f, 3f)));
        }
    }
}
