using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Utils;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(SaveData))]
public static class SaveDataPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(SaveData.GetResearchedItemData))]
    public static void SaveDataGetResearchedItemData_Postfix(ref List<SpatialItemData> __result)
    {
        if (!ApSave.IsActive)
        {
            return;
        }

        var apItems = ApSave.ReceivedItemNames;
        var unlockedItemIds = ItemNames.ItemsByName.Where(itemName => apItems.Contains(itemName.Key))
            .Select(itemName => ItemNames.ItemToDredgeId(itemName.Value));
        __result = __result.Where(r => unlockedItemIds.Contains(r.id)).ToList();
    }
}