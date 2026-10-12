using System;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Services;
using ArchipelagoDredge.Game.Ui;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Network.Enums;
using ArchipelagoDredge.Utils;
using CommandTerminal;
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
            LocationCheckService.Initialize();
            DeathLinkManager.Initialize();
            AberrationChanceManager.Initialize();

            _harmony = new Harmony("com.alextric234.archipelago.dredge");
            _harmony.PatchAll();

            GameManager.Instance.OnGameStarted += OnGameStarted;
            GameManager.Instance.OnGameEnded += OnGameEnded;

            GetConnectionConfigPanel();
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error($"Error in awake: {ex}");
        }
    }

    private void Update()
    {
        try
        {
            NotificationHelper.Update();

#if DEBUG
            if (Input.GetKeyDown(KeyCode.F4))
            {
                WinchCore.Log.Debug("Activating debug mode!");
                GameManager.Instance.Player.IsGodModeEnabled = true;
                GameManager.Instance.Player.IsImmuneModeEnabled = true;
                Terminal.Shell.RunCommand("player.move 200");
                Terminal.Shell.RunCommand("player.turn 250");
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                WinchCore.Log.Debug("Debug key pressed...");

                //Debug code goes here

                WinchCore.Log.Debug("Debug complete.");
            }
#endif

            if (Input.GetKeyDown(KeyCode.F8))
            {
                ArchipelagoCommandManager.ConfigConnect();
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                ArchipelagoCommandManager.Disconnect();
            }

            var connected = ArchipelagoClient.IsConnected;
            var ready = GameManager.Instance.DataLoader.HasLoaded();

            if (ready && ApSave.IsActive && ArchipelagoClient.SlotData.AddPassageItems &&
                GameManager.Instance.Player != null)
            {
                PassageManager.Update();
            }

            var hasItems = ArchipelagoClient.HasItemsToProcess();

            if (ready && connected && hasItems)
            {
                ArchipelagoItemManager.GetItem();
            }

            DeathLinkManager.ProcessPendingDeathLink();
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error($"Update processing error: {ex}");
        }
    }

    public void OnGameStarted()
    {
        if (ArchipelagoClient.State == ConnectionState.Connected || ArchipelagoClient.State == ConnectionState.Connecting)
        {
            return;
        }

        if (!ArchipelagoStateManager.IsBound)
        {
            WinchCore.Log.Info("Game started with a save not bound to Archipelago; connect with F8, F7 or 'ap connect'");
            return;
        }

        WinchCore.Log.Info($"Game started, connecting with Archipelago configuration");
        ArchipelagoCommandManager.ConfigConnect();
    }

    public void OnGameEnded()
    {
        PassageManager.Reset();

        if (ArchipelagoStateManager.AwaitingDeathScreenChoice)
        {
            return;
        }

        ArchipelagoStateManager.RevertStateData();
        WinchCore.Log.Info("Game ended, disconnecting from Archipelago");
        ArchipelagoCommandManager.Disconnect();
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