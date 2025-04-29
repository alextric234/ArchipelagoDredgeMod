using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDredge.Utils;
using System;
using ArchipelagoDredge.Game.Managers;

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

            Connected = true;
        }

        public static void SendEncyclopediaLocationCheck(SpatialItemInstance spatialItemInstance)
        {
            var apLocationName = ArchipelagoItemManager.ItemIdToNameCache[spatialItemInstance._itemData.id];
            var apLocationId = Session.Locations.GetLocationIdFromName("Dredge", apLocationName);
            Session.Locations.CompleteLocationChecksAsync(apLocationId);
        }

        public static void Disconnect()
        {
            Connected = false;
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