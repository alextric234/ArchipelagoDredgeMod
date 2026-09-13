using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(QuestManager))]
public class QuestManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(QuestManager.CompleteQuestStep))]
    public static void CompleteQuestStep(string questStepId)
    {
        WinchCore.Log.Info($"Quest Step completed: {questStepId}");
        LocationHelper.ReportLocationCheck(questStepId);
    }
}
