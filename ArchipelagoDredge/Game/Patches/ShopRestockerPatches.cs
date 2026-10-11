using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Network;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;
[HarmonyPatch(typeof(ShopRestocker))]
public class ShopRestockerPatches
{
    // When connected, a refresh replaces everything with AP stock. When not connected,
    // keep the vanilla "always keep in stock" items so the save behaves like a normal save.
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ShopRestocker.TryRefreshShops))]
    public static void TryRefreshShopsPrefix(
        List<SpatialItemData> ___itemsToKeepInStock,
        ref List<string> ___itemIdsToKeep)
    {
        ___itemIdsToKeep = ArchipelagoClient.IsConnected
            ? new List<string>()
            : ___itemsToKeepInStock.Select(data => data.id).ToList();
    }
}
