using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;

public static class ArchipelagoLocationManager
{
    public static void SendLocationCheck(string itemId)
    {
        if (LocationNames.TryParseLocation(itemId, out var locationName))
        {
            var apLocationId = LocationNames.locationToArchipelagoId[locationName];
            ArchipelagoClient.Session.Locations.CompleteLocationChecksAsync(apLocationId);
        }
        else
        {
            WinchCore.Log.Error($"Could not find location: {itemId}");
        }
    }

    public static bool HasThisLocationBeenChecked(string category, string itemId)
    {
        if (LocationNames.TryParseLocation(itemId, out var locationName))
        {
            var apLocations = ArchipelagoClient.Session.Locations.AllLocationsChecked;
            var apLocationId = LocationNames.locationToArchipelagoId[locationName];
            return apLocations.Contains(apLocationId);
        }

        WinchCore.Log.Error($"Could not find location: {itemId}");

        return false;
    }
}