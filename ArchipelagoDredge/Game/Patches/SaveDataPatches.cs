using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches
{
    [HarmonyPatch(typeof(SaveData))]
    public static class SaveDataPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SaveData.GetResearchedItemData))]
        public static bool SaveDataGetResearchedItemData_Prefix()
        {
            if (ArchipelagoClient.Session == null || !ArchipelagoClient.Session.Socket.Connected)
                return false;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SaveData.GetResearchedItemData))]
        public static void SaveDataGetResearchedItemData_Postfix(ref List<SpatialItemData> __result)
        {
            if (ArchipelagoClient.Session == null || !ArchipelagoClient.Session.Socket.Connected)
                return;
            var apItems = ArchipelagoClient.Session.Items.AllItemsReceived.Select(i=>i.ItemDisplayName).ToList();
            var unlockedItemIds = ArchipelagoItemManager.NameToItemCache.Where(i => apItems.Contains(i.Key)).Select(i=>i.Value.id).ToList();
            __result = __result.Where(r => unlockedItemIds.Contains(r.id)).ToList();
        }
    }
}
