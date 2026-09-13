using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using HarmonyLib;
using System;
using Winch.Core;
using Winch.Util;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(GameEvents))]
public static class GameEventsPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameEvents.TriggerResearchCompleted))]
    public static void TriggerResearchCompletedPre(SpatialItemData spatialItemData)
    {
        if (spatialItemData.id == "net1")
        {
            return;
        }
        LocationHelper.TryParseLocation(spatialItemData.id, out var location);

        if (LocationHelper.ResearchLocations.Contains(location))
        {
            var researchedLocationName = LocationHelper.locationNames[location] + " Researched";
            var researchedLocation = LocationHelper.NameToLocation(researchedLocationName);
            var apLocationId = LocationHelper.locationToArchipelagoId[researchedLocation];
            LocationHelper.ReportLocationCheck(apLocationId);
        }
        LocationHelper.ReportLocationCheck(spatialItemData.id);
    }
}