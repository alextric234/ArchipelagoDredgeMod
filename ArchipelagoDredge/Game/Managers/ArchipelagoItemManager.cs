using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using InControl.UnityDeviceProfiles;
using UnityAsyncAwaitUtil;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Winch.Core;
using Winch.Util;

namespace ArchipelagoDredge.Game.Managers;

public class ArchipelagoItemManager
{
    public static Dictionary<string, ItemData> itemCache;
    public static async void Initialize()
    {
        await BuildItemNameCache();
        ArchipelagoClient.GameReady = true;
    }

    public static async Task BuildItemNameCache(int batchSize = 25)
    {
        var allItems = ItemUtil.GetAllItemData();
        itemCache = new Dictionary<string, ItemData>();

        for (int i = 0; i < allItems.Length; i += batchSize)
        {
            var batch = allItems.Skip(i).Take(batchSize);
            var batchTasks = batch.Select(async item =>
            {
                string name = await GetItemNameAsync(
                    item.itemNameKey.TableReference,
                    item.itemNameKey.TableEntryReference
                );
                return (name, item);
            });

            var batchResults = await Task.WhenAll(batchTasks);
            foreach (var (name, item) in batchResults)
            {
                if (!string.IsNullOrEmpty(name))
                    itemCache[name] = item;
            }
        }
    }

    public static async void LogItemInformation(RelicItemData item)
    {
        var itemName =
            await GetItemNameAsync(item.itemNameKey.TableReference, item.itemNameKey.TableEntryReference);
        WinchCore.Log.Info($"RelicName: {itemName} RelicId: {item.id}");
    }

    private static async Task<string> GetItemNameAsync(TableReference tableRef, TableEntryReference entryRef)
    {
        var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableRef, entryRef);
        await op.Task;
        return op.Result;
    }

    public static void GetItem()
    {
        try
        {
            var indexOfItemToProcess = ArchipelagoStateManager.StateData.LastProcessedIndex + 1;
            var apItem = ArchipelagoClient.Session.Items.AllItemsReceived[indexOfItemToProcess];
            if (apItem.ItemGame != "Dredge")
            {
                ArchipelagoStateManager.StateData.LastProcessedIndex = indexOfItemToProcess;
                ArchipelagoStateManager.SaveData();
                return;
            }

            var dredgeItem = itemCache[apItem.ItemName];
            Vector3Int foundPosition = Vector3Int.zero;
            if (!GameManager.Instance.SaveData.Inventory.FindPositionForObject((SpatialItemData)dredgeItem,
                    out foundPosition))
            {
                return;
            }
            var spatialItemInstance = GameManager.Instance.ItemManager.CreateItem<SpatialItemInstance>(dredgeItem);
            GameManager.Instance.SaveData.Inventory.AddObjectToGridData(spatialItemInstance, foundPosition, true);
            ArchipelagoStateManager.StateData.LastProcessedIndex = indexOfItemToProcess;
            ArchipelagoStateManager.SaveData();
        }
        catch (Exception e)
        {
            WinchCore.Log.Error("Error getting item from multiworld");
            WinchCore.Log.Error(e);
        }
    }
}