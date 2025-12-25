using System;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Utils;
using CommandTerminal;

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
            throw new Exception(loginResult.ToString());
        }

        LocationNames.LoadArchipelagoIds();

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
}