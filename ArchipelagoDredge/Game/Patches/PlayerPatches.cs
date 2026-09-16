using System;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
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
        WinchCore.Log.Info($"Player.Die patch");
        DeathLinkManager.HandleLocalDeath();
    }
}