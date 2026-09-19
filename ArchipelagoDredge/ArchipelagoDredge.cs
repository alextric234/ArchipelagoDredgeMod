using System;
using ArchipelagoDredge.Game.Managers;
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
            if (Input.GetKeyDown(KeyCode.F4))
            {
                WinchCore.Log.Info("Activating debug mode!");
                GameManager.Instance.Player.IsGodModeEnabled = true;
                GameManager.Instance.Player.IsImmuneModeEnabled = true;
                Terminal.Shell.RunCommand("player.move 200");
                Terminal.Shell.RunCommand("player.turn 250");
            }

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

            var connected = ArchipelagoClient.Session?.Socket?.Connected == true;
            var ready = GameManager.Instance.DataLoader.HasLoaded();

            if (ready && connected && ArchipelagoClient.SlotData.AddPassageItems)
            {
                PassageManager.Update();
            }

            var hasItems = ArchipelagoClient.HasItemsToProcess();

            if (ready && connected && hasItems)
            {
                ArchipelagoItemManager.GetItem();
            }

            if (DeathLinkManager.HasPendingDeathLink())
            {
                GameManager.Instance.Player.Die();
            }
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error($"Update processing error: {ex}");
        }
    }

    public void OnGameStarted()
    {
        if (ArchipelagoClient.State != ConnectionState.Connected && ArchipelagoClient.State != ConnectionState.Connecting)
        {
            WinchCore.Log.Info($"Game started, connecting with Archipelago configuration");
            ArchipelagoCommandManager.ConfigConnect();
        }
    }

    public void OnGameEnded()
    {
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