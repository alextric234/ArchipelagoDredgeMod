using ArchipelagoDredge.Utils;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(GameManager))]
public class GameManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameManager.GameOver))]
    private static void Prefix(GameOverMode endMode)
    {
        ArchipelagoStateManager.AwaitingDeathScreenChoice =
            endMode == GameOverMode.DEATH;
    }
}