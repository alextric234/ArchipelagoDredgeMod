using ArchipelagoDredge.Network;

namespace ArchipelagoDredge.Game.Managers
{
    public static class ArchipelagoLocationManager
    {
        public static void SendLocationCheck(string category, string itemId)
        {
            var apLocationName = ArchipelagoItemManager.ItemIdToNameCache[itemId];
            var apLocationId = ArchipelagoClient.Session.Locations.GetLocationIdFromName("DREDGE", $"{category} - {apLocationName}");
            ArchipelagoClient.Session.Locations.CompleteLocationChecksAsync(apLocationId);
        }

        public static bool HasThisLocationBeenChecked(string category, string itemId)
        {
            var apLocationName = ArchipelagoItemManager.ItemIdToNameCache[itemId];
            var apLocations = ArchipelagoClient.Session.Locations.AllLocationsChecked;
            var apLocationId = ArchipelagoClient.Session.Locations.GetLocationIdFromName("DREDGE", $"{category} - {apLocationName}");
            
            if (apLocationId == -1)
            {
                return true;
            }
            
            
            return apLocations.Contains(apLocationId);
        }
    }
}