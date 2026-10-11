using System;
using System.Threading.Tasks;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Services;
using ArchipelagoDredge.Network.Enums;
using ArchipelagoDredge.Utils;
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
    private static int _connectAttempt;

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

        var attempt = ++_connectAttempt;
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

            if (ArchipelagoClient.IsConnected)
            {
                ArchipelagoClient.State = ConnectionState.Connected;
                WinchCore.Log.Info($"Connected to {_apHost}:{_apPort} as '{_slotName}'. " +
                                   $"Items received so far: {ArchipelagoClient.Session.Items.AllItemsReceived.Count}, " +
                                   $"last processed item index: {ArchipelagoStateManager.StateData?.LastProcessedIndex}");
                SessionStartService.OnSessionStarted();
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
                ArchipelagoClient.Disconnect();
                ArchipelagoClient.State = ConnectionState.Disconnected;
            }
        }
        catch (OperationCanceledException) when (attempt != _connectAttempt)
        {
            // Disconnect() was called while connecting; it has already cleaned up and notified.
            WinchCore.Log.Info("Connection attempt cancelled");
        }
        catch (Exception e)
        {
            if (attempt != _connectAttempt)
            {
                return;
            }

            ArchipelagoClient.Disconnect();
            ArchipelagoClient.State = ConnectionState.Disconnected;
            TerminalCommandManager.LogMessage(TerminalLogType.Error, $"Archipelago Connection failed: {e.Message}");
            NotificationHelper.ShowNotificationWithColour(NotificationType.NONE, "Archipelago Connection failed.",
                DredgeColorTypeEnum.NEGATIVE);
            WinchCore.Log.Error($"Connection failed: {e}");
        }
    }

    public static void Disconnect()
    {
        if (ArchipelagoClient.State == ConnectionState.Disconnected && ArchipelagoClient.Session == null)
        {
            return;
        }

        TearDown();
        WinchCore.Log.Info("Disconnected from Archipelago");
        TerminalCommandManager.LogMessage(TerminalLogType.Message, "Disconnected from Archipelago");
        NotificationHelper.ShowNotificationWithColour(NotificationType.NONE, "Disconnected from Archipelago",
            DredgeColorTypeEnum.NEUTRAL);
    }

    /// <summary>
    ///     Called on the main thread when the server closes the connection or the socket drops.
    /// </summary>
    public static void HandleConnectionLost(string reason)
    {
        // While connecting, TryConnect sees the failure itself.
        if (ArchipelagoClient.State != ConnectionState.Connected)
        {
            return;
        }

        TearDown();

        var detail = string.IsNullOrEmpty(reason) ? "" : $": {reason}";
        WinchCore.Log.Warn($"Lost connection to Archipelago{detail}");
        TerminalCommandManager.LogMessage(TerminalLogType.Error,
            $"Lost connection to Archipelago{detail}. Reconnect with F8 or 'ap connect'.");
        NotificationHelper.ShowNotificationWithColour(NotificationType.NONE, "Lost connection to Archipelago",
            DredgeColorTypeEnum.NEGATIVE);
    }

    private static void TearDown()
    {
        // Invalidates any connect attempt still in flight.
        _connectAttempt++;
        // Keep the latest received items for offline play before the session goes away.
        SessionStartService.SaveSessionSnapshot();
        ArchipelagoClient.Disconnect();
        ArchipelagoClient.State = ConnectionState.Disconnected;
        PassageManager.Reset();
    }
}