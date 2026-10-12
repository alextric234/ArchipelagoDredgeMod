using System;
using System.Collections.Generic;
using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using Winch.Core;
using Winch.Data.Item;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     Winch replaces <see cref="BaitAbility.DeployBait" /> and the bait preview with its own
///     <see cref="WinchExtensions" /> methods, so crab bait's fishing-license check is applied to those:
///     <list type="number">
///         <item>GetBaitItemsFromItemData postfix: unlicensed crabs are dropped from crab bait's catch and its preview.</item>
///         <item>DeployBaitModified prefix: when the current zone's license is missing, the bait is kept and the missing license is named.</item>
///     </list>
/// </summary>
[HarmonyPatch(typeof(WinchExtensions))]
public static class WinchExtensionsPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(WinchExtensions.GetBaitItemsFromItemData))]
    private static void GetBaitItemsFromItemData_Postfix(SpatialItemData itemData, List<HarvestableItemData> __result)
    {
        try
        {
            if (itemData == null || itemData.GetBaitTypeFromItemData() != BaitType.CRAB)
            {
                return;
            }

            __result.RemoveAll(item => !FishingLicenseManager.HasLicenseFor(item));
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in WinchExtensionsPatches.GetBaitItemsFromItemData_Postfix: {e}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(WinchExtensions.DeployBaitModified))]
    private static bool DeployBaitModified_Prefix(SpatialItemInstance baitInstance, ref bool __result)
    {
        try
        {
            var baitItemData = baitInstance.GetItemData<SpatialItemData>();
            if (baitItemData.GetBaitTypeFromItemData() != BaitType.CRAB)
            {
                return true;
            }

            var zone = GameManager.Instance.Player.PlayerZoneDetector.GetCurrentZone();
            if (!FishingLicenseManager.TryGetMissingFishingLicense(zone, out var missingLicense) ||
                baitItemData.GetBaitItemsFromItemData().Count > 0)
            {
                return true;
            }

            // Not deploying leaves the bait in the inventory, as Winch's own bait-failed path does.
            WinchCore.Log.Debug($"Blocked crab bait: missing fishing license for {zone}.");
            GameManager.Instance.UI.ShowNotification(NotificationType.ERROR, missingLicense.MissingMessageKey);
            __result = false;
            return false;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in WinchExtensionsPatches.DeployBaitModified_Prefix: {e}");
            return true;
        }
    }
}
