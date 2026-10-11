using System.Collections.Generic;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Utils;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(ShopData))]
public class ShopDataPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopData.GetNewStock))]
    public static void PostFix(ref List<SpatialItemData> __result)
    {
        if (!ApSave.IsActive)
        {
            return;
        }

        __result.Clear();
        if (GameManager.Instance.DataLoader.HasLoaded())
        {
            var items = ArchipelagoItemManager.GetItemsForShops();
            __result.AddRange(items);
        }
    }
}