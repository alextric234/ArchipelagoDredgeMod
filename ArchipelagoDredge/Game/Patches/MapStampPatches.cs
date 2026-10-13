using System;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Tracker;
using HarmonyLib;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     Draws the tracker's map pins with the AP icon, on the map and through the spyglass, so they stand out from the
///     player's own stamps.
/// </summary>
[HarmonyPatch]
public static class MapStampPatches
{
    private const string ApIcon = "archipelago_icon";

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapStamp), nameof(MapStamp.Init))]
    private static void MapStamp_Init_Prefix(ref Sprite sprite, ref Color color, SerializedMapStamp stampData)
    {
        UseApIcon(ref sprite, ref color, stampData);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SpyglassMapStamp), nameof(SpyglassMapStamp.Init), typeof(Sprite), typeof(Color),
        typeof(SerializedMapStamp))]
    private static void SpyglassMapStamp_Init_Prefix(ref Sprite sprite, ref Color color, SerializedMapStamp stampData)
    {
        UseApIcon(ref sprite, ref color, stampData);
    }

    private static void UseApIcon(ref Sprite sprite, ref Color color, SerializedMapStamp stampData)
    {
        try
        {
            if (TrackerStamps.IsTrackerStamp(stampData))
            {
                sprite = ApHarvestIconHelper.GetIcon(ApIcon);
                color = Color.white;
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in MapStampPatches: {e}");
        }
    }
}
