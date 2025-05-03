using ArchipelagoDredge.Game.Managers;
using CommandTerminal;
using System;
using ArchipelagoDredge.Game.Helpers;
using Winch.Core;

namespace ArchipelagoDredge.Network
{
    public static class ArchipelagoCommandManager
    {
        public static ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        private static string _apHost;
        private static int _apPort;
        private static string _slotName;
        private static string _password;

        public static void TryConnect(string apHost, string apPort, string slotName, string password)
        {
            if (State == ConnectionState.Connecting || State == ConnectionState.Connected)
                return;

            _apHost = apHost;
            int.TryParse(apPort, out _apPort);
            _slotName = slotName;
            _password = password;

            State = ConnectionState.Connecting;
            TerminalCommandManager.LogMessage(TerminalLogType.Message, "Connecting to Archipelago...");

            try
            {
                ArchipelagoClient.Connect(
                    _apHost,
                    _apPort,
                    _slotName,
                    _password
                );

                if (ArchipelagoClient.Session != null &&
                    ArchipelagoClient.Session.Socket != null &&
                    ArchipelagoClient.Session.Socket.Connected)
                {
                    State = ConnectionState.Connected;
                    TerminalCommandManager.LogMessage(TerminalLogType.Message, "Connected to Archipelago!");
                    NotificationHelper.ShowNotificationWithColour(NotificationType.NONE, "Connected to Archipelago!", DredgeColorTypeEnum.POSITIVE);
                }
                else
                {
                    State = ConnectionState.Disconnected;
                }
            }
            catch (Exception e)
            {
                State = ConnectionState.Disconnected;
                TerminalCommandManager.LogMessage(TerminalLogType.Error, $"Connection failed.");
                NotificationHelper.ShowNotificationWithColour(NotificationType.NONE, "Connection failed.", DredgeColorTypeEnum.NEGATIVE);
                WinchCore.Log.Error($"Connection failed: {e}");
            }
        }

        public static void Disconnect()
        {
            if (State != ConnectionState.Connected)
                return;

            ArchipelagoClient.Disconnect();
            State = ConnectionState.Disconnected;
            TerminalCommandManager.LogMessage(TerminalLogType.Message, "Disconnected from Archipelago");
            NotificationHelper.ShowNotificationWithColour(NotificationType.NONE, "Disconnected from Archipelago", DredgeColorTypeEnum.NEUTRAL);
        }

        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Error
        }
    }
}