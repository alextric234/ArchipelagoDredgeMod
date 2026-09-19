using ArchipelagoDredge.Utils;
using System.Collections.Generic;
using ArchipelagoDredge.Network;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;
public static class FishingLicenseManager
{
    public const string GaleCliffsFishingLicenseKey = "fishing_license.gale_cliffs";
    public const string StellarBasinFishingLicenseKey = "fishing_license.stellar_basin";
    public const string TwistedStrandFishingLicenseKey = "fishing_license.twisted_strand";
    public const string DevilsSpineFishingLicenseKey = "fishing_license.devils_spine";
    public const string OpenOceanFishingLicenseKey = "fishing_license.open_ocean";
    public const string PaleReachFishingLicenseKey = "fishing_license.pale_reach";

    private static readonly IReadOnlyDictionary<ZoneEnum, FishingLicenseDefinition>
        FishingLicenses =
            new Dictionary<ZoneEnum, FishingLicenseDefinition>
            {
                [ZoneEnum.GALE_CLIFFS] = new()
                {
                    StateKey = GaleCliffsFishingLicenseKey,
                    MissingMessageKey = "alextric234.archipelagodredge.poi-label.missing-license.gale-cliffs"
                },
                [ZoneEnum.STELLAR_BASIN] = new()
                {
                    StateKey = StellarBasinFishingLicenseKey,
                    MissingMessageKey = "alextric234.archipelagodredge.poi-label.missing-license.stellar-basin"
                },
                [ZoneEnum.TWISTED_STRAND] = new()
                {
                    StateKey = TwistedStrandFishingLicenseKey,
                    MissingMessageKey = "alextric234.archipelagodredge.poi-label.missing-license.twisted-strand"
                },
                [ZoneEnum.DEVILS_SPINE] = new()
                {
                    StateKey = DevilsSpineFishingLicenseKey,
                    MissingMessageKey = "alextric234.archipelagodredge.poi-label.missing-license.devils-spine"
                },
                [ZoneEnum.OPEN_OCEAN] = new()
                {
                    StateKey = OpenOceanFishingLicenseKey,
                    MissingMessageKey = "alextric234.archipelagodredge.poi-label.missing-license.open-ocean"
                },
                [ZoneEnum.PALE_REACH] = new()
                {
                    StateKey = PaleReachFishingLicenseKey,
                    MissingMessageKey = "alextric234.archipelagodredge.poi-label.missing-license.pale-reach"
                }
            };

    public static bool TryGetMissingFishingLicense(
        HarvestableItemData item,
        out FishingLicenseDefinition missingLicense)
    {
        missingLicense = null;

        if ((UnityEngine.Object)item == null ||
            item.itemSubtype != ItemSubtype.FISH ||
            !FishingLicenses.TryGetValue(item.zonesFoundIn, out var license))
        {
            return false;
        }

        if (ArchipelagoStateManager.HasVirtualItem(license.StateKey))
        {
            return false;
        }

        missingLicense = license;
        return true;
    }

    public static bool HasLicenseForZone(ZoneEnum zone)
    {
        if (!ArchipelagoClient.SlotData.AddFishingLicenses)
        {
            return true;
        }

        return zone switch
        {
            ZoneEnum.THE_MARROWS => true,
            ZoneEnum.GALE_CLIFFS =>
                ArchipelagoStateManager.HasVirtualItem("fishing_license.gale_cliffs"),
            ZoneEnum.STELLAR_BASIN =>
                ArchipelagoStateManager.HasVirtualItem("fishing_license.stellar_basin"),
            ZoneEnum.TWISTED_STRAND =>
                ArchipelagoStateManager.HasVirtualItem("fishing_license.twisted_strand"),
            ZoneEnum.DEVILS_SPINE =>
                ArchipelagoStateManager.HasVirtualItem("fishing_license.devils_spine"),
            ZoneEnum.OPEN_OCEAN =>
                ArchipelagoStateManager.HasVirtualItem("fishing_license.open_ocean"),
            ZoneEnum.PALE_REACH =>
                ArchipelagoStateManager.HasVirtualItem("fishing_license.pale_reach"),
            ZoneEnum.NONE => true,

            _ => LogAndAllowUnknownZone(zone)
        };
    }

    private static bool LogAndAllowUnknownZone(ZoneEnum zone)
    {
        WinchCore.Log.Warn($"No fishing-license rule configured for zone: {zone}");
        return true;
    }
}
public sealed class FishingLicenseDefinition
{
    public string StateKey { get; set; }
    public string MissingMessageKey { get; set; }
}
