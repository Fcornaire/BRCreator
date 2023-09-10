using BRCreator.RacePlugin.Race;
using HarmonyLib;
using Reptile;
using UnityEngine;

namespace BRCreator.RacePlugin.Patch
{
    [HarmonyPatch(typeof(StageManager))]
    public class StageManagerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("SetupStage")]
        private static void SetupStage()
        {
            UnityEngine.Object.Instantiate<GameObject>(new GameObject("Racer")).AddComponent<RaceVelocityModifier>();
        }
    }
}
