using System;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
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
                if (!ArchipelagoLocationManager.HasThisLocationBeenChecked(___itemDataToHarvest.id))
                {
                    __instance.hintImage.sprite = TextureUtil.GetSprite("archipelago_icon");
                    return;
                }

                var aberrationsToCatch = ((FishItemData) ___itemDataToHarvest).Aberrations;
                foreach (var aberration in aberrationsToCatch)
                {
                    if (!ArchipelagoLocationManager.HasThisLocationBeenChecked(aberration.id))
                    {
                        __instance.hintImage.sprite = TextureUtil.GetSprite("aberration_archipelago_icon");
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"[AP] Error in HarvestMinigameViewPatches.RefreshHarvestTarget: {e}");
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

            if (!ArchipelagoLocationManager.HasThisLocationBeenChecked(locationToCheck))
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