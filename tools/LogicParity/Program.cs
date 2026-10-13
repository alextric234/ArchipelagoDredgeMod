using System;
using System.IO;
using System.Linq;
using ArchipelagoDredge.Game.Tracker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

if (args.Length != 1)
{
    Console.WriteLine("Usage: LogicParity <fixtures.json>");
    return 2;
}

var fixtures = JArray.Parse(File.ReadAllText(args[0]));
int failures = 0, index = 0;
foreach (var fixture in fixtures)
{
    var o = fixture["options"];
    var options = new TrackerOptions
    {
        IncludeIronRig = (bool) o["include_iron_rig_dlc"],
        IncludePaleReach = (bool) o["include_pale_reach_dlc"],
        AddFishingLicenses = (bool) o["add_fishing_licenses"],
        AddPassageItems = (bool) o["add_passage_items"],
        LogicalNets = (bool) o["logical_nets"],
        RequireBoatPerformance = (bool) o["require_boat_performance"],
        ProgressiveAberrationChanceItems = (int?) o["progressive_aberration_chance_items"] ?? 0,
        GuaranteedAberrationsItem = (bool?) o["guaranteed_aberrations_item"] ?? false
    };
    var items = ((JObject) fixture["items"]).Properties().ToDictionary(p => p.Name, p => (int) p.Value);
    var expected = fixture["reachable"].Values<string>().ToHashSet();
    var actual = TrackerLogic.AccessibleLocations(items, options, fixture["locations"].Values<string>());

    var missing = expected.Except(actual).ToList();
    var extra = actual.Except(expected).ToList();
    if (missing.Count + extra.Count > 0)
    {
        failures++;
        Console.WriteLine($"Case {index} {o.ToString(Formatting.None)} with {items.Values.Sum()} items:");
        Console.WriteLine($"  in apworld logic only ({missing.Count}): {string.Join(", ", missing.Take(10))}");
        Console.WriteLine($"  in mod logic only ({extra.Count}): {string.Join(", ", extra.Take(10))}");
    }

    index++;
}

Console.WriteLine($"{index - failures}/{index} cases match");
return failures == 0 ? 0 : 1;
