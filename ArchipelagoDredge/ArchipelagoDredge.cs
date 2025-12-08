using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Ui;
using ArchipelagoDredge.Network;
using HarmonyLib;
using UnityEngine;
using Winch.Core;
using Winch.Util;

namespace ArchipelagoDredge
{
    [HarmonyPatch]
    public class ArchipelagoDredge : MonoBehaviour
    {
        private static Harmony _harmony;
        public void Awake()
        {
            try
            {
                WinchCore.Log.Info($"{nameof(ArchipelagoDredge)} has loaded!");

                ArchipelagoClient.GameReady = false;

                TerminalCommandManager.Initialize();

                _harmony = new Harmony("com.alextric234.archipelago.dredge");
                _harmony.PatchAll();

                GetConnectionConfigPanel();
            }
            catch(System.Exception ex)
            {
                WinchCore.Log.Error($"[AP] Error in awake: {ex}");
            }
        }

        public void OnGameUnloaded()
        {
            ArchipelagoClient.Disconnect();
        }

        public void Quit()
        {
            ArchipelagoClient.Disconnect();
        }

        private void Update()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    WinchCore.Log.Info("Debug key pressed...");

                    //Debug code goes here
                    
                    WinchCore.Log.Info("Debug complete.");
                }

                if (Input.GetKeyDown(KeyCode.F8))
                {
                    ArchipelagoCommandManager.ConfigConnect();
                }

                if (Input.GetKeyDown(KeyCode.F10))
                {
                    ArchipelagoCommandManager.Disconnect();
                }

                bool connected = ArchipelagoClient.Session?.Socket?.Connected == true;
                bool ready = ArchipelagoClient.GameReady == true;
                bool hasItems = ArchipelagoClient.HasItemsToProcess() == true;

                if (ready && connected && hasItems)
                {
                    ArchipelagoItemManager.GetItem();
                }
            }
            catch (System.Exception ex)
            {
                WinchCore.Log.Error($"[AP] Update processing error: {ex}");
            }
        }

        private void GetConnectionConfigPanel()
        {
            var existing = FindObjectOfType<ApConfigPanel>();
            if (existing == null)
            {
                gameObject.AddComponent<ApConfigPanel>();
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
