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

        public static bool HasThisLocationBeenChecked(string category, string itemId)
        {
            var apLocationName = ArchipelagoItemManager.ItemIdToNameCache[itemId];
            var apLocationId = ArchipelagoClient.Session.Locations.GetLocationIdFromName("Dredge", $"{category} - {apLocationName}");
            var apLocations = ArchipelagoClient.Session.Locations.AllLocationsChecked;
            
            return apLocations.Contains(apLocationId);
        }
    }
}