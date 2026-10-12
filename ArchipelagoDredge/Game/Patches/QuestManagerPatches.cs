using System;
using System.Collections.Generic;
using System.Linq;
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
    private const string StrangeFishQuestId = "Quest_StrangeFish";
    private const string HandkerchiefId = "handkerchief";

    private static bool isPreparingPursuits;

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
        PreparePursuits();
    }

    /// <summary>
    ///     Removes the aberration that pursuits normally hinge on. Strange Fish (Grotesque Fish) needs an aberrant fish
    ///     traded to the fishmonger for a handkerchief before the Collector will meet you, and A Place to Rest waits for
    ///     it. In AP saves those steps are done for you and the handkerchief handed over, so meeting the Collector at
    ///     Outcast Isle is all Grotesque Fish needs, and A Place to Rest is offered from the start.
    /// </summary>
    public static void PreparePursuits()
    {
        var questManager = GameManager.Instance?.QuestManager;
        if (!ApSave.IsActive || questManager == null || isPreparingPursuits)
        {
            return;
        }

        // Offering and completing steps raise quest events that come back through EvaluateUnavailableQuests.
        isPreparingPursuits = true;
        try
        {
            if (questManager.GetQuestStateById(APlaceToRestQuestId) == QuestState.UNAVAILABLE)
            {
                WinchCore.Log.Info($"Offering {APlaceToRestQuestId} without Strange Fish");
                questManager.OfferQuest(APlaceToRestQuestId);
            }

            SkipToCollectorMeeting(questManager);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error preparing pursuits: {e}");
        }
        finally
        {
            isPreparingPursuits = false;
        }
    }

    private static void SkipToCollectorMeeting(QuestManager questManager)
    {
        if (questManager.GetQuestStateById(StrangeFishQuestId) == QuestState.UNAVAILABLE)
        {
            questManager.OfferQuest(StrangeFishQuestId);
        }

        if (questManager.GetIsQuestStepActive("StrangeFish_Catch"))
        {
            questManager.CompleteQuestStep("StrangeFish_Catch", suppressNotifications: true);
        }

        if (questManager.GetIsQuestStepActive("StrangeFish_Fishmonger"))
        {
            questManager.CompleteQuestStep("StrangeFish_Fishmonger", suppressNotifications: true);
            GameManager.Instance.SaveData.AddMapMarker("blackstone-isle");
        }

        if (questManager.GetIsQuestStepActive("StrangeFish_CollectorMeet") && !HasHandkerchief())
        {
            GiveHandkerchief();
        }
    }

    private static bool HasHandkerchief()
    {
        var saveData = GameManager.Instance.SaveData;
        return saveData.Inventory.spatialItems.Any(i => i.id == HandkerchiefId) ||
               saveData.Storage.spatialItems.Any(i => i.id == HandkerchiefId);
    }

    private static void GiveHandkerchief()
    {
        var saveData = GameManager.Instance.SaveData;
        var item = GameManager.Instance.ItemManager.GetItemDataById<SpatialItemData>(HandkerchiefId);
        var grid = saveData.Inventory.FindPositionForObject(item, out _) ? saveData.Inventory
            : saveData.Storage.FindPositionForObject(item, out _) ? saveData.Storage
            : null;
        if (grid == null)
        {
            WinchCore.Log.Warn("No room for the handkerchief; trying again later");
            return;
        }

        GameManager.Instance.ItemManager.AddItemById(HandkerchiefId, grid);
        NotificationHelper.TryToSendNotification(new DredgeNotification
        {
            Message = "Take the handkerchief to the Collector at Outcast Isle."
        });
    }
}
