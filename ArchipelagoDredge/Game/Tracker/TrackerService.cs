using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;

namespace ArchipelagoDredge.Game.Tracker;

/// <summary>
///     The player's unchecked locations that are in logic, recalculated when items or checks change.
/// </summary>
public static class TrackerService
{
    private static (int received, int missing) cacheKey = (-1, -1);
    private static List<string> cachedInLogic = [];

    public static TrackerOptions CurrentOptions()
    {
        var slotData = ArchipelagoClient.SlotData;
        return new TrackerOptions
        {
            IncludeIronRig = slotData.IncludeIronRigDLC,
            IncludePaleReach = slotData.IncludePaleReachDLC,
            AddFishingLicenses = slotData.AddFishingLicenses,
            AddPassageItems = slotData.AddPassageItems,
            LogicalNets = slotData.LogicalNets,
            RequireBoatPerformance = slotData.RequireBoatPerformance
        };
    }

    /// <summary>Unchecked locations in logic, or null when not connected.</summary>
    public static IReadOnlyList<string> UncheckedInLogic()
    {
        if (!ArchipelagoClient.IsConnected)
        {
            return null;
        }

        var received = ApSave.ReceivedItemNames;
        var locations = ArchipelagoClient.Session.Locations;
        var missingIds = locations.AllMissingLocations;
        var key = (received.Count, missingIds.Count);
        if (key == cacheKey)
        {
            return cachedInLogic;
        }

        var counts = received.GroupBy(name => name).ToDictionary(g => g.Key, g => g.Count());
        var missingNames = missingIds
            .Select(id => locations.GetLocationNameFromId(id, ArchipelagoClient.GameName))
            .Where(name => name != null);
        var inLogic = TrackerLogic.AccessibleLocations(counts, CurrentOptions(), missingNames);

        cachedInLogic = inLogic
            .OrderBy(name => ApWorldData.Locations[name].Region)
            .ThenBy(name => name)
            .ToList();
        cacheKey = key;
        return cachedInLogic;
    }
}
