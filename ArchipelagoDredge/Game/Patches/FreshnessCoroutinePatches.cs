using System;
using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Game.Helpers;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     Keeps <see cref="MultiworldFish" /> fresh: they're lifted out of rot range for the decay pass,
///     then set back to full freshness afterwards. Winch replaces the decay pass with its own prefix,
///     so ours has to run first.
/// </summary>
[HarmonyPatch(typeof(FreshnessCoroutine))]
public static class FreshnessCoroutinePatches
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch("AdjustFreshnessForGrid")]
    private static void AdjustFreshnessForGrid_Prefix(SerializableGrid grid, out List<FishItemInstance> __state)
    {
        __state = [];
        try
        {
            __state = grid.spatialItems.Where(MultiworldFish.IsMarked).Cast<FishItemInstance>().ToList();
            __state.ForEach(fish => fish.freshness = float.MaxValue);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in FreshnessCoroutinePatches.AdjustFreshnessForGrid_Prefix: {e}");
        }
    }

    [HarmonyFinalizer]
    [HarmonyPatch("AdjustFreshnessForGrid")]
    private static Exception AdjustFreshnessForGrid_Finalizer(Exception __exception, List<FishItemInstance> __state)
    {
        var maxFreshness = GameManager.Instance.GameConfigData.MaxFreshness;
        __state?.ForEach(fish => fish.freshness = maxFreshness);
        return __exception;
    }
}
