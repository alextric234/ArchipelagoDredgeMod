using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Network.Enums;
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
            ApplySavedSlotOptions();
            WinchCore.Log.Info($"Loaded Archipelago state for slot {slot} (last processed item index {StateData.LastProcessedIndex}, " +
                               $"hull tier {StateData.CurrentHullUpgrade}, {StateData.ReceivedVirtualItems.Count} virtual item(s), " +
                               (IsBound ? $"bound to seed '{StateData.Seed}' as '{StateData.SlotName}')" : "not bound to a multiworld)"));
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
            ApplySavedSlotOptions();
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
    ///     True once this save has connected to a multiworld. A bound save follows AP rules even offline.
    /// </summary>
    public static bool IsBound => !string.IsNullOrEmpty(StateData?.Seed);

    /// <summary>
    ///     Binds this save to the multiworld being played. The first connect claims it. Returns a description
    ///     of the previous binding when the save was bound to a different seed or slot (it's rebound, not
    ///     blocked), otherwise null. Checks recorded for a different seed are dropped so they aren't sent
    ///     to the wrong room.
    /// </summary>
    public static string BindToRoom(string seed, string slotName)
    {
        if (StateData == null || string.IsNullOrEmpty(seed))
        {
            return null;
        }

        if (StateData.Seed == seed && StateData.SlotName == slotName)
        {
            return null;
        }

        string previousBinding = null;
        if (IsBound)
        {
            previousBinding = $"seed '{StateData.Seed}', slot '{StateData.SlotName}'";
            if (StateData.Seed != seed)
            {
                WinchCore.Log.Warn($"Discarding {StateData.CheckedLocations?.Count ?? 0} location check(s) recorded for seed '{StateData.Seed}'");
                StateData.CheckedLocations = new List<string>();
            }
        }

        StateData.Seed = seed;
        StateData.SlotName = slotName ?? "";
        SaveData();
        return previousBinding;
    }

    /// <summary>
    ///     Saves what an offline session needs to keep following AP rules: the seed's options and the names
    ///     of the AP items received so far. Call while connected.
    /// </summary>
    public static void SaveSessionSnapshot(SlotData slotData, IEnumerable<string> receivedItemNames)
    {
        if (StateData == null)
        {
            return;
        }

        StateData.SlotOptions = new SavedSlotOptions
        {
            DeathLink = slotData.DeathLink,
            IncludeIronRigDLC = slotData.IncludeIronRigDLC,
            IncludePaleReachDLC = slotData.IncludePaleReachDLC,
            AddFishingLicenses = slotData.AddFishingLicenses,
            AddPassageItems = slotData.AddPassageItems
        };
        StateData.ReceivedItemNames = receivedItemNames.ToList();
        SaveData();
    }

    /// <summary>
    ///     AP item names received as of the last snapshot; used for shop stock and research while offline.
    /// </summary>
    public static IReadOnlyList<string> SavedReceivedItemNames =>
        (IReadOnlyList<string>) StateData?.ReceivedItemNames ?? Array.Empty<string>();

    // Use the saved seed options until (and unless) a connect replaces them, so slot-dependent rules
    // (passages, licences, DLC content) apply offline. An unbound save gets the all-off defaults.
    private static void ApplySavedSlotOptions()
    {
        var options = StateData?.SlotOptions ?? new SavedSlotOptions();
        ArchipelagoClient.SlotData = new SlotData(
            options.DeathLink,
            options.IncludeIronRigDLC,
            options.IncludePaleReachDLC,
            options.AddFishingLicenses,
            options.AddPassageItems);
    }
}

[Serializable]
public class SavedSlotOptions
{
    public bool DeathLink;
    public bool IncludeIronRigDLC;
    public bool IncludePaleReachDLC;
    public bool AddFishingLicenses;
    public bool AddPassageItems;
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

    // The multiworld this save is bound to; empty until the save first connects.
    public string Seed = "";
    public string SlotName = "";
    public List<string> CheckedLocations = new();

    // Snapshot from the last connected session, so a bound save follows AP rules offline.
    public SavedSlotOptions SlotOptions = new();
    public List<string> ReceivedItemNames = new();
}