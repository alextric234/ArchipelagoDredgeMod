using System;
using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     Trawl nets catch on a timer without the harvest minigame, so the fishing-license check is applied to each catch roll:
///     <list type="number">
///         <item>CheckCanBeCaughtByThisNet postfix: unlicensed fish are left out of the pick.</item>
///         <item>AddTrawlItem prefix/postfix: when a roll catches nothing because of a missing license, that license is named once,
///         until something licensed is caught or the net is redeployed.</item>
///     </list>
/// </summary>
[HarmonyPatch(typeof(TrawlNetAbility))]
public static class TrawlNetAbilityPatches
{
    private static bool _inCatchRoll;
    private static int _licensedCount;
    private static FishingLicenseDefinition _missingLicense;
    private static FishingLicenseDefinition _lastWarnedLicense;

    [HarmonyPostfix]
    [HarmonyPatch("CheckCanBeCaughtByThisNet")]
    private static void CheckCanBeCaughtByThisNet_Postfix(HarvestableItemData itemData, ref bool __result)
    {
        try
        {
            if (!__result)
            {
                return;
            }

            if (FishingLicenseManager.TryGetMissingFishingLicense(itemData, out var missingLicense))
            {
                __result = false;
                if (_inCatchRoll)
                {
                    _missingLicense ??= missingLicense;
                }
            }
            else if (_inCatchRoll)
            {
                _licensedCount++;
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in TrawlNetAbilityPatches.CheckCanBeCaughtByThisNet_Postfix: {e}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("AddTrawlItem", new Type[0])]
    private static void AddTrawlItem_Prefix()
    {
        _inCatchRoll = true;
        _licensedCount = 0;
        _missingLicense = null;
    }

    [HarmonyPostfix]
    [HarmonyPatch("AddTrawlItem", new Type[0])]
    private static void AddTrawlItem_Postfix()
    {
        try
        {
            _inCatchRoll = false;
            if (_licensedCount > 0)
            {
                _lastWarnedLicense = null;
                return;
            }

            if (_missingLicense == null || _missingLicense == _lastWarnedLicense)
            {
                return;
            }

            _lastWarnedLicense = _missingLicense;
            GameManager.Instance.UI.ShowNotification(NotificationType.ERROR, _missingLicense.MissingMessageKey);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in TrawlNetAbilityPatches.AddTrawlItem_Postfix: {e}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(TrawlNetAbility.Activate))]
    private static void Activate_Postfix()
    {
        _lastWarnedLicense = null;
    }
}
