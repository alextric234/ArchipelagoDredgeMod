using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(SaveData))]
public static class SaveDataPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(SaveData.GetResearchedItemData))]
    public static bool SaveDataGetResearchedItemData_Prefix()
    {
        if (ArchipelagoClient.Session == null || !ArchipelagoClient.Session.Socket.Connected)
        {
            return false;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(SaveData.GetResearchedItemData))]
    public static void SaveDataGetResearchedItemData_Postfix(ref List<SpatialItemData> __result)
    {
        if (ArchipelagoClient.Session == null || !ArchipelagoClient.Session.Socket.Connected)
        {
            return;
        }

        var apItems = ArchipelagoClient.Session.Items.AllItemsReceived.Select(i => i.ItemDisplayName).ToList();
        var unlockedItemIds = ItemNames.itemNamesReversed.Where(itemName => apItems.Contains(itemName.Key))
            .Select(itemName => ItemNames.ItemToDredgeId(itemName.Value));
        __result = __result.Where(r => unlockedItemIds.Contains(r.id)).ToList();
    }
}