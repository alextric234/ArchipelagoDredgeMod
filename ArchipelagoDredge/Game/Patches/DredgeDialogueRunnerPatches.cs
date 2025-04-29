using System;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(DredgeDialogueRunner))]
public class DredgeDialogueRunnerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DredgeDialogueRunner.DoFinalePreparations))]
    public static void DoFinalePreparationsPost()
    {
        WinchCore.Log.Info("Finale prepared. Sending goal");
        try
        {
            ArchipelagoClient.Session.SetGoalAchieved();
        }
        catch (Exception e)
        {
            WinchCore.Log.Error("Failed to set goal");
            WinchCore.Log.Info(e);
        }
    }
}