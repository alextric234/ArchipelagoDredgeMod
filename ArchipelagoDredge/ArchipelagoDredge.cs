using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.UI;
using ArchipelagoDredge.Utils;
using HarmonyLib;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge
{
    [HarmonyPatch]
    public class ArchipelagoDredge : MonoBehaviour
    {
        private static Harmony _harmony;
        public void Awake()
        {
            WinchCore.Log.Info($"{nameof(ArchipelagoDredge)} has loaded!");

            ArchipelagoClient.GameReady = false;

            LocationManager.Initialize();
            TerminalCommandManager.Initialize();

            ArchipelagoNotificationUi.Initialize();

            _harmony = new Harmony("com.alextric234.archipelago.dredge");
            _harmony.PatchAll();

        }

        public void OnGameUnloaded()
        {
            ArchipelagoClient.Disconnect();
        }

        public void Quit()
        {
            WinchCore.Log.Info("Game quit");
            ArchipelagoClient.Disconnect();
        }

        private async void Update()
        {
            if (ArchipelagoClient.GameReady &&
                ArchipelagoClient.Connected &&
                ArchipelagoClient.HasItemsToProcess())
            {
                ArchipelagoItemManager.GetItem();
            }
        }
    }
}
