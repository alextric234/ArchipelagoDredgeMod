using System;
using System.IO;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Network.Enums;
using TMPro;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Game.Ui;

public class ApConfigPanel : MonoBehaviour
{
    private string _host = "";

    private bool _loadedOnce;
    private bool _pendingReload;
    private string _portText = "";
    private string _pwd = "";
    private Rect _rect = new(60, 60, 460, 230);

    private bool _show;
    private string _slot = "";
    private FileSystemWatcher _watcher;

    private void Update()
    {
        if (_pendingReload)
        {
            _pendingReload = false;
            ReloadFromConfig();
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            _show = !_show;
            if (_show && !_loadedOnce)
            {
                ReloadFromConfig();
                _loadedOnce = true;
            }

            if (_show)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    private void OnEnable()
    {
        try
        {
            _watcher = new FileSystemWatcher(
                Path.GetDirectoryName(ApConfigHelper.ConfigPath)!,
                Path.GetFileName(ApConfigHelper.ConfigPath))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime
            };
            _watcher.Changed += (_, __) => _pendingReload = true;
            _watcher.Created += (_, __) => _pendingReload = true;
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error("Config watcher init failed: " + ex);
        }
    }

    private void OnDisable()
    {
        try
        {
            _watcher?.Dispose();
        }
        catch
        {
            /* ignore */
        }
    }

    private void OnGUI()
    {
        if (!_show)
        {
            return;
        }
        _rect = GUILayout.Window(0xA11CED, _rect, Draw, "Archipelago (Config + Connect)");
    }

    private void Draw(int id)
    {
        GUILayout.BeginVertical();
        LabeledText("Host", ref _host);
        LabeledText("Port", ref _portText);
        LabeledText("Slot Name", ref _slot);
        LabeledPassword("Password", ref _pwd);

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Connect"))
        {
            ConnectUsingFields();
        }

        if (GUILayout.Button("Disconnect"))
        {
            ArchipelagoCommandManager.Disconnect();
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    private void ReloadFromConfig()
    {
        try
        {
            var (host, port, slot, pwd, deathLink) = ApConfigHelper.Read();
            _host = host;
            _portText = port.ToString();
            _slot = slot;
            _pwd = pwd;
            SetDeathLink(deathLink);
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error("Reload failed: " + ex);
        }
    }

    private void SetDeathLink(bool deathLink)
    {
        if (ArchipelagoClient.State != ConnectionState.Connected || deathLink == DeathLinkManager.IsDeathLinkEnabled)
        {
            return;
        }
        if (deathLink)
        {
            DeathLinkManager.EnableDeathLink();
            return;
        }
        DeathLinkManager.DisableDeathLink();
    }

    private void ConnectUsingFields()
    {
        if (!int.TryParse(_portText, out var port) || port <= 0 || port > 65535)
        {
            WinchCore.Log.Error("Invalid port.");
            return;
        }

        var host = _host?.Trim() ?? "";
        var slot = _slot?.Trim() ?? "";
        var pwd = _pwd ?? "";

        var saved = ApConfigHelper.SaveValues(host, port, slot, pwd);
        if (!saved)
        {
            WinchCore.Log.Error("Proceeding to connect despite save error.");
        }

        var deathLink = ApConfigHelper.ReadDeathLink();

        _ = ArchipelagoCommandManager.TryConnect(host, port, slot, pwd, deathLink);
    }


    private void LabeledText(string label, ref string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(100));
        value = GUILayout.TextField(value);
        GUILayout.EndHorizontal();
    }

    private void LabeledPassword(string label, ref string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(100));
        var r = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        value = GUI.PasswordField(r, value, '*');
        GUILayout.EndHorizontal();
    } 
}