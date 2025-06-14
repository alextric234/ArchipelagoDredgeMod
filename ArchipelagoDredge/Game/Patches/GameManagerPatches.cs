using ArchipelagoDredge.Network;
using HarmonyLib;

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
