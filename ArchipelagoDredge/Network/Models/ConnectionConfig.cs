using Newtonsoft.Json;
using System.IO;
using Winch.Config;
using Winch.Core;

namespace ArchipelagoDredge.Network.Models
{
    public class ConnectionConfig
    {
        public static ConnectionConfig Current { get; private set; }

        public string Host { get; set; } = "archipelago.gg:38281";
        public string SlotName { get; set; } = "Player";
        public string Password { get; set; } = "";
        public bool AutoConnect { get; set; } = false;

        public static void Load()
        {
            var configPath = Path.Combine(WinchCore.WinchInstallLocation, "archipelago_config.json");

            if (File.Exists(configPath))
            {
                try
                {
                    Current = JsonConvert.DeserializeObject<ConnectionConfig>(File.ReadAllText(configPath));
                    return;
                }
                catch { /* Fall through to create new config */ }
            }

            Current = new ConnectionConfig();
            Save();
        }

        public static void Save()
        {
            var configPath = Path.Combine(WinchCore.WinchInstallLocation, "archipelago_config.json");
            File.WriteAllText(configPath, JsonConvert.SerializeObject(Current, Formatting.Indented));
        }
    }
}