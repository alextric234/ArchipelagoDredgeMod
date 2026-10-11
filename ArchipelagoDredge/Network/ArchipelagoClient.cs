using System;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network.Enums;
using ArchipelagoDredge.Utils;
using CommandTerminal;
using Winch.Core;
using Winch.Util;

namespace ArchipelagoDredge.Network;

public static class ArchipelagoClient
{
    public static ArchipelagoSession Session { get; private set; }
    public static ConnectionState State { get; set; } = ConnectionState.Disconnected;
    public static SlotData SlotData = new SlotData();

    /// <summary>
    ///     True when there is a session and its socket is open. Safe to call at any time.
    /// </summary>
    public static bool IsConnected => Session?.Socket?.Connected == true;

    public static async Task ConnectAsync(string apHost, int apPort, string slotName, string password, bool deathLink)
    {
        if (!GameManager.Instance.DataLoader.HasLoaded())
        {
            TerminalCommandManager.LogMessage(TerminalLogType.Error,
                "Please load a save before connecting to a multiworld");
            return;
        }

        Disconnect(); // Ensure clean state

        Session = ArchipelagoSessionFactory.CreateSession(apHost, apPort);

        Session.Socket.PacketReceived += OnPacketReceived;

        try
        {
            await Session.ConnectAsync();
        }
        catch (Exception e)
        {
            throw new Exception("Failed to connect to Archipelago", e);
        }

        var loginResult = await Session.LoginAsync(
            "DREDGE",
            slotName,
            ItemsHandlingFlags.AllItems,
            password: password);

        if (!loginResult.Successful)
        {
            LoginFailure loginFailure = (LoginFailure)loginResult;
            throw new Exception($"Login failed: {string.Join(", ", loginFailure.Errors)}");
        }

        var successfulLogin = (LoginSuccessful)loginResult;
        SlotData.DeathLink = successfulLogin.SlotData.TryGetValue("death_link", out var deathLinkOption) && int.Parse(deathLinkOption.ToString()) == 1;
        SlotData.IncludeIronRigDLC = successfulLogin.SlotData.TryGetValue("include_iron_rig_dlc", out var ironRigDlcOption) && int.Parse(ironRigDlcOption.ToString()) == 1;
        SlotData.IncludePaleReachDLC = successfulLogin.SlotData.TryGetValue("include_pale_reach_dlc", out var paleReachDlcOption) && int.Parse(paleReachDlcOption.ToString()) == 1;
        SlotData.AddFishingLicenses = successfulLogin.SlotData.TryGetValue("add_fishing_licenses", out var addFishingLicensesOption) && int.Parse(addFishingLicensesOption.ToString()) == 1;
        SlotData.AddPassageItems = successfulLogin.SlotData.TryGetValue("add_passage_items", out var addPassageItemsOption) && int.Parse(addPassageItemsOption.ToString()) == 1;

        WinchCore.Log.Info($"Slot data: death_link={SlotData.DeathLink}, include_iron_rig_dlc={SlotData.IncludeIronRigDLC}, " +
                           $"include_pale_reach_dlc={SlotData.IncludePaleReachDLC}, add_fishing_licenses={SlotData.AddFishingLicenses}, " +
                           $"add_passage_items={SlotData.AddPassageItems}");

        StartupActions();
    }


    private static void StartupActions()
    {
        GameManager.Instance.SaveData.CanCatchAberrations = true;
        var worldPhase = GameManager.Instance.SaveData.WorldPhase;

        if (worldPhase < 1)
        {
            GameManager.Instance.SaveData.WorldPhase = 1;
        }
        LocationHelper.LoadArchipelagoIds();
        RemoveCheckedRelicPois();
        Terminal.Shell.RunCommand("ency.all");

        LocationHelper.ReconcileArchipelagoChecks();
        LocationHelper.FlushPendingLocation();
        ArchipelagoItemManager.RestockShops();
    }

    private static void OnPacketReceived(ArchipelagoPacketBase packet)
    {
        // Runs on the socket thread; an exception here would stop the library from
        // dispatching the rest of the packets in the same message.
        try
        {
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.PrintJSON:
                    PrintJsonHelper.ShowPrintJsonMessage((PrintJsonPacket) packet);
                    break;
                default:
                    return;
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error handling {packet.PacketType} packet: {e}");
        }
    }

    public static void Disconnect()
    {
        Session?.Socket?.DisconnectAsync();
        Session = null;
    }

    public static bool HasItemsToProcess()
    {
        if (!IsConnected)
        {
            return false;
        }

        var latestItemIndex = Session.Items.AllItemsReceived.Count - 1;
        return ArchipelagoStateManager.StateData.LastProcessedIndex < latestItemIndex;
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