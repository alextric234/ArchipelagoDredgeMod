using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Colors;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Models;
using ArchipelagoDredge.Game.Services;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using CommandTerminal;
using HarmonyLib;
using UnityEngine;
using Winch.Core;
using Winch.Util;

namespace ArchipelagoDredge.Game.Managers;

public static class ArchipelagoItemManager
{
    private const int CrabPotStockCount = 3;

    public static void GetItem()
    {
        try
        {
            var indexOfItemToProcess = ArchipelagoStateManager.StateData.LastProcessedIndex + 1;
            var apItem = ArchipelagoClient.Session.Items.AllItemsReceived[indexOfItemToProcess];
            if (apItem.ItemGame != ArchipelagoClient.GameName ||
                apItem.ItemName.EndsWith(ItemNames.ResearchedSuffix) ||
                apItem.ItemName.Contains(ItemNames.StartingGearTag))
            {
                ArchipelagoStateManager.SetLastProcessedIndex(indexOfItemToProcess);
                return;
            }

            if (!ItemNames.TryNameToItem(apItem.ItemName, out var item))
            {
                SkipUnknownItem(apItem.ItemName, indexOfItemToProcess);
                return;
            }

            if (item is Item.PROGRESSIVE_ABERRATION_CHANCE or Item.GUARANTEED_ABERRATIONS)
            {
                ArchipelagoStateManager.SetLastProcessedIndex(indexOfItemToProcess);
                var offNote = AberrationChanceManager.IsEnabled ? "" : " (boost is switched off)";
                UpgradeEquipped($"Aberration chance is now at least {AberrationChanceManager.GrantedChance:P0}{offNote}");
                return;
            }

            if (ItemNames.VirtualItems.Contains(item))
            {
                ReceiveVirtualItem(apItem.ItemName);
                ArchipelagoStateManager.SetLastProcessedIndex(indexOfItemToProcess);
                return;
            }

            if (item == Item.PROGRESSIVE_HULL)
            {
                UpgradeService.ApplyNextHullTier();
                ArchipelagoStateManager.SetLastProcessedIndex(indexOfItemToProcess);
                UpgradeEquipped("Hull Upgraded");
                return;
            }

            if (item == Item.DREDGE1)
            {
                GameManager.Instance.DialogueRunner.AddItemById(ItemNames.ItemToDredgeId(Item.DREDGE1),
                    GameManager.Instance.SaveData.Inventory);
                ArchipelagoStateManager.SetLastProcessedIndex(indexOfItemToProcess);
                UpgradeEquipped("Dredge Crane Equipped");
                return;
            }

            if (item == Item.WHOLE_ICEBREAKER)
            {
                GameManager.Instance.SaveData.SetBoolVariable(BoatSubModelToggler.ICEBREAKER_EQUIP_STRING_KEY, true);
                GameEvents.Instance.TriggerIcebreakerEquipChanged();
                ArchipelagoStateManager.SetLastProcessedIndex(indexOfItemToProcess);
                UpgradeEquipped("Icebreaker equipped");
                return;
            }

            if (ItemNames.IcebreakerParts.Contains(item) && GameManager.Instance.SaveData.GetIsIcebreakerEquipped())
            {
                ArchipelagoStateManager.SetLastProcessedIndex(indexOfItemToProcess);
                return;
            }

            if (GameManager.Instance.GridManager.IsCurrentlyHoldingObject())
            {
                return;
            }

            SerializableGrid validGrid = null;
            var dredgeItem = ItemUtil.GetItemData(ItemNames.ItemToDredgeId(item));
            if (dredgeItem == null)
            {
                SkipUnknownItem(apItem.ItemName, indexOfItemToProcess);
                return;
            }

            if (!HasEntitlementsFor(item, dredgeItem))
            {
                SkipUnownedDlcItem(apItem.ItemName, indexOfItemToProcess);
                return;
            }

            if (dredgeItem is SpatialItemData)
            {
                validGrid = GetValidGrid(dredgeItem);

                if (validGrid == null)
                {
                    return;
                }
            }

            var itemInstance = GameManager.Instance.ItemManager.AddItemById(dredgeItem.id, validGrid, false);
            if (itemInstance is FishItemInstance fish)
            {
                MultiworldFish.Mark(fish);
            }

            RestockShops();
            ArchipelagoStateManager.SetLastProcessedIndex(indexOfItemToProcess);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error("Error getting item from multiworld");
            WinchCore.Log.Error(e);
        }
    }

    private static void SkipUnknownItem(string apItemName, int index)
    {
        WinchCore.Log.Warn($"Skipping unknown item '{apItemName}' at index {index}; the mod may be out of date.");
        TerminalCommandManager.LogMessage(TerminalLogType.Warning,
            $"Couldn't receive unknown item '{apItemName}'. Is the mod out of date?");

        var notification = new DredgeNotification();
        notification.Message = $"Unknown item '{apItemName}'. Is the mod out of date?";
        notification.MessageColor = PaletteColor.Red;
        NotificationHelper.TryToSendNotification(notification);

        ArchipelagoStateManager.SetLastProcessedIndex(index);
    }

    // The game only tags some DLC items with their entitlement, so the mod's own list is checked too.
    private static bool HasEntitlementsFor(Item item, ItemData itemData)
    {
        IEnumerable<Entitlement> required = itemData.entitlementsRequired ?? [];
        if (ItemNames.RequiredEntitlements.TryGetValue(item, out var entitlement))
        {
            required = required.Append(entitlement);
        }

        return required.All(e => e == Entitlement.NONE || GameManager.Instance.EntitlementManager.GetHasEntitlement(e));
    }

    private static void SkipUnownedDlcItem(string apItemName, int index)
    {
        WinchCore.Log.Warn($"Skipping '{apItemName}' at index {index}; it needs DLC this game doesn't own.");

        var notification = new DredgeNotification();
        notification.Message = $"Couldn't receive '{apItemName}': it needs DLC you don't own.";
        notification.MessageColor = PaletteColor.Red;
        NotificationHelper.TryToSendNotification(notification);

        ArchipelagoStateManager.SetLastProcessedIndex(index);
    }

    private static void ReceiveVirtualItem(string apItemName)
    {
        var item = ItemNames.NameToItem(apItemName);
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

            _ => null
        };

        if (!ArchipelagoStateManager.AddVirtualItem(itemKey))
        {
            WinchCore.Log.Error($"Couldn't add virtual item '{apItemName}'");
        }

        if (ItemNames.PassageItems.Contains(item))
        {
            PassageManager.ShowPassageUnlocked(apItemName);
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
        var apItemNames = ApSave.ReceivedItemNames;
        List<Item> shopItems = [];
        List<SpatialItemData> shopDredgeItems = [];
        foreach (var apItemName in apItemNames)
        {
            if (!ItemNames.TryNameToItem(apItemName, out var item))
            {
                continue;
            }

            if (ItemNames.ShopItems.Contains(item))
            {
                if (shopItems.Contains(item))
                {
                    continue;
                }
                shopItems.Add(item);
                var dredgeItemId = ItemNames.ItemToDredgeId(item);
                var dredgeItem = (SpatialItemData)ItemUtil.GetItemData(dredgeItemId);
                var copies = dredgeItem.itemSubtype == ItemSubtype.POT ? CrabPotStockCount : 1;
                shopDredgeItems.AddRange(Enumerable.Repeat(dredgeItem, copies));
            }
        }

        return shopDredgeItems;
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