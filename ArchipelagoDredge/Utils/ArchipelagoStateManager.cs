using System;
using System.IO;
using UnityEngine;

namespace ArchipelagoDredge.Utils;

public static class ArchipelagoStateManager
{
    private static readonly string SaveFile = "ArchipelagoState";
    private static string SaveFilePath;
    public static ArchipelagoStateData StateData;

    public static void Load(int slot)
    {
        SaveFilePath = Path.Combine(Application.persistentDataPath, SaveFile, $"{SaveFile}-{slot}.json");
        if (File.Exists(SaveFilePath))
        {
            var json = File.ReadAllText(SaveFilePath);
            StateData = JsonUtility.FromJson<ArchipelagoStateData>(json);
            StateData.LastProcessedIndex = StateData.LastProcessedIndexSinceSave;
        }
        else
        {
            StateData = new ArchipelagoStateData();

            Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath)!);

            var defaultJson = JsonUtility.ToJson(StateData, true);
            File.WriteAllText(SaveFilePath, defaultJson);
        }
    }

    public static void SaveData()
    {
        var updatedJson = JsonUtility.ToJson(StateData, true);
        File.WriteAllText(SaveFilePath, updatedJson);
    }

    public static void Delete(int slot)
    {
        SaveFilePath = Path.Combine(Application.persistentDataPath, SaveFile, $"{SaveFile}-{slot}.json");
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
        }
    }
}

[Serializable]
public class ArchipelagoStateData
{
    public int LastProcessedIndexSinceSave = -1;
    public int LastProcessedIndex = -1;
    public int CurrentHullUpgrade;
    public int HullUpgradeSinceSave;
}