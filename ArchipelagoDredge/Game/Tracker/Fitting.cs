using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoDredge.Game.Tracker;

/// <summary>
///     Port of the apworld's fitting.py: whether equipment fits the hull's slots.
/// </summary>
public static class Fitting
{
    // Hull cells each equipment group can use: N cells take rods or nets, R cells take rods only.
    private static readonly Dictionary<string, string> CellKindsByItemGroup = new()
    {
        ["Rod"] = "RN",
        ["Net"] = "N",
        ["Engine"] = "E",
        ["Light"] = "L"
    };

    private static readonly Dictionary<(string, bool), int> HullsNeededCache = new();
    private static readonly Dictionary<(string, string, int, bool), int> BestTotalCache = new();

    private static HashSet<(int x, int y)> ParseCells(IEnumerable<string> rows, string kinds)
    {
        var cells = new HashSet<(int, int)>();
        var y = 0;
        foreach (var row in rows)
        {
            for (var x = 0; x < row.Length; x++)
            {
                if (kinds.IndexOf(row[x]) >= 0)
                {
                    cells.Add((x, y));
                }
            }

            y++;
        }

        return cells;
    }

    private static List<HashSet<(int x, int y)>> Rotations(HashSet<(int x, int y)> shape)
    {
        var result = new List<HashSet<(int, int)>>();
        for (var i = 0; i < 4; i++)
        {
            var minX = shape.Min(c => c.x);
            var minY = shape.Min(c => c.y);
            result.Add(new HashSet<(int, int)>(shape.Select(c => (c.x - minX, c.y - minY))));
            shape = new HashSet<(int x, int y)>(shape.Select(c => (c.y, -c.x)));
        }

        return result;
    }

    private static List<HashSet<(int x, int y)>> Placements(HashSet<(int x, int y)> shape, HashSet<(int x, int y)> cells)
    {
        var result = new List<HashSet<(int x, int y)>>();
        foreach (var rotation in Rotations(shape))
        {
            var anchor = rotation.First();
            foreach (var cell in cells)
            {
                int dx = cell.x - anchor.x, dy = cell.y - anchor.y;
                var placed = new HashSet<(int x, int y)>(rotation.Select(c => (c.x + dx, c.y + dy)));
                if (placed.IsSubsetOf(cells) && !result.Any(p => p.SetEquals(placed)))
                {
                    result.Add(placed);
                }
            }
        }

        return result;
    }

    // Copies of one item share the same options list and sit next to each other, so each copy only tries placements
    // after the previous one's to skip reordered duplicates.
    private static bool FitsTogether(List<List<HashSet<(int x, int y)>>> options, int index, HashSet<(int x, int y)> used,
        int start)
    {
        if (index == options.Count)
        {
            return true;
        }

        var current = options[index];
        for (var i = start; i < current.Count; i++)
        {
            var placed = current[i];
            if (placed.Overlaps(used))
            {
                continue;
            }

            var nextStart = index + 1 < options.Count && ReferenceEquals(options[index + 1], current) ? i + 1 : 0;
            var nowUsed = new HashSet<(int x, int y)>(used);
            nowUsed.UnionWith(placed);
            if (FitsTogether(options, index + 1, nowUsed, nextStart))
            {
                return true;
            }
        }

        return false;
    }

    private static string[] LayoutRows(int hullCount, bool includeIronRig)
    {
        var layouts = ApWorldData.HullLayouts[includeIronRig ? "with_iron_rig" : "without_iron_rig"];
        return layouts[Math.Min(hullCount, layouts.Length - 1)];
    }

    public static int HullsNeeded(string itemName, bool includeIronRig)
    {
        if (HullsNeededCache.TryGetValue((itemName, includeIronRig), out var cached))
        {
            return cached;
        }

        var result = 0;
        if (ApWorldData.Items.TryGetValue(itemName, out var item) &&
            CellKindsByItemGroup.TryGetValue(item.Group ?? "", out var kinds) && item.Shape.Length > 0)
        {
            var shape = ParseCells(item.Shape, "X");
            var layouts = ApWorldData.HullLayouts[includeIronRig ? "with_iron_rig" : "without_iron_rig"];
            result = -1;
            for (var hullCount = 0; hullCount < layouts.Length; hullCount++)
            {
                if (Placements(shape, ParseCells(layouts[hullCount], kinds)).Count > 0)
                {
                    result = hullCount;
                    break;
                }
            }

            if (result < 0)
            {
                throw new InvalidOperationException($"{itemName} doesn't fit on any hull");
            }
        }

        HullsNeededCache[(itemName, includeIronRig)] = result;
        return result;
    }

    /// <summary>
    ///     The highest summed stat (speed or lumens) of items that can all be equipped on the hull at once, counting
    ///     extra copies of rebuyable items.
    /// </summary>
    public static int BestTotal(IEnumerable<string> itemNames, string stat, int hullCount, bool includeIronRig)
    {
        var names = itemNames.OrderBy(n => n, StringComparer.Ordinal).ToList();
        var key = (string.Join("\n", names), stat, hullCount, includeIronRig);
        if (BestTotalCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var result = ComputeBestTotal(names, stat, hullCount, includeIronRig);
        BestTotalCache[key] = result;
        return result;
    }

    private static int StatOf(string name, string stat)
    {
        var item = ApWorldData.Items[name];
        return stat == "speed" ? item.Speed : item.Lumens;
    }

    private static int ComputeBestTotal(List<string> names, string stat, int hullCount, bool includeIronRig)
    {
        var rows = LayoutRows(hullCount, includeIronRig);
        var options = names.ToDictionary(name => name, name =>
            Placements(ParseCells(ApWorldData.Items[name].Shape, "X"),
                ParseCells(rows, CellKindsByItemGroup[ApWorldData.Items[name].Group])));
        var fittingNames = names.Where(name => options[name].Count > 0).ToList();
        if (fittingNames.Count == 0)
        {
            return 0;
        }

        var freeCells = ParseCells(rows, CellKindsByItemGroup[ApWorldData.Items[fittingNames[0]].Group]).Count;
        var loadouts = Loadouts(fittingNames, 0, freeCells)
            .OrderByDescending(loadout => loadout.Sum(name => StatOf(name, stat)))
            .ToList();
        foreach (var loadout in loadouts)
        {
            if (FitsTogether(loadout.Select(name => options[name]).ToList(), 0, new HashSet<(int x, int y)>(), 0))
            {
                return loadout.Sum(name => StatOf(name, stat));
            }
        }

        return 0;
    }

    // Every combination of the items that could fill at most freeCells, taking rebuyable items more than once.
    private static List<List<string>> Loadouts(List<string> names, int index, int freeCells)
    {
        if (index == names.Count)
        {
            return [[]];
        }

        var item = ApWorldData.Items[names[index]];
        var size = ParseCells(item.Shape, "X").Count;
        var maxCopies = item.Rebuyable ? freeCells / size : Math.Min(1, freeCells / size);
        var result = new List<List<string>>();
        for (var copies = 0; copies <= maxCopies; copies++)
        {
            foreach (var rest in Loadouts(names, index + 1, freeCells - copies * size))
            {
                result.Add(Enumerable.Repeat(names[index], copies).Concat(rest).ToList());
            }
        }

        return result;
    }
}
