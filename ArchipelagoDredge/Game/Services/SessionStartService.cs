using System;
using System.Linq;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using CommandTerminal;
using Winch.Core;
using Winch.Util;

namespace ArchipelagoDredge.Game.Services;

/// <summary>
///     Game-side setup to run each time an AP session connects.
/// </summary>
public static class SessionStartService
{
    /// <summary>
    ///     Call on the main thread once the connection is up. Never throws: a failure here is logged
    ///     but doesn't drop a working connection.
    /// </summary>
    public static void OnSessionStarted()
    {
        try
        {
            var saveData = GameManager.Instance.SaveData;

            saveData.CanCatchAberrations = true;
            if (saveData.WorldPhase < 1)
            {
                saveData.WorldPhase = 1;
            }

            // Hulls arrive as AP items, so the player may never have the shipwright conversation that
            // normally opens the dry docks; open them so slot upgrades can still be bought.
            saveData.availableDestinations.Add("destination.gm-shipwright-upgrades");
            saveData.availableDestinations.Add("destination.tm-upgrades");

            RevealEncyclopedia();

            LocationHelper.LoadArchipelagoIds();
            RemoveCheckedRelicPois();
            LocationHelper.ReconcileArchipelagoChecks();
            LocationHelper.FlushPendingLocation();
            ArchipelagoItemManager.RestockShops();
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error setting up the game for the Archipelago session: {e}");
            TerminalCommandManager.LogMessage(TerminalLogType.Error,
                "Archipelago session setup failed; some features may not work. See the log for details.");
        }
    }

    /// <summary>
    ///     Shows every fish's name, picture and value in the encyclopedia, which needs a caught count and a
    ///     sold count above zero. Only fills counters that are still zero, so reconnecting doesn't inflate
    ///     them (the <c>ency.all</c> debug command used before added one to each on every connect).
    /// </summary>
    private static void RevealEncyclopedia()
    {
        var saveData = GameManager.Instance.SaveData;
        foreach (var fish in GameManager.Instance.ItemManager.GetAllItemsOfType<FishItemData>())
        {
            if (saveData.GetCaughtCountById(fish.id) == 0)
            {
                saveData.IncrementCaughtCounterById(fish.id);
            }

            if ((saveData.GetItemTransactionById(fish.id)?.sold ?? 0) == 0)
            {
                saveData.RecordItemTransaction(fish.id, true);
            }
        }
    }

    private static void RemoveCheckedRelicPois()
    {
        PoiUtil.GetAllPOI()
            .Select(p => p.Value)
            .OfType<HarvestPOI>()
            .Where(h => LocationHelper.RelicLocations.Contains(LocationHelper.DredgeIdToLocation(h.harvestPOIData.id)))
            .Where(h => h.Harvestable.GetStockCount(true) > 0)
            .Where(h => LocationHelper.HasThisLocationBeenChecked(h.HarvestPOIData.id))
            .ForEach(h => h.OnHarvested(true));
    }
}
