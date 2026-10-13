using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoDredge.Game.Tracker;

public class LogicRequirement(string text, bool met)
{
    public string Text { get; } = text;
    public bool Met { get; } = met;
}

/// <summary>
///     Spells out what logic needs for a location, using the same checks as CanReachLocation.
/// </summary>
public partial class TrackerLogic
{
    private const int MaxListedItems = 4;

    private static readonly Dictionary<string, string> PassageByRegion = new()
    {
        ["Gale Cliffs"] = "The Windward Litany",
        ["Stellar Basin"] = "The Astral Testament",
        ["Twisted Strand"] = "The Mangrove Canticle",
        ["Devil's Spine"] = "The Cinder Gospel"
    };

    public static List<LogicRequirement> Explain(IReadOnlyDictionary<string, int> received, TrackerOptions options,
        string locationName)
    {
        var logic = new TrackerLogic(received, options);
        logic.CollectEvents();
        return ApWorldData.Locations.TryGetValue(locationName, out var location) ? logic.ExplainLocation(location) : [];
    }

    private List<LogicRequirement> ExplainLocation(ApLocationData location)
    {
        var lines = new List<LogicRequirement>();
        ExplainRegion(location.Region, lines);

        if (options.AddFishingLicenses && location.Group == "Encyclopedia" &&
            LicenseByRegion.TryGetValue(location.Region, out var license))
        {
            lines.Add(new LogicRequirement(license, Has(license)));
        }

        if (location.Region == "Open Ocean" && AtSeaLocationGroups.Contains(location.Group))
        {
            ExplainPerformance(OpenOceanPerformance, "out at sea", lines);
        }

        if (location.IsAberration && location.Group == "Encyclopedia")
        {
            ExplainAberrations(lines);
        }

        foreach (var requirement in location.Requirements)
        {
            ExplainRequirement(requirement, location, lines);
        }

        return lines;
    }

    private void ExplainRegion(string region, List<LogicRequirement> lines)
    {
        switch (region)
        {
            case "Open Ocean":
                ExplainPassage("The Pelagic Psalm", lines);
                break;
            case "Gale Cliffs":
            case "Stellar Basin":
            case "Twisted Strand":
            case "Devil's Spine":
                ExplainRegion("Open Ocean", lines);
                ExplainPassage(PassageByRegion[region], lines);
                ExplainPerformance(MidRegionsPerformance, $"to enter {region}", lines);
                break;
            case "The Iron Rig":
                ExplainRegion("Open Ocean", lines);
                ExplainPerformance(OpenOceanPerformance, "to reach the Iron Rig", lines);
                break;
            case "The Pale Reach":
                ExplainRegion("Open Ocean", lines);
                ExplainPassage("The Rimebound Chronicle", lines);
                ExplainPerformance(PaleReachPerformance, "to enter the Pale Reach", lines);
                break;
        }
    }

    private void ExplainPassage(string passage, List<LogicRequirement> lines)
    {
        if (options.AddPassageItems)
        {
            lines.Add(new LogicRequirement(passage, Has(passage)));
        }
    }

    private void ExplainPerformance((int knots, int lumens) performance, string where, List<LogicRequirement> lines)
    {
        if (!options.RequireBoatPerformance)
        {
            return;
        }

        var hullCount = Count("Progressive Hull");
        var knots = BaseSpeed + Fitting.BestTotal(Engines.Where(e => Has(e)), "speed", hullCount, options.IncludeIronRig);
        var lumens = Fitting.BestTotal(Lights.Where(l => Has(l)), "lumens", hullCount, options.IncludeIronRig);
        lines.Add(new LogicRequirement(
            $"{performance.knots} kn engines and {performance.lumens} lm lights {where} (fitted: {knots} kn, {lumens} lm)",
            HasBoatPerformance(performance)));
    }

    private void ExplainAberrations(List<LogicRequirement> lines)
    {
        var progressiveCount = options.ProgressiveAberrationChanceItems;
        if (progressiveCount == 0 && !options.GuaranteedAberrationsItem)
        {
            return;
        }

        var text = progressiveCount > 0
            ? $"{(progressiveCount + 1) / 2} Progressive Aberration Chance (have {Count("Progressive Aberration Chance")})"
            : "Guaranteed Aberrations";
        if (progressiveCount > 0 && options.GuaranteedAberrationsItem)
        {
            text += ", or Guaranteed Aberrations";
        }

        lines.Add(new LogicRequirement(text, CanGetAberrations()));
    }

    private void ExplainRequirement(ApRequirement requirement, ApLocationData location, List<LogicRequirement> lines)
    {
        switch (requirement.Type)
        {
            case "items":
                lines.AddRange(requirement.AllOf.Select(item => new LogicRequirement(DescribeItem(item), HasItem(item))));
                if (requirement.AnyOf.Length > 0)
                {
                    lines.Add(new LogicRequirement(DescribeAnyOf(requirement.AnyOf), requirement.AnyOf.Any(HasItem)));
                }

                break;
            case "catch_type":
                lines.Add(new LogicRequirement(DescribeTackle(location), CanCatch(location)));
                break;
            case "research":
                lines.Add(new LogicRequirement($"{requirement.Cost} Research Parts (have {Count("Research Part")})",
                    Has("Research Part", requirement.Cost)));
                break;
            case "iron_rig_phase":
                lines.Add(new LogicRequirement($"Iron Rig phase {requirement.Phase}",
                    MeetsIronRigPhase(requirement.Phase)));
                break;
            case "locations":
                lines.AddRange(requirement.AllOf.Select(name =>
                    new LogicRequirement($"Catch {name}", CanReachLocation(name))));
                break;
            case "regions":
                lines.Add(new LogicRequirement($"Reach one of {string.Join(", ", requirement.AnyOf)}",
                    requirement.AnyOf.Any(CanReachRegion)));
                break;
            case "aberrations":
                ExplainAberrations(lines);
                break;
        }
    }

    // An item counts once it fits on the boat, so a received item that doesn't fit yet says why.
    private string DescribeItem(string item)
    {
        if (item == "Icebreaker" || !Has(item) || CanEquip(item))
        {
            return item;
        }

        return $"{item} (needs {Fitting.HullsNeeded(item, options.IncludeIronRig)} Progressive Hull to fit)";
    }

    private static string DescribeAnyOf(string[] items)
    {
        if (items.Length <= MaxListedItems)
        {
            return "One of " + string.Join(", ", items);
        }

        var groups = items
            .Select(i => ApWorldData.Items.TryGetValue(i, out var data) ? data.Group : null)
            .Where(g => g != null)
            .Distinct()
            .Select(g => g.ToLowerInvariant());
        return $"Any {string.Join(", ", groups)} ({items.Length} items)";
    }

    private string DescribeTackle(ApLocationData location)
    {
        var catchType = location.Requirements.FirstOrDefault(r => r.Type == "catch_type")?.CatchType ?? "?";
        if (catchType == "Crab")
        {
            return "A crab pot";
        }

        var ways = new List<string>();
        if (location.CanCatchRod)
        {
            ways.Add("A rod");
        }

        if (location.CanCatchNet && (!location.CanCatchRod || options.LogicalNets))
        {
            ways.Add(ways.Count == 0 ? "A net" : "net");
        }

        var text = $"{string.Join(" or ", ways)} for {catchType} fish";
        return location.CanCatchRod ? text + ", or Atrophy" : text;
    }
}
