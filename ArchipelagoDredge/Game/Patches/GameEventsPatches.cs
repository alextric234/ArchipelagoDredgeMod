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
        LocationNames.TryParseLocation(spatialItemData.id, out var location);

        if (LocationNames.ResearchLocations.Contains(location))
        {
            var researchedLocationName = LocationNames.locationNames[location] + " Researched";
            var researchedLocation = LocationNames.NameToLocation(researchedLocationName);
            var apLocationId = LocationNames.locationToArchipelagoId[researchedLocation];
            ArchipelagoClient.Session.Locations.CompleteLocationChecksAsync(apLocationId);
        }
        ArchipelagoLocationManager.SendLocationCheck(spatialItemData.id);
    }
}