using System;
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
            ArchipelagoStateManager.PersistStateData();
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error(ex);
        }
    }

    // LoadIntoMemory runs for every slot (title screen, save-slot previews), so load AP state
    // only from the calls that actually start a game: Load(slot) or CreateSaveData(slot).
    [HarmonyPostfix]
    [HarmonyPatch(nameof(SaveManager.Load))]
    public static void LoadPost(int slot, bool __result)
    {
        if (!__result)
        {
            return;
        }

        ArchipelagoStateManager.Load(slot);
    }

    // Prefix: CreateSaveData saves the new game internally, and SaveGameFilePost must
    // persist the fresh state rather than whatever was loaded before.
    [HarmonyPrefix]
    [HarmonyPatch(nameof(SaveManager.CreateSaveData))]
    public static void CreateSaveDataPre(int slot)
    {
        ArchipelagoStateManager.CreateNew(slot);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(SaveManager.DeleteSaveFile))]
    public static void DeleteSaveFilePost(int slot)
    {
        try
        {
            ArchipelagoStateManager.Delete(slot);
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error(ex);
        }
    }
}