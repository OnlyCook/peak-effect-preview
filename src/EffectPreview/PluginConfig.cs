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
        internal ConfigEntry<bool> EnableWeightPreview;
        internal ConfigEntry<bool> EnableTimedUsagePreview;
        internal ConfigEntry<bool> EnableJetpackPreview;

        internal PluginConfig(ConfigFile config)
        {
            EnablePreview = config.Bind("General", "enable-preview", true,
                                        "Show a ghost preview of status effect changes on both stamina bars while holding an item that would cause them.");

            EnableWorldObjectPreviews = config.Bind("Interactions", "enable-world-object-previews", true,
                                                    "Show a preview when empty-handed and able to interact with a world object that changes your status effects (unlit campfires, ancient luggage).");
            EnablePlayerEntityPreviews = config.Bind("Interactions", "enable-player-entity-previews", true,
                                                     "Show a preview when empty-handed and able to interact with a physical Thorn/Arrow stuck on your own body, or able to cannibalize another player.");
            StickyThornRemoval = config.Bind("Interactions", "sticky-thorn-removal", true,
                                             "Keep removing a physical Thorn/Arrow you're holding interact on even if your aim drifts off it, instead of vanilla's behavior of cancelling the moment you're not looking straight at it.");

            EnableWeightPreview = config.Bind("Item Previews", "enable-weight-preview", true,
                                               "Whether to calculate and preview the Weight status effect.");
            EnableTimedUsagePreview = config.Bind("Item Previews", "enable-timed-usage-preview", true,
                                               "While holding certain items that remove status effect based on duration used, show a struck-through status icon for any status effect the item could fully remove if fully used, or both icons side by side if only partially.");
            EnableJetpackPreview = config.Bind("Item Previews", "enable-jetpack-preview", true,
                                               "While holding a fuel item and hovering the jetpack fuel slot, preview where its fuel gauge needle would land.");
            EnableCookingPreview = config.Bind("Item Previews", "enable-cooking-preview", true,
                                               "While holding an item you're able to cook (near a lit campfire or portable stove), hold the cooking preview key to preview what its next cook stage would do instead of its current stage.");
            CookingPreviewKey = config.Bind("Item Previews", "cooking-preview-key", KeyCode.C,
                                            "Key to hold to preview a held item's next cook stage instead of its current one.");

            EnableWasteIndicator = config.Bind("Bar Display", "enable-waste-indicator", false,
                                               "Show a marker on the ghost preview when the held item's effect would only be partially applied (some of it wasted).");
            EnableRemovalBlink = config.Bind("Bar Display", "enable-removal-blink", true,
                                             "Pulse the ghost bar for anything the held item would remove, instead of showing it as a steady translucent bar.");
#if BARS_DEFAULT_ON
            const bool barCountsDefault = true;
#else
            const bool barCountsDefault = false;
#endif
            ShowGhostBarCounts = config.Bind("Bar Display", "show-ghost-bar-counts", barCountsDefault,
                                             "Show a number on each ghost preview bar (this mod's own) for how much it would add or remove.");
            ShowVanillaBarCounts = config.Bind("Bar Display", "show-vanilla-bar-counts", barCountsDefault,
                                               "Show a number on the game's own (non-ghost) bars for their current amount.");
            BarCountFontScale = config.Bind("Bar Display", "bar-count-font-scale", 1f,
                                            new ConfigDescription("Multiplier applied on top of the automatic size-to-fit bar scaling used by both ghost/vanilla bar count numbers.",
                                            new AcceptableValueRange<float>(0.5f, 3f)));
        }
    }
}
