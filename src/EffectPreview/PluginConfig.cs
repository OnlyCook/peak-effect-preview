using BepInEx.Configuration;

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

        internal PluginConfig(ConfigFile config)
        {
            // keep config simple by only having "General" (won't add many stuff here either way)

            EnablePreview = config.Bind("General", "enable-preview", true,
                "Show a ghost preview of status effect changes on both stamina bars while holding an item that would cause them.");
            EnableWasteIndicator = config.Bind("General", "enable-waste-indicator", false,
                "Show a marker on the ghost preview when the held item's effect would only be partially applied (some of it wasted).");
            EnableRemovalBlink = config.Bind("General", "enable-removal-blink", true,
                "Pulse the ghost bar for anything the held item would remove, instead of showing it as a steady translucent bar.");
            ShowGhostBarCounts = config.Bind("General", "show-ghost-bar-counts", false,
                "Show a number on each ghost preview bar (this mod's own) for how much it would add or remove.");
            ShowVanillaBarCounts = config.Bind("General", "show-vanilla-bar-counts", false,
                "Show a number on the game's own (non-ghost) bars for their current amount.");
            BarCountFontScale = config.Bind("General", "bar-count-font-scale", 1f,
                "Multiplier applied on top of the automatic size-to-fit bar scaling used by both ghost/vanilla bar count numbers.");
        }
    }
}
