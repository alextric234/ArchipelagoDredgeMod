using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using Winch.Core;

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
            WinchCore.Log.Info($"");
            var stackTrace = new System.Diagnostics.StackTrace();
            var caller = stackTrace.GetFrame(2)?.GetMethod()?.DeclaringType?.Name;
            WinchCore.Log.Info($"Caller: {caller}");

            if (caller == "HarvestMinigameView")
            {
                ArchipelagoLocationManager.SendEncyclopediaLocationCheck(spatialItemInstance);
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}