using HarmonyLib;
using ArchipelagoDredge.Utils;

namespace ArchipelagoDredge.Game.Patches;

/// <summary>
///     Opens every research tab from the start of an AP save. The nets and pots tabs normally wait for the first net
///     slot upgrade, but logic expects their research to be available without it.
/// </summary>
[HarmonyPatch(typeof(ResearchTabShowQuery))]
public static class ResearchTabShowQueryPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ResearchTabShowQuery.GetCanNavigate))]
    private static void GetCanNavigate_Postfix(ref bool __result)
    {
        if (ApSave.IsActive)
        {
            __result = true;
        }
    }
}
