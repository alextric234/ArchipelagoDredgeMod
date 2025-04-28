using System;
using System.IO;
using ArchipelagoDredge.Network;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Utils;

public static class ArchipelagoStateManager
{
    private static readonly string SaveFile = "ArchipelagoState";
    private static string SaveFilePath;
    public static ArchipelagoStateData StateData;

    public static void Load()
    {
        var slot = ArchipelagoClient.Session.Players.ActivePlayer.Name;
        SaveFilePath = Path.Combine(Application.persistentDataPath, SaveFile, $"{SaveFile}-{slot}.json");
        if (File.Exists(SaveFilePath))
        {
            string json = File.ReadAllText(SaveFilePath);
            WinchCore.Log.Info(json);
            StateData = JsonUtility.FromJson<ArchipelagoStateData>(json);
            StateData.LastProcessedIndex = StateData.LastProcessedIndexSinceSave;
        }
        else
        {
            StateData = new ArchipelagoStateData();

            Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath)!);

            string defaultJson = JsonUtility.ToJson(StateData, true);
            File.WriteAllText(SaveFilePath, defaultJson);
        }

    }
    public static void SaveData()
    {
        string updatedJson = JsonUtility.ToJson(StateData, true);
        WinchCore.Log.Info(updatedJson);
        File.WriteAllText(SaveFilePath, updatedJson);
    }

    public static void DeleteData()
    {
        File.Delete(SaveFilePath);
    }
}

[Serializable]
public class ArchipelagoStateData
{
    public int LastProcessedIndexSinceSave = -1;
    public int LastProcessedIndex = -1;
}