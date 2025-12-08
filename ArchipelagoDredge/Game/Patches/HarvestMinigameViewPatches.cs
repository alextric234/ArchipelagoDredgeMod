using System;
using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using Winch.Core;
using Winch.Util;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(HarvestMinigameView))]
public class HarvestMinigameViewPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(HarvestMinigameView.RefreshHarvestTarget))]
    static void Postfix(
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
                if (!ArchipelagoLocationManager.HasThisLocationBeenChecked("Fish", ___itemDataToHarvest.id))
                {
                    __instance.hintImage.sprite = TextureUtil.GetSprite("archipelago_icon");
                    return;
                }

                var aberrationsToCatch = ((FishItemData) ___itemDataToHarvest).Aberrations;
                foreach (var aberration in aberrationsToCatch)
                {
                    if (!ArchipelagoLocationManager.HasThisLocationBeenChecked("Fish", aberration.id))
                    {
                        __instance.hintImage.sprite = TextureUtil.GetSprite("aberration_archipelago_icon");
                        return;
                    }
                }
            }
        }
        catch(System.Exception ex)
        {
            WinchCore.Log.Error($"[AP] Error in HarvestMinigameViewPatches: {ex}");
        }
    }
}