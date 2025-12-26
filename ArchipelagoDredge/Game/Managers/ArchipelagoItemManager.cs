using System;
using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using HarmonyLib;
using UnityEngine;
using Winch.Core;
using Winch.Util;

namespace ArchipelagoDredge.Game.Managers;

public class ArchipelagoItemManager
{
    public static void GetItem()
    {
        try
        {
            var indexOfItemToProcess = ArchipelagoStateManager.StateData.LastProcessedIndex + 1;
            var apItem = ArchipelagoClient.Session.Items.AllItemsReceived[indexOfItemToProcess];
            if (apItem.ItemGame != "DREDGE" ||
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

            if (apItem.ItemName.Equals("Dredge Crane"))
            {
                GameManager.Instance.DialogueRunner.AddItemById("dredge1", GameManager.Instance.SaveData.Inventory);
                UpdateStateData(indexOfItemToProcess);
                return;
            }

            SerializableGrid validGrid = null;
            var itemId = ItemNames.NameToItem(apItem.ItemName);
            var dredgeItem = ItemUtil.GetItemData(ItemNames.ItemToDredgeId(itemId));
            if (dredgeItem is SpatialItemData)
            {
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
        var apItemNames = ArchipelagoClient.Session.Items.AllItemsReceived.Where(item => item.ItemGame == "DREDGE")
            .Select(item => item.ItemName).ToList();
        var collectedItems = apItemNames
            .Select(i => ItemNames.ItemToDredgeId(ItemNames.NameToItem(i)))
            .Select(dredgeItemId => new
            {
                IsValidShopItem = TryGetValidShopItem(dredgeItemId, out var dredgeSpatialItemData),
                DredgeSpatialItemData = dredgeSpatialItemData
            })
            .Where(x => x.IsValidShopItem)
            .Select(x => x.DredgeSpatialItemData)
            .ToList();

        return collectedItems;
    }

    private static bool TryGetValidShopItem(string dredgeItemId, out SpatialItemData dredgeSpatialItemData)
    {
        dredgeSpatialItemData = null;
        var dredgeItem = ItemUtil.GetItemData(dredgeItemId);
        if (!dredgeItem)
        {
            return false;
        }

        var invalidShopItems = new List<string>
        {
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

        if (dredgeItem.id.StartsWith("tir"))
        {
            return false;
        }

        if (invalidShopItems.Contains(dredgeItem.id))
        {
            return false;
        }

        if (!gearSubTypes.Contains(dredgeItem.itemSubtype))
        {
            return false;
        }

        if (dredgeItem.itemSubtype == ItemSubtype.GENERAL && !validGeneralItems.Contains(dredgeItem.id))
        {
            return false;
        }

        dredgeSpatialItemData = (SpatialItemData) dredgeItem;
        return true;
    }

    private static SerializableGrid GetValidGrid(ItemData dredgeItem)
    {
        Vector3Int foundPosition;
        if (GameManager.Instance.SaveData.Storage.FindPositionForObject((SpatialItemData) dredgeItem,
                out foundPosition))
        {
            return GameManager.Instance.SaveData.Storage;
        }

        if (GameManager.Instance.SaveData.Inventory.FindPositionForObject((SpatialItemData) dredgeItem,
                out foundPosition))
        {
            return GameManager.Instance.SaveData.Inventory;
        }

        if (GameManager.Instance.SaveData.OverflowStorage.FindPositionForObject((SpatialItemData) dredgeItem,
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
        if (shopRestocker)
        {
            AccessTools.Method(typeof(ShopRestocker), "TryRefreshShops")
                .Invoke(shopRestocker, null);
        }
    }
}