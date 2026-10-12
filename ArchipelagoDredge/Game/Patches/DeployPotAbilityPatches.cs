using System;
using System.Linq;
using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     Warns when a crab pot is deployed where every crab needs a missing fishing license,
///     since <see cref="SerializedCrabPotPOIDataPatches" /> will keep it empty.
/// </summary>
[HarmonyPatch(typeof(DeployPotAbility))]
public static class DeployPotAbilityPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DeployPotAbility.DeployCrabPot))]
    private static void DeployCrabPot_Postfix(bool __result)
    {
        try
        {
            // DeployCrabPot has just added the new pot to the end of the list.
            var pot = __result ? GameManager.Instance.SaveData.serializedCrabPotPOIs.LastOrDefault() : null;
            if (pot != null && FishingLicenseManager.TryGetMissingFishingLicenseForAll(pot.items, out var missingLicense))
            {
                GameManager.Instance.UI.ShowNotification(NotificationType.ERROR, missingLicense.MissingMessageKey);
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in DeployPotAbilityPatches.DeployCrabPot_Postfix: {e}");
        }
    }
}
