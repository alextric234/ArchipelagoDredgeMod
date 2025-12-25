using System.Linq;
using System.Text;
using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Models;
using ArchipelagoDredge.Network;
using CommandTerminal;

namespace ArchipelagoDredge.Game.Helpers;

public static class PrintJsonHelper
{
    public static void ShowPrintJsonMessage(PrintJsonPacket printJsonPacket)
    {
        switch (printJsonPacket.MessageType)
        {
            case JsonMessageType.ItemSend:
                HandleItemSendMessage((ItemPrintJsonPacket) printJsonPacket);
                break;
        }
    }

    private static void HandleItemSendMessage(ItemPrintJsonPacket itemPrintJsonPacket)
    {
        var sendingPlayer = ArchipelagoClient.Session.Players
            .AllPlayers.FirstOrDefault(player => player.Slot == itemPrintJsonPacket.Item.Player);

        var receivingPlayer = ArchipelagoClient.Session.Players
            .AllPlayers.FirstOrDefault(player => player.Slot == itemPrintJsonPacket.ReceivingPlayer);

        BuildAndSendTerminalMessage(itemPrintJsonPacket.Data, sendingPlayer?.Game, receivingPlayer?.Game);

        var activePlayer = ArchipelagoClient.Session.Players.ActivePlayer.Slot;
        if (activePlayer != itemPrintJsonPacket.ReceivingPlayer &&
            activePlayer != itemPrintJsonPacket.Item.Player) return;

        var itemName =
            ArchipelagoClient.Session.Items.GetItemName(itemPrintJsonPacket.Item.Item, receivingPlayer?.Game);

        var itemNameColor = ColorUtils.GetColor(itemPrintJsonPacket.Item.Flags);
        var notification = new DredgeNotification();

        if (activePlayer == itemPrintJsonPacket.ReceivingPlayer && activePlayer == sendingPlayer?.Slot)
            notification.Message = $"Found {itemName}";
        else if (activePlayer == itemPrintJsonPacket.ReceivingPlayer && activePlayer != sendingPlayer?.Slot)
            notification.Message = $"Received {itemName}";
        else
            notification.Message = $"Found {receivingPlayer?.Name}'s {itemName}";

        notification.MessageColor = itemNameColor ?? PaletteColor.White;

        NotificationHelper.TryToSendNotification(notification);
    }

    private static void BuildAndSendTerminalMessage(JsonMessagePart[] messageParts, string locationGameName,
        string receivingGameName)
    {
        var sb = new StringBuilder();
        foreach (var part in messageParts)
        {
            var text = "";

            switch (part.Type)
            {
                case JsonMessagePartType.PlayerId:
                    text = ArchipelagoClient.Session.Players
                        .AllPlayers.FirstOrDefault(player => player.Slot == int.Parse(part.Text))?.Name;
                    break;
                case JsonMessagePartType.ItemId:
                    text = ArchipelagoClient.Session.Items.GetItemName(long.Parse(part.Text), receivingGameName);
                    break;
                case JsonMessagePartType.LocationId:
                    text = ArchipelagoClient.Session.Locations.GetLocationNameFromId(long.Parse(part.Text),
                        locationGameName);
                    break;
                default:
                    text = part.Text;
                    break;
            }

            sb.Append(text);
        }

        TerminalCommandManager.LogMessage(TerminalLogType.Message, sb.ToString());
    }
}