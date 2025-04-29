using ArchipelagoDredge.Network;

namespace ArchipelagoDredge.Game.Managers
{
    public static class ArchipelagoLocationManager
    {
        public static void SendEncyclopediaLocationCheck(SpatialItemInstance spatialItemInstance)
        {
            var apLocationName = ArchipelagoItemManager.ItemIdToNameCache[spatialItemInstance._itemData.id];
            var apLocationId = ArchipelagoClient.Session.Locations.GetLocationIdFromName("Dredge", apLocationName);
            ArchipelagoClient.Session.Locations.CompleteLocationChecksAsync(apLocationId);
        }
    }
}