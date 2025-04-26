using ArchipelagoDredge.Network;
using System.Collections.Generic;

namespace ArchipelagoDredge.Game.Managers
{
    public static class LocationManager
    {
        private static Dictionary<string, long> locationMapping;

        public static void Initialize()
        {
            // Create mapping between in-game locations and AP location IDs
            locationMapping = new Dictionary<string, long>
            {
                {"Fish_Cod", 100001},
                {"Relic_Ancient", 100002},
                // etc.
            };
        }

        public static void CheckLocation(string gameLocation)
        {
            if (locationMapping.TryGetValue(gameLocation, out var locationId))
            {
                ArchipelagoClient.Session.Locations.CompleteLocationChecks(locationId);
            }
        }
    }
}