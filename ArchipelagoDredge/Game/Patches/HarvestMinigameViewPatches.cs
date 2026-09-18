using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine.Localization.Components;
using Winch.Core;
using Winch.Core.API;
using Winch.Util;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(HarvestMinigameView))]
public class HarvestMinigameViewPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(HarvestMinigameView.RefreshHarvestTarget))]
    private static void RefreshHarvestTarget_Postfix(
        HarvestMinigameView __instance,
        ItemData ___itemDataToHarvest
    )
    {
        try
        {
            if (___itemDataToHarvest == null)
            {
                return;
            }

            if (___itemDataToHarvest.itemSubtype == ItemSubtype.FISH)
            {
                if (!LocationHelper.HasThisLocationBeenChecked(___itemDataToHarvest.id))
                {
                    __instance.hintImage.sprite = TextureUtil.GetSprite("archipelago_icon");
                    return;
                }

                var aberrationsToCatch = ((FishItemData) ___itemDataToHarvest).Aberrations;
                foreach (var aberration in aberrationsToCatch)
                {
                    if (!LocationHelper.HasThisLocationBeenChecked(aberration.id))
                    {
                        __instance.hintImage.sprite = TextureUtil.GetSprite("aberration_archipelago_icon");
                        return;
                    }
                }
            }
            else if(__instance.currentPOI.IsDredgePOI)
            {
                if (!LocationHelper.HasThisLocationBeenChecked(__instance.currentPOI.HarvestPOIData.id))
                {
                    __instance.hintImage.sprite = TextureUtil.GetSprite("archipelago_icon");
                }
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in HarvestMinigameViewPatches.RefreshHarvestTarget: {e}");
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

            if (!LocationHelper.HasThisLocationBeenChecked(locationToCheck))
            {
                var spatialItemInstance = new SpatialItemInstance();
                spatialItemInstance.id = ___itemDataToHarvest.id;
                spatialItemInstance._itemData = ___itemDataToHarvest;
                GameEvents.Instance.TriggerFishCaught(spatialItemInstance);
                DredgeEvent.TriggerPOIHarvested(__instance.currentPOI,  spatialItemInstance);
                __instance.currentPOI.OnHarvested(true);
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in HarvestMinigameViewPatches.SpawnItem_Prefix: {e.Message}");
            throw;
        }
    }
}

[HarmonyPatch]
internal static class HarvestMinigameViewFishingLicensePatches
{
    private static readonly AccessTools.FieldRef<HarvestMinigameView, HarvestableItemData>
        ItemDataToHarvest =
            AccessTools.FieldRefAccess<HarvestMinigameView, HarvestableItemData>(
                "itemDataToHarvest");

    [HarmonyPatch(typeof(HarvestMinigameView), "RefreshHarvestTarget")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> RefreshHarvestTargetTranspiler(
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
            typeof(HarvestMinigameViewFishingLicensePatches),
            nameof(HasRequiredFishingLicense));

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

    [HarmonyPatch(typeof(HarvestMinigameView), "StartGame")]
    [HarmonyPrefix]
    private static bool StartGamePrefix(HarvestMinigameView __instance)
    {
        if (HasRequiredFishingLicense(__instance))
        {
            return true;
        }

        WinchCore.Log.Info("Blocked harvest minigame: missing fishing license.");
        return false;
    }

    private static bool HasRequiredFishingLicense(HarvestMinigameView HarvestMinigameView)
    {
        ref var itemToHarvest = ref ItemDataToHarvest(HarvestMinigameView);
        if ((UnityEngine.Object)itemToHarvest == null)
        {
            itemToHarvest = HarvestMinigameView.currentPOI
                .harvestPOIData
                .GetNextHarvestableItem();
        }

        if ((UnityEngine.Object)itemToHarvest == null ||
            itemToHarvest.itemSubtype != ItemSubtype.FISH)
        {
            return true;
        }

        return FishingLicenseManager.HasLicenseForZone(itemToHarvest.zonesFoundIn);
    }

    [HarmonyPatch(typeof(HarvestMinigameView), "RefreshHarvestTarget")]
    [HarmonyPostfix]
    private static void PostFix(
        HarvestMinigameView __instance,
        HarvestableItemData ___itemDataToHarvest,
        LocalizeStringEvent ___cannotStartText)
    {
        // Check vanilla rod first.
        var hasRod = GameManager.Instance.PlayerStats
            .GetHasEquipmentForHarvestType(
                __instance.currentPOI.Harvestable.GetHarvestType(),
                __instance.currentPOI.Harvestable.GetIsAdvancedHarvestType());

        if (!hasRod ||
            !FishingLicenseManager.TryGetMissingFishingLicense(
                ___itemDataToHarvest,
                out var missingLicense))
        {
            return;
        }

        ___cannotStartText.StringReference.SetReference(
            LanguageManager.STRING_TABLE,
            missingLicense.MissingMessageKey);
    }
}