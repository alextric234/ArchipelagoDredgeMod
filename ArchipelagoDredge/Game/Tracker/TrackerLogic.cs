using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoDredge.Game.Tracker;

public class TrackerOptions
{
    public bool IncludeIronRig;
    public bool IncludePaleReach;
    public bool AddFishingLicenses;
    public bool AddPassageItems;
    public bool LogicalNets;
    public bool RequireBoatPerformance;
    public int ProgressiveAberrationChanceItems;
    public bool GuaranteedAberrationsItem;
}

/// <summary>
///     Port of the apworld's rules.py and region graph. Given received items, works out which locations are in logic.
///     Keep in step with the apworld; tools/LogicParity compares the two.
/// </summary>
public class TrackerLogic
{
    // (knots, lumens), as in rules.py.
    private static readonly (int knots, int lumens) MidRegionsPerformance = (50, 1800);
    private static readonly (int knots, int lumens) OpenOceanPerformance = (50, 2100);
    private static readonly (int knots, int lumens) PaleReachPerformance = (60, 2100);
    private const int BaseSpeed = 10;

    private static readonly HashSet<string> AtSeaLocationGroups = ["Dredge", "Encyclopedia", "World", "Message", "Pursuit"];

    // locations.py leaves these groups out of every seed.
    private static readonly HashSet<string> ExcludedLocationGroups = ["Shop", "Unused Pursuit", "Unused World", "Relic"];

    private static readonly HashSet<string> CatchToolGroups = ["Rod", "Net", "Crab Pot", "Consumable"];
    private static readonly HashSet<string> ExcludedCatchTools = ["Tendon Rod", "Viscera Crane"];

    private static readonly string[] IcebreakerParts = ["Icebreaker Plow Half 1", "Icebreaker Plow Half 2", "Icebreaker Bracing"];
    private static readonly string[] Relics =
        ["Ornate Key", "Rusted Music Box", "Jewel Encrusted Band", "Shimmering Necklace", "Antique Pocket Watch"];

    private static readonly Dictionary<string, string> LicenseByRegion = new()
    {
        ["Gale Cliffs"] = "Gale Cliffs Fishing License",
        ["Stellar Basin"] = "Stellar Basin Fishing License",
        ["Twisted Strand"] = "Twisted Strand Fishing License",
        ["Devil's Spine"] = "Devil's Spine Fishing License",
        ["Open Ocean"] = "Open Ocean Fishing License",
        ["The Pale Reach"] = "Pale Reach Fishing License"
    };

    private static readonly Dictionary<(string catchType, string group), string[]> CatchToolIndex = BuildCatchToolIndex();

    private static readonly string[] Engines =
        ApWorldData.Items.Values.Where(i => i.Group == "Engine").Select(i => i.Name).ToArray();

    private static readonly string[] Lights =
        ApWorldData.Items.Values.Where(i => i.Group == "Light").Select(i => i.Name).ToArray();

    private readonly TrackerOptions options;
    private readonly Dictionary<string, int> counts;
    private readonly Dictionary<string, bool> regionCache = new();

    private TrackerLogic(IReadOnlyDictionary<string, int> received, TrackerOptions options)
    {
        this.options = options;
        counts = new Dictionary<string, int>(received.ToDictionary(p => p.Key, p => p.Value));
    }

    /// <summary>
    ///     The given locations that are in logic, after collecting every event that becomes reachable.
    /// </summary>
    public static HashSet<string> AccessibleLocations(IReadOnlyDictionary<string, int> received, TrackerOptions options,
        IEnumerable<string> locationNames)
    {
        var logic = new TrackerLogic(received, options);
        logic.CollectEvents();
        return new HashSet<string>(locationNames.Where(logic.CanReachLocation));
    }

    private void CollectEvents()
    {
        var events = ApWorldData.Locations.Values.Where(l => !l.HasId && IsInSeed(l)).ToList();
        bool collected;
        do
        {
            collected = false;
            foreach (var e in events.Where(e => !Has(e.Name) && CanReachLocation(e.Name)).ToList())
            {
                counts[e.Name] = 1;
                regionCache.Clear();
                collected = true;
            }
        } while (collected);
    }

    private bool IsInSeed(ApLocationData location)
    {
        var expansionEnabled = location.Expansion switch
        {
            "Base" => true,
            "IronRig" => options.IncludeIronRig,
            "PaleReach" => options.IncludePaleReach,
            "Both" => options.IncludeIronRig && options.IncludePaleReach,
            _ => false
        };
        return expansionEnabled && !ExcludedLocationGroups.Contains(location.Group);
    }

    private int Count(string item) => counts.TryGetValue(item, out var count) ? count : 0;

    private bool Has(string item, int count = 1) => Count(item) >= count;

    private bool HasPassage(string passage) => !options.AddPassageItems || Has(passage);

    public bool CanReachRegion(string region)
    {
        if (regionCache.TryGetValue(region, out var cached))
        {
            return cached;
        }

        var result = region switch
        {
            "Menu" or "The Marrows" or "Research" => true,
            "Open Ocean" => HasPassage("The Pelagic Psalm"),
            "Gale Cliffs" => MidRegion("The Windward Litany"),
            "Stellar Basin" => MidRegion("The Astral Testament"),
            "Twisted Strand" => MidRegion("The Mangrove Canticle"),
            "Devil's Spine" => MidRegion("The Cinder Gospel"),
            "The Iron Rig" => CanReachRegion("Open Ocean") && HasBoatPerformance(OpenOceanPerformance),
            "The Pale Reach" => CanReachRegion("Open Ocean") && HasPassage("The Rimebound Chronicle") &&
                                HasBoatPerformance(PaleReachPerformance),
            "Insanity" => CanReachRegion("Open Ocean") && Relics.All(r => Has(r)),
            _ => false
        };
        regionCache[region] = result;
        return result;
    }

