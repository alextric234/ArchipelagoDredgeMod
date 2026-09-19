using System;
using System.Threading.Tasks;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network.Enums;
using CommandTerminal;
using Winch.Core;

namespace ArchipelagoDredge.Network;

public static class ArchipelagoCommandManager
{
    private static string _apHost;
    private static int _apPort;
    private static string _slotName;
    private static string _password;
    private static bool _deathLink;

    public static void ConfigConnect()
    {
        var (host, port, slot, password, deathLink) = ApConfigHelper.Read();

        if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535)
        {
            WinchCore.Log.Error("Host and Port are required.");
            WinchCore.Log.Error($"Failed to connect to Archipelago slot. Check your config (mod menu or F7)");
            NotificationHelper.ShowNotificationWithColour(NotificationType.NONE, "Connection failed.",
                DredgeColorTypeEnum.NEGATIVE);
            ArchipelagoClient.State = ConnectionState.Error;
            return;
        }

        if (string.IsNullOrEmpty(slot))
        {
            WinchCore.Log.Error("Player slot is required");
            WinchCore.Log.Error($"Failed to connect to Archipelago slot. Check your config (mod menu or F7)");
            NotificationHelper.ShowNotificationWithColour(NotificationType.NONE, "Connection failed.",
                DredgeColorTypeEnum.NEGATIVE);
            ArchipelagoClient.State = ConnectionState.Error;
            return;
        }

        _ = TryConnect(host, port, slot, password, deathLink);
    }

    public static async Task TryConnect(string apHost, int apPort, string slotName, string password, bool deathLink)
    {
        if (ArchipelagoClient.State == ConnectionState.Connecting || ArchipelagoClient.State == ConnectionState.Connected)
        {
            return;
        }

        _apHost = apHost;
        _apPort = apPort;
        _slotName = slotName;
        _password = password;
        _deathLink = deathLink;

        ArchipelagoClient.State = ConnectionState.Connecting;
        TerminalCommandManager.LogMessage(TerminalLogType.Message, "Connecting to Archipelago...");

        try
        {
            await ArchipelagoClient.ConnectAsync(
                _apHost,
                _apPort,
                _slotName,
                _password,
                _deathLink
            );

            if (ArchipelagoClient.Session != null &&
                ArchipelagoClient.Session.Socket != null &&
                ArchipelagoClient.Session.Socket.Connected)
            {
                ArchipelagoClient.State = ConnectionState.Connected;
                DeathLinkManager.SetupDredgeDeathLinkService();
                TerminalCommandManager.LogMessage(TerminalLogType.Message, "Connected to Archipelago!");
                NotificationHelper.ShowNotificationWithColour(NotificationType.NONE, "Connected to Archipelago!",
                    DredgeColorTypeEnum.POSITIVE);


                if (ArchipelagoClient.SlotData.DeathLink)
                {
                    DeathLinkManager.EnableDeathLink();
                }
                else
                {
                    DeathLinkManager.DisableDeathLink();
                }
            }
            else
            {
                ArchipelagoClient.State = ConnectionState.Disconnected;
            }
        }
        catch (Exception e)
        {
            ArchipelagoClient.State = ConnectionState.Disconnected;
            TerminalCommandManager.LogMessage(TerminalLogType.Error, "Archipelago Connection failed.");
            NotificationHelper.ShowNotificationWithColour(NotificationType.NONE, "Archipelago Connection failed.",
                DredgeColorTypeEnum.NEGATIVE);
            WinchCore.Log.Error($"Connection failed: {e}");
        }
    }

    public static void Disconnect()
    {
        if (ArchipelagoClient.State != ConnectionState.Connected)
        {
            return;
        }

        ArchipelagoClient.Disconnect();
        ArchipelagoClient.State = ConnectionState.Disconnected;
        TerminalCommandManager.LogMessage(TerminalLogType.Message, "Disconnected from Archipelago");
        NotificationHelper.ShowNotificationWithColour(NotificationType.NONE, "Disconnected from Archipelago",
            DredgeColorTypeEnum.NEUTRAL);
    }
}