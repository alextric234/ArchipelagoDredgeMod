using System;
using ArchipelagoDredge.Utils;
using HarmonyLib;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     Research and Iron Rig recipes cost nothing in AP saves: Research Parts aren't in the item pool, so spending one
///     always succeeds without taking anything. Each notch is still clicked through.
/// </summary>
[HarmonyPatch(typeof(ResearchHelper))]
public static class ResearchHelperPatches
{
    private static readonly AccessTools.FieldRef<ResearchHelper, AudioClip> SuccessSfxField =
        AccessTools.FieldRefAccess<ResearchHelper, AudioClip>("spendResearchSuccessSFX");

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ResearchHelper.SpendResearchItem))]
    private static bool SpendResearchItem_Prefix(ResearchHelper __instance, ref bool __result)
    {
        try
        {
            if (!ApSave.IsActive)
            {
                return true;
            }

            GameManager.Instance.AudioPlayer.PlaySFX(SuccessSfxField(__instance), AudioLayer.SFX_UI);
            __result = true;
            return false;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in ResearchHelperPatches.SpendResearchItem_Prefix: {e}");
            return true;
        }
    }
}
