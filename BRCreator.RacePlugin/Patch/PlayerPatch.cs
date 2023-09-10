using BRCreator.RacePlugin.Race;
using HarmonyLib;
using Reptile;

namespace BRCreator.RacePlugin.Patch
{
    [HarmonyPatch(typeof(Player))]
    public class PlayerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("FixedUpdatePlayer")]
        public static void FixedUpdatePlayerPrefix(Player __instance)
        {
            var currentPlayer = WorldHandler.instance?.GetCurrentPlayer();
            if (__instance.name == currentPlayer?.name && Plugin.RaceManager.IsInRace())
            {
                GrindAbility grindAbility = Traverse.Create(__instance).Field<GrindAbility>("grindAbility").Value;

                grindAbility.speedTarget = RaceVelocityModifier.GrindSpeedTarget;
                __instance.normalBoostSpeed = RaceVelocityModifier.BoostSpeedTarget;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("LateUpdatePlayer")]
        public static void LateUpdatePlayer(Player __instance)
        {
            var currentPlayer = WorldHandler.instance.GetCurrentPlayer();
            if (Plugin.RaceManager.IsStarting() && __instance.name == currentPlayer.name)
            {
                var cp = Plugin.RaceManager.GetNextCheckpointPin();

                //Shouldn't happen, but just in case
                if (cp == null)
                {
                    return;
                }

                //Make the camera look at the next checkpoint before the race starts
                GameplayCamera cam = Traverse.Create(__instance).Field("cam").GetValue<GameplayCamera>();
                UnityEngine.Transform realTf = Traverse.Create(cam).Field("realTf").GetValue<UnityEngine.Transform>();
                realTf.transform.LookAt(cp.UIIndicator.trans.position);
            }
        }
    }
}
