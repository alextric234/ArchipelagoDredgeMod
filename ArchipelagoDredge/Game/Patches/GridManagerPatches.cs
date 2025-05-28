using ArchipelagoDredge.Game.Patches.Helpers;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;


[HarmonyPatch(typeof(GridManager))]
public static class GridManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GridManager.AddItemOfTypeToCursor))]
    public static void Prefix(SpatialItemInstance spatialItemInstance, GridObjectState state)
    {
        HarvestContext.SetContext(spatialItemInstance, "HarvestMinigame");
    }
}