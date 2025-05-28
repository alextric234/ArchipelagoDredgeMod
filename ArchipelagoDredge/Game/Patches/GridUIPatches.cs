using System.Diagnostics.CodeAnalysis;
using ArchipelagoDredge.Game.Patches.Helpers;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(GridUI))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class GridUIPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GridUI.PlaceObjectOnGrid))]
    static void Prefix(GridObject o, UnityEngine.Vector3Int pos, bool needsSaving, bool instant)
    {
        if (!HarvestContext.IsHarvesterActive)
            return;

        SpatialItemInstance item = o.SpatialItemInstance;
        if (item != null)
        {
            HarvestContext.SetContext(item, "Harvester");
        }
    }
}