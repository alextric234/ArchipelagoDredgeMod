using HarmonyLib;
using ArchipelagoDredge.Game.Helpers;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(QuestManager))]
public class QuestManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(QuestManager.CompleteQuestStep))]
    public static void CompleteQuestStep(string questStepId)
    {
        LocationNames.TryParseLocation(questStepId, out var questLocation);

        if (LocationNames.QuestLocations.Contains(questLocation))
        {
            WinchCore.Log.Info($"{questStepId} location found and completed");
        }
    }
}
