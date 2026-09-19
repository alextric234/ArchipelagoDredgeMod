using ArchipelagoDredge.Utils;
using System.Collections.Generic;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;
public static class PassageManager
{
    private const float ViolationWindowSeconds = 60f;
    public const string GaleCliffsPassageItemKey = "passage.gale_cliffs";
    public const string StellarBasinPassageItemKey = "passage.stellar_basin";
    public const string TwistedStrandPassageItemKey = "passage.twisted_strand";
    public const string DevilsSpinePassageItemKey = "passage.devils_spine";
    public const string OpenOceanPassageItemKey = "passage.open_ocean";
    public const string PaleReachPassageItemKey = "passage.pale_reach";

    private static ZoneEnum previousZone = ZoneEnum.NONE;
    private static Vector3 lastAllowedPosition;
    private static float nextZoneCheckTime;
    private static int violationCount;
    private static float nextWarningTime;
    private static Vector3 activeEjectionTarget;
    private static bool isEjecting;
    private static BannersUI bannersUi;

    private static readonly Dictionary<ZoneEnum, ViolationState> violationsByZone = new();

    public static float PushSpeed = 30f;

    public static void Update()
    {
        if (Time.time < nextZoneCheckTime)
        {
            return;
        }

        nextZoneCheckTime = Time.time + 0.25f;

        var currentZone =
            GameManager.Instance.Player.PlayerZoneDetector.GetCurrentZone();

        var playerPosition = GameManager.Instance.Player.transform.position;

        if (HasPassageFor(currentZone))
        {
            lastAllowedPosition = playerPosition;
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
        return zone switch
        {
            ZoneEnum.THE_MARROWS => true,
            ZoneEnum.NONE => true,

            ZoneEnum.GALE_CLIFFS =>
                ArchipelagoStateManager.HasVirtualItem(GaleCliffsPassageItemKey),

            ZoneEnum.STELLAR_BASIN =>
                ArchipelagoStateManager.HasVirtualItem(StellarBasinPassageItemKey),

            ZoneEnum.TWISTED_STRAND =>
                ArchipelagoStateManager.HasVirtualItem(TwistedStrandPassageItemKey),

            ZoneEnum.DEVILS_SPINE =>
                ArchipelagoStateManager.HasVirtualItem(DevilsSpinePassageItemKey),

            ZoneEnum.OPEN_OCEAN =>
                ArchipelagoStateManager.HasVirtualItem(OpenOceanPassageItemKey),

            ZoneEnum.PALE_REACH =>
                ArchipelagoStateManager.HasVirtualItem(PaleReachPassageItemKey),

            _ => true
        };
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

        if (state.Attempts <= 2)
        {
            ShowPassageWarning(zone);
            PushBoatToward();
            return;
        }

        //SummonBoundaryMonster(zone);
        PushBoatToward();
    }

    private static void PushBoatToward()
    {
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

        PassageDeniedBanner.TryShow(
            bannersUi,
            GetRequiredPassageName(lockedZone));
    }

    private static string GetRequiredPassageName(ZoneEnum zone)
    {
        return zone switch
        {
            ZoneEnum.GALE_CLIFFS => "The Windward Litany",
            ZoneEnum.STELLAR_BASIN => "The Astral Testament",
            ZoneEnum.TWISTED_STRAND => "The Mangrove Canticle",
            ZoneEnum.DEVILS_SPINE => "The Cinder Gospel",
            ZoneEnum.OPEN_OCEAN => "The Pelagic Psalm",
            ZoneEnum.PALE_REACH => "The Rimebound Chronicle",
            _ => "the required passage"
        };
    }
}

sealed class ViolationState
{
    public int Attempts;
    public float LastAttemptTime;
    public float NextWarningTime;
    public float NextMonsterTime;
}
