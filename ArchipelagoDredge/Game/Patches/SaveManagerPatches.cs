using ArchipelagoDredge.Utils;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(SaveManager))]
public static class SaveManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(SaveManager.SaveGameFile))]
    public static void SaveGameFilePost(SaveManager __instance, bool useBackupHistory)
    {
        try
        {
            ArchipelagoStateManager.StateData.LastProcessedIndexSinceSave =
                ArchipelagoStateManager.StateData.LastProcessedIndex;
            ArchipelagoStateManager.SaveData();
        }
        catch (System.Exception ex)
        {
            WinchCore.Log.Error(ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(SaveManager.LoadIntoMemory))]
    public static void LoadIntoMemoryPost(int slot)
    {
        try
        {
            ArchipelagoStateManager.Load(slot);
        }
        catch (System.Exception ex)
        {
            WinchCore.Log.Error(ex);
        }
    }
}