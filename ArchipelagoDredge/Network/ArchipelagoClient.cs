using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoDredge.Network.Models;
using System;
using UnityEngine.Networking;
using ConnectionConfig = ArchipelagoDredge.Network.Models.ConnectionConfig;

namespace ArchipelagoDredge.Network
{
    public static class ArchipelagoClient
    {
        public static ArchipelagoSession Session { get; private set; }

        public static void Connect(string host, string slotName, string password)
        {
            Disconnect(); // Ensure clean state

            Session = ArchipelagoSessionFactory.CreateSession(host);
            var loginResult = Session.TryConnectAndLogin(
                "Dredge",
                slotName,
                ItemsHandlingFlags.AllItems,
                password: password
            );

            if (!loginResult.Successful)
            {
                throw new Exception(loginResult.ToString());
            }

            // Register handlers
            PacketHandler.RegisterHandlers();

            // Apply game settings from config
            ApplyGameSettings();
        }

        public static void Disconnect()
        {
            Session?.Socket?.DisconnectAsync();
            Session = null;
        }

        private static void ApplyGameSettings()
        {
            var config = ConnectionConfig.Current;
        }
    }
}