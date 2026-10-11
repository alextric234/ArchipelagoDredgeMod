using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using Winch.Core;

namespace ArchipelagoDredge.Game.Services;

/// <summary>
///     Grants boat upgrades through the game's <see cref="UpgradeManager" />.
/// </summary>
public static class UpgradeService
{
    private const string IronRigHullUpgradeId = "tier-5-hull";

    // Each Progressive Hull grants the next row: that tier's slot upgrades plus the next hull.
    private static readonly Dictionary<int, List<string>> HullTierUpgrades = new()
    {
        {1, ["tier-1-engines-1", "tier-1-fishing-1", "tier-1-lights-1", "tier-1-net-1", "tier-2-hull"]},
        {2, ["tier-2-engines-1", "tier-2-fishing-1", "tier-2-storage-1", "tier-3-hull"]},
        {3, ["tier-3-engines-1", "tier-3-fishing-1", "tier-3-lights-1", "tier-3-net-1", "tier-3-storage-1", "tier-4-hull"]},
        {4, ["tier-4-engines-1", "tier-4-fishing-1", "tier-4-lights-1", "tier-4-storage-1", IronRigHullUpgradeId]}
    };

    // The tier that grants tier-4-hull, the last base-game hull. Without the Iron Rig there's no
    // further Progressive Hull, so this tier also grants the tier-4 slot upgrades.
    private const int LastBaseGameHullTier = 3;
    private const int IronRigHullTier = 4;

    public static void ApplyNextHullTier()
    {
        var nextHullTier = ArchipelagoStateManager.StateData.CurrentHullUpgrade + 1;
        if (!HullTierUpgrades.TryGetValue(nextHullTier, out var upgradeIds))
        {
            WinchCore.Log.Warn($"Received a Progressive Hull beyond the last hull tier ({HullTierUpgrades.Keys.Max()})");
            return;
        }

        ArchipelagoStateManager.StateData.CurrentHullUpgrade = nextHullTier;

        foreach (var upgradeId in upgradeIds)
        {
            if (upgradeId == IronRigHullUpgradeId && !CanGrantIronRigHull())
            {
                continue;
            }

            AddUpgrade(upgradeId);
        }

        if (nextHullTier == LastBaseGameHullTier && !ArchipelagoClient.SlotData.IncludeIronRigDLC)
        {
            foreach (var upgradeId in HullTierUpgrades[IronRigHullTier].Where(id => id != IronRigHullUpgradeId))
            {
                AddUpgrade(upgradeId);
            }
        }
    }

    /// <summary>
    ///     Gives the upgrade for free. Returns false if the game has no upgrade with that id.
    /// </summary>
    public static bool AddUpgrade(string upgradeId)
    {
        var upgradeManager = GameManager.Instance.UpgradeManager;
        var upgradeData = upgradeManager.GetUpgradeDataById(upgradeId);
        if (upgradeData == null)
        {
            WinchCore.Log.Warn($"Unknown upgrade id '{upgradeId}'; not granted");
            return false;
        }

        upgradeManager.AddUpgrade(upgradeData, true);
        return true;
    }

    // The seed must include the Iron Rig and the player must own it.
    private static bool CanGrantIronRigHull()
    {
        if (!ArchipelagoClient.SlotData.IncludeIronRigDLC)
        {
            return false;
        }

        if (!GameManager.Instance.EntitlementManager.GetHasEntitlement(Entitlement.DLC_2))
        {
            WinchCore.Log.Warn("This seed includes The Iron Rig, but the DLC isn't owned; skipping the Iron Rig hull");
            return false;
        }

        return true;
    }
}
