using System;
using System.Linq;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     Crab pots fill on a timer without the harvest minigame, so the fishing-license check
///     is applied to each catch roll instead: unlicensed crabs are left out of the pick.
/// </summary>
[HarmonyPatch(typeof(SerializedCrabPotPOIData))]
public static class SerializedCrabPotPOIDataPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(SerializedCrabPotPOIData.GetRandomHarvestableItem))]
    private static bool GetRandomHarvestableItem_Prefix(
        SerializedCrabPotPOIData __instance,
        ref HarvestableItemData __result)
    {
        try
        {
            if (!ArchipelagoClient.SlotData.AddFishingLicenses)
            {
                return true;
            }

            // A null result is the game's own "nothing caught this roll".
            var licensedItems = __instance.items.Where(FishingLicenseManager.HasLicenseFor).ToList();
            __result = licensedItems.Count > 0
                ? licensedItems[MathUtil.GetRandomWeightedIndex(licensedItems.Select(i => i.harvestItemWeight).ToArray())]
                : null;
            return false;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in SerializedCrabPotPOIDataPatches.GetRandomHarvestableItem_Prefix: {e}");
            return true;
        }
    }
}
