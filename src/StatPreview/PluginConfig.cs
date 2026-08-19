using BepInEx.Configuration;

namespace StatPreview
{
    internal class PluginConfig
    {
        internal ConfigEntry<bool> EnablePreview;

        internal PluginConfig(ConfigFile config)
        {
            EnablePreview = config.Bind("General", "enable-preview", true,
                "Show a ghost preview of hunger/stamina/status changes on the stamina bar while holding an item that would cause them");
        }
    }
}
