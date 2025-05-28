using System;
using System.Collections.Generic;

namespace ArchipelagoDredge.Game.Patches.Helpers;

public static class HarvestContext
{
    [ThreadStatic]
    public static bool IsHarvesterActive;

    private static readonly Dictionary<SpatialItemInstance, string> ContextMap = new();

    public static void SetContext(SpatialItemInstance item, string source)
    {
        if (!ContextMap.ContainsKey(item))
            ContextMap[item] = source;
    }

    public static string GetContext(SpatialItemInstance item)
    {
        return ContextMap.TryGetValue(item, out var context) ? context : null;
    }
    public static void RemoveContext(SpatialItemInstance item)
    {
        ContextMap.Remove(item);
    }
}