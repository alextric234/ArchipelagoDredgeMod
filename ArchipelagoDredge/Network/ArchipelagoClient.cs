using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoDredge.Network.Models;
using System;
using Archipelago.MultiClient.Net.Helpers;
using ArchipelagoDredge.Game.Managers;
using UnityEngine.Networking;
using Winch.Core;
using Winch.Util;
using ConnectionConfig = ArchipelagoDredge.Network.Models.ConnectionConfig;

namespace ArchipelagoDredge.Network
{
    public static class ArchipelagoClient
    {
        public static ArchipelagoSession Session { get; private set; }
        public static bool GameReady { get; set; }

        public static void Connect(string apHost, int apPort, string slotName, string password)
        {
            Disconnect(); // Ensure clean state

            Session = ArchipelagoSessionFactory.CreateSession(apHost, apPort);

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