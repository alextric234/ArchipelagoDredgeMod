using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using Archipelago.MultiClient.Net.Colors;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Models;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(DredgeDialogueRunner))]
public class DredgeDialogueRunnerPatches
{
    private static readonly MethodInfo LastQuestGridResultSetter =
        AccessTools.PropertySetter(typeof(DredgeDialogueRunner), "LastQuestGridResult");

    // In AP saves a pursuit's reward grid is replaced by its check. 0 is the result dialogue reads as "collected".
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DredgeDialogueRunner.ShowQuestGrid))]
    public static bool ShowQuestGrid_Prefix(DredgeDialogueRunner __instance, string param, ref IEnumerator __result)
    {
        try
        {
            if (!ApSave.IsActive || !LocationHelper.IsGridInSeed(param))
            {
                return true;
            }

            if (LocationHelper.TryGetRewardGridLocation(param, out var location))
            {
                WinchCore.Log.Info($"Reward grid {param} skipped; sending {LocationHelper.LocationNames[location]} instead");
                LocationHelper.ReportLocationCheck(location);
                GiveQuestItems(param);
            }
            else if (LocationHelper.SkippedGrids.Contains(param))
            {
                GiveQuestItems(param);
            }
            else if (param != LocationHelper.RareFishRewardGrid)
            {
                return true;
            }

            LastQuestGridResultSetter.Invoke(__instance, [0]);
            __result = Array.Empty<object>().GetEnumerator();
            return false;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in DredgeDialogueRunnerPatches.ShowQuestGrid_Prefix: {e}");
            return true;
        }
    }

    // Dialogue's AddItemById command calls this overload; the mod gives AP items through the grid overload, so those
    // aren't affected.
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DredgeDialogueRunner.AddItemById), typeof(string))]
    public static bool AddItemById_Prefix(string id)
    {
        try
        {
            if (!ApSave.IsActive)
            {
                return true;
            }

            // The Pale Reach logs are checks, but the log is still handed over, like a bottle's message.
            if (id.StartsWith("message-") && LocationHelper.TryParseLocation(id, out var log))
            {
                LocationHelper.ReportLocationCheck(log);
                return true;
            }

            if (!LocationHelper.DialogueRewardLocations.TryGetValue(id, out var location) ||
                !LocationHelper.IsInSeed(location))
            {
                return true;
            }

            WinchCore.Log.Info($"Dialogue reward {id} withheld; sending {LocationHelper.LocationNames[location]} instead");
            LocationHelper.ReportLocationCheck(location);
            return false;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in DredgeDialogueRunnerPatches.AddItemById_Prefix: {e}");
            return true;
        }
    }

    private static void GiveQuestItems(string gridName)
    {
        if (!LocationHelper.RewardGridQuestItems.TryGetValue(gridName, out var itemIds))
        {
            return;
        }

        foreach (var itemId in itemIds)
        {
            if (!ArchipelagoItemManager.TryGiveToHold(itemId))
            {
                WinchCore.Log.Warn($"No room for {itemId} from {gridName}");
                NotificationHelper.TryToSendNotification(new DredgeNotification
                {
                    Message = "No room anywhere for a quest item from here; it was not collected.",
                    MessageColor = PaletteColor.Red
                });
            }
        }
    }

    /// <summary>
    ///     The Researcher hands over a Sampling Device straight after taking the prototype components, and that's an
    ///     AP item. In AP saves the components can't be handed in until the Sampling Device has arrived from the
    ///     multiworld, so Research Assistant waits for it; the handover grid is then skipped (see SkippedGrids).
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DredgeDialogueRunner.GetNumItemInInventoryById))]
    public static void GetNumItemInInventoryById_Postfix(string id, ref int __result)
    {
        if (id == "quest-components" && ApSave.IsActive && !ApSave.ReceivedItemNames.Contains("Sampling Device"))
        {
            __result = 0;
        }
    }

    // The Collector's abilities are AP items in AP saves.
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DredgeDialogueRunner.UnlockAbility))]
    public static bool UnlockAbility_Prefix(string abilityName)
    {
        if (!ApSave.IsActive || !ItemNames.AbilityIds.Values.Contains(abilityName))
        {
            return true;
        }

        WinchCore.Log.Info($"Ability {abilityName} withheld; it comes from the multiworld");
        return false;
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