    private bool MidRegion(string passage)
    {
        return CanReachRegion("Open Ocean") && HasPassage(passage) && HasBoatPerformance(MidRegionsPerformance);
    }

    private bool CanReachLocation(string name)
    {
        if (!ApWorldData.Locations.TryGetValue(name, out var location) || !CanReachRegion(location.Region))
        {
            return false;
        }

        if (options.AddFishingLicenses && location.Group == "Encyclopedia" &&
            LicenseByRegion.TryGetValue(location.Region, out var license) && !Has(license))
        {
            return false;
        }

        if (location.IsAberration && location.Group == "Encyclopedia" && !CanGetAberrations())
        {
            return false;
        }

        if (location.Region == "Open Ocean" && AtSeaLocationGroups.Contains(location.Group) &&
            !HasBoatPerformance(OpenOceanPerformance))
        {
            return false;
        }

        return location.Requirements.All(r => MeetsRequirement(r, location));
    }

    private bool MeetsRequirement(ApRequirement requirement, ApLocationData location)
    {
        switch (requirement.Type)
        {
            case "items":
                return requirement.AllOf.All(HasItem) && (requirement.AnyOf.Length == 0 || requirement.AnyOf.Any(HasItem));
            case "catch_type":
                return CanCatch(location);
            case "research":
                return Has("Research Part", requirement.Cost);
            case "iron_rig_phase":
                return MeetsIronRigPhase(requirement.Phase);
            case "locations":
                return requirement.AllOf.All(CanReachLocation);
            case "regions":
                return requirement.AnyOf.Any(CanReachRegion);
            case "aberrations":
                return CanGetAberrations();
            default:
                return false;
        }
    }

    // As rules.can_get_aberrations: half the Progressive Aberration Chance items, or Guaranteed Aberrations.
    private bool CanGetAberrations()
    {
        var progressiveCount = options.ProgressiveAberrationChanceItems;
        if (progressiveCount == 0 && !options.GuaranteedAberrationsItem)
        {
            return true;
        }

        return (options.GuaranteedAberrationsItem && Has("Guaranteed Aberrations")) ||
               (progressiveCount > 0 && Has("Progressive Aberration Chance", (progressiveCount + 1) / 2));
    }

    private bool MeetsIronRigPhase(int phase)
    {
        if (phase > 4 && !CanEquipRodFor("Infused Mangrove"))
        {
            return false;
        }

        if (phase > 3 && !(CanEquipRodFor("Infused Hadal") && CanEquipRodFor("Infused Abyssal")))
        {
            return false;
        }

        if (phase > 2 && !(CanEquipRodFor("Infused Oceanic") && CanEquip("Siphon Trawler")))
        {
            return false;
        }

        if (phase > 1 && !(CanEquipRodFor("Infused Shallow") && CanEquipRodFor("Infused Coastal")))
        {
            return false;
        }

        return phase <= 0 || (Has("Dredge Crane") && CanReachRegion("The Iron Rig"));
    }

    private bool CanEquipRodFor(string catchType) => ToolsFor(catchType, "Rod").Any(CanEquip);

    private bool HasItem(string itemName)
    {
        if (itemName == "Icebreaker")
        {
            return Has(itemName) || IcebreakerParts.All(p => Has(p));
        }

        return CanEquip(itemName);
    }

    private bool CanEquip(string itemName)
    {
        return Has(itemName) && Has("Progressive Hull", Fitting.HullsNeeded(itemName, options.IncludeIronRig));
    }

    private bool HasBoatPerformance((int knots, int lumens) performance)
    {
        if (!options.RequireBoatPerformance)
        {
            return true;
        }

        var hullCount = Count("Progressive Hull");
        var engines = Engines.Where(e => Has(e));
        var lights = Lights.Where(l => Has(l));
        return BaseSpeed + Fitting.BestTotal(engines, "speed", hullCount, options.IncludeIronRig) >= performance.knots &&
               Fitting.BestTotal(lights, "lumens", hullCount, options.IncludeIronRig) >= performance.lumens;
    }

    private bool CanCatch(ApLocationData location)
    {
        var catchType = location.Requirements.FirstOrDefault(r => r.Type == "catch_type")?.CatchType;
        if (catchType == null)
        {
            return false;
        }

        if (catchType == "Crab")
        {
            return ToolsFor("Crab", "Crab Pot").Any(t => Has(t));
        }

        var hasRod = location.CanCatchRod && ToolsFor(catchType, "Rod").Any(CanEquip);
        var allowNetLogic = location.CanCatchNet && (!location.CanCatchRod || options.LogicalNets);
        var hasNet = allowNetLogic && ToolsFor(catchType, "Net").Any(CanEquip);
        // Atrophy harvests a whole fishing spot without any tackle.
        var hasAtrophy = location.CanCatchRod && Has("Atrophy");
        return hasRod || hasNet || hasAtrophy;
    }

    private static string[] ToolsFor(string catchType, string group)
    {
        return CatchToolIndex.TryGetValue((catchType, group), out var tools) ? tools : [];
    }

    private static Dictionary<(string, string), string[]> BuildCatchToolIndex()
    {
        return ApWorldData.Items.Values
            .Where(i => CatchToolGroups.Contains(i.Group ?? "") && i.CanCatch.Length > 0 && !ExcludedCatchTools.Contains(i.Name))
            .SelectMany(i => i.CanCatch.Select(catchType => (catchType, i.Group, i.Name)))
            .GroupBy(t => (t.catchType, t.Group))
            .ToDictionary(g => g.Key, g => g.Select(t => t.Name).ToArray());
    }
}
