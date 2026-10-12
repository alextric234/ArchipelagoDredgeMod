using System;
using Winch.Config;
using Winch.Core;

namespace ArchipelagoDredge.Game.Helpers;

/// <summary>
///     The mod's settings, read and written through Winch's <see cref="ModConfig" /> so the mod menu,
///     the F7 panel and the mod all share one in-memory copy of <c>config.json</c>.
/// </summary>
public static class ApConfigHelper
{
    private const string ModGuid = "alextric234.archipelagodredge";

    public const string HostKey = "apIpAddress";
    public const string PortKey = "apPort";
    public const string SlotKey = "apSlotName";
    public const string PasswordKey = "apPassword";
    public const string DeathLinkKey = "apEnableDeathLink";
    public const string BoostedAberrationsKey = "apBoostedAberrations";

    private const int DefaultPort = 38281;

    public static ModConfig Config => ModConfig.GetConfig(ModGuid);

    public static (string host, int port, string slot, string pwd, bool deathLink) Read()
    {
        var host = Get(HostKey, "");
        var port = Get(PortKey, DefaultPort);
        var slot = Get(SlotKey, "");
        var pwd = Get(PasswordKey, "");
        var deathLink = Get(DeathLinkKey, false);

        return ((host ?? "").Trim(), port, (slot ?? "").Trim(), pwd ?? "", deathLink);
    }

    public static bool ReadDeathLink()
    {
        return Get(DeathLinkKey, false);
    }

    public static bool SaveValues(string host, int port, string slot, string pwd)
    {
        try
        {
            var config = Config;
            config.SetProperty(HostKey, host ?? "");
            config.SetProperty(PortKey, port);
            config.SetProperty(SlotKey, slot ?? "");
            config.SetProperty(PasswordKey, pwd ?? "");
            return true;
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error("Save failed: " + ex);
            return false;
        }
    }

    public static bool SaveDeathLinkValue(bool deathLink)
    {
        try
        {
            Config.SetProperty(DeathLinkKey, deathLink);
            return true;
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error("Save failed: " + ex);
            return false;
        }
    }

    public static bool ReadBoostedAberrations()
    {
        return Get(BoostedAberrationsKey, true);
    }

    public static void SaveBoostedAberrationsValue(bool enabled)
    {
        try
        {
            Config.SetProperty(BoostedAberrationsKey, enabled);
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error("Save failed: " + ex);
        }
    }

    private static T Get<T>(string key, T fallback)
    {
        try
        {
            var value = Config.GetProperty<T>(key);
            return value == null ? fallback : value;
        }
        catch (Exception ex)
        {
            WinchCore.Log.Warn($"Couldn't read config value '{key}', using {fallback}: {ex.Message}");
            return fallback;
        }
    }
}