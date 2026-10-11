using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Game.Models;
using CommandTerminal;

namespace ArchipelagoDredge.Game.Helpers;

public static class PrintJsonHelper
{
    /// <summary>
    ///     Shows a server message (from <c>Session.MessageLog</c>). Call on the main thread.
    /// </summary>
    public static void ShowMessage(LogMessage message)
    {
        // MultiClient has already resolved player, item and location names into the parts.
        TerminalCommandManager.LogMessage(TerminalLogType.Message, message.ToString());

        if (message is ItemSendLogMessage itemSend)
        {
            ShowItemSendNotification(itemSend);
        }
    }

    private static void ShowItemSendNotification(ItemSendLogMessage message)
    {
        if (!message.IsReceiverTheActivePlayer && !message.IsSenderTheActivePlayer)
        {
            return;
        }

        var itemName = message.Item.ItemDisplayName;

        if (itemName.EndsWith("Researched"))
        {
            return;
        }

        var notification = new DredgeNotification();

        if (message.IsReceiverTheActivePlayer && message.IsSenderTheActivePlayer)
        {
            notification.Message = $"Found {itemName}";
        }
        else if (message.IsReceiverTheActivePlayer)
        {
            notification.Message = $"Received {itemName}";
        }
        else
        {
            notification.Message = $"Found {message.Receiver.Name}'s {itemName}";
        }

        notification.MessageColor = ColorUtils.GetColor(message.Item.Flags) ?? PaletteColor.White;

        NotificationHelper.TryToSendNotification(notification);
    }
}