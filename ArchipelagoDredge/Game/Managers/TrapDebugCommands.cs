#if DEBUG
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using CommandTerminal;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;

/// <summary>
///     Debug-only "ap trap" commands for trying out trap effects before they become AP items.
/// </summary>
public static class TrapDebugCommands
{
    private const string Usage =
        "Usage: ap trap <events | event <name> | spawn <damaging|scary> [count] | queue [clear] | money <amount|percent%> | teleport | sanity [amount] | damage [count] | " +
        "engine | night | rot [count] | slow [factor] [seconds] | lights [seconds] | scramble | reverse [seconds]>";

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
                "money" => LoseMoney(rest.FirstOrDefault() ?? "10%"),
                "teleport" => Teleport(),
                "sanity" => LoseSanity(ParseFloat(rest, 0, 0.5f)),
                "damage" => Damage((int) ParseFloat(rest, 0, 1)),
                "engine" => DamageEngine(),
                "night" => SkipToNight(),
                "rot" => RotFish((int) ParseFloat(rest, 0, 1)),
                "slow" => Slow(ParseFloat(rest, 0, 0.5f), ParseFloat(rest, 1, 30f)),
                "lights" => LightsOut(ParseFloat(rest, 0, 60f)),
                "scramble" => ScrambleCargo(),
                "reverse" => ReverseControls(ParseFloat(rest, 0, 30f)),
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

    private static string LoseMoney(string amount)
    {
        var funds = GameManager.Instance.SaveData.Funds;
        var loss = amount.EndsWith("%")
            ? funds * decimal.Parse(amount.TrimEnd('%'), CultureInfo.InvariantCulture) / 100m
            : decimal.Parse(amount, CultureInfo.InvariantCulture);
        loss = Math.Min(Math.Round(loss), funds);
        GameManager.Instance.AddFunds(-loss);
        return $"Lost ${loss} (had ${funds}).";
    }

    private static string Teleport()
    {
        var player = GameManager.Instance.Player;
        if (player.IsDocked)
        {
            return "Can't teleport while docked.";
        }

        var destination = GameObject.FindGameObjectWithTag("TeleportDestination");
        if (destination == null)
        {
            return "No teleport destination in this scene.";
        }

        player.PlayerTeleport.Teleport(destination.transform.position, 0f);
        return $"Teleporting to {destination.name}.";
    }

    private static string LoseSanity(float amount)
    {
        var sanity = GameManager.Instance.Player.Sanity;
        var before = sanity.CurrentSanity;
        sanity.ChangeSanity(-amount);
        return $"Sanity {before:0.00} -> {sanity.CurrentSanity:0.00}.";
    }

    private static string Damage(int count)
    {
        var collider = GameManager.Instance.Player.GetComponentInChildren<PlayerCollider>();
        if (collider != null && count == 1)
        {
            collider.ProcessHit(isSafeCollider: false, isMonster: true, hasUniqueVibration: false);
            return "Simulated a monster hit.";
        }

        var damaged = GameManager.Instance.GridManager.AddDamageToInventory(count);
        return damaged ? $"Added {count} damage." : "No damage added (immune mode or no space to damage).";
    }

    private static string DamageEngine()
    {
        GameManager.Instance.GridManager.AddDamageToInventory(ItemType.EQUIPMENT, ItemSubtype.ENGINE);
        return "Damaged an engine (or a random cell if no engine could be damaged).";
    }

    private static string SkipToNight()
    {
        var time = GameManager.Instance.Time;
        if (!time.IsDaytime)
        {
            return "It's already night.";
        }

        var hours = (time.DuskTime - time.Time) * 24f + 0.25f;
        time.ForcefullyPassTime(hours, "feedback.pass-time-rest", TimePassageMode.OTHER);
        return $"Passing {hours:0.0} hours to nightfall.";
    }

    private static string RotFish(int count)
    {
        var grid = GameManager.Instance.SaveData.Inventory;
        var fish = grid.spatialItems.OfType<FishItemInstance>().OrderBy(_ => UnityEngine.Random.value).Take(count).ToList();
        fish.ForEach(f => GameManager.Instance.ItemManager.ReplaceFishWithRot(f, grid, sendNotification: true));
        return $"Rotted {fish.Count} fish.";
    }

    private static string Slow(float factor, float seconds)
    {
        GameManager.Instance.PlayerStats.OozeMovementSpeedModifier = factor;
        GameManager.Instance.StartCoroutine(RestoreSpeed(seconds));
        return $"Speed x{factor} for {seconds}s.";
    }

    private static IEnumerator RestoreSpeed(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        GameManager.Instance.PlayerStats.OozeMovementSpeedModifier = 1f;
    }

    // Same approach as the FlickerLights world event: a locked light ability can't be switched on.
    private static string LightsOut(float seconds)
    {
        var light = UnityEngine.Object.FindObjectOfType<LightAbility>();
        if (light == null)
        {
            return "No light ability found.";
        }

        light.Locked = false;
        light.Deactivate();
        light.Locked = true;
        GameManager.Instance.StartCoroutine(UnlockLights(light, seconds));
        return $"Lights out for {seconds}s.";
    }

    private static IEnumerator UnlockLights(LightAbility light, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        light.Locked = false;
    }

    private static string ScrambleCargo()
    {
        if (GameManager.Instance.GridManager.IsCurrentlyHoldingObject())
        {
            return "Can't scramble while holding an item.";
        }

        var grid = GameManager.Instance.SaveData.Inventory;
        var cargo = grid.spatialItems
            .Where(item => item.GetItemData<SpatialItemData>() is { itemType: not ItemType.EQUIPMENT } data &&
                           data is not DamageItemData && !data.isUnderlayItem)
            .ToList();
        if (cargo.Count == 0)
        {
            return "No cargo to scramble.";
        }

        var originalPositions = cargo.ToDictionary(item => item, item => new Vector3Int(item.x, item.y, item.z));
        cargo.ForEach(item => grid.RemoveObjectFromGridData(item, notify: false));

        var placed = new System.Collections.Generic.List<SpatialItemInstance>();
        foreach (var item in cargo.OrderByDescending(item => item.GetItemData<SpatialItemData>().dimensions.Count))
        {
            if (!grid.FindRandomPositionForObject(item.GetItemData<SpatialItemData>(), out var position))
            {
                // Couldn't fit everything back in a new layout, so put the hold back as it was.
                placed.ForEach(p => grid.RemoveObjectFromGridData(p, notify: false));
                cargo.ForEach(c => grid.AddObjectToGridData(c, originalPositions[c], dispatchEvent: false));
                grid.TriggerRefreshEvent();
                return "Couldn't fit the cargo in a new layout; left it as it was.";
            }

            grid.AddObjectToGridData(item, position, dispatchEvent: false);
            placed.Add(item);
        }

        grid.TriggerRefreshEvent();
        return $"Scrambled {cargo.Count} cargo item(s).";
    }

    public static float ReversedUntil { get; private set; }

    private static string ReverseControls(float seconds)
    {
        ReversedUntil = Time.time + seconds;
        return $"Controls reversed for {seconds}s.";
    }
}

[HarmonyLib.HarmonyPatch(typeof(DredgeInputManager), nameof(DredgeInputManager.GetValue), typeof(DredgePlayerActionTwoAxis))]
internal static class ReversedControlsPatch
{
    [HarmonyLib.HarmonyPostfix]
    private static void Postfix(DredgePlayerActionTwoAxis playerActionAxis, ref Vector2 __result)
    {
        if (Time.time < TrapDebugCommands.ReversedUntil &&
            playerActionAxis == GameManager.Instance?.Player?.Controller?.MoveAction)
        {
            __result = -__result;
        }
    }
}
#endif
