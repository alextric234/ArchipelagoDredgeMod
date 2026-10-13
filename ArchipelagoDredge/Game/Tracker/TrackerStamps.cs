using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Game.Helpers;
using UnityEngine;

namespace ArchipelagoDredge.Game.Tracker;

/// <summary>
///     Map stamps for tracker locations that aren't harvest spots (shrines, caches, bottles, pursuit characters and
///     docks), since the game's spot markers only work on harvest spots. A stamp sitting exactly on one of these targets
///     is treated as the tracker's pin, so no extra save data is needed.
/// </summary>
public static class TrackerStamps
{
    // Any stamp from the game's stamp list; the map and spyglass draw tracker pins with the AP icon instead.
    private const int StampType = 0;
    private const float SamePlace = 0.01f;

    private enum TargetKind
    {
        Conversation,
        Dock
    }

    private static readonly Dictionary<string, (TargetKind kind, string key)> Targets = BuildTargets();

    private static object cachedFor;
    private static Dictionary<string, Vector2> cachedPositions = new();

    private static Dictionary<string, (TargetKind, string)> BuildTargets()
    {
        var targets = new Dictionary<string, (TargetKind, string)>();
        void Conversation(string location, string node) => targets[location] = (TargetKind.Conversation, node);
        void Dock(string location, string dock) => targets[location] = (TargetKind.Dock, dock);

        Conversation("The Marrows - Cod Shrine(J-9)", "CodShrine");
        Conversation("Stellar Basin - Shark Shrine(H-2)", "SharkShrine");
        Conversation("Gale Cliffs - Crab Shrine(P-3)", "CrabShrine");
        Conversation("Twisted Strand - Aberration Shrine(D-13)", "AberrationShrine");
        Conversation("Devil's Spine - Shrine 1(P-13)", "DSShrine1");
        Conversation("Devil's Spine - Shrine 2(P-14)", "DSShrine2");
        Conversation("Devil's Spine - Shrine 3(O-14)", "DSShrine3");
        Conversation("The Marrows - Shore Cache 1(J-8)", "GM_ShoreCache1");
        Conversation("The Marrows - Shore Cache 2(L-10)", "GM_ShoreCache2");
        Conversation("Open Ocean - Old Wreck(L-5)", "GM_ShoreCache3");
        Conversation("Open Ocean - Shore Cache 1(M-6)", "GC_ShoreCache1");
        Conversation("Gale Cliffs - Shore Cache 1(P-3)", "GC_ShoreCache2");
        Conversation("Open Ocean - Shore Cache 2(P-6)", "GC_ShoreCache3");
        Conversation("Stellar Basin - Shore Cache 1(G-4)", "SB_ShoreCache1");
        Conversation("Stellar Basin - Shore Cache 2(I-4)", "SB_ShoreCache2");
        Conversation("Stellar Basin - Shore Cache 3(D-3)", "SB_ShoreCache3");
        Conversation("Stellar Basin - Shore Cache 4(C-3)", "SB_ShoreCache4");
        Conversation("Open Ocean - Shore Cache 3(I-5)", "SB_ShoreCache5");
        Conversation("Twisted Strand - Shore Cache 1(E-14)", "TS_ShoreCache1");
        Conversation("Twisted Strand - Shore Cache 2(D-12)", "TS_ShoreCache2");
        Conversation("Figure in Blue", "HoodedFigure1_Root");
        Conversation("Figure in Gold", "HoodedFigure2_Root");
        Conversation("Figure in Purple", "HoodedFigure3_Root");
        Conversation("Figure in Red", "HoodedFigure4_Root");
        Conversation("Figure in White", "HoodedFigure5_Root");
        Conversation("Craven Courier", "Courier_Root");
        Conversation("Castaway", "Castaway_Root");
        Conversation("Lost Dog", "Dog_Root");

        Dock("Gale Cliffs - Old Mayor's Camp(O-2)", "Old Mayor Dock GC");
        Dock("Open Ocean - Old Mayor's Camp(J-3)", "Old Mayor Dock SB");
        Dock("Stellar Basin - Old Laboratory(G-4)", "Research Pontoon");
        Dock("Stone Tablets", "Ancient Lighthouse Ruin");
        foreach (var location in new[] { "Fishmonger's Gift", "Shipwright's Gift", "A Place to Rest", "A Fresh Start", "Caught to Order" })
        {
            Dock(location, "Greater Marrow");
        }

        foreach (var location in new[] { "Little Marrow Trader's Gift", "Package Delivery", "Lost at Sea" })
        {
            Dock(location, "Little Marrow");
        }

        foreach (var location in new[]
                 {
                     "Grotesque Fish", "The Collector - Ornate Key", "The Collector - Rusted Music Box",
                     "The Collector - Jewel Encrusted Band", "The Collector - Shimmering Necklace",
                     "The Collector - Antique Pocket Watch"
                 })
        {
            Dock(location, "Outcast Isle");
        }

        Dock("The Bitter End", "Pilot Camp");
        Dock("The Bitter End - Dog Tags", "Pilot Camp");
        Dock("Flames of The Deep", "Temple");
        return targets;
    }

