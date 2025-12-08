using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchipelagoDredge.Game.Patches;
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

            SerializableGrid validGrid = null;
            var dredgeItem = NameToItemCache[apItem.ItemName];
            if (dredgeItem is SpatialItemData){
                validGrid = GetValidGrid(dredgeItem);
            
                if (validGrid == null)
                {
                    return;
                }
            }

            GameManager.Instance.ItemManager.AddItemById(dredgeItem.id, validGrid, false);
            
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
        var apItemNames = ArchipelagoClient.Session.Items.AllItemsReceived.Where(item => item.ItemGame == "Dredge").Select(item => item.ItemName).ToList();
        var collectedItems = NameToItemCache.Where(entry => apItemNames.Contains(entry.Key))
            .Where(entry => CheckIfValidShopItem(entry.Value))
            .Select(entry => entry.Value)
            .OfType<SpatialItemData>().ToList();

        return collectedItems;
    }

    private static bool CheckIfValidShopItem(ItemData item)
    {

        var invalidShopItems = new List<string> {
            "rod19",
            "rod8",
            "rod16",
            "rod17",
            "rod20",
            "pot8",
            "engine10",
            "net7"
        };
        var gearSubTypes = new HashSet<ItemSubtype>
        {
            ItemSubtype.ENGINE,
            ItemSubtype.LIGHT,
            ItemSubtype.NET,
            ItemSubtype.POT,
            ItemSubtype.ROD,
            ItemSubtype.GENERAL
        };

        var validGeneralItems = new List<string>
        {
            "explosives",
            "bait",
            "bait-ab",
            "bait-crab",
            "bait-exotic"
        };

        if (item.id.StartsWith("tir"))
        {
            return false;
        }

        if (invalidShopItems.Contains(item.id))
        {
            return false;
        }

        if (!gearSubTypes.Contains(item.itemSubtype))
        {
            return false;
        }

        if (item.itemSubtype == ItemSubtype.GENERAL && !validGeneralItems.Contains(item.id))
        {
            return false;
        }

        return true;
    }

    private static SerializableGrid GetValidGrid(ItemData dredgeItem)
    {
        Vector3Int foundPosition;
        if (GameManager.Instance.SaveData.Storage.FindPositionForObject((SpatialItemData)dredgeItem, out foundPosition))
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