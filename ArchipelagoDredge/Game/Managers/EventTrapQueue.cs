using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;

public enum EventTrapKind
{
    Damaging,
    Scary
}

/// <summary>
///     Starts world events as traps. Traps wait while another event is running or the player is docked, then pick a
///     random event of their kind that can spawn where the boat is. Unlike natural events, traps ignore the sanity
///     band, time of day, repeat delay and world phase, so a damaging trap can come at full sanity early in a run.
/// </summary>
public static class EventTrapQueue
{
    public static readonly IReadOnlyDictionary<EventTrapKind, string[]> EventsByKind =
        new Dictionary<EventTrapKind, string[]>
        {
            [EventTrapKind.Damaging] =
            [
                "PhantomShark", "MonsterRayAttack1", "MonsterRayAttack2", "TentacleAttack", "Vines", "Waterspout",
                "Waterspout_Corrupt", "LeviathanCircle"
            ],
            // Ravens can steal a small fish, but nothing here damages the boat.
            [EventTrapKind.Scary] =
            [
                "Eyes", "FlickerLights", "FogGhost", "GhostBoat_Pirate", "GhostBoat_Player1", "GhostBoat_Player2",
                "GhostBoat_Player3", "GhostWind", "Splash", "MonsterRayFollow", "BlueWhale", "DolphinPod", "OrcaPod",
                "WhaleSighting", "SpermWhale", "LeviathanCall", "Leviathan", "GreatWhite", "Ravens", "Parasite"
            ]
        };

    private const float RetryIntervalSec = 2f;

    private static readonly Queue<EventTrapKind> Pending = new();
    private static float nextAttemptTime;

    // An event that can't find room to spawn destroys itself during DoEvent; checked a frame later.
    private static WorldEventData startedEvent;
    private static EventTrapKind startedKind;
    private static int startedFrame;
    private static readonly HashSet<string> FailedThisTrap = [];

    private static readonly AccessTools.FieldRef<WorldEventManager, GameMode> CurrentGameMode =
        AccessTools.FieldRefAccess<WorldEventManager, GameMode>("currentGameMode");

    private static readonly AccessTools.FieldRef<WorldEventManager, bool> IsBanishActive =
        AccessTools.FieldRefAccess<WorldEventManager, bool>("isBanishActive");

    private static readonly Func<WorldEventManager, List<Vector3>, float, float, bool, bool> CheckDepthPath =
        AccessTools.MethodDelegate<Func<WorldEventManager, List<Vector3>, float, float, bool, bool>>(
            AccessTools.Method(typeof(WorldEventManager), "CheckDepthPath"));

    private static readonly Func<WorldEventManager, Vector3, Transform, float, bool, bool> CheckDepthRelativePoint =
        AccessTools.MethodDelegate<Func<WorldEventManager, Vector3, Transform, float, bool, bool>>(
            AccessTools.Method(typeof(WorldEventManager), "CheckDepthRelativePoint"));

    public static int PendingCount => Pending.Count;

    public static void Enqueue(EventTrapKind kind)
    {
        Pending.Enqueue(kind);
    }

    public static void Clear()
    {
        Pending.Clear();
        startedEvent = null;
        FailedThisTrap.Clear();
    }

    public static void Update()
    {
        try
        {
            if (startedEvent != null)
            {
                CheckStartedEvent();
                return;
            }

            if (Pending.Count == 0 || Time.time < nextAttemptTime || !CanStartNow())
            {
                return;
            }

            nextAttemptTime = Time.time + RetryIntervalSec;
            var kind = Pending.Peek();
            var candidates = SpawnableEvents(kind).Where(e => !FailedThisTrap.Contains(e.name)).ToList();
            if (candidates.Count == 0)
            {
                FailedThisTrap.Clear();
                return;
            }

            var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            WinchCore.Log.Debug($"Event trap ({kind}): starting {chosen.name}");
            startedEvent = chosen;
            startedKind = kind;
            startedFrame = Time.frameCount;
            GameManager.Instance.WorldEventManager.DoEvent(chosen);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in EventTrapQueue.Update: {e}");
            startedEvent = null;
        }
    }

    private static void CheckStartedEvent()
    {
        if (Time.frameCount <= startedFrame)
        {
            return;
        }

        var current = GameManager.Instance.WorldEventManager.CurrentEvent;
        if (current != null && current.worldEventData == startedEvent)
        {
            Pending.Dequeue();
            FailedThisTrap.Clear();
        }
        else
        {
            WinchCore.Log.Debug($"Event trap ({startedKind}): {startedEvent.name} didn't spawn here; trying another");
            FailedThisTrap.Add(startedEvent.name);
            nextAttemptTime = 0f;
        }

        startedEvent = null;
    }

    private static bool CanStartNow()
    {
        var gm = GameManager.Instance;
        return gm != null && gm.IsPlaying && !gm.IsPaused && gm.Player != null && gm.Player.IsAlive &&
               !gm.Player.IsDocked && gm.WorldEventManager != null && gm.WorldEventManager.CurrentEvent == null &&
               !gm.Time.IsTimePassingForcefully();
    }

    public static IEnumerable<WorldEventData> SpawnableEvents(EventTrapKind kind)
    {
        var names = EventsByKind[kind];
        return GameManager.Instance.DataLoader.allWorldEvents.Where(e => names.Contains(e.name) && CanSpawnHere(e));
    }

    /// <summary>
    ///     <see cref="WorldEventManager.TestWorldEvent" /> without the sanity, time of day, repeat delay and world phase
    ///     checks. World phase only paces the story; most damaging events would otherwise wait for the late game.
    /// </summary>
    public static bool CanSpawnHere(WorldEventData e)
    {
        var manager = GameManager.Instance.WorldEventManager;
        var player = GameManager.Instance.Player.transform;

        if (CurrentGameMode(manager) == GameMode.PASSIVE && !e.allowInPassiveMode)
        {
            return false;
        }

        if (IsBanishActive(manager) && e.dispelByBanish)
        {
            return false;
        }

        if (e.doSafeZoneHitCheck)
        {
            var offset = player.right * e.safeZoneHitCheckOffset.x + player.up * e.safeZoneHitCheckOffset.y +
                         player.forward * e.safeZoneHitCheckOffset.z;
            if (manager.DoesHitSafeZone(player.position) || manager.DoesHitSafeZone(player.position + offset))
            {
                return false;
            }
        }

        if (!e.itemInventoryConditions.All(condition => condition.Evaluate()))
        {
            return false;
        }

        if (e.hasMinDepth)
        {
            var deepEnough = e.isPath
                ? CheckDepthPath(manager, e.depthTestPath, e.depthPathNumChecks, e.minDepth, e.forbidOoze)
                : e.depthTestPath.All(p => CheckDepthRelativePoint(manager, p, player, e.minDepth, e.forbidOoze));
            if (!deepEnough)
            {
                return false;
            }
        }

        if (e.forbiddenZones != ZoneEnum.NONE)
        {
            var testPoint = player.position + player.right * e.zoneTestOffset.x + player.up * e.zoneTestOffset.y +
                            player.forward * e.zoneTestOffset.z;
            if (e.forbiddenZones.HasFlag(PlayerZoneDetector.GetZoneForPoint(testPoint)))
            {
                return false;
            }
        }

        return true;
    }
}
