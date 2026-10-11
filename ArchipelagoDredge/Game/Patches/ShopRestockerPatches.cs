using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Utils;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;
[HarmonyPatch(typeof(ShopRestocker))]
public class ShopRestockerPatches
{
    // For an AP save (connected, or bound to a multiworld), a refresh replaces everything with AP stock.
    // Otherwise keep the vanilla "always keep in stock" items so the save behaves like a normal save.
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ShopRestocker.TryRefreshShops))]
    public static void TryRefreshShopsPrefix(
        List<SpatialItemData> ___itemsToKeepInStock,
        ref List<string> ___itemIdsToKeep)
    {
        ___itemIdsToKeep = ApSave.IsActive
            ? new List<string>()
            : ___itemsToKeepInStock.Select(data => data.id).ToList();
    }
}
