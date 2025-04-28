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
using ArchipelagoDredge.Utils;

namespace ArchipelagoDredge.Network
{
    public static class ArchipelagoClient
    {
        public static ArchipelagoSession Session { get; private set; }
        public static bool GameReady { get; set; }
        public static bool Connected { get; set; }

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
            Connected = true;
        }

        public static void Disconnect()
        {
            Connected = false;
            Session?.Socket?.DisconnectAsync();
            Session = null;
        }

        private static void ApplyGameSettings()
        {
            var config = ConnectionConfig.Current;
        }

        public static bool HasItemsToProcess()
        {
            var latestItemIndex = Session.Items.AllItemsReceived.Count - 1;
            return ArchipelagoStateManager.StateData.LastProcessedIndex < latestItemIndex;
        }
    }
}