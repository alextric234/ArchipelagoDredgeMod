using HarmonyLib;
using System.Collections.Generic;

namespace ArchipelagoDredge.Game.Patches;
[HarmonyPatch(typeof(ShopRestocker))]
public class ShopRestockerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ShopRestocker.Awake))]
    public static void PostFix(List<SpatialItemData> ___itemsToKeepInStock)
    {
        ___itemsToKeepInStock.Clear();
    }
}
