using System;
using System.Collections.Generic;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Models;
using ArchipelagoDredge.Utils;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(QuestManager))]
public class QuestManagerPatches
{
    private const string APlaceToRestQuestId = "Quest_APlaceToRest";

    private sealed class Rollback(string questId, string failingStepId, string pickupStepId, string message)
    {
        public string QuestId { get; } = questId;
        public string FailingStepId { get; } = failingStepId;

        // Null rolls the whole pursuit back to being offered.
        public string PickupStepId { get; } = pickupStepId;
        public string Message { get; } = message;
    }

    // Losing these mid-pursuit fails it for good in vanilla, which would make its check impossible.
    private static readonly Dictionary<string, Rollback> RollbacksByItemId = new()
    {
        ["quest-builder"] = new Rollback(APlaceToRestQuestId, "APlaceToRest_Escort", "APlaceToRest_Return",
            "The Builder was lost. Pick her up again at Greater Marrow."),
        ["quest-package"] = new Rollback("Quest_PackageDelivery", "PackageDelivery_Deliver", null,
            "The package was lost. Collect another from the Mayor at Greater Marrow.")
    };

    [HarmonyPrefix]
    [HarmonyPatch(nameof(QuestManager.CompleteQuestStep))]
    public static void CompleteQuestStep(string questStepId)
    {
        WinchCore.Log.Debug($"Quest Step completed: {questStepId}");
        if (LocationHelper.TryGetQuestStepLocation(questStepId, out var location))
        {
            LocationHelper.ReportLocationCheck(location);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnItemDestroyed")]
    public static bool OnItemDestroyed_Prefix(QuestManager __instance, ItemData itemData)
    {
        try
        {
            if (!ApSave.IsActive || itemData == null ||
                !RollbacksByItemId.TryGetValue(itemData.id, out var rollback) ||
                !__instance.GetIsQuestStepActive(rollback.FailingStepId))
            {
                return true;
            }

            var entry = __instance.GetQuestEntryById(rollback.QuestId);
            if (rollback.PickupStepId != null)
            {
                entry.completedStepIds.Remove(rollback.PickupStepId);
                entry.activeStepId = rollback.PickupStepId;
            }
            else
            {
                entry.state = QuestState.OFFERED;
                entry.activeStepId = __instance.GetQuestDataById(rollback.QuestId).onOfferedQuestStep?.name;
            }

            WinchCore.Log.Info($"{itemData.id} lost; rolled {rollback.QuestId} back to {entry.activeStepId}");
            GameEvents.Instance.TriggerQuestStateChanged(rollback.QuestId, entry.state);
            NotificationHelper.TryToSendNotification(new DredgeNotification { Message = rollback.Message });
            return false;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in QuestManagerPatches.OnItemDestroyed_Prefix: {e}");
            return true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("EvaluateUnavailableQuests")]
    public static void EvaluateUnavailableQuests_Postfix()
    {
        OfferPursuitsWithoutPrerequisites();
    }

    /// <summary>
    ///     A Place to Rest is normally offered only after Strange Fish, which needs an aberration. In AP saves it's
    ///     offered from the start so its check only needs the Dredge Crane.
    /// </summary>
    public static void OfferPursuitsWithoutPrerequisites()
    {
        try
        {
            var questManager = GameManager.Instance?.QuestManager;
            if (!ApSave.IsActive || questManager == null ||
                questManager.GetQuestStateById(APlaceToRestQuestId) != QuestState.UNAVAILABLE)
            {
                return;
            }

            WinchCore.Log.Info($"Offering {APlaceToRestQuestId} without Strange Fish");
            questManager.OfferQuest(APlaceToRestQuestId);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error offering pursuits: {e}");
        }
    }
}
