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
public static class SessionStartService
{
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

            saveData.availableDestinations.Add("destination.rest");
            saveData.availableDestinations.Add("destination.storage");
            saveData.availableDestinations.Add("destination.research");
            saveData.availableDestinations.Add("destination.gm-fishmonger");
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
