using System;
using System.Linq;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Network.Enums;
using ArchipelagoDredge.Utils;
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
            BindSaveToRoom();
            SaveSessionSnapshot();

            var saveData = GameManager.Instance.SaveData;

            saveData.CanCatchAberrations = true;
            if (saveData.WorldPhase < 1)
            {
                saveData.WorldPhase = 1;
            }

            // Hulls arrive as AP items, so the player may never have the shipwright conversation that
            // normally opens the dry docks; open them, and the shipwright, so slot upgrades can still be bought.
            saveData.availableDestinations.Add("destination.gm-shipwright");
            saveData.availableDestinations.Add("destination.gm-shipwright-upgrades");
            saveData.availableDestinations.Add("destination.tm-upgrades");

            RevealEncyclopedia();

            LocationHelper.LoadArchipelagoIds();
            RemoveCheckedRelicPois();
            LocationHelper.ReconcileArchipelagoChecks();
            LocationHelper.ResendRecordedChecks();
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
    ///     Saves the current session's seed options and received items, so the save keeps following AP
    ///     rules offline. Call on connect and just before the session is torn down (the socket may already
    ///     be closed then, but the session still holds the received items).
    /// </summary>
    public static void SaveSessionSnapshot()
    {
        try
        {
            var session = ArchipelagoClient.Session;
            if (session == null || ArchipelagoClient.State != ConnectionState.Connected)
            {
                return;
            }

            var receivedItemNames = session.Items.AllItemsReceived
                .Where(item => item.ItemGame == ArchipelagoClient.GameName)
                .Select(item => item.ItemName);
            ArchipelagoStateManager.SaveSessionSnapshot(ArchipelagoClient.SlotData, receivedItemNames);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Couldn't save the Archipelago session snapshot: {e}");
        }
    }

    // Binds the save to this room on its first connect. A save bound to a different seed or slot is
    // rebound with a warning rather than blocked.
    private static void BindSaveToRoom()
    {
        var session = ArchipelagoClient.Session;
        var seed = session.RoomState.Seed;
        var slotName = session.Players.ActivePlayer.Name;

        var previousBinding = ArchipelagoStateManager.BindToRoom(seed, slotName);
        if (previousBinding == null)
        {
            return;
        }

        var message = $"This save was used with a different multiworld ({previousBinding}). " +
                      $"It's now bound to seed '{seed}', slot '{slotName}'; received items may not line up.";
        WinchCore.Log.Warn(message);
        TerminalCommandManager.LogMessage(TerminalLogType.Warning, message);
        NotificationHelper.ShowNotificationWithColour(NotificationType.NONE,
            "This save was used with a different multiworld", DredgeColorTypeEnum.WARNING);
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
