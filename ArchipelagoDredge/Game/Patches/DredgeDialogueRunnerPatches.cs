﻿using ArchipelagoDredge.Network;
using HarmonyLib;
using System;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(DredgeDialogueRunner))]
public class DredgeDialogueRunnerPatches
{
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