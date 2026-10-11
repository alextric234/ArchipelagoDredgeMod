using System;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;

public static class DeathLinkManager
{
    public static DeathLinkService DredgeDeathLinkService;
    public static bool IsDeathLinkEnabled;

    private static readonly object PendingDeathLinkLock = new object();

    private static bool _hasPendingDeath;
    private static string _pendingDeathMessage;

    // Main thread only: set while we are killing the player because of a received DeathLink.
    private static bool _isDyingFromDeathLink;
    private static string _deathScreenMessage;

    public static void SetupDredgeDeathLinkService()
    {
        if (DredgeDeathLinkService != null)
        {
            DredgeDeathLinkService.OnDeathLinkReceived -= OnDeathLinkReceived;
        }

        DredgeDeathLinkService =
            ArchipelagoClient.Session.CreateDeathLinkService();

        DredgeDeathLinkService.OnDeathLinkReceived += OnDeathLinkReceived;
    }

    private static void OnDeathLinkReceived(DeathLink deathLink)
    {
        if (!IsDeathLinkEnabled)
        {
            return;
        }

        string deathLinkMessage = string.IsNullOrWhiteSpace(deathLink.Cause)
            ? $"DeathLink from {deathLink.Source}"
            : $"DeathLink from {deathLink.Source}: {deathLink.Cause}";

        WinchCore.Log.Info($"{deathLinkMessage}");

        lock (PendingDeathLinkLock)
        {
            _hasPendingDeath = true;
            _pendingDeathMessage = deathLinkMessage;
        }
    }

    /// <summary>
    ///     Called every frame on the main thread. Kills the player once for a received DeathLink,
    ///     or drops it if the player can't die right now (menu, death screen, god mode).
    /// </summary>
    public static void ProcessPendingDeathLink()
    {
        if (!TryConsumePendingDeathLink(out var deathLinkMessage))
        {
            return;
        }

        var player = GameManager.Instance.Player;
        if (!GameManager.Instance.IsPlaying || player == null || !player.IsAlive || player.IsGodModeEnabled)
        {
            WinchCore.Log.Info($"Ignoring {deathLinkMessage}: player can't die right now");
            return;
        }

        _isDyingFromDeathLink = true;
        _deathScreenMessage = deathLinkMessage;
        try
        {
            // Synchronous all the way to the death popup, which reads _deathScreenMessage.
            player.Die();
        }
        finally
        {
            _isDyingFromDeathLink = false;
            _deathScreenMessage = null;
        }
    }

    public static bool TryGetDeathScreenMessage(out string deathLinkMessage)
    {
        deathLinkMessage = _deathScreenMessage;
        return !string.IsNullOrWhiteSpace(deathLinkMessage);
    }

    private static bool TryConsumePendingDeathLink(out string deathLinkMessage)
    {
        lock (PendingDeathLinkLock)
        {
            if (!_hasPendingDeath)
            {
                deathLinkMessage = null;
                return false;
            }

            deathLinkMessage = _pendingDeathMessage;

            _hasPendingDeath = false;
            _pendingDeathMessage = null;

            return true;
        }
    }

    public static void EnableDeathLink()
    {
        IsDeathLinkEnabled = true;
        ApConfigHelper.SaveDeathLinkValue(IsDeathLinkEnabled);
        DredgeDeathLinkService.EnableDeathLink();
        WinchCore.Log.Info($"DeathLink enabled");
    }

    public static void DisableDeathLink()
    {
        IsDeathLinkEnabled = false;
        ApConfigHelper.SaveDeathLinkValue(IsDeathLinkEnabled);
        DredgeDeathLinkService.DisableDeathLink();
        WinchCore.Log.Info($"DeathLink disabled");
    }

    public static void SendDeathLink()
    {
        try
        {
            var playerName = ArchipelagoClient.Session.Players.ActivePlayer.Name;
            var cause = "Took on too much water";
            var deathlink = new DeathLink(playerName, cause);

            WinchCore.Log.Info($"sending death link");
            DredgeDeathLinkService.SendDeathLink(deathlink);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Deathlink Exception: {e}");
        }
    }

    public static void HandleLocalDeath()
    {
        if (_isDyingFromDeathLink)
        {
            WinchCore.Log.Debug($"death due to deathlink");
            return;
        }

        if (!IsDeathLinkEnabled || DredgeDeathLinkService == null || !ArchipelagoClient.IsConnected)
        {
            return;
        }

        SendDeathLink();
    }
}