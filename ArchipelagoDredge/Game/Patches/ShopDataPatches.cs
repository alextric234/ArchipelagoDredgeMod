using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using HarmonyLib;
using System.Collections.Generic;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(ShopData))]
public class ShopDataPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopData.GetNewStock))]
    public static void PostFix(ref List<SpatialItemData> __result)
    {
        if (ArchipelagoClient.GameReady &&
            ArchipelagoClient.Session.Socket.Connected)
        {
            var items = ArchipelagoItemManager.GetItemsForShops();
            __result.AddRange(items);
        }
    }
}