    public static bool TryGetPosition(string locationName, out Vector2 position)
    {
        return Positions().TryGetValue(locationName, out position);
    }

    public static bool IsPinned(Vector2 position)
    {
        return FindStamp(position) != null;
    }

    public static string TogglePin(Vector2 position)
    {
        var saveData = GameManager.Instance.SaveData;
        var stamp = FindStamp(position);
        if (stamp != null)
        {
            saveData.mapStamps.Remove(stamp);
            return "Unpinned it from your map.";
        }

        if (saveData.GetTotalUserPlacedMapMarkers() >= GameManager.Instance.GameConfigData.MaxNumMapMarkers)
        {
            return "The map is out of markers. Remove some to pin this.";
        }

        saveData.mapStamps.Add(new SerializedMapStamp { stampType = StampType, x = position.x, z = position.y });
        return "Pinned it on your map.";
    }

    public static bool IsTrackerStamp(SerializedMapStamp stamp)
    {
        return stamp != null && Positions().Values.Any(p => SameSpot(stamp, p));
    }

    private static SerializedMapStamp FindStamp(Vector2 position)
    {
        return GameManager.Instance.SaveData.mapStamps.FirstOrDefault(s => SameSpot(s, position));
    }

    private static bool SameSpot(SerializedMapStamp stamp, Vector2 position)
    {
        return Mathf.Abs(stamp.x - position.x) < SamePlace && Mathf.Abs(stamp.z - position.y) < SamePlace;
    }

    // Targets don't move within a loaded game, so positions are found once per HarvestPOIManager (one per game load).
    private static Dictionary<string, Vector2> Positions()
    {
        var owner = GameManager.Instance?.HarvestPOIManager;
        if (owner == null)
        {
            return new Dictionary<string, Vector2>();
        }

        if (ReferenceEquals(owner, cachedFor))
        {
            return cachedPositions;
        }

        var conversations = Object.FindObjectsOfType<ConversationPOI>(true)
            .GroupBy(p => p.ConversationNodeName)
            .ToDictionary(g => g.Key ?? "", g => Flatten(g.First().transform.position));
        var docks = Object.FindObjectsOfType<Dock>(true)
            .GroupBy(d => d.name)
            .ToDictionary(g => g.Key, g => Flatten(g.First().transform.position));

        var positions = new Dictionary<string, Vector2>();
        foreach (var pair in Targets)
        {
            var source = pair.Value.kind == TargetKind.Conversation ? conversations : docks;
            if (source.TryGetValue(pair.Value.key, out var position))
            {
                positions[pair.Key] = position;
            }
        }

        foreach (var bottle in Object.FindObjectsOfType<ItemPOI>(true))
        {
            var itemId = bottle.Harvestable?.GetFirstItem()?.id;
            if (itemId != null && LocationHelper.TryParseLocation(itemId, out var location) &&
                LocationHelper.LocationNames.TryGetValue(location, out var name))
            {
                positions[name] = Flatten(bottle.transform.position);
            }
        }

        cachedFor = owner;
        cachedPositions = positions;
        return positions;
    }

    private static Vector2 Flatten(Vector3 position) => new(position.x, position.z);
}
