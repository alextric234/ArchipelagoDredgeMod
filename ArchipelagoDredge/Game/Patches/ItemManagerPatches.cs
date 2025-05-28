using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Patches.Helpers;
using HarmonyLib;
using UnityEngine.Timeline;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(ItemManager))]
public class ItemManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemManager.SetItemSeen))]
    public static bool Prefix(SpatialItemInstance spatialItemInstance)
    {
        var context = HarvestContext.GetContext(spatialItemInstance);
        if (context == "Harvester" || context == "HarvestMinigame")
        {
            ArchipelagoLocationManager.SendLocationCheck("Fish", spatialItemInstance.id);
            HarvestContext.RemoveContext(spatialItemInstance);
        }
        else
        {
            return false;
        }
        return true;
    }
}