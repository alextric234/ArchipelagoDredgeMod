using System.Collections.Generic;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(ShopData))]
public class ShopDataPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopData.GetNewStock))]
    public static void PostFix(ref List<SpatialItemData> __result)
    {
        if (GameManager.Instance.DataLoader.HasLoaded() &&
            ArchipelagoClient.Session.Socket.Connected)
        {
            var items = ArchipelagoItemManager.GetItemsForShops();
            __result.AddRange(items);
        }
    }
}