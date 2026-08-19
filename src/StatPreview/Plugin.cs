using BepInEx;
using HarmonyLib;

namespace StatPreview
{
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            _harmony = new Harmony(PluginInfo.Guid);

            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded.");
        }
    }
}
