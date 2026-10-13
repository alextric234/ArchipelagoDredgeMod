using System.Collections.Generic;
using System.Linq;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;

/// <summary>
///     Iron Rig buildings cost no Ironhaven Crates in AP saves that include the Iron Rig. The building recipes are shared
///     assets that stay loaded between saves, so the original costs are put back when the save ends.
/// </summary>
public static class IronRigConstructionManager
{
    private const string BuildingGridPrefix = "TIR_Building_";
    private const string CrateId = "crate";

    private static readonly Dictionary<QuestGridConfig, (List<CompletedGridCondition> conditions,
        List<SpatialItemInstance> preset)> Originals = new();

    public static void MakeBuildingsFree()
    {
        if (Originals.Count > 0)
        {
            return;
        }

        foreach (var grid in GameManager.Instance.DataLoader.allQuestGrids.Where(g => g.name.StartsWith(BuildingGridPrefix)))
        {
            var preset = grid.presetGrid?.spatialItems;
            Originals[grid] = (grid.completeConditions.ToList(), preset?.ToList());

            grid.completeConditions.RemoveAll(c => c is ItemCountCondition count && count.item != null &&
                                                   count.item.id == CrateId);
            // A recipe with no conditions never counts as complete, so an empty one stands in.
            if (grid.completeConditions.Count == 0)
            {
                grid.completeConditions.Add(new EmptyCondition());
            }

            preset?.RemoveAll(item => item.id == CrateId);
        }

        WinchCore.Log.Info($"Iron Rig buildings need no crates ({Originals.Count} recipes)");
    }

    public static void Restore()
    {
        foreach (var pair in Originals)
        {
            pair.Key.completeConditions.Clear();
            pair.Key.completeConditions.AddRange(pair.Value.conditions);
            if (pair.Value.preset != null)
            {
                pair.Key.presetGrid.spatialItems.Clear();
                pair.Key.presetGrid.spatialItems.AddRange(pair.Value.preset);
            }
        }

        Originals.Clear();
    }
}
