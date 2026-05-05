using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDredge.Game.Helpers;
using Winch.Config;
using Winch.Core;
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
                    WinchCore.Log.Info($"result id: {__result.id}");
                    if (!ArchipelagoLocationManager.HasThisLocationBeenChecked(__result.id))
                    {
                        var resultClone = UnityEngine.Object.Instantiate(__result);
                        resultClone.sprite = TextureUtil.GetSprite("archipelago_icon");
                        __result = resultClone;
                        return;
                    }

                    var aberrationsToCatch = ((FishItemData)__result).Aberrations;
                    foreach (var aberration in aberrationsToCatch)
                    {
                        if (!ArchipelagoLocationManager.HasThisLocationBeenChecked(aberration.id))
                        {
                            var resultClone = UnityEngine.Object.Instantiate(__result);
                            resultClone.sprite = TextureUtil.GetSprite("aberration_archipelago_icon");
                            __result = resultClone;
                            return;
                        }
                    }
                }
                else if (LocationNames.TryParseLocation(__instance.id, out _))
                {
                    if (!ArchipelagoLocationManager.HasThisLocationBeenChecked(__instance.id))
                    {
                        var resultClone = UnityEngine.Object.Instantiate(__result);
                        resultClone.sprite = TextureUtil.GetSprite("archipelago_icon");
                        __result = resultClone;
                        return;
                    }
                }
            }
        }
    }
}
