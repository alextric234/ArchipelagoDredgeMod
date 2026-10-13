using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace ArchipelagoDredge.Game.Tracker;

public class ApItemData
{
    public string Name;
    public string Group;
    public string Expansion;
    public string[] CanCatch;
    public string[] Shape;
    public int Speed;
    public int Lumens;
    public bool Rebuyable;
}

public class ApRequirement
{
    public string Type;
    public string[] AnyOf = [];
    public string[] AllOf = [];
    public int Cost;
    public string CatchType;
    public int Phase;
    public string Item;
}

public class ApLocationData
{
    public string Name;
    public string Region;
    public string Group;
    public string Expansion;
    public bool HasId;
    public ApRequirement[] Requirements;
    public bool CanCatchRod;
    public bool CanCatchNet;
    public bool IsAberration;
}

/// <summary>
///     The apworld's data files, embedded at build time so the tracker uses the same data as generation.
/// </summary>
public static class ApWorldData
{
    public static readonly IReadOnlyDictionary<string, ApItemData> Items = LoadItems();
    public static readonly IReadOnlyDictionary<string, ApLocationData> Locations = LoadLocations();
    public static readonly IReadOnlyDictionary<string, string[][]> HullLayouts = LoadHulls();

    private static JObject Load(string fileName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"ApWorldData.{fileName}");
        using var reader = new StreamReader(stream!);
        return JObject.Parse(reader.ReadToEnd());
    }

    private static string[] Strings(JToken token)
    {
        return token?.Values<string>().ToArray() ?? [];
    }

    private static Dictionary<string, ApItemData> LoadItems()
    {
        return Load("items.json").Properties().ToDictionary(p => p.Name, p => new ApItemData
        {
            Name = p.Name,
            Group = (string) p.Value["item_group"],
            Expansion = (string) p.Value["expansion"],
            CanCatch = Strings(p.Value["can_catch"]),
            Shape = Strings(p.Value["shape"]),
            Speed = (int?) p.Value["speed"] ?? 0,
            Lumens = (int?) p.Value["lumens"] ?? 0,
            Rebuyable = (bool?) p.Value["rebuyable"] ?? false
        });
    }

    private static Dictionary<string, ApLocationData> LoadLocations()
    {
        return Load("locations.json").Properties().ToDictionary(p => p.Name, p => new ApLocationData
        {
            Name = p.Name,
            Region = (string) p.Value["region"],
            Group = (string) p.Value["location_group"],
            Expansion = (string) p.Value["expansion"],
            HasId = p.Value["base_id_offset"] != null,
            Requirements = (p.Value["requirements"] as JArray ?? []).Select(r => new ApRequirement
            {
                Type = (string) r["type"],
                AnyOf = Strings(r["any_of"]),
                AllOf = Strings(r["all_of"]),
                Cost = (int?) r["cost"] ?? (int?) r["count"] ?? 0,
                CatchType = (string) r["type"] == "catch_type" ? (string) r["value"] : null,
                Phase = (string) r["type"] == "iron_rig_phase" ? (int) r["value"] : 0,
                Item = (string) r["item"]
            }).ToArray(),
            CanCatchRod = (bool?) p.Value["can_catch_rod"] ?? true,
            CanCatchNet = (bool?) p.Value["can_catch_net"] ?? false,
            IsAberration = (bool?) p.Value["is_aberration"] ?? false
        });
    }

    private static Dictionary<string, string[][]> LoadHulls()
    {
        return Load("hulls.json").Properties().ToDictionary(
            p => p.Name,
            p => p.Value.Select(rows => Strings(rows)).ToArray());
    }
}
