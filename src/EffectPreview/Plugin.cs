using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace EffectPreview
{
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }

        internal PluginConfig Cfg { get; private set; }

        internal BepInEx.Logging.ManualLogSource Log => Logger;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Cfg = new PluginConfig(Config);
            _harmony = new Harmony(PluginInfo.Guid);
            _harmony.PatchAll();

            GameObject go = new GameObject("EffectPreview.Runtime");
            DontDestroyOnLoad(go);
            go.AddComponent<Preview.HeldItemPreviewTracker>();
            go.AddComponent<Ui.GhostBarOverlay>();
            go.AddComponent<Ui.GhostJetpackFuelGauge>();

            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded.");
        }
    }
}
