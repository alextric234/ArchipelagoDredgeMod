using System;
using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     The ability panel's spot-quality readout counts what the selected net or pot can catch here,
///     so unlicensed fish are left out of that count to match what will actually be caught.
/// </summary>
[HarmonyPatch(typeof(ActiveAbilityInfoPanel))]
public static class ActiveAbilityInfoPanelPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("CheckCanBeCaughtByThisNet")]
    private static void CheckCanBeCaughtByThisNet_Postfix(HarvestableItemData itemData, ref bool __result)
    {
        ExcludeUnlicensed(itemData, ref __result);
    }

    [HarmonyPostfix]
    [HarmonyPatch("CheckCanBeCaughtByThisPot")]
    private static void CheckCanBeCaughtByThisPot_Postfix(HarvestableItemData itemData, ref bool __result)
    {
        ExcludeUnlicensed(itemData, ref __result);
    }

    private static void ExcludeUnlicensed(HarvestableItemData itemData, ref bool result)
    {
        try
        {
            result = result && FishingLicenseManager.HasLicenseFor(itemData);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in ActiveAbilityInfoPanelPatches.ExcludeUnlicensed: {e}");
        }
    }
}
