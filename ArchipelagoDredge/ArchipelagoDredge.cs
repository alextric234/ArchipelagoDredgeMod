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

            ArchipelagoStateManager.Load();

            LocationManager.Initialize();
            TerminalCommandManager.Initialize();

            ArchipelagoNotificationUi.Initialize();

            _harmony = new Harmony("com.alextric234.archipelago.dredge");
            _harmony.PatchAll();
        }

        public void Quit()
        {
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

            if (Input.GetKeyDown(KeyCode.F9))
            {
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                if (!ArchipelagoClient.Session.Items.Any())
                {
                    WinchCore.Log.Info("No Items");
                    return;
                }
                var apItem = ArchipelagoClient.Session.Items.PeekItem();
                var dredgeItem = ArchipelagoItemManager.itemCache[apItem.ItemDisplayName];
                WinchCore.Log.Info($"Got a {dredgeItem.id}!");
            }
        }
    }
}
