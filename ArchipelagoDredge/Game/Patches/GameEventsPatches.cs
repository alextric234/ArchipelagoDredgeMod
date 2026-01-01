using ArchipelagoDredge.Game.Managers;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(GameEvents))]
public static class GameEventsPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameEvents.TriggerResearchCompleted))]
    public static void TriggerResearchCompletedPre(SpatialItemData spatialItemData)
    {
        if (spatialItemData.id == "net1")
        {
            return;
        }
        ArchipelagoLocationManager.SendLocationCheck(spatialItemData.id);
    }
}