using ArchipelagoDredge.Network;
using System.Collections.Generic;

namespace ArchipelagoDredge.Game.Managers
{
    public static class LocationManager
    {
        private static Dictionary<string, long> locationMapping;

        public static void Initialize()
        {

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