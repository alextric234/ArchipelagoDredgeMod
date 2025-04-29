using ArchipelagoDredge.Game.Managers;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(ItemManager))]
public class ItemManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemManager.SetItemSeen))]
    public static bool Prefix(SpatialItemInstance spatialItemInstance)
    {
        if (spatialItemInstance?._itemData == null)
            return true;

        if (spatialItemInstance._itemData.itemSubtype == ItemSubtype.FISH)
        {
            var stackTrace = new System.Diagnostics.StackTrace();
            var caller = stackTrace.GetFrame(2)?.GetMethod()?.DeclaringType?.Name;

            if (caller == "GridUI" || caller == "SellModeActionHandler")
            {
                return false;
            }
            else
            {
                ArchipelagoLocationManager.SendEncyclopediaLocationCheck(spatialItemInstance);
            }
        }

        return true;
    }
}