using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Winch.Core;
using Winch.Util;

namespace ArchipelagoDredge.Game.Managers;

public class ArchipelagoItemManager
{
    public static Dictionary<string, ItemData> NameToItemCache;
    public static Dictionary<string, string> ItemIdToNameCache;
    public static async void Initialize()
    {
        await BuildItemNameCache();
        ArchipelagoClient.GameReady = true;
    }

    public static async Task BuildItemNameCache(int batchSize = 25)
    {
        var allItems = ItemUtil.GetAllItemData();
        NameToItemCache = new Dictionary<string, ItemData>();
        ItemIdToNameCache = new Dictionary<string, string>();

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
                {
                    NameToItemCache[name] = item;
                    ItemIdToNameCache[item.id] = name;
                }

            }
        }
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
            if (apItem.ItemGame != "Dredge" || 
                apItem.ItemName.Contains("Starting Gear"))
            {
                UpdateStateData(indexOfItemToProcess);
                return;
            }
            if (apItem.ItemName.StartsWith("Progressive"))
            {
                UpgradeHelper.UpgradeItem(apItem.ItemName);
                UpdateStateData(indexOfItemToProcess);
                return;
            }

            Vector3Int foundPosition = Vector3Int.zero;
            var dredgeItem = NameToItemCache[apItem.ItemName];
            var inventoryTarget = GetInventoryTarget(dredgeItem, out foundPosition);
            if (inventoryTarget == null)
            {
                return;
            }
            var spatialItemInstance = GameManager.Instance.ItemManager.CreateItem<SpatialItemInstance>(dredgeItem);
            inventoryTarget.AddObjectToGridData(spatialItemInstance, foundPosition, true);
            RestockShops();
            UpdateStateData(indexOfItemToProcess);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error("Error getting item from multiworld");
            WinchCore.Log.Error(e);
        }
    }

    public static List<SpatialItemData> GetItemsForShops()
    {
        var gearSubTypes = new HashSet<ItemSubtype>
        {
            ItemSubtype.ENGINE,
            ItemSubtype.LIGHT,
            ItemSubtype.NET,
            ItemSubtype.POT,
            ItemSubtype.ROD
        };
        var apItemNames = ArchipelagoClient.Session.Items.AllItemsReceived.Where(item => item.ItemGame == "Dredge").Select(item => item.ItemName).ToList();
        var collectedItems = NameToItemCache.Where(entry => apItemNames.Contains(entry.Key))
            .Where(entry => gearSubTypes.Contains(entry.Value.itemSubtype))
            .Select(entry => entry.Value)
            .OfType<SpatialItemData>().ToList();
        return collectedItems;
    }

    private static SerializableGrid GetInventoryTarget(ItemData dredgeItem, out Vector3Int foundPosition)
    {
        if (GameManager.Instance.SaveData.Storage.FindPositionForObject((SpatialItemData)dredgeItem,
                out foundPosition))
        {
            return GameManager.Instance.SaveData.Storage;
        }
        if (GameManager.Instance.SaveData.Inventory.FindPositionForObject((SpatialItemData)dredgeItem,
                out foundPosition))
        {
            return GameManager.Instance.SaveData.Inventory;
        }
        if (GameManager.Instance.SaveData.OverflowStorage.FindPositionForObject((SpatialItemData)dredgeItem,
                out foundPosition))
        {
            return GameManager.Instance.SaveData.OverflowStorage;
        }
        return null;
    }

    private static void UpdateStateData(int indexOfItemToProcess)
    {
        ArchipelagoStateManager.StateData.LastProcessedIndex = indexOfItemToProcess;
        ArchipelagoStateManager.SaveData();
    }

    public static void RestockShops()
    {
        var shopRestocker = GameObject.FindObjectOfType<ShopRestocker>();
        if (shopRestocker != null)
        {
            AccessTools.Method(typeof(ShopRestocker), "TryRefreshShops")
                .Invoke(shopRestocker, null);
        }
    }
}