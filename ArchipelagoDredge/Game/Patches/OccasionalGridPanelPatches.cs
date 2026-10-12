using System;
using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     Explains an empty crab pot when it is empty because every crab there needs a missing fishing license.
/// </summary>
[HarmonyPatch(typeof(OccasionalGridPanel))]
public static class OccasionalGridPanelPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(OccasionalGridPanel.Show))]
    private static void Show_Postfix(HarvestPOI harvestPOI)
    {
        try
        {
            if (harvestPOI?.Harvestable is SerializedCrabPotPOIData { grid.spatialItems.Count: 0 } pot &&
                FishingLicenseManager.TryGetMissingFishingLicenseForAll(pot.items, out var missingLicense))
            {
                GameManager.Instance.UI.ShowNotification(NotificationType.ERROR, missingLicense.MissingMessageKey);
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in OccasionalGridPanelPatches.Show_Postfix: {e}");
        }
    }
}
