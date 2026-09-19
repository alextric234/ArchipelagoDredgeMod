
using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using UnityEngine;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
internal static class PlayerControllerRegionGatePatch
{
    private static readonly AccessTools.FieldRef<PlayerController, Rigidbody>
        RigidbodyField =
            AccessTools.FieldRefAccess<PlayerController, Rigidbody>("rb");

    [HarmonyPostfix]
    private static void Postfix(PlayerController __instance)
    {
        if (!PassageManager.TryGetActiveEjectionTarget(
                out var targetPosition))
        {
            return;
        }

        ref var rigidbody = ref RigidbodyField(__instance);

        var directionToSafety = targetPosition - __instance.transform.position;
        directionToSafety.y = 0f;

        if (directionToSafety.sqrMagnitude < 1f)
        {
            return;
        }

        rigidbody.AddForce(
            directionToSafety.normalized * PassageManager.PushSpeed,
            ForceMode.Acceleration);
    }
}
