using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Game.Ui;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using UnityEngine;

namespace ArchipelagoDredge.Game.Managers;
public static class PassageManager
{
    private const float ViolationWindowSeconds = 60f;
    private const int WarningsPerWindow = 2;
    private const float ZoneCheckIntervalSeconds = 0.25f;
    public const float PushSpeed = 30f;
    private const string UnknownPassageName = "the required passage";

    public const string GaleCliffsPassageItemKey = "passage.gale_cliffs";
    public const string StellarBasinPassageItemKey = "passage.stellar_basin";
    public const string TwistedStrandPassageItemKey = "passage.twisted_strand";
    public const string DevilsSpinePassageItemKey = "passage.devils_spine";
    public const string OpenOceanPassageItemKey = "passage.open_ocean";
    public const string PaleReachPassageItemKey = "passage.pale_reach";

    private static readonly IReadOnlyDictionary<ZoneEnum, PassageDefinition> Passages =
        new Dictionary<ZoneEnum, PassageDefinition>
        {
            [ZoneEnum.GALE_CLIFFS] = new(GaleCliffsPassageItemKey, "The Windward Litany", "Gale Cliffs"),
            [ZoneEnum.STELLAR_BASIN] = new(StellarBasinPassageItemKey, "The Astral Testament", "Stellar Basin"),
            [ZoneEnum.TWISTED_STRAND] = new(TwistedStrandPassageItemKey, "The Mangrove Canticle", "Twisted Strand"),
            [ZoneEnum.DEVILS_SPINE] = new(DevilsSpinePassageItemKey, "The Cinder Gospel", "Devil's Spine"),
            [ZoneEnum.OPEN_OCEAN] = new(OpenOceanPassageItemKey, "The Pelagic Psalm", "Open Ocean"),
            [ZoneEnum.PALE_REACH] = new(PaleReachPassageItemKey, "The Rimebound Chronicle", "Pale Reach")
        };

    private static ZoneEnum previousZone = ZoneEnum.NONE;
    private static Vector3 lastAllowedPosition;
    private static bool hasAllowedPosition;
    private static float nextZoneCheckTime;
    private static Vector3 activeEjectionTarget;
    private static bool isEjecting;
    private static BannersUI bannersUi;

    private static readonly Dictionary<ZoneEnum, ViolationState> violationsByZone = new();

    public static void Reset()
    {
        previousZone = ZoneEnum.NONE;
        lastAllowedPosition = Vector3.zero;
        hasAllowedPosition = false;
        nextZoneCheckTime = 0f;
        activeEjectionTarget = Vector3.zero;
        isEjecting = false;
        bannersUi = null;
        violationsByZone.Clear();
    }

    public static void Update()
    {
        if (Time.time < nextZoneCheckTime)
        {
            return;
        }

        nextZoneCheckTime = Time.time + ZoneCheckIntervalSeconds;

        // GetCurrentZone() reports Open Ocean until the zone triggers first fire after a load, so ask the zone
        // colliders directly.
        var playerPosition = GameManager.Instance.Player.transform.position;
        var currentZone = PlayerZoneDetector.GetZoneForPoint(playerPosition);

        if (HasPassageFor(currentZone))
        {
            lastAllowedPosition = playerPosition;
            hasAllowedPosition = true;
            previousZone = currentZone;
            isEjecting = false;
            return;
        }

        if (currentZone != previousZone)
        {
            RegisterLockedZoneAttempt(currentZone);
        }

        previousZone = currentZone;
    }

    private static bool HasPassageFor(ZoneEnum zone)
    {
        if (zone == ZoneEnum.PALE_REACH && !ArchipelagoClient.SlotData.IncludePaleReachDLC)
        {
            return true;
        }

        return !Passages.TryGetValue(zone, out var passage) ||
               ArchipelagoStateManager.HasVirtualItem(passage.StateKey);
    }

    private static void RegisterLockedZoneAttempt(ZoneEnum zone)
    {
        if (!violationsByZone.TryGetValue(zone, out var state))
        {
            state = new ViolationState();
            violationsByZone[zone] = state;
        }

        if (Time.time - state.LastAttemptTime > ViolationWindowSeconds)
        {
            state.Attempts = 0;
        }

        state.Attempts++;
        state.LastAttemptTime = Time.time;

        if (state.Attempts <= WarningsPerWindow)
        {
            ShowPassageWarning(zone);
            PushBoatToward();
            return;
        }

        PushBoatToward();
    }

    private static void PushBoatToward()
    {
        if (!hasAllowedPosition)
        {
            return;
        }

        activeEjectionTarget = lastAllowedPosition;
        isEjecting = true;
    }

    public static bool TryGetActiveEjectionTarget(out Vector3 target)
    {
        target = activeEjectionTarget;
        return isEjecting;
    }

    private static void ShowPassageWarning(ZoneEnum lockedZone)
    {
        bannersUi = Object.FindObjectOfType<BannersUI>();

        PassageBanner.TryShow(
            bannersUi,
            "THE CURRENT REJECTS YOU",
            BuildPassageWarningSubtitle(lockedZone),
            DredgeColorTypeEnum.WARNING);
    }

    private static string BuildPassageWarningSubtitle(ZoneEnum lockedZone)
    {
        var passageName = GetRequiredPassageName(lockedZone);
        return $"Requires: {passageName}";
    }

    public static void ShowPassageUnlocked(string passageItemName)
    {
        bannersUi = Object.FindObjectOfType<BannersUI>();
        var zoneName = GetPassageZoneName(passageItemName);


        PassageBanner.TryShow(
            bannersUi,
            $"{zoneName} PASSAGE UNLOCKED",
            $"You can now travel through {zoneName} unimpeded",
            DredgeColorTypeEnum.POSITIVE);
    }

    private static string GetPassageZoneName(string passageItemName)
    {
        return Passages.Values.FirstOrDefault(p => p.ItemName == passageItemName)?.ZoneName ?? UnknownPassageName;
    }

    private static string GetRequiredPassageName(ZoneEnum zone)
    {
        return Passages.TryGetValue(zone, out var passage) ? passage.ItemName : UnknownPassageName;
    }

    private sealed class PassageDefinition
    {
        public PassageDefinition(string stateKey, string itemName, string zoneName)
        {
            StateKey = stateKey;
            ItemName = itemName;
            ZoneName = zoneName;
        }

        public string StateKey { get; }
        public string ItemName { get; }
        public string ZoneName { get; }
    }
}

sealed class ViolationState
{
    public int Attempts;
    public float LastAttemptTime;
}
