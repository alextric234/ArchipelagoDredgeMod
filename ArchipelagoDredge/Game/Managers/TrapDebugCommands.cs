#if DEBUG
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using CommandTerminal;
using UnityEngine;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Utils;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;

/// <summary>
///     Debug-only "ap trap" commands for trying out trap effects before they become AP items.
/// </summary>
public static class TrapDebugCommands
{
    private const string Usage =
        "Usage: ap trap <events | event <name> | spawn <damaging|scary> [count] | queue [clear] | item <trap item name> | pending | " +
        "money [percent] | teleport | sanity [amount] | damage [count] | engine | night | rot [count] | slow [factor] [seconds] | " +
        "lights [seconds] | scramble | reverse [seconds]>";

    public static void Run(CommandArg[] args, Action<TerminalLogType, string> print)
    {
        if (args.Length == 0 || GameManager.Instance?.Player == null)
        {
            print(TerminalLogType.Error, args.Length == 0 ? Usage : "Load a save first.");
            return;
        }

        try
        {
            var rest = args.Skip(1).Select(arg => arg.ToString()).ToArray();
            print(TerminalLogType.Message, args[0].ToString().ToLower() switch
            {
                "events" => ListEvents(),
                "event" => StartEvent(string.Join(" ", rest)),
                "spawn" => QueueEventTrap(rest),
                "queue" => QueueStatus(rest),
                "item" => ReceiveTrapItem(string.Join(" ", rest)),
                "pending" => "Pending traps: " + string.Join(", ", ArchipelagoStateManager.PendingTraps),
                "money" => TrapEffects.LoseMoney((decimal) ParseFloat(rest, 0, (float) TrapEffects.MoneyLossPercent)),
                "teleport" => TrapEffects.CanTeleport() ? TrapEffects.Teleport() : "Can't teleport now (docked or on cooldown).",
                "sanity" => TrapEffects.Panic(ParseFloat(rest, 0, TrapEffects.PanicAmount)),
                "damage" => TrapEffects.DamageHull((int) ParseFloat(rest, 0, 1)),
                "engine" => TrapEffects.DamageEngine(),
                "night" => TrapEffects.SkipToNight(),
                "rot" => TrapEffects.RotFish((int) ParseFloat(rest, 0, 1)),
                "slow" => TrapEffects.Slow(ParseFloat(rest, 0, TrapEffects.SlowFactor), ParseFloat(rest, 1, TrapEffects.TimedTrapSeconds)),
                "lights" => TrapEffects.LightsOut(ParseFloat(rest, 0, TrapEffects.TimedTrapSeconds)),
                "scramble" => TrapEffects.CanScrambleCargo() ? TrapEffects.ScrambleCargo() : "Can't scramble while holding an item.",
                "reverse" => TrapEffects.ReverseControls(ParseFloat(rest, 0, TrapEffects.TimedTrapSeconds)),
                _ => Usage
            });
        }
        catch (Exception e)
        {
            print(TerminalLogType.Error, $"Trap failed: {e}");
        }
    }

    private static float ParseFloat(string[] args, int index, float fallback)
    {
        return args.Length > index && float.TryParse(args[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private static string ListEvents()
    {
        var manager = GameManager.Instance.WorldEventManager;
        var lines = GameManager.Instance.DataLoader.allWorldEvents.Select(e =>
        {
            var kind = EventTrapQueue.EventsByKind.FirstOrDefault(pair => pair.Value.Contains(e.name)).Key;
            var group = EventTrapQueue.EventsByKind.Values.Any(names => names.Contains(e.name)) ? kind.ToString() : "not a trap";
            return $"{e.name} [{group}] ({e.eventType}, hit sources: {HitSourceCount(e)}, sanity {e.minSanity}-{e.maxSanity}, phase {e.minWorldPhase}+, " +
                   $"natural spawn here: {manager.TestWorldEvent(e, exitEarly: true)}, trap spawn here: {EventTrapQueue.CanSpawnHere(e)})";
        });
        var list = "World events:\n" + string.Join("\n", lines);
        WinchCore.Log.Info(list);
        return list;
    }

    // Monsters hurt the boat through colliders tagged as monsters or a VariablePlayerDamager. Waterspouts and phantom
    // sharks hit the boat from their own code, so a count of 0 doesn't rule damage out.
    private static int HitSourceCount(WorldEventData e)
    {
        if (e.prefab == null)
        {
            return 0;
        }

        var playerCollider = GameManager.Instance.Player.GetComponentInChildren<PlayerCollider>();
        var monsterTag = playerCollider != null ? HarmonyLib.Traverse.Create(playerCollider).Field<string>("monsterTag").Value : null;
        var monsterColliders = string.IsNullOrEmpty(monsterTag)
            ? 0
            : e.prefab.GetComponentsInChildren<Collider>(true).Count(c => c.CompareTag(monsterTag));
        return monsterColliders + e.prefab.GetComponentsInChildren<VariablePlayerDamager>(true).Length;
    }

    private static string QueueEventTrap(string[] args)
    {
        if (args.Length == 0 || !Enum.TryParse<EventTrapKind>(args[0], true, out var kind))
        {
            return "Usage: ap trap spawn <damaging|scary> [count]";
        }

        var count = Math.Max(1, (int) ParseFloat(args, 1, 1));
        for (var i = 0; i < count; i++)
        {
            EventTrapQueue.Enqueue(kind);
        }

        var spawnable = EventTrapQueue.SpawnableEvents(kind).Select(e => e.name).ToList();
        return $"Queued {count} {kind} trap(s); {EventTrapQueue.PendingCount} pending. " +
               $"Spawnable here now: {(spawnable.Count == 0 ? "none" : string.Join(", ", spawnable))}";
    }

    private static string QueueStatus(string[] args)
    {
        if (args.FirstOrDefault()?.ToLower() == "clear")
        {
            EventTrapQueue.Clear();
            return "Cleared the event trap queue.";
        }

        var current = GameManager.Instance.WorldEventManager.CurrentEvent;
        return $"{EventTrapQueue.PendingCount} event trap(s) pending; current event: {(current != null ? current.worldEventData.name : "none")}.";
    }

    private static string StartEvent(string name)
    {
        var data = GameManager.Instance.DataLoader.allWorldEvents
            .FirstOrDefault(e => string.Equals(e.name, name, StringComparison.OrdinalIgnoreCase));
        if (data == null)
        {
            return $"No world event named '{name}'. Try 'ap trap events'.";
        }

        var manager = GameManager.Instance.WorldEventManager;
        if (manager.CurrentEvent != null)
        {
            return $"Another event is running ({manager.CurrentEvent.worldEventData.name}); wait for it to finish.";
        }

        var wouldSpawn = manager.TestWorldEvent(data, exitEarly: true);
        manager.DoEvent(data);
        return $"Started {data.name}" + (wouldSpawn ? "." : " (its spawn checks failed here, so it may look wrong).");
    }

    private static string ReceiveTrapItem(string name)
    {
        if (!ItemNames.TryNameToItem(name, out var item) || !TrapManager.IsTrap(item))
        {
            return $"No trap item named '{name}'.";
        }

        TrapManager.Receive(item);
        return $"Received {name}; it fires when it can.";
    }
}
#endif
