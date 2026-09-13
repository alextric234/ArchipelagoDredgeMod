using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(QuestManager))]
public class QuestManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(QuestManager.CompleteQuestStep))]
    public static void CompleteQuestStep(string questStepId)
    {
        LocationHelper.ReportLocationCheck(questStepId);
    }
}
