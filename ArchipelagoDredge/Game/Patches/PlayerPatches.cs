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
        // This runs before Die()'s own guard, so skip the calls the game will ignore.
        if (!__instance.IsAlive || __instance.IsGodModeEnabled)
        {
            return;
        }

        WinchCore.Log.Info($"Player.Die patch");
        DeathLinkManager.HandleLocalDeath();
    }
}