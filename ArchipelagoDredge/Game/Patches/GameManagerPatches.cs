using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Patches.Helpers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchipelagoDredge.Network;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(GameManager))]
public class GameManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameManager.EndGame))]
    public static void PostFix()
    {
        ArchipelagoClient.GameReady = false;
        ArchipelagoCommandManager.Disconnect();
    }
}
