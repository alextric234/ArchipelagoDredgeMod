using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Game.Helpers;
using UnityEngine;

namespace ArchipelagoDredge.Game.Tracker;

/// <summary>
///     Finds what in the world a tracker location refers to: the harvest spots to pin on the map, and the fish for its
///     encyclopedia page.
/// </summary>
public static class TrackerTargets
{
    private const string EncyclopediaGroup = "Encyclopedia";
    private const string DredgeGroup = "Dredge";

    // Location enum names are game ids upper-cased with '-' as '_'.
    private static string Key(string id) => id.ToUpperInvariant().Replace('-', '_');

    private static bool TryGetLocationKey(string locationName, out string key)
    {
        key = LocationHelper.LocationsByName.TryGetValue(locationName, out var location) ? location.ToString() : null;
        return key != null;
    }

    public static FishItemData GetFish(TrackerEntry entry)
    {
        if (entry.LocationGroup != EncyclopediaGroup || !TryGetLocationKey(entry.Name, out var key))
        {
            return null;
        }

        return GameManager.Instance.ItemManager.allItems
            .OfType<FishItemData>()
            .FirstOrDefault(fish => Key(fish.id) == key);
    }

    /// <summary>Harvest spots for a dredge location, or every spot that holds a fish location's fish.</summary>
    public static List<HarvestPOI> GetSpots(TrackerEntry entry)
    {
        var lookup = GameManager.Instance.HarvestPOIManager?.harvestPOILookup;
        if (lookup == null)
        {
            return [];
        }

        if (entry.LocationGroup == DredgeGroup)
        {
            return TryGetLocationKey(entry.Name, out var key)
                ? lookup.Where(p => p.Value != null && Key(p.Key) == key).Select(p => p.Value).ToList()
                : [];
        }

        var fish = GetFish(entry);
        if (fish == null)
        {
            return [];
        }

        // Aberrations turn up at their parent fish's spots.
        var parent = fish.IsAberration ? fish.NonAberrationParent : null;
        return lookup.Values
            .Where(poi => poi != null && !poi.IsCrabPotPOI &&
                          (poi.Harvestable.ContainsItem(fish) || (parent != null && poi.Harvestable.ContainsItem(parent))))
            .ToList();
    }

    public static bool AreAllPinned(List<HarvestPOI> spots)
    {
        var markers = GameManager.Instance.SaveData.harvestPOIMapMarkers;
        return spots.Count > 0 && spots.All(poi => markers.Contains(poi.Harvestable.GetId()));
    }

    /// <summary>
    ///     Unpins the spots if all are pinned, otherwise pins the unpinned ones nearest the boat first, up to the map
    ///     marker limit. Returns what happened.
    /// </summary>
    public static string TogglePins(List<HarvestPOI> spots)
    {
        var saveData = GameManager.Instance.SaveData;
        if (AreAllPinned(spots))
        {
            spots.ForEach(poi => saveData.RemoveHarvestPOIMarker(poi.Harvestable.GetId()));
            return spots.Count == 1 ? "Unpinned the spot." : $"Unpinned {spots.Count} spots.";
        }

        var room = (int) GameManager.Instance.GameConfigData.MaxNumMapMarkers - saveData.GetTotalUserPlacedMapMarkers();
        var boat = GameManager.Instance.Player.transform.position;
        var toPin = spots
            .Where(poi => !saveData.harvestPOIMapMarkers.Contains(poi.Harvestable.GetId()))
            .OrderBy(poi => Vector3.Distance(poi.transform.position, boat))
            .Take(Mathf.Max(room, 0))
            .ToList();
        toPin.ForEach(poi => saveData.AddHarvestPOIMarker(poi.Harvestable.GetId()));

        var pinned = spots.Count(poi => saveData.harvestPOIMapMarkers.Contains(poi.Harvestable.GetId()));
        if (pinned < spots.Count)
        {
            return $"Pinned {pinned} of {spots.Count} spots; the map is out of markers. Remove some to pin the rest.";
        }

        return spots.Count == 1 ? "Pinned the spot on your map." : $"Pinned {spots.Count} spots on your map.";
    }
}
