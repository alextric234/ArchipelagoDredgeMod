using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     Hints every research check on a research tab when it's shown, so players can see which branches hold
///     progression before spending research parts.
/// </summary>
[HarmonyPatch(typeof(ResearchPanel))]
public static class ResearchPanelPatches
{
    private static readonly AccessTools.FieldRef<ResearchableEntry, SpatialItemData> EntryItemData =
        AccessTools.FieldRefAccess<ResearchableEntry, SpatialItemData>("spatialItemData");

    // The server only announces each hint once, but this saves re-sending scouts every time a tab opens.
    private static readonly HashSet<long> HintedLocationIds = [];
    private static ArchipelagoSession hintedSession;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ResearchPanel.ShowFinish))]
    private static void ShowFinish_Postfix(ResearchPanel __instance)
    {
        try
        {
            if (!ArchipelagoClient.IsConnected)
            {
                return;
            }

            var session = ArchipelagoClient.Session;
            if (hintedSession != session)
            {
                hintedSession = session;
                HintedLocationIds.Clear();
            }

            var checkedIds = new HashSet<long>(session.Locations.AllLocationsChecked);
            var idsToHint = __instance.GetComponentsInChildren<ResearchableEntry>(true)
                .Select(entry => EntryItemData(entry))
                .Where(itemData => itemData != null)
                .Select(itemData => LocationHelper.TryParseLocation(itemData.id, out var location) &&
                                    LocationHelper.TryGetArchipelagoId(location, out var id)
                    ? id
                    : (long?)null)
                .Where(id => id.HasValue && !checkedIds.Contains(id.Value) && HintedLocationIds.Add(id.Value))
                .Select(id => id.Value)
                .ToArray();

            if (idsToHint.Length == 0)
            {
                return;
            }

            session.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, idsToHint)
                .ContinueWith(task => WinchCore.Log.Error($"Error hinting research locations: {task.Exception}"),
                    System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in ResearchPanelPatches.ShowFinish_Postfix: {e}");
        }
    }
}
