using BepInEx;
using BepInEx.Logging;
using BRCreator.RacePlugin.Race;
using HarmonyLib;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace BRCreator.RacePlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log = null!;
        private static int shouldIgnoreInput = 0;
        public static RaceManager RaceManager = null!;


        public static bool ShouldIgnoreInput
        {
            get => Interlocked.CompareExchange(ref shouldIgnoreInput, 0, 0) == 1;
            set => Interlocked.Exchange(ref shouldIgnoreInput, value ? 1 : 0);
        }

        private void Awake()
        {
            Log = this.Logger;
            RaceManager = new();
            Application.runInBackground = true;
            SetupHarmony();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void SetupHarmony()
        {
            var harmony = new Harmony("BRCreator.RacePlugin.Harmony");

            var patches = typeof(Plugin).Assembly.GetTypes()
                .Where(m => m.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0)
                .ToArray();

            foreach (var patch in patches)
            {
                harmony.PatchAll(patch);
            }
        }
    }
}
