using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Utils;

public static class ArchipelagoStateManager
{
    private static readonly string SaveFile = "ArchipelagoState";
    private static string SaveFilePath;
    public static ArchipelagoStateData StateData;

    public static bool AwaitingDeathScreenChoice { get; internal set; }

    private static string GetSaveFilePath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, SaveFile, $"{SaveFile}-{slot}.json");
    }

    /// <summary>
    ///     Loads the state for the save slot being played. Creates a default state if none exists.
    /// </summary>
    public static void Load(int slot)
    {
        try
        {
            var path = GetSaveFilePath(slot);
            if (!File.Exists(path))
            {
                CreateNew(slot);
                return;
            }

            SaveFilePath = path;
            var json = File.ReadAllText(SaveFilePath);
            StateData = JsonUtility.FromJson<ArchipelagoStateData>(json);
            // The file is rewritten after every processed item, but the game save may be older (quit
            // without saving, death screen). Go back to the values recorded at the last game save so
            // the item index, hull tier and virtual items all match it.
            RevertStateData();
            WinchCore.Log.Info($"Loaded Archipelago state for slot {slot} (last processed item index {StateData.LastProcessedIndex}, " +
                               $"hull tier {StateData.CurrentHullUpgrade}, {StateData.ReceivedVirtualItems.Count} virtual item(s))");
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error(ex);
        }
    }

    /// <summary>
    ///     Starts a fresh state for a new game in this slot, replacing any leftover file.
    /// </summary>
    public static void CreateNew(int slot)
    {
        try
        {
            SaveFilePath = GetSaveFilePath(slot);
            StateData = new ArchipelagoStateData();
            Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath)!);
            SaveData();
            WinchCore.Log.Info($"Created new Archipelago state for slot {slot}");
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error(ex);
        }
    }

    private static void SaveData()
    {
        var updatedJson = JsonUtility.ToJson(StateData, true);
        File.WriteAllText(SaveFilePath, updatedJson);
    }

    public static void Delete(int slot)
    {
        var path = GetSaveFilePath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static void SetLastProcessedIndex(int index)
    {
        StateData.LastProcessedIndex = index;
        SaveData();
    }

    public static void PersistStateData()
    {
        StateData.LastProcessedIndexSinceSave = StateData.LastProcessedIndex;
        StateData.HullUpgradeSinceSave = StateData.CurrentHullUpgrade;
        StateData.ReceivedVirtualItemsSinceSave = StateData.ReceivedVirtualItems.ToList();
        SaveData();
    }

    public static void RevertStateData()
    {
        StateData.LastProcessedIndex = StateData.LastProcessedIndexSinceSave;
        StateData.CurrentHullUpgrade = StateData.HullUpgradeSinceSave;
        StateData.ReceivedVirtualItems = StateData.ReceivedVirtualItemsSinceSave?.ToList() ?? new List<string>();
    }

    public static bool HasVirtualItem(string itemKey)
    {
        return StateData.ReceivedVirtualItems?.Contains(itemKey) == true;
    }

    public static bool AddVirtualItem(string itemKey)
    {
        if (string.IsNullOrEmpty(itemKey))
        {
            WinchCore.Log.Error($"Unknown virtual item");
            return false;
        }
        StateData.ReceivedVirtualItems ??= new List<string>();

        if (StateData.ReceivedVirtualItems.Contains(itemKey))
        {
            WinchCore.Log.Error($"Already received virtual item");
            return false;
        }

        StateData.ReceivedVirtualItems.Add(itemKey);
        SaveData();
        return true;
    }

    /// <summary>
    ///     Locations checked in this save, by AP location name. Written as soon as a check happens and
    ///     never reverted (AP checks only ever add up), so checks made offline survive a restart.
    /// </summary>
    public static IReadOnlyList<string> CheckedLocations =>
        (IReadOnlyList<string>) StateData?.CheckedLocations ?? Array.Empty<string>();

    public static void RecordCheckedLocation(string locationName)
    {
        if (StateData == null)
        {
            return;
        }

        StateData.CheckedLocations ??= new List<string>();
        if (StateData.CheckedLocations.Contains(locationName))
        {
            return;
        }

        StateData.CheckedLocations.Add(locationName);
        SaveData();
    }

    /// <summary>
    ///     Ties this save's recorded checks to the multiworld being played. The first connect claims them
    ///     for that seed; connecting to a different seed drops them so they aren't sent to the wrong room.
    /// </summary>
    public static void BindChecksToSeed(string seed)
    {
        if (StateData == null || string.IsNullOrEmpty(seed) || StateData.Seed == seed)
        {
            return;
        }

        if (!string.IsNullOrEmpty(StateData.Seed))
        {
            WinchCore.Log.Warn($"This save was used with seed '{StateData.Seed}' but is now connected to '{seed}'; " +
                               $"discarding {StateData.CheckedLocations?.Count ?? 0} location check(s) recorded for the old seed");
            StateData.CheckedLocations = new List<string>();
        }

        StateData.Seed = seed;
        SaveData();
    }
}

[Serializable]
public class ArchipelagoStateData
{
    public int LastProcessedIndexSinceSave = -1;
    public int LastProcessedIndex = -1;
    public int CurrentHullUpgrade;
    public int HullUpgradeSinceSave;

    public List<string> ReceivedVirtualItemsSinceSave = new();
    public List<string> ReceivedVirtualItems = new();

    // The multiworld seed CheckedLocations belongs to; empty until the save first connects.
    public string Seed = "";
    public List<string> CheckedLocations = new();
}