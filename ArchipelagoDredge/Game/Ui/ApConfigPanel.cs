using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using System.IO;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Game.Ui
{
    public class ApConfigPanel : MonoBehaviour
    {
        private FileSystemWatcher _watcher;

        private bool _show;
        private Rect _rect = new Rect(60, 60, 460, 230);

        private string _host = "";
        private string _portText = "";
        private string _slot = "";
        private string _pwd = "";

        private bool _loadedOnce;
        private bool _pendingReload;

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
            catch (System.Exception ex)
            {
                WinchCore.Log.Error("[AP] Config watcher init failed: " + ex);
            }
        }

        private void OnDisable()
        {
            try { _watcher?.Dispose(); } catch { /* ignore */ }
        }

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
            }
        }

        private void OnGUI()
        {
            if (!_show) return;
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
                ConnectUsingFields();

            if (GUILayout.Button("Disconnect"))
                ArchipelagoCommandManager.Disconnect();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void ReloadFromConfig()
        {
            try
            {
                var (host, port, slot, pwd) = ApConfigHelper.Read();
                _host = host;
                _portText = port.ToString();
                _slot = slot;
                _pwd = pwd;
                WinchCore.Log.Info("[AP] Reloaded values from config.");
            }
            catch (System.Exception ex)
            {
                WinchCore.Log.Error("[AP] Reload failed: " + ex);
            }
        }

        private void ConnectUsingFields()
        {
            if (!int.TryParse(_portText, out var port) || port <= 0 || port > 65535)
            {
                WinchCore.Log.Error("[AP] Invalid port.");
                return;
            }

            var host = _host?.Trim() ?? "";
            var slot = _slot?.Trim() ?? "";
            var pwd = _pwd ?? "";

            var saved = ApConfigHelper.SaveValues(host, port, slot, pwd);
            if (!saved)
            {
                WinchCore.Log.Error("[AP] Proceeding to connect despite save error.");
            }

            ArchipelagoCommandManager.TryConnect(host, port, slot, pwd);
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
}
