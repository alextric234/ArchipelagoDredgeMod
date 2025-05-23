﻿using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDredge.Utils;
using System;
using System.Linq;
using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoDredge.Game.Helpers;
using CommandTerminal;
using Winch.Core;
using ArchipelagoDredge.Game.Managers;
using Yarn.Compiler;

namespace ArchipelagoDredge.Network
{
    public static class ArchipelagoClient
    {
        public static ArchipelagoSession Session { get; private set; }
        public static bool GameReady { get; set; }

        public static void Connect(string apHost, int apPort, string slotName, string password)
        {
            if (!GameReady)
            {
                TerminalCommandManager.LogMessage(TerminalLogType.Error, "Please load a save before connecting to a multiworld");
                return;
            }
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

            ArchipelagoItemManager.RestockShops();
        }

        private static void OnMessageReceived(LogMessage message)
        {
            TerminalCommandManager.LogMessage(TerminalLogType.Message, message.ToString());
            NotificationHelper.TryToSendNotification(message);
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