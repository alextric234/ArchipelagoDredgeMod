using HarmonyLib;
using Newtonsoft.Json;

namespace ArchipelagoDredge.Network.Patches;

[HarmonyPatch(typeof(JsonConvert))]
public class ArchipelagoCommandPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(JsonConvert.SerializeObject), new[] { typeof(object) })]
    public static void SerializeObjectPost(ref string __result)
    {
        if (__result.Contains("AllItems"))
        {
            __result = __result.Replace("\"AllItems\"", "7");
        }
    }
}