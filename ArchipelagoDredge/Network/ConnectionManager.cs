using System;
using ArchipelagoDredge.Network.Models;
using ArchipelagoDredge.UI;
using UnityEngine.Networking;
using Winch.Core;
using ConnectionConfig = ArchipelagoDredge.Network.Models.ConnectionConfig;

namespace ArchipelagoDredge.Network
{
    public static class ConnectionManager
    {
        public static ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        private static string _apHost;
        private static int _apPort;
        private static string _slotName;
        private static string _password;

        public static void TryConnect(string apHost, string apPort, string slotName, string password)
        {
            WinchCore.Log.Info("Calling tryconnect");
            if (State == ConnectionState.Connecting || State == ConnectionState.Connected)
                return;

            _apHost = apHost;
            int.TryParse(apPort, out _apPort);
            _slotName = slotName;
            _password = password;

            State = ConnectionState.Connecting;
            WinchCore.Log.Info("Connecting to Archipelago....");
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
                ArchipelagoNotificationUi.ShowMessage("Connected to Archipelago!");
            }
            catch (Exception e)
            {
                State = ConnectionState.Disconnected;
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