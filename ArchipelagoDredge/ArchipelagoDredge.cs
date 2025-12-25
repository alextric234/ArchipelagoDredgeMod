using System;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Ui;
using ArchipelagoDredge.Network;
using HarmonyLib;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge;

[HarmonyPatch]
public class ArchipelagoDredge : MonoBehaviour
{
    private static Harmony _harmony;

    public void Awake()
    {
        try
        {
            WinchCore.Log.Info($"{nameof(ArchipelagoDredge)} has loaded!");

            TerminalCommandManager.Initialize();

            _harmony = new Harmony("com.alextric234.archipelago.dredge");
            _harmony.PatchAll();

            GetConnectionConfigPanel();
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error($"[AP] Error in awake: {ex}");
        }
    }

    private void Update()
    {
        try
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                WinchCore.Log.Info("Debug key pressed...");

                var hasLoaded = GameManager.Instance.DataLoader.HasLoaded();
                WinchCore.Log.Info($"HasLoaded: {hasLoaded}");

                WinchCore.Log.Info("Debug complete.");
            }

            if (Input.GetKeyDown(KeyCode.F8)) ArchipelagoCommandManager.ConfigConnect();

            if (Input.GetKeyDown(KeyCode.F10)) ArchipelagoCommandManager.Disconnect();

            var connected = ArchipelagoClient.Session?.Socket?.Connected == true;
            var ready = GameManager.Instance.DataLoader.HasLoaded();
            var hasItems = ArchipelagoClient.HasItemsToProcess();

            if (ready && connected && hasItems) ArchipelagoItemManager.GetItem();
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error($"[AP] Update processing error: {ex}");
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