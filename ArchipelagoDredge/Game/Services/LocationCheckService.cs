using System;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using Winch.Core;
using Winch.Core.API;

namespace ArchipelagoDredge.Game.Services;

/// <summary>
///     Reports AP location checks from Winch's catch and harvest events.
/// </summary>
public static class LocationCheckService
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        DredgeEvent.OnPOIHarvested += OnPOIHarvested;
        DredgeEvent.OnFishCaught += OnFishCaught;
        _initialized = true;
    }

    // Event handlers must not throw: a throwing subscriber stops Winch from invoking the rest.
    private static void OnPOIHarvested(HarvestPOI harvestPOI, SpatialItemInstance itemInstance)
    {
        try
        {
            if (itemInstance.ToItemData().itemSubtype == ItemSubtype.FISH)
                return;
            if (harvestPOI.IsDredgePOI &&
                harvestPOI.Harvestable.GetNextHarvestableItem().id == "crate" &&
                !ArchipelagoClient.SlotData.IncludeIronRigDLC)
            {
                WinchCore.Log.Debug($"It's a crate");
                return;
            }
            LocationHelper.ReportLocationCheck(harvestPOI.Harvestable.GetId());
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error sending location check for {harvestPOI?.Harvestable?.GetId()}/{itemInstance?.id}: {e}");
        }
    }

    private static void OnFishCaught(SpatialItemInstance itemInstance)
    {
        Report(itemInstance?.id);
    }

    /// <summary>
    ///     Reports a location check by its DREDGE id. Never throws.
    /// </summary>
    public static void Report(string locationId)
    {
        try
        {
            LocationHelper.ReportLocationCheck(locationId);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error sending location check for {locationId}: {e}");
        }
    }
}
