using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Colors;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Models;
using ArchipelagoDredge.Utils;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;

/// <summary>
///     Applies received trap items. Traps wait in the AP state until they make sense: nothing fires while fishing,
///     harvesting, holding an item, in a menu, paused or during forced time passage, and teleports, timed traps, hull
///     damage and events also wait until the boat leaves the dock. One trap fires per frame.
/// </summary>
public static class TrapManager
{
    private sealed class Trap(Func<bool> canApply, Func<string> apply)
    {
        public Func<bool> CanApply { get; } = canApply;
        public Func<string> Apply { get; } = apply;
    }

    private static readonly Dictionary<Item, Trap> Traps = new()
    {
        [Item.TRAP_DAMAGING_EVENT] = new Trap(CanStartEvent, () => QueueEvent(EventTrapKind.Damaging)),
        [Item.TRAP_SCARY_EVENT] = new Trap(CanStartEvent, () => QueueEvent(EventTrapKind.Scary)),
        [Item.TRAP_MONEY_LOSS] = new Trap(Always, () => TrapEffects.LoseMoney(TrapEffects.MoneyLossPercent)),
        [Item.TRAP_TELEPORT] = new Trap(() => Undocked() && TrapEffects.CanTeleport(), TrapEffects.Teleport),
        [Item.TRAP_PANIC] = new Trap(Always, () => TrapEffects.Panic(TrapEffects.PanicAmount)),
        [Item.TRAP_HULL_DAMAGE] = new Trap(Undocked, () => TrapEffects.DamageHull()),
        [Item.TRAP_ENGINE_DAMAGE] = new Trap(Always, TrapEffects.DamageEngine),
        [Item.TRAP_NIGHTFALL] = new Trap(Always, TrapEffects.SkipToNight),
        [Item.TRAP_ROT] = new Trap(Always, () => TrapEffects.RotFish()),
        [Item.TRAP_SLOW] = new Trap(Undocked,
            () => TrapEffects.Slow(TrapEffects.SlowFactor, TrapEffects.TimedTrapSeconds)),
        [Item.TRAP_LIGHTS_OUT] = new Trap(Undocked, () => TrapEffects.LightsOut(TrapEffects.TimedTrapSeconds)),
        [Item.TRAP_CARGO_SCRAMBLE] = new Trap(TrapEffects.CanScrambleCargo, TrapEffects.ScrambleCargo),
        [Item.TRAP_REVERSE_CONTROLS] = new Trap(Undocked,
            () => TrapEffects.ReverseControls(TrapEffects.TimedTrapSeconds))
    };

    public static bool IsTrap(Item item) => Traps.ContainsKey(item);

    public static void Receive(Item item)
    {
        var name = ItemNames.Names[item];
        ArchipelagoStateManager.AddPendingTrap(name);

        var notification = new DredgeNotification();
        notification.Message = $"{name}!";
        notification.MessageColor = PaletteColor.Red;
        NotificationHelper.TryToSendNotification(notification);
    }

    public static void Update()
    {
        try
        {
            TrapEffects.Update();
            EventTrapQueue.Update();

            var pending = ArchipelagoStateManager.PendingTraps;
            if (pending.Count == 0 || !CanApplyAnyNow())
            {
                return;
            }

            foreach (var name in pending.ToList())
            {
                if (!ItemNames.TryNameToItem(name, out var item) || !Traps.TryGetValue(item, out var trap))
                {
                    WinchCore.Log.Warn($"Dropping unknown pending trap '{name}'");
                    ArchipelagoStateManager.RemovePendingTrap(name);
                    continue;
                }

                if (!trap.CanApply())
                {
                    continue;
                }

                ArchipelagoStateManager.RemovePendingTrap(name);
                WinchCore.Log.Info($"Applying {name}: {trap.Apply()}");
                return;
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Error in TrapManager.Update: {e}");
        }
    }

    public static void Reset()
    {
        TrapEffects.Reset();
        EventTrapQueue.Clear();
    }

    private static bool CanApplyAnyNow()
    {
        return ApSave.IsActive && IsPlayerFree();
    }

    /// <summary>
    ///     True when nothing is mid-way through: no fishing or harvest screen, no held item and no open window other
    ///     than the dock menu. A trap during a catch can leave the harvest screen stuck.
    /// </summary>
    public static bool IsPlayerFree()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying || gm.IsPaused || gm.Player == null || !gm.Player.IsAlive ||
            gm.Time.IsTimePassingForcefully())
        {
            return false;
        }

        var ui = gm.UI;
        var windows = ui.ShowingWindowTypes;
        var onlyDockMenu = windows.Count == 0 || (windows.Count == 1 && windows[0] == UIWindowType.DOCK);
        return !gm.Player.IsFishing && !ui.IsHarvesting && !ui.IsShowingQuestGrid && !ui.IsShowingRadialMenu &&
               onlyDockMenu && !gm.GridManager.IsCurrentlyHoldingObject();
    }

    private static bool Always() => true;

    private static bool Undocked() => !GameManager.Instance.Player.IsDocked;

    // Event traps are handed to the event queue one at a time, so a quit loses at most the one waiting there.
    private static bool CanStartEvent() => Undocked() && EventTrapQueue.PendingCount == 0;

    private static string QueueEvent(EventTrapKind kind)
    {
        EventTrapQueue.Enqueue(kind);
        return $"Queued a {kind} event.";
    }
}
