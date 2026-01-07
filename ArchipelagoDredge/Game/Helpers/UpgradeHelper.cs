using System;
using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Utils;
using CommandTerminal;

namespace ArchipelagoDredge.Game.Helpers;

public static class UpgradeHelper
{
    private static Dictionary<int, List<string>> HullUpgradeBoatUpgrades =  new()
    {
        {1, ["tier-1-engines-1", "tier-1-fishing-1", "tier-1-lights-1", "tier-1-net-1", "tier-2-hull"]},
        {2, ["tier-2-engines-1", "tier-2-fishing-1", "tier-2-storage-1", "tier-3-hull"]},
        {3, ["tier-3-engines-1", "tier-3-fishing-1", "tier-3-lights-1", "tier-3-net-1", "tier-3-storage-1", "tier-4-hull"]},
        {4, ["tier-4-engines-1", "tier-4-fishing-1", "tier-4-lights-1", "tier-4-storage-1", "tier-5-hull"]}
    };

    public static void UpgradeBoat()
    {
        var currentHullTier = ArchipelagoStateManager.StateData.CurrentHullUpgrade;

        if (currentHullTier + 1 > HullUpgradeBoatUpgrades.Last().Key)
        {
            return;
        }
        
        var nextHullTier = ++ArchipelagoStateManager.StateData.CurrentHullUpgrade;
        var tirHullUpgrade = HullUpgradeBoatUpgrades[4].Last();

        foreach (var boatUpgrade in HullUpgradeBoatUpgrades[nextHullTier])
        {
            if (boatUpgrade != tirHullUpgrade)
            {
                Terminal.Shell.RunCommand($"upgrade.add {boatUpgrade}");
            }
            else if(GameManager.Instance.EntitlementManager.GetHasEntitlement(Entitlement.DLC_2))
            {
                Terminal.Shell.RunCommand($"upgrade.add {boatUpgrade}");
            }
        }
        
    }
}
