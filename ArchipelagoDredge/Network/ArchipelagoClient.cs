using System;
using System.Linq;
using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Utils;
using CommandTerminal;
using Winch.Util;

namespace ArchipelagoDredge.Network;

public static class ArchipelagoClient
{
    public static ArchipelagoSession Session { get; private set; }

    public static void Connect(string apHost, int apPort, string slotName, string password)
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

        var loginResult = Session.TryConnectAndLogin(
            "DREDGE",
            slotName,
            ItemsHandlingFlags.AllItems,
            password: password
        );

        if (!loginResult.Successful)
        {
            LoginFailure loginFailure = (LoginFailure)loginResult;
            StringBuilder errorsStringBuilder = new StringBuilder();
            foreach (var loginFailureError in loginFailure.Errors)
            {
                errorsStringBuilder.AppendLine(loginFailureError);
            }
            throw new Exception($"Errors: {loginFailure.Errors}");
        }

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

        ArchipelagoItemManager.RestockShops();
    }

    private static void OnPacketReceived(ArchipelagoPacketBase packet)
    {
        switch (packet.PacketType)
        {
            case ArchipelagoPacketType.PrintJSON:
                PrintJsonHelper.ShowPrintJsonMessage((ItemPrintJsonPacket) packet);
                break;
            default:
                return;
        }
    }

    public static void Disconnect()
    {
        Session?.Socket?.DisconnectAsync();
        Session = null;
    }

    public static bool HasItemsToProcess()
    {
        if (Session == null || Session.Socket == null || !Session.Socket.Connected)
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