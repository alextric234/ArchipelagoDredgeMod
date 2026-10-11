using System;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using Winch.Core;
using Winch.Core.API;

namespace ArchipelagoDredge.Game.Services;
public static class LocationCheckService
{
    private const string IronRigCrateItemId = "crate";

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

    private static void OnPOIHarvested(HarvestPOI harvestPOI, SpatialItemInstance itemInstance)
    {
        try
        {
            if (itemInstance.ToItemData().itemSubtype == ItemSubtype.FISH)
                return;
            if (harvestPOI.IsDredgePOI &&
                harvestPOI.Harvestable.GetNextHarvestableItem().id == IronRigCrateItemId &&
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
        try
        {
            if (itemInstance?.ToItemData()?.itemSubtype != ItemSubtype.FISH)
                return;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error reading caught item {itemInstance?.id}: {e}");
            return;
        }

        Report(itemInstance.id);
    }

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
