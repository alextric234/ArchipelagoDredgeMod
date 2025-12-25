using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Winch.Core;

namespace ArchipelagoDredge.Game.Helpers;

public static class ApConfigHelper
{
    private static string ModDir
    {
        get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!; }
    }

    public static string ConfigPath
    {
        get { return Path.Combine(ModDir, "Config.json"); }
    }

    public static string DefaultPath
    {
        get { return Path.Combine(ModDir, "default_config.json"); }
    }

    private static JObject LoadConfigObject(out bool isUserConfig)
    {
        isUserConfig = File.Exists(ConfigPath);
        var path = isUserConfig ? ConfigPath : DefaultPath;
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Config not found: {path}");
        }

        return JObject.Parse(File.ReadAllText(path));
    }

    public static (string host, int port, string slot, string pwd) Read()
    {
        var cfg = LoadConfigObject(out _);

        var host = cfg["apIpAddress"]?["value"]?.ToString() ?? "";
        var port = (int?) cfg["apPort"]?["value"] ?? 38281;
        var slot = cfg["apSlotName"]?["value"]?.ToString() ?? "";
        var pwd = cfg["apPassword"]?["value"]?.ToString() ?? "";

        return (host.Trim(), port, (slot ?? "").Trim(), pwd ?? "");
    }

    public static bool SaveValues(string host, int port, string slot, string pwd)
    {
        try
        {
            JObject cfg;

            var modDir = ModDir;
            Directory.CreateDirectory(modDir);

            if (File.Exists(ConfigPath))
            {
                cfg = JObject.Parse(File.ReadAllText(ConfigPath));
            }
            else if (File.Exists(DefaultPath))
            {
                cfg = JObject.Parse(File.ReadAllText(DefaultPath));
            }
            else
            {
                cfg = new JObject();
            }

            EnsureSetting(cfg, "apIpAddress", "text");
            EnsureSetting(cfg, "apPort", "integer");
            EnsureSetting(cfg, "apSlotName", "text");
            EnsureSetting(cfg, "apPassword", "text");

            cfg["apIpAddress"]["value"] = host ?? "";
            cfg["apPort"]["value"] = port;
            cfg["apSlotName"]["value"] = slot ?? "";
            cfg["apPassword"]["value"] = pwd ?? "";

            File.WriteAllText(ConfigPath, cfg.ToString());
            return true;
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error("[AP] Save failed: " + ex);
            return false;
        }
    }


    private static void EnsureSetting(JObject root, string key, string typeIfNew)
    {
        if (root[key] is JObject obj)
        {
            return;
        }

        root[key] = new JObject
        {
            ["type"] = typeIfNew,
            ["title"] = key, // safe fallback; Mods UI will show the key if no localization
            ["tooltip"] = key,
            ["value"] = typeIfNew == "integer" ? 0 : ""
        };
    }
}