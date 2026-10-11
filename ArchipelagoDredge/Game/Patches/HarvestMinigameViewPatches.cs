using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Services;
using HarmonyLib;
using UnityEngine.Localization.Components;
using Winch.Core;
using Object = UnityEngine.Object;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     All patches on <see cref="HarvestMinigameView" />, so their order is explicit:
///     <list type="number">
///         <item>RefreshHarvestTarget transpiler: the fishing-license check is ANDed into the equipment check.</item>
///         <item>RefreshHarvestTarget postfix: the missing-license text, then the AP hint icon.</item>
///         <item>StartGame prefix: blocks the minigame without the license.</item>
///         <item>SpawnItem prefix: an unchecked AP location gives the check instead of the item.</item>
///     </list>
/// </summary>
[HarmonyPatch(typeof(HarvestMinigameView))]
public static class HarvestMinigameViewPatches
{
    private static readonly AccessTools.FieldRef<HarvestMinigameView, HarvestableItemData> ItemDataToHarvest =
        AccessTools.FieldRefAccess<HarvestMinigameView, HarvestableItemData>("itemDataToHarvest");

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(HarvestMinigameView.RefreshHarvestTarget))]
    private static IEnumerable<CodeInstruction> RefreshHarvestTarget_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var equipmentCheck = AccessTools.Method(
            typeof(PlayerStats),
            nameof(PlayerStats.GetHasEquipmentForHarvestType),
            new[]
            {
                typeof(HarvestableType),
                typeof(bool)
            });

        var licenseCheck = AccessTools.Method(
            typeof(HarvestMinigameViewPatches),
            nameof(ChooseItemAndCheckLicense));

        var matcher = new CodeMatcher(instructions, generator);

        matcher.MatchForward(
            false,
            new CodeMatch(instruction => instruction.Calls(equipmentCheck)));

        if (!matcher.IsValid)
        {
            WinchCore.Log.Error(
                "Could not find the harvest equipment check for fishing-license patch.");
            return matcher.InstructionEnumeration();
        }

        matcher.Advance(1);
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, licenseCheck),
            new CodeInstruction(OpCodes.And));

        return matcher.InstructionEnumeration();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(HarvestMinigameView.RefreshHarvestTarget))]
    private static void RefreshHarvestTarget_Postfix(
        HarvestMinigameView __instance,
        HarvestableItemData ___itemDataToHarvest,
        LocalizeStringEvent ___cannotStartText)
    {
        if (__instance.currentPOI == null)
        {
            return;
        }

        ShowMissingLicenseText(__instance, ___itemDataToHarvest, ___cannotStartText);
        ShowApHintIcon(__instance, ___itemDataToHarvest);
    }

    [HarmonyPrefix]
    [HarmonyPatch("StartGame")]
    private static bool StartGame_Prefix(HarvestMinigameView __instance)
    {
        try
        {
            if (ChooseItemAndCheckLicense(__instance))
            {
                return true;
            }

            WinchCore.Log.Debug("Blocked harvest minigame: missing fishing license.");
            return false;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in HarvestMinigameViewPatches.StartGame_Prefix: {e}");
            return true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HarvestMinigameView.SpawnItem))]
    private static bool SpawnItem_Prefix(
        HarvestMinigameView __instance,
        ItemData ___itemDataToHarvest)
    {
        try
        {
            if (___itemDataToHarvest == null)
            {
                return true;
            }

            string locationToCheck;
            if (___itemDataToHarvest.itemSubtype == ItemSubtype.FISH)
            {
                locationToCheck = ___itemDataToHarvest.id;
            }
            else
            {
                locationToCheck = __instance.currentPOI.Harvestable.GetId();
            }

            if (LocationHelper.DoesApWorldContainLocation(locationToCheck) && !LocationHelper.HasThisLocationBeenChecked(locationToCheck))
            {
                LocationCheckService.Report(locationToCheck);
                __instance.currentPOI.OnHarvested(true);
                GameEvents.Instance.TriggerFishCaught();
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in HarvestMinigameViewPatches.SpawnItem_Prefix: {e}");
            return true;
        }
    }
    private static bool ChooseItemAndCheckLicense(HarvestMinigameView view)
    {
        try
        {
            ref var itemToHarvest = ref ItemDataToHarvest(view);
            if ((Object) itemToHarvest == null)
            {
                itemToHarvest = view.currentPOI.Harvestable.GetNextHarvestableItem();
            }

            if ((Object) itemToHarvest == null || itemToHarvest.itemSubtype != ItemSubtype.FISH)
            {
                return true;
            }

            return FishingLicenseManager.HasLicenseForZone(itemToHarvest.zonesFoundIn);
        }
        catch (Exception e)
        {
            // Runs inside vanilla RefreshHarvestTarget via the transpiler; never throw there.
            WinchCore.Log.Error($"Error in HarvestMinigameViewPatches.ChooseItemAndCheckLicense: {e}");
            return true;
        }
    }

    private static void ShowMissingLicenseText(
        HarvestMinigameView view,
        HarvestableItemData itemToHarvest,
        LocalizeStringEvent cannotStartText)
    {
        try
        {
            if (cannotStartText == null)
            {
                return;
            }

            // Only replace the text when the rod isn't the problem.
            var hasRod = GameManager.Instance.PlayerStats
                .GetHasEquipmentForHarvestType(
                    view.currentPOI.Harvestable.GetHarvestType(),
                    view.currentPOI.Harvestable.GetIsAdvancedHarvestType());

            if (!hasRod ||
                !FishingLicenseManager.TryGetMissingFishingLicense(itemToHarvest, out var missingLicense))
            {
                return;
            }

            cannotStartText.StringReference.SetReference(
                LanguageManager.STRING_TABLE,
                missingLicense.MissingMessageKey);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in HarvestMinigameViewPatches.ShowMissingLicenseText: {e}");
        }
    }

    private static void ShowApHintIcon(HarvestMinigameView view, HarvestableItemData itemToHarvest)
    {
        try
        {
            // Only dredge spots can be AP locations by spot id; other spots go by the item.
            var spotLocationId = view.currentPOI.IsDredgePOI ? view.currentPOI.HarvestPOIData.id : null;
            var iconName = ApHarvestIconHelper.GetIconName(itemToHarvest, spotLocationId);
            if (iconName != null)
            {
                view.hintImage.sprite = ApHarvestIconHelper.GetIcon(iconName);
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in HarvestMinigameViewPatches.ShowApHintIcon: {e}");
        }
    }
}