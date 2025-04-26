using HarmonyLib;

namespace ArchipelagoDredge.Game.Patches
{
    [HarmonyPatch]
    public static class CorePatches
    {
        private static Harmony harmony;

        public static void Apply()
        {
            harmony = new Harmony("com.archipelago.dredge");
            harmony.PatchAll(typeof(CorePatches));
        }
    }
}