using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Ui;
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
            ArchipelagoClient.Disconnect();
        }

        private async void Update()
        {
            if (ArchipelagoClient.GameReady &&
                ArchipelagoClient.Session.Socket.Connected &&
                ArchipelagoClient.HasItemsToProcess())
            {
                ArchipelagoItemManager.GetItem();
            }
        }
    }
}
