using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDredge.Utils;
using System;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using ArchipelagoDredge.Ui;
using CommandTerminal;
using Winch.Core;
using ArchipelagoDredge.Game.Managers;

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

            Session.MessageLog.OnMessageReceived += OnMessageReceived;

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
        }

        private static void OnMessageReceived(LogMessage message)
        {
            try
            {
                ArchipelagoNotificationUi.ShowMessage(message.ToString());
                TerminalCommandManager.LogMessage(TerminalLogType.Message, message.ToString());
            }
            catch (Exception e)
            {
                WinchCore.Log.Error(e);
            }
        }

        public static void Disconnect()
        {
            Session?.Socket?.DisconnectAsync();
            Session = null;
        }

        public static bool HasItemsToProcess()
        {
            var latestItemIndex = Session.Items.AllItemsReceived.Count - 1;
            return ArchipelagoStateManager.StateData.LastProcessedIndex < latestItemIndex;
        }
    }
}