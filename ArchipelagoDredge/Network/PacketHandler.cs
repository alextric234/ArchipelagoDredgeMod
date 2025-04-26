using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;

namespace ArchipelagoDredge.Network
{
    public static class PacketHandler
    {
        public static void RegisterHandlers()
        {
            var session = ArchipelagoClient.Session;

            session.Items.ItemReceived += OnItemReceived;
            session.Socket.PacketReceived += OnPacketReceived;
            // Add more handlers as needed
        }

        private static void OnItemReceived(ReceivedItemsHelper receivedItemsHelper)
        {
            // Handle received items
        }

        private static void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            // Handle other packet types
        }
    }
}