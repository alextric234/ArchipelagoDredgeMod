using ArchipelagoDredge.Game.Helpers;
using HarmonyLib;
using UnityEngine;
using Winch.Util;

namespace ArchipelagoDredge.Game.Patches
{
    [HarmonyPatch(typeof(HarvestPOIDataModel))]
    public class HarvestPOIDataModelPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(HarvestPOIDataModel.GetActiveFirstHarvestableItem))]
        private static void GetActiveFirstHarvestableItem_PostFix(HarvestPOIDataModel __instance,
            ref HarvestableItemData __result)
        {
            if (__result != null)
            {
                if (__result.itemSubtype == ItemSubtype.FISH)
                {
                    var id = __result.id;
                    if (LocationHelper.DoesApWorldContainLocation(id) && !LocationHelper.HasThisLocationBeenChecked(id))
                    {
                        var resultClone = Object.Instantiate(__result);
                        resultClone.sprite = TextureUtil.GetSprite("archipelago_icon");
                        __result = resultClone;
                        return;
                    }

                    var aberrationsToCatch = ((FishItemData)__result).Aberrations;
                    foreach (var aberration in aberrationsToCatch)
                    {
                        if (LocationHelper.DoesApWorldContainLocation(aberration.id) && !LocationHelper.HasThisLocationBeenChecked(aberration.id))
                        {
                            var resultClone = Object.Instantiate(__result);
                            resultClone.sprite = TextureUtil.GetSprite("aberration_archipelago_icon");
                            __result = resultClone;
                            return;
                        }
                    }
                }
                else if (LocationHelper.TryParseLocation(__instance.id, out _))
                {
                    var id = __instance.id;
                    if (LocationHelper.DoesApWorldContainLocation(id) && !LocationHelper.HasThisLocationBeenChecked(id))
                    {
                        var resultClone = Object.Instantiate(__result);
                        resultClone.sprite = TextureUtil.GetSprite("archipelago_icon");
                        __result = resultClone;
                        return;
                    }
                }
            }
        }
    }
}
