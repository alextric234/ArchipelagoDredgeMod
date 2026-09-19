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

    private static bool _suppressNextOutgoingDeathLink;


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
        _suppressNextOutgoingDeathLink = true;
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

    public static bool HasPendingDeathLink()
    {
        lock (PendingDeathLinkLock)
        {
            return _hasPendingDeath;
        }
    }

    public static bool TryConsumePendingDeathLink(out string deathLinkMessage)
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
        var playerName = ArchipelagoClient.Session.Players.ActivePlayer.Name;
        var cause = "Took on too much water";
        var deathlink = new DeathLink(playerName, cause);

        WinchCore.Log.Info($"sending death link");
        try
        {
            DredgeDeathLinkService.SendDeathLink(deathlink);
        }
        catch (Exception e)
        {
            WinchCore.Log.Info($"Deathlink Exception: {e}");
        }
    }

    public static void HandleLocalDeath()
    {
        WinchCore.Log.Info($"Handling player death");
        if (_suppressNextOutgoingDeathLink)
        {
            WinchCore.Log.Info($"death due to deathlink");
            _suppressNextOutgoingDeathLink = false;
            return;
        }
        SendDeathLink();
    }
}