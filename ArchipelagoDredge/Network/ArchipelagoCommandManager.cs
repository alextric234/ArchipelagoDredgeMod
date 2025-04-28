using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.UI;
using CommandTerminal;
using System;
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
            ArchipelagoNotificationUi.ShowMessage("Connecting to Archipelago...");

            try
            {
                ArchipelagoClient.Connect(
                    _apHost,
                    _apPort,
                    _slotName,
                    _password
                );

                State = ConnectionState.Connected;
                TerminalCommandManager.LogMessage(TerminalLogType.Message, "Connected to Archipelago!");
                ArchipelagoNotificationUi.ShowMessage("Connected to Archipelago!");
            }
            catch (Exception e)
            {
                State = ConnectionState.Disconnected;
                TerminalCommandManager.LogMessage(TerminalLogType.Error, $"Connection failed: {e.Message}");
                ArchipelagoNotificationUi.ShowMessage($"Connection failed: {e.Message}");
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
            ArchipelagoNotificationUi.ShowMessage("Disconnected from Archipelago");
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