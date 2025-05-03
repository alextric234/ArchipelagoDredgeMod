using ArchipelagoDredge.Network;

namespace ArchipelagoDredge.Game.Managers
{
    public static class ArchipelagoLocationManager
    {
        public static void SendLocationCheck(string category, string itemId)
        {
            var apLocationName = ArchipelagoItemManager.ItemIdToNameCache[itemId];
            var apLocationId = ArchipelagoClient.Session.Locations.GetLocationIdFromName("Dredge", $"{category} - {apLocationName}");
            ArchipelagoClient.Session.Locations.CompleteLocationChecksAsync(apLocationId);
        }
    }
}