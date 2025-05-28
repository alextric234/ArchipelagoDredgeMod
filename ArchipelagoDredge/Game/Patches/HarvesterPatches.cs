using ArchipelagoDredge.Game.Patches.Helpers;
using HarmonyLib;
using System;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(Harvester))]
public static class HarvesterPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Harvester.ShowHarvestGrid))]
    public static void Prefix_ShowHarvestGrid() => HarvestContext.IsHarvesterActive = true;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Harvester.ShowHarvestGrid))]
    public static void Postfix_ShowHarvestGrid() => HarvestContext.IsHarvesterActive = false;
}