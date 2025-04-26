using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Patches;
using ArchipelagoDredge.Network.Models;
using HarmonyLib;
using InControl;
using Newtonsoft.Json;
using System.Threading.Tasks;
using ArchipelagoDredge.UI;
using InControl.NativeDeviceProfiles;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Winch.Config;
using Winch.Core;

namespace ArchipelagoDredge
{
    [HarmonyPatch]
	public class ArchipelagoDredge : MonoBehaviour
    {
        private ArchipelagoSession theSession;
        private static ModConfig Config => ModConfig.GetConfig();
        public void Awake()
        {
            WinchCore.Log.Info($"{nameof(ArchipelagoDredge)} has loaded!");

            // Load configuration
            ConnectionConfig.Load();

            // Initialize managers
            LocationManager.Initialize();

            // Apply Harmony patches
            CorePatches.Apply();

        }

        private async void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                var roomInfo = await theSession.ConnectAsync();
                var loginResult = await theSession.LoginAsync("Dredge", "dredgetester", ItemsHandlingFlags.AllItems, password:"");
                WinchCore.Log.Info($"Hosted loginResult Successful: {loginResult.Successful}");
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                await theSession.Locations.CompleteLocationChecksAsync(3459028911689314);
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
    }
}
