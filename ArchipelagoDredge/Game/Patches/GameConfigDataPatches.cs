using System;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Utils;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     The aberration roll is min(max chance, base chance + bonuses), so raising both the base chances and the cap to
///     the items' minimum chance makes every roll at least that likely.
/// </summary>
[HarmonyPatch(typeof(GameConfigData))]
public class GameConfigDataPatches
{
    private const float ApMaxAberrationSpawnChance = 0.5f;

    [HarmonyPostfix]
    [HarmonyPatch("get_MaxAberrationSpawnChance")]
    private static void MaxAberrationSpawnChance_Postfix(ref float __result)
    {
        if (!ApSave.IsActive)
        {
            return;
        }

        __result = Math.Max(ApMaxAberrationSpawnChance, AberrationChanceManager.MinimumChance);
    }

    [HarmonyPostfix]
    [HarmonyPatch("get_BaseAberrationSpawnChance")]
    private static void BaseAberrationSpawnChance_Postfix(ref float __result)
    {
        __result = Math.Max(__result, AberrationChanceManager.MinimumChance);
    }

    [HarmonyPostfix]
    [HarmonyPatch("get_NightAberrationSpawnChance")]
    private static void NightAberrationSpawnChance_Postfix(ref float __result)
    {
        __result = Math.Max(__result, AberrationChanceManager.MinimumChance);
    }
}
