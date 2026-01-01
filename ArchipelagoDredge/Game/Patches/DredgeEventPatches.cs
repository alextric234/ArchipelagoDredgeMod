using System;
using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using Winch.Core;
using Winch.Core.API;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(DredgeEvent))]
public static class DredgeEventPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DredgeEvent.TriggerPOIHarvested))]
    public static void TriggerPOIHarvested_Prefix(HarvestPOI harvestPOI, SpatialItemInstance itemInstance)
    {
        try
        {
            ArchipelagoLocationManager.SendLocationCheck(harvestPOI.Harvestable.GetId());
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error sending location check for {harvestPOI.Harvestable.GetId()}/{itemInstance.id}: {e.Message}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(DredgeEvent.TriggerFishCaught))]
    public static void TriggerFishCaught_Prefix(SpatialItemInstance itemInstance)
    {
        try
        {
            ArchipelagoLocationManager.SendLocationCheck(itemInstance.id);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error sending location check for {itemInstance.id}: {e.Message}");
        }
    }
}