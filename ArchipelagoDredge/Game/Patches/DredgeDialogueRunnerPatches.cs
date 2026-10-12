using System;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(DredgeDialogueRunner))]
public class DredgeDialogueRunnerPatches
{
    // Dialogue's AddItemById command calls this overload; the mod gives AP items through the grid overload, so those
    // aren't affected.
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DredgeDialogueRunner.AddItemById), typeof(string))]
    public static bool AddItemById_Prefix(string id)
    {
        try
        {
            if (!ApSave.IsActive || !LocationHelper.BookRewardLocations.TryGetValue(id, out var location) ||
                !IsInSeed(location))
            {
                return true;
            }

            WinchCore.Log.Info($"Pursuit reward {id} withheld; sending {LocationHelper.LocationNames[location]} instead");
            LocationHelper.ReportLocationCheck(location);
            return false;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in DredgeDialogueRunnerPatches.AddItemById_Prefix: {e}");
            return true;
        }
    }

    // Without the Pale Reach in the seed its book isn't an AP item, so the game should still hand it out.
    private static bool IsInSeed(Location location)
    {
        return location != Location.PURSUIT_FIGURE_IN_WHITE || ArchipelagoClient.SlotData.IncludePaleReachDLC;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(DredgeDialogueRunner.DoFinalePreparations))]
    public static void DoFinalePreparationsPost()
    {
        try
        {
            ArchipelagoClient.Session.SetGoalAchieved();
        }
        catch (Exception e)
        {
            WinchCore.Log.Error("Failed to set goal");
            WinchCore.Log.Error(e);
        }
    }
}