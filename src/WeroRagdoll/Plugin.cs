using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using WeroRagdoll.Config;
using WeroRagdoll.Core;

namespace WeroRagdoll
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.fordg.weroragdoll";
        public const string PluginName = "Wero Ragdoll";
        public const string PluginVersion = "0.3.4";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }
        internal static WeroConfig Cfg { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Cfg = new WeroConfig(Config);
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();
            LodService.Reset();
            TestRange.InitFromConfig();
            TestPanel.InitFromConfig();
            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Test UI: {Cfg.TestUiHotkey.Value}  Test Range: {Cfg.TestRangeHotkey.Value}");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            LodService.Tick(Time.unscaledDeltaTime);
            TestRange.Tick();
            TestPanel.Tick();
            TestHarness.Tick();
        }

        private void OnGUI()
        {
            TestRange.OnGui();
            TestPanel.OnGui();
        }
    }
}
