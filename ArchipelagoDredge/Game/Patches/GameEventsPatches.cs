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
        ArchipelagoLocationManager.SendLocationCheck("Research", spatialItemData.id);
    }
}