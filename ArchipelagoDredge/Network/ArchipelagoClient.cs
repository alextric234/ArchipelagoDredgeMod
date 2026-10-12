using System;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Tracker;
using ArchipelagoDredge.Network.Enums;
using ArchipelagoDredge.Utils;
using CommandTerminal;
using Winch.Core;
using Winch.Util;

namespace ArchipelagoDredge.Network;

public static class ArchipelagoClient
{
    /// <summary>
    ///     The AP game name, as used by the APWorld for items, locations and login.
    /// </summary>
    public const string GameName = "DREDGE";

    public static ArchipelagoSession Session { get; private set; }

    /// <summary>
    ///     The connection state machine. Only <see cref="ArchipelagoCommandManager" /> changes it.
    /// </summary>
    public static ConnectionState State { get; internal set; } = ConnectionState.Disconnected;

    /// <summary>
    ///     The seed's options: from slot data on login, or from the save's snapshot while offline
    ///     (see <see cref="ArchipelagoStateManager" />).
    /// </summary>
    public static SlotData SlotData { get; internal set; } = new SlotData();

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

        Disconnect(); 

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
            GameName,
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
        SlotData = new SlotData(
            deathLink: SlotDataParser.GetBool(slotData, "death_link"),
            includeIronRigDlc: SlotDataParser.GetBool(slotData, "include_iron_rig_dlc"),
            includePaleReachDlc: SlotDataParser.GetBool(slotData, "include_pale_reach_dlc"),
            addFishingLicenses: SlotDataParser.GetBool(slotData, "add_fishing_licenses"),
            addPassageItems: SlotDataParser.GetBool(slotData, "add_passage_items"),
            progressiveAberrationChanceItems: SlotDataParser.GetInt(slotData, "progressive_aberration_chance_items"),
            logicalNets: SlotDataParser.GetBool(slotData, "logical_nets"),
            requireBoatPerformance: SlotDataParser.GetBool(slotData, "require_boat_performance"));

        WinchCore.Log.Info($"Slot data: death_link={SlotData.DeathLink}, include_iron_rig_dlc={SlotData.IncludeIronRigDLC}, " +
                           $"include_pale_reach_dlc={SlotData.IncludePaleReachDLC}, add_fishing_licenses={SlotData.AddFishingLicenses}, " +
                           $"add_passage_items={SlotData.AddPassageItems}, " +
                           $"progressive_aberration_chance_items={SlotData.ProgressiveAberrationChanceItems}, " +
                           $"logical_nets={SlotData.LogicalNets}, require_boat_performance={SlotData.RequireBoatPerformance}");

        TrackerService.TrackHints(session);
    }

    private static void OnMessageReceived(LogMessage message)
    {
        MainThreadDispatcher.Enqueue(() => ShowMessage(message));
    }

    private static void ShowMessage(LogMessage message)
    {
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

    private static void ThrowIfSuperseded(ArchipelagoSession session)
    {
        if (Session != session)
        {
            throw new OperationCanceledException("Connection attempt was cancelled");
        }
    }

    private static void OnErrorReceived(Exception e, string message)
    {
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
        var session = Session;
        if (session != null)
        {
            MainThreadDispatcher.Enqueue(() => OnConnectionLost(session, reason));
        }
    }

    private static void OnConnectionLost(ArchipelagoSession session, string reason)
    {
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