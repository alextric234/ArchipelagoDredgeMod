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
            if (itemInstance._itemData.itemSubtype == ItemSubtype.FISH)
            {
                ArchipelagoLocationManager.SendLocationCheck(itemInstance.id);
            }
            else
            {
                ArchipelagoLocationManager.SendLocationCheck(harvestPOI.Harvestable.GetId());
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error sending location check for {harvestPOI.Harvestable.GetId()}/{itemInstance.id}: {e.Message}");
        }
    }
}