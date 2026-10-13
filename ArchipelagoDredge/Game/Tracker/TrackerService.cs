using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;

namespace ArchipelagoDredge.Game.Tracker;

public enum TrackerStatus
{
    InLogic,
    OutOfLogic,
    Checked
}

public class TrackerEntry
{
    public string Name;
    public string Group;
    public string LocationGroup;
    public string Region;
    public TrackerStatus Status;
    public string HintText;
}

/// <summary>
///     The player's locations with their logic status and hints, recalculated when items, checks or hints change.
/// </summary>
public static class TrackerService
{
    public const string ResearchGroup = "Research";

    private static readonly string[] GroupOrder =
    [
        "The Marrows", "Gale Cliffs", "Stellar Basin", "Twisted Strand", "Devil's Spine", "Open Ocean", "The Pale Reach",
        "The Iron Rig", ResearchGroup
    ];

    private static (int received, int missing) inLogicKey = (-1, -1);
    private static List<string> cachedInLogic = [];

    private static (int received, int missing, int hints) entriesKey = (-1, -1, -1);
    private static List<TrackerEntry> cachedEntries = [];

    // Replaced whole from the socket thread, so readers always see a complete array.
    private static volatile Hint[] hints = [];
    private static volatile int hintsVersion;

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
            RequireBoatPerformance = slotData.RequireBoatPerformance,
            ProgressiveAberrationChanceItems = slotData.ProgressiveAberrationChanceItems,
            GuaranteedAberrationsItem = slotData.GuaranteedAberrationsItem
        };
    }

    public static void TrackHints(ArchipelagoSession session)
    {
        hints = [];
        hintsVersion++;
        session.DataStorage.TrackHints(updated =>
        {
            if (ArchipelagoClient.Session != session)
            {
                return;
            }

            hints = updated ?? [];
            hintsVersion++;
        });
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
        if (key == inLogicKey)
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
        inLogicKey = key;
        return cachedInLogic;
    }

    /// <summary>
    ///     Every location in this slot, grouped by region (research on its own) with in-logic ones first, or null when
    ///     not connected.
    /// </summary>
    public static IReadOnlyList<TrackerEntry> Entries()
    {
        var inLogic = UncheckedInLogic();
        if (inLogic == null)
        {
            return null;
        }

        var session = ArchipelagoClient.Session;
        var locations = session.Locations;
        var key = (ApSave.ReceivedItemNames.Count, locations.AllMissingLocations.Count, hintsVersion);
        if (key == entriesKey)
        {
            return cachedEntries;
        }

        var inLogicSet = new HashSet<string>(inLogic);
        var checkedIds = new HashSet<long>(locations.AllLocationsChecked);
        var slot = session.ConnectionInfo.Slot;
        var hintsByLocation = hints
            .Where(h => h.FindingPlayer == slot)
            .GroupBy(h => h.LocationId)
            .ToDictionary(g => g.Key, g => g.First());

        var entries = new List<TrackerEntry>();
        foreach (var id in locations.AllLocations)
        {
            var name = locations.GetLocationNameFromId(id, ArchipelagoClient.GameName);
            if (name == null || !ApWorldData.Locations.TryGetValue(name, out var data))
            {
                continue;
            }

            entries.Add(new TrackerEntry
            {
                Name = name,
                Group = data.Group == ResearchGroup ? ResearchGroup : data.Region,
                LocationGroup = data.Group,
                Region = data.Region,
                Status = checkedIds.Contains(id) ? TrackerStatus.Checked
                    : inLogicSet.Contains(name) ? TrackerStatus.InLogic
                    : TrackerStatus.OutOfLogic,
                HintText = hintsByLocation.TryGetValue(id, out var hint) ? DescribeHint(session, hint) : null
            });
        }

        cachedEntries = entries
            .OrderBy(e => GroupIndex(e.Group))
            .ThenBy(e => e.Status)
            .ThenBy(e => e.Name)
            .ToList();
        entriesKey = key;
        return cachedEntries;
    }

    private static int GroupIndex(string group)
    {
        var index = System.Array.IndexOf(GroupOrder, group);
        return index < 0 ? GroupOrder.Length : index;
    }

    private static string DescribeHint(ArchipelagoSession session, Hint hint)
    {
        var receiver = session.Players.GetPlayerInfo(hint.ReceivingPlayer);
        var item = session.Items.GetItemName(hint.ItemId, receiver?.Game) ?? $"Item {hint.ItemId}";
        var owner = hint.ReceivingPlayer == session.ConnectionInfo.Slot
            ? "your"
            : $"{session.Players.GetPlayerAlias(hint.ReceivingPlayer)}'s";
        var importance = hint.ItemFlags.HasFlag(ItemFlags.Advancement) ? " (progression)"
            : hint.ItemFlags.HasFlag(ItemFlags.NeverExclude) ? " (useful)"
            : hint.ItemFlags.HasFlag(ItemFlags.Trap) ? " (trap)"
            : "";
        return $"Holds {owner} {item}{importance}";
    }
}
