using HarmonyLib;
using Reptile;

namespace BRCreator.RacePlugin.Patch
{
    [HarmonyPatch(typeof(WorldHandler))]
    public class WorldHandlerPatchs
    {
        [HarmonyPrefix]
        [HarmonyPatch("UpdateWorldHandler")]
        public static void UpdateWorldHandlerPrefix()
        {
            BaseModule instance = Traverse.Create(typeof(BaseModule)).Field("instance").GetValue<BaseModule>();
            Stage currentStage = Traverse.Create(instance).Field("currentStage").GetValue<Stage>();

            if (Plugin.RaceManager.HasAdditionRaceConfigToLoad() && currentStage == Plugin.RaceManager.GetStage())
            {
                Plugin.RaceManager.AdditionalRaceInitialization();
                Plugin.RaceManager.SetHasAdditionRaceConfigToLoad(false);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateWorldHandler")]
        public static void UpdateWorldHandlerPostfix()
        {
            if (Plugin.RaceManager.ShouldLoadRaceStageASAP())
            {
                BaseModule instance = Traverse.Create(typeof(BaseModule)).Field("instance").GetValue<BaseModule>();
                Stage toLoad = Plugin.RaceManager.GetStage();

                Plugin.RaceManager.SetShouldLoadRaceStageASAP(false);
                Plugin.RaceManager.SetHasAdditionRaceConfigToLoad(true);
                instance.SwitchStage(toLoad);
            }
        }
    }
}
