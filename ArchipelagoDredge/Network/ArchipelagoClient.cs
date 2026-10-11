using System;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
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

        var session = ArchipelagoSessionFactory.CreateSession(apHost, apPort);
        Session = session;

        session.MessageLog.OnMessageReceived += OnMessageReceived;
        session.Socket.ErrorReceived += OnErrorReceived;
        session.Socket.SocketClosed += OnSocketClosed;

        try
        {
            await session.ConnectAsync();
        }
        catch (Exception e)
        {
            ThrowIfSuperseded(session);
            throw new Exception("Failed to connect to Archipelago", e);
        }

        ThrowIfSuperseded(session);

        var loginResult = await session.LoginAsync(
            "DREDGE",
            slotName,
            ItemsHandlingFlags.AllItems,
            password: password);

        ThrowIfSuperseded(session);

        if (!loginResult.Successful)
        {
            LoginFailure loginFailure = (LoginFailure)loginResult;
            throw new Exception($"Login failed: {string.Join(", ", loginFailure.Errors)}");
        }

        var successfulLogin = (LoginSuccessful)loginResult;
        var slotData = successfulLogin.SlotData;
        SlotData.DeathLink = SlotDataParser.GetBool(slotData, "death_link");
        SlotData.IncludeIronRigDLC = SlotDataParser.GetBool(slotData, "include_iron_rig_dlc");
        SlotData.IncludePaleReachDLC = SlotDataParser.GetBool(slotData, "include_pale_reach_dlc");
        SlotData.AddFishingLicenses = SlotDataParser.GetBool(slotData, "add_fishing_licenses");
        SlotData.AddPassageItems = SlotDataParser.GetBool(slotData, "add_passage_items");

        WinchCore.Log.Info($"Slot data: death_link={SlotData.DeathLink}, include_iron_rig_dlc={SlotData.IncludeIronRigDLC}, " +
                           $"include_pale_reach_dlc={SlotData.IncludePaleReachDLC}, add_fishing_licenses={SlotData.AddFishingLicenses}, " +
                           $"add_passage_items={SlotData.AddPassageItems}");
    }

    private static void OnMessageReceived(LogMessage message)
    {
        // Runs on the socket thread, so hand the work to the main thread (it touches the terminal
        // and game UI). Nothing here may throw: that would stop the library from dispatching the
        // rest of the packets in the same websocket message.
        MainThreadDispatcher.Enqueue(() => ShowMessage(message));
    }

    private static void ShowMessage(LogMessage message)
    {
        // The session may have closed between receiving the message and this frame.
        if (Session == null)
        {
            return;
        }

        try
        {
            PrintJsonHelper.ShowMessage(message);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error showing server message: {e}");
        }
    }

    // A disconnect while ConnectAsync is awaiting replaces or clears Session; stop that connect attempt.
    private static void ThrowIfSuperseded(ArchipelagoSession session)
    {
        if (Session != session)
        {
            throw new OperationCanceledException("Connection attempt was cancelled");
        }
    }

    private static void OnErrorReceived(Exception e, string message)
    {
        // Socket thread. Also raised for non-fatal errors (e.g. a handler throwing), so only treat it
        // as a lost connection when the socket is actually down.
        WinchCore.Log.Warn($"Archipelago socket error: {message}");
        WinchCore.Log.Debug($"Archipelago socket error details: {e}");

        var session = Session;
        if (session != null && !session.Socket.Connected)
        {
            MainThreadDispatcher.Enqueue(() => OnConnectionLost(session, message));
        }
    }

    private static void OnSocketClosed(string reason)
    {
        // Socket thread. Only raised for a server-side close here: Disconnect() unsubscribes first.
        var session = Session;
        if (session != null)
        {
            MainThreadDispatcher.Enqueue(() => OnConnectionLost(session, reason));
        }
    }

    private static void OnConnectionLost(ArchipelagoSession session, string reason)
    {
        // Ignore reports from a session that has already been replaced or torn down.
        if (Session != session)
        {
            return;
        }

        ArchipelagoCommandManager.HandleConnectionLost(reason);
    }

    public static void Disconnect()
    {
        var session = Session;
        Session = null;

        if (session == null)
        {
            return;
        }

        session.MessageLog.OnMessageReceived -= OnMessageReceived;
        session.Socket.ErrorReceived -= OnErrorReceived;
        session.Socket.SocketClosed -= OnSocketClosed;

        // Nothing to close if the socket already dropped or never opened.
        if (!session.Socket.Connected)
        {
            return;
        }

        try
        {
            session.Socket.DisconnectAsync().ContinueWith(
                t => WinchCore.Log.Debug($"Archipelago socket close failed: {t.Exception?.GetBaseException().Message}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }
        catch (Exception e)
        {
            WinchCore.Log.Debug($"Archipelago socket close failed: {e.Message}");
        }
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
}