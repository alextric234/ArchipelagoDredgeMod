using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ArchipelagoDredge.Game.Managers;

/// <summary>
///     What each trap does. Timed effects restart their timer when hit again instead of stacking.
/// </summary>
public static class TrapEffects
{
    public const float TimedTrapSeconds = 15f;
    public const decimal MoneyLossPercent = 10m;
    public const float PanicAmount = 0.4f;
    public const float SlowFactor = 0.5f;

    private const float TeleportCooldownSeconds = 5f;

    private static float slowUntil;
    private static float lastTeleportTime = float.NegativeInfinity;
    private static LightAbility lockedLight;
    private static float lightsUntil;

    public static float ReversedUntil { get; private set; }

    public static void Update()
    {
        if (slowUntil > 0f && Time.time >= slowUntil)
        {
            slowUntil = 0f;
            GameManager.Instance.PlayerStats.OozeMovementSpeedModifier = 1f;
        }

        if (lockedLight != null && Time.time >= lightsUntil)
        {
            lockedLight.Locked = false;
            lockedLight = null;
        }
    }

    public static void Reset()
    {
        slowUntil = 0f;
        lockedLight = null;
        ReversedUntil = 0f;
    }

    public static string LoseMoney(decimal percent)
    {
        var funds = GameManager.Instance.SaveData.Funds;
        var loss = Math.Min(Math.Round(funds * percent / 100m), funds);
        GameManager.Instance.AddFunds(-loss);
        return $"Lost ${loss} (had ${funds}).";
    }

    public static bool CanTeleport()
    {
        return !GameManager.Instance.Player.IsDocked && Time.time > lastTeleportTime + TeleportCooldownSeconds &&
               GameObject.FindGameObjectWithTag("TeleportDestination") != null;
    }

    public static string Teleport()
    {
        var destination = GameObject.FindGameObjectWithTag("TeleportDestination");
        lastTeleportTime = Time.time;
        GameManager.Instance.Player.PlayerTeleport.Teleport(destination.transform.position, 0f);
        return $"Teleporting to {destination.name}.";
    }

    public static string Panic(float amount)
    {
        var sanity = GameManager.Instance.Player.Sanity;
        var before = sanity.CurrentSanity;
        sanity.ChangeSanity(-amount);
        return $"Sanity {before:0.00} -> {sanity.CurrentSanity:0.00}.";
    }

    public static string DamageHull(int count = 1)
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

    public static string DamageEngine()
    {
        GameManager.Instance.GridManager.AddDamageToInventory(ItemType.EQUIPMENT, ItemSubtype.ENGINE);
        return "Damaged an engine (or a random cell if no engine could be damaged).";
    }

    public static string SkipToNight()
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

    public static string RotFish(int count = 1)
    {
        var grid = GameManager.Instance.SaveData.Inventory;
        var fish = grid.spatialItems.OfType<FishItemInstance>().OrderBy(_ => UnityEngine.Random.value).Take(count).ToList();
        fish.ForEach(f => GameManager.Instance.ItemManager.ReplaceFishWithRot(f, grid, sendNotification: true));
        return $"Rotted {fish.Count} fish.";
    }

    public static string Slow(float factor, float seconds)
    {
        GameManager.Instance.PlayerStats.OozeMovementSpeedModifier = factor;
        slowUntil = Time.time + seconds;
        return $"Speed x{factor} for {seconds}s.";
    }

    // Same approach as the FlickerLights world event: a locked light ability can't be switched on.
    public static string LightsOut(float seconds)
    {
        var light = UnityEngine.Object.FindObjectOfType<LightAbility>();
        if (light == null)
        {
            return "No light ability found.";
        }

        light.Locked = false;
        light.Deactivate();
        light.Locked = true;
        lockedLight = light;
        lightsUntil = Time.time + seconds;
        return $"Lights out for {seconds}s.";
    }

    public static bool CanScrambleCargo()
    {
        return !GameManager.Instance.GridManager.IsCurrentlyHoldingObject();
    }

    public static string ScrambleCargo()
    {
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

        var placed = new List<SpatialItemInstance>();
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

    public static string ReverseControls(float seconds)
    {
        ReversedUntil = Time.time + seconds;
        return $"Controls reversed for {seconds}s.";
    }
}

[HarmonyPatch(typeof(DredgeInputManager), nameof(DredgeInputManager.GetValue), typeof(DredgePlayerActionTwoAxis))]
internal static class ReversedControlsPatch
{
    [HarmonyPostfix]
    private static void Postfix(DredgePlayerActionTwoAxis playerActionAxis, ref Vector2 __result)
    {
        if (Time.time < TrapEffects.ReversedUntil &&
            playerActionAxis == GameManager.Instance?.Player?.Controller?.MoveAction)
        {
            __result = -__result;
        }
    }
}
