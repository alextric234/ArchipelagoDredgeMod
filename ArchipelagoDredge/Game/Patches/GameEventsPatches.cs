using System;
using ArchipelagoDredge.Game.Helpers;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(GameEvents))]
public static class GameEventsPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameEvents.TriggerResearchCompleted))]
    public static void TriggerResearchCompletedPre(SpatialItemData spatialItemData)
    {
        try
        {
            if (spatialItemData.id == "net1")
            {
                return;
            }

            if (LocationHelper.TryParseLocation(spatialItemData.id, out var location) &&
                LocationHelper.ResearchLocations.Contains(location))
            {
                var researchedLocationName = LocationHelper.locationNames[location] + " Researched";
                if (LocationHelper.locationNamesReversed.TryGetValue(researchedLocationName, out var researchedLocation) &&
                    LocationHelper.TryGetArchipelagoId(researchedLocation, out var apLocationId))
                {
                    LocationHelper.ReportLocationCheck(apLocationId);
                }
            }

            LocationHelper.ReportLocationCheck(spatialItemData.id);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error sending research check for {spatialItemData?.id}: {e}");
        }
    }
}
