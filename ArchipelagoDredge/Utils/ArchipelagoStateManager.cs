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
            StateData.LastProcessedIndex = StateData.LastProcessedIndexSinceSave;
            WinchCore.Log.Info($"Loaded Archipelago state for slot {slot} (last processed item index {StateData.LastProcessedIndex})");
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

    public static void IncrementLastProcessedIndex(int index)
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
        StateData.ReceivedVirtualItems = StateData.ReceivedVirtualItemsSinceSave.ToList();
    }

    public static bool HasVirtualItem(string itemKey)
    {
        return StateData.ReceivedVirtualItems?.Contains(itemKey) == true;
    }

    public static bool AddVirtualItem(string itemKey)
    {
        if (itemKey == "unknown")
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
}