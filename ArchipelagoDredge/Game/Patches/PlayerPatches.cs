using System;
using ArchipelagoDredge.Game.Managers;
using HarmonyLib;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(Player), new Type[] { })]
public class PlayerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Player.Die))]
    public static void PreFix(Player __instance)
    {
        if (!__instance.IsAlive || __instance.IsGodModeEnabled)
        {
            return;
        }

        WinchCore.Log.Debug($"Player.Die patch");
        DeathLinkManager.HandleLocalDeath();
    }
}