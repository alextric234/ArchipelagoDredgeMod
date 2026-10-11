using ArchipelagoDredge.Network;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(GameConfigData))]
public class GameConfigDataPatches
{
    // Aberration spawn chance while connected to an AP session.
    private const float ApMaxAberrationSpawnChance = 0.5f;

    [HarmonyPostfix]
    [HarmonyPatch("get_MaxAberrationSpawnChance")]
    private static void Postfix(ref float __result)
    {
        if (!ArchipelagoClient.IsConnected)
        {
            return;
        }

        __result = ApMaxAberrationSpawnChance;
    }
}