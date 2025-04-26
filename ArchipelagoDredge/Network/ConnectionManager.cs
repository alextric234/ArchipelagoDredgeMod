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

        public static void Initialize()
        {
            ConnectionConfig.Load();

            if (ConnectionConfig.Current.AutoConnect)
            {
                TryConnect();
            }
        }

        public static void TryConnect()
        {
            if (State == ConnectionState.Connecting || State == ConnectionState.Connected)
                return;

            State = ConnectionState.Connecting;
            ArchipelagoNotificationUi.ShowMessage("Connecting to Archipelago...");

            try
            {
                ArchipelagoClient.Connect(
                    ConnectionConfig.Current.Host,
                    ConnectionConfig.Current.SlotName,
                    ConnectionConfig.Current.Password
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