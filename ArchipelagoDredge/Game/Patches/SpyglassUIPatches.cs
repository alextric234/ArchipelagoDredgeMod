using System;
using ArchipelagoDredge.Game.Helpers;
using HarmonyLib;
using UnityEngine.UI;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     The spyglass shows a question mark for every trinket spot, which hid the AP icon on trinket dredge checks.
/// </summary>
[HarmonyPatch(typeof(SpyglassUI))]
public static class SpyglassUIPatches
{
    private static readonly AccessTools.FieldRef<SpyglassUI, HarvestPOI> FocusedHarvestPOIField =
        AccessTools.FieldRefAccess<SpyglassUI, HarvestPOI>("focusedHarvestPOI");

    private static readonly AccessTools.FieldRef<SpyglassUI, Image> ItemImageField =
        AccessTools.FieldRefAccess<SpyglassUI, Image>("itemImage");

    [HarmonyPostfix]
    [HarmonyPatch("RefreshUI")]
    private static void RefreshUI_Postfix(SpyglassUI __instance)
    {
        try
        {
            var poi = FocusedHarvestPOIField(__instance);
            var item = poi == null ? null : poi.Harvestable.GetActiveFirstHarvestableItem();
            if (ApHarvestIconHelper.IsIconCopy(item))
            {
                ItemImageField(__instance).sprite = item.sprite;
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in SpyglassUIPatches.RefreshUI_Postfix: {e}");
        }
    }
}
