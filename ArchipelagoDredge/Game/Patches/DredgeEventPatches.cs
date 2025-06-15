using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using Winch.Core.API;

namespace ArchipelagoDredge.Game.Patches;
[HarmonyPatch(typeof(DredgeEvent))]
public static class DredgeEventPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DredgeEvent.TriggerFishCaught))]
    public static void TriggerFishCaught_Prefix(SpatialItemInstance itemInstance)
    {
        ArchipelagoLocationManager.SendLocationCheck("Fish", itemInstance.id);
    }

}