using System;
using ArchipelagoDredge.Game.Helpers;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches
{
    [HarmonyPatch(typeof(HarvestPOIDataModel))]
    public class HarvestPOIDataModelPatches
    {
        // Used by the spyglass and other UI to show what a spot holds: swap in the AP icon.
        [HarmonyPostfix]
        [HarmonyPatch(nameof(HarvestPOIDataModel.GetActiveFirstHarvestableItem))]
        private static void GetActiveFirstHarvestableItem_PostFix(HarvestPOIDataModel __instance,
            ref HarvestableItemData __result)
        {
            try
            {
                var iconName = ApHarvestIconHelper.GetIconName(__result, __instance.id);
                if (iconName != null)
                {
                    __result = ApHarvestIconHelper.GetItemWithIcon(__result, iconName);
                }
            }
            catch (Exception e)
            {
                WinchCore.Log.Error($"Error in HarvestPOIDataModelPatches.GetActiveFirstHarvestableItem: {e}");
            }
        }
    }
}
