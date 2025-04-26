using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using Winch.Core;

namespace ArchipelagoDredge
{
	[HarmonyPatch]
	public class ArchipelagoDredge : MonoBehaviour
    {
        private ArchipelagoSession theSession;
        public void Awake()
		{
			WinchCore.Log.Info($"{nameof(ArchipelagoDredge)} has loaded!");
            new Harmony(nameof(ArchipelagoDredge)).PatchAll();
            theSession = ArchipelagoSessionFactory.CreateSession("localhost", 62852);

        }

        private async void Update()
        {

            if (Input.GetKeyDown(KeyCode.F9))
            {
                var itemManager = GameManager.Instance.ItemManager;
                itemManager.GetFishItems().ForEach(CreateLocation);
            }

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

        private async void CreateLocation(FishItemData fish)
        {
            var fishName = await GetItemNameAsync(fish.itemNameKey.TableReference, fish.itemNameKey.TableEntryReference);
            WinchCore.Log.Info($"\"{fishName}\": DredgeLocationData(\"{fish.id}\", \"Open Ocean\", \"Encyclopedia\"),");
        }

        public async Task<string> GetItemNameAsync(TableReference tableRef, TableEntryReference entryRef)
        {
            var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableRef, entryRef);
            await op.Task;
            return op.Result;
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
