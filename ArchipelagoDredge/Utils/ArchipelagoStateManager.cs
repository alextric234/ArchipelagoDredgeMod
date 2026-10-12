using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArchipelagoDredge.Game.Managers;
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
            RevertStateData();
            ApplySavedSlotOptions();
            AberrationChanceManager.SyncConfigFromSave();
            WinchCore.Log.Info($"Loaded Archipelago state for slot {slot} (last processed item index {StateData.LastProcessedIndex}, " +
                               $"hull tier {StateData.CurrentHullUpgrade}, {StateData.ReceivedVirtualItems.Count} virtual item(s), " +
                               (IsBound ? $"bound to seed '{StateData.Seed}' as '{StateData.SlotName}')" : "not bound to a multiworld)"));
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error(ex);
        }
    }

    public static void CreateNew(int slot)
    {
        try
        {
            SaveFilePath = GetSaveFilePath(slot);
            StateData = new ArchipelagoStateData();
            ApplySavedSlotOptions();
            AberrationChanceManager.SyncConfigFromSave();
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
        StateData.PendingTrapsSinceSave = StateData.PendingTraps.ToList();
        SaveData();
    }

    public static void RevertStateData()
    {
        StateData.LastProcessedIndex = StateData.LastProcessedIndexSinceSave;
        StateData.CurrentHullUpgrade = StateData.HullUpgradeSinceSave;
        StateData.ReceivedVirtualItems = StateData.ReceivedVirtualItemsSinceSave?.ToList() ?? new List<string>();
        StateData.PendingTraps = StateData.PendingTrapsSinceSave?.ToList() ?? new List<string>();
    }

    public static void SetBoostedAberrationsEnabled(bool enabled)
    {
        if (StateData == null)
        {
            return;
        }

        StateData.BoostedAberrationsEnabled = enabled;
        SaveData();
    }

    public static IReadOnlyList<string> PendingTraps =>
        (IReadOnlyList<string>) StateData?.PendingTraps ?? Array.Empty<string>();

    public static void AddPendingTrap(string trapName)
    {
        if (StateData == null)
        {
            return;
        }

        StateData.PendingTraps ??= new List<string>();
        StateData.PendingTraps.Add(trapName);
        SaveData();
    }

    public static void RemovePendingTrap(string trapName)
    {
        if (StateData?.PendingTraps?.Remove(trapName) == true)
        {
            SaveData();
        }
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

    public static void SaveSessionSnapshot(SlotData slotData, IEnumerable<string> receivedItemNames)
    {
        if (StateData == null)
        {
            return;
        }

        StateData.SlotDeathLink = slotData.DeathLink;
        StateData.SlotIncludeIronRigDLC = slotData.IncludeIronRigDLC;
        StateData.SlotIncludePaleReachDLC = slotData.IncludePaleReachDLC;
        StateData.SlotAddFishingLicenses = slotData.AddFishingLicenses;
        StateData.SlotAddPassageItems = slotData.AddPassageItems;
        StateData.SlotProgressiveAberrationChanceItems = slotData.ProgressiveAberrationChanceItems;
        StateData.ReceivedItemNames = receivedItemNames.ToList();
        SaveData();
    }

    public static IReadOnlyList<string> SavedReceivedItemNames =>
        (IReadOnlyList<string>) StateData?.ReceivedItemNames ?? Array.Empty<string>();

    private static void ApplySavedSlotOptions()
    {
        var state = StateData ?? new ArchipelagoStateData();
        ArchipelagoClient.SlotData = new SlotData(
            state.SlotDeathLink,
            state.SlotIncludeIronRigDLC,
            state.SlotIncludePaleReachDLC,
            state.SlotAddFishingLicenses,
            state.SlotAddPassageItems,
            state.SlotProgressiveAberrationChanceItems);
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

    // Received traps that haven't fired yet, reverted with the rest of the state when a save is reloaded.
    public List<string> PendingTrapsSinceSave = new();
    public List<string> PendingTraps = new();
    public string Seed = "";
    public string SlotName = "";
    public List<string> CheckedLocations = new();
    public List<string> ReceivedItemNames = new();

    // Slot options from the last connection, so AP rules still apply offline and before the connection finishes.
    // Kept as plain fields: Unity's JsonUtility silently drops a nested options object from a mod assembly.
    public bool SlotDeathLink;
    public bool SlotIncludeIronRigDLC;
    public bool SlotIncludePaleReachDLC;
    public bool SlotAddFishingLicenses;
    public bool SlotAddPassageItems;
    public int SlotProgressiveAberrationChanceItems;

    public bool BoostedAberrationsEnabled = true;
}