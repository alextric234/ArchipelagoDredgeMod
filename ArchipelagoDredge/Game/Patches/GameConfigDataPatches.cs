using ArchipelagoDredge.Utils;
using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(GameConfigData))]
public class GameConfigDataPatches
{
    private const float ApMaxAberrationSpawnChance = 0.5f;

    [HarmonyPostfix]
    [HarmonyPatch("get_MaxAberrationSpawnChance")]
    private static void Postfix(ref float __result)
    {
        if (!ApSave.IsActive)
        {
            return;
        }

        __result = ApMaxAberrationSpawnChance;
    }
}