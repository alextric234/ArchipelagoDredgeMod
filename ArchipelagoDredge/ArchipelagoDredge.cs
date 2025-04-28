using System;
using System.Linq;
using System.Reflection;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Patches;
using ArchipelagoDredge.Network.Models;
using HarmonyLib;
using InControl;
using Newtonsoft.Json;
using System.Threading.Tasks;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.UI;
using InControl.NativeDeviceProfiles;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Winch.Config;
using Winch.Core;
using Winch.Util;
using System.Collections.Generic;
using UnityAsyncAwaitUtil;
using Winch.Core.API;
using Winch.Data.POI.Dock.Destinations;
using Winch.Data;
using ArchipelagoDredge.Utils;

namespace ArchipelagoDredge
{
    [HarmonyPatch]
	public class ArchipelagoDredge : MonoBehaviour
    {
        private static ModConfig Config => ModConfig.GetConfig();
        public void Awake()
        {
            WinchCore.Log.Info($"{nameof(ArchipelagoDredge)} has loaded!");

            ArchipelagoClient.GameReady = false;

            ArchipelagoStateManager.Load();

            ConnectionConfig.Load();

            LocationManager.Initialize();
            TerminalCommandManager.Initialize();

            ArchipelagoNotificationUi.Initialize();

            // Apply Harmony patches
            CorePatches.Apply();

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

        [HarmonyPatch(typeof(HarvestMinigameView), nameof(HarvestMinigameView.SpawnItem))]
        [HarmonyPrefix]
        public static void HarvestMinigameView_SpawnItem(HarvestMinigameView __instance)
        {
            WinchCore.Log.Info($"Got a {__instance.itemDataToHarvest.id}");
            ItemManager itemManager = GameManager.Instance.ItemManager;
            string newFishId = itemManager.GetFishItems()[10].id;
            WinchCore.Log.Info($"ItemManager has {itemManager.GetFishItems().Count} fish in it");
            WinchCore.Log.Info($"It should become a {newFishId}");
            if (__instance.itemDataToHarvest.itemSubtype == ItemSubtype.FISH)
            {
                __instance.itemDataToHarvest.id = newFishId;
                WinchCore.Log.Info($"Now it's a {__instance.itemDataToHarvest.id}");
            }
        }

        [HarmonyPatch(typeof(JsonConvert), nameof(JsonConvert.SerializeObject), new[] { typeof(object) })]
        public static class JsonConvert_SerializeObject_Patch
        {
            public static void Postfix(ref string __result)
            {
                if (__result.Contains("AllItems"))
                {
                    __result = __result.Replace("\"AllItems\"", "7");
                }
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveGameFile))]
        public static void SaveGameFilePost(SaveManager __instance, bool useBackupHistory)
        {
            WinchCore.Log.Debug($"SaveGameFilePost({useBackupHistory})");
            try
            {
                ArchipelagoStateManager.StateData.LastProcessedIndexSinceSave =
                    ArchipelagoStateManager.StateData.LastProcessedIndex;
                ArchipelagoStateManager.SaveData();
            }
            catch (System.Exception ex)
            {
                WinchCore.Log.Error(ex);
            }
        }
    }
}
