using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(GameConfigData))]
public class GameConfigDataPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("get_MaxAberrationSpawnChance")]
    private static void Postfix(ref float __result)
    {
        __result = 0.5f;
    }
}