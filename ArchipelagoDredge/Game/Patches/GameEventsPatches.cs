using System;
using System.Linq;
using ArchipelagoDredge.Game.Helpers;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(GameEvents))]
public static class GameEventsPatches
{
    // The game completes this research itself when the net slot upgrade is bought
    // (ItemLogicHandler.OnUpgradesChanged); it isn't a player research.
    private const string BasicNetItemId = "net1";

    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameEvents.TriggerResearchCompleted))]
    public static void TriggerResearchCompletedPre(SpatialItemData spatialItemData)
    {
        try
        {
            if (spatialItemData.id == BasicNetItemId)
            {
                return;
            }

            if (LocationHelper.TryParseLocation(spatialItemData.id, out var location) &&
                LocationHelper.ResearchLocations.Contains(location))
            {
                var researchedLocationName = $"{LocationHelper.LocationNames[location]} {ItemNames.ResearchedSuffix}";
                if (LocationHelper.LocationsByName.TryGetValue(researchedLocationName, out var researchedLocation))
                {
                    LocationHelper.ReportLocationCheck(researchedLocation);
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
