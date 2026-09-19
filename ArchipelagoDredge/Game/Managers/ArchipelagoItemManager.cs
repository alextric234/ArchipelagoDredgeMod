using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Colors;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Models;
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
                apItem.ItemName.EndsWith("Researched") ||
                apItem.ItemName.Contains("Starting Gear"))
            {
                ArchipelagoStateManager.IncrementLastProcessedIndex(indexOfItemToProcess);
                return;
            }

            if (ItemNames.VirtualItems.Contains(ItemNames.NameToItem(apItem.ItemName)))
            {
                var item = ItemNames.NameToItem(apItem.ItemName);
                string itemKey = item switch
                {
                    //Fishing Licenses
                    Item.VIRTUAL_LICENSE_GALE_CLIFFS => FishingLicenseManager.GaleCliffsFishingLicenseKey,
                    Item.VIRTUAL_LICENSE_STELLAR_BASIN => FishingLicenseManager.StellarBasinFishingLicenseKey,
                    Item.VIRTUAL_LICENSE_TWISTED_STRAND => FishingLicenseManager.TwistedStrandFishingLicenseKey,
                    Item.VIRTUAL_LICENSE_DEVILS_SPINE => FishingLicenseManager.DevilsSpineFishingLicenseKey,
                    Item.VIRTUAL_LICENSE_OPEN_OCEAN => FishingLicenseManager.OpenOceanFishingLicenseKey,
                    Item.VIRTUAL_LICENSE_PALE_REACH => FishingLicenseManager.PaleReachFishingLicenseKey,

                    //Passage Items
                    Item.VIRTUAL_PASSAGE_GALE_CLIFFS => PassageManager.GaleCliffsPassageItemKey,
                    Item.VIRTUAL_PASSAGE_STELLAR_BASIN => PassageManager.StellarBasinPassageItemKey,
                    Item.VIRTUAL_PASSAGE_TWISTED_STRAND => PassageManager.TwistedStrandPassageItemKey,
                    Item.VIRTUAL_PASSAGE_DEVILS_SPINE => PassageManager.DevilsSpinePassageItemKey,
                    Item.VIRTUAL_PASSAGE_OPEN_OCEAN => PassageManager.OpenOceanPassageItemKey,
                    Item.VIRTUAL_PASSAGE_PALE_REACH => PassageManager.PaleReachPassageItemKey,

                    _ => "unknown"
                };

                if (!ArchipelagoStateManager.AddVirtualItem(itemKey))
                {
                    WinchCore.Log.Error($"{apItem.ItemName}");
                }

                ArchipelagoStateManager.IncrementLastProcessedIndex(indexOfItemToProcess);
                return;
            }

            if (apItem.ItemName.Equals("Progressive Hull"))
            {
                UpgradeHelper.UpgradeBoat();
                ArchipelagoStateManager.IncrementLastProcessedIndex(indexOfItemToProcess);
                UpgradeEquipped("Hull Upgraded");
                return;
            }

            if (apItem.ItemName.Equals("Dredge Crane"))
            {
                GameManager.Instance.DialogueRunner.AddItemById("dredge1", GameManager.Instance.SaveData.Inventory);
                ArchipelagoStateManager.IncrementLastProcessedIndex(indexOfItemToProcess);
                UpgradeEquipped("Dredge Crane Equipped");
                return;
            }

            if (apItem.ItemName.Equals("Icebreaker"))
            {
                GameManager.Instance.SaveData.SetBoolVariable(BoatSubModelToggler.ICEBREAKER_EQUIP_STRING_KEY, true);
                GameEvents.Instance.TriggerIcebreakerEquipChanged();
                ArchipelagoStateManager.IncrementLastProcessedIndex(indexOfItemToProcess);
                UpgradeEquipped("Icebreaker equipped");
                return;
            }

            if (GameManager.Instance.GridManager.IsCurrentlyHoldingObject())
            {
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
            ArchipelagoStateManager.IncrementLastProcessedIndex(indexOfItemToProcess);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error("Error getting item from multiworld");
            WinchCore.Log.Error(e);
        }
    }

    private static void UpgradeEquipped(string upgradeMessage)
    {
        var notification = new DredgeNotification();
        notification.Message = upgradeMessage;
        notification.MessageColor = PaletteColor.Plum;
        NotificationHelper.TryToSendNotification(notification);
    }

    public static List<SpatialItemData> GetItemsForShops()
    {
        var apItemNames = ArchipelagoClient.Session.Items.AllItemsReceived.Where(item => item.ItemGame == "DREDGE")
            .Select(item => item.ItemName).ToList();
        var collectedItems = apItemNames
            .Where(i => ItemNames.itemNamesReversed.ContainsKey(i))
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