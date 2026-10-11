using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Archipelago.MultiClient.Net;
using HarmonyLib;
using Newtonsoft.Json;

namespace ArchipelagoDredge.Network.Patches;

/// <summary>
///     Winch replaces <see cref="JsonConvert.DefaultSettings" /> for the whole process (string enums,
///     <c>$type</c> names, indenting, ...). MultiClient sends packets with plain
///     <c>JsonConvert.SerializeObject(packets)</c>, so they'd pick those up and the server rejects
///     e.g. <c>"items_handling": "AllItems"</c>. Serialize outgoing packets with stock Newtonsoft settings
///     instead; every other call goes through untouched.
/// </summary>
[HarmonyPatch(typeof(JsonConvert))]
public class PacketSerializationPatches
{
    private static readonly JsonSerializer PacketSerializer = JsonSerializer.Create(new JsonSerializerSettings());

    [HarmonyPrefix]
    [HarmonyPatch(nameof(JsonConvert.SerializeObject), typeof(object))]
    public static bool SerializeObjectPrefix(object value, ref string __result)
    {
        if (value is not IEnumerable<ArchipelagoPacketBase>)
        {
            return true;
        }

        var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.Formatting = Formatting.None;
            PacketSerializer.Serialize(jsonWriter, value);
        }

        __result = stringWriter.ToString();
        return false;
    }
}