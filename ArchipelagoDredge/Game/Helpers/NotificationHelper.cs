using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Colors;
using ArchipelagoDredge.Game.Models;
using ArchipelagoDredge.Utils;
using Winch.Core;

namespace ArchipelagoDredge.Game.Helpers;

internal static class NotificationHelper
{
    private static readonly Queue<DredgeNotification> _messageQueue = new();
    private static bool _isProcessing;

    public static void ShowNotificationWithColour(NotificationType notificationType, string text,
        DredgeColorTypeEnum colour)
    {
        ShowNotificationWithColour(notificationType, text, GameManager.Instance.LanguageManager.GetColorCode(colour));
    }

    public static void ShowNotificationWithColour(NotificationType notificationType, string text, string colourCode)
    {
        ShowNotification(notificationType, $"<color=#{colourCode}>{text}</color>");
    }

    public static void ShowNotification(NotificationType notificationType, string text)
    {
        GameEvents.Instance.TriggerNotification(notificationType, text);
    }

    public static void BuildAndSendNotification(DredgeNotification notification)
    {
        try
        {
            var type = NotificationType.ITEM_ADDED;
            var color = DredgeColorTypeEnum.NEUTRAL;


            switch (notification.MessageColor)
            {
                case PaletteColor.Plum:
                    color = DredgeColorTypeEnum.POSITIVE;
                    break;
                case PaletteColor.SlateBlue:
                    color = DredgeColorTypeEnum.NEUTRAL;
                    break;
                case PaletteColor.Cyan:
                    type = NotificationType.ANY_REPAIR_KIT_USED;
                    break;
                case PaletteColor.Red:
                    type = NotificationType.ROT;
                    color = DredgeColorTypeEnum.NEGATIVE;
                    break;
            }

            MainThreadDispatcher.Enqueue(() => ShowNotificationWithColour(type, notification.Message, color));
        }
        catch (Exception e)
        {
            WinchCore.Log.Error(e.Message);
        }
    }

    public static void TryToSendNotification(DredgeNotification notification)
    {
        _messageQueue.Enqueue(notification);
        if (!_isProcessing)
        {
            _ = ProcessQueue();
        }
    }

    private static async Task ProcessQueue()
    {
        _isProcessing = true;
        while (_messageQueue.Count > 0)
        {
            var notification = _messageQueue.Dequeue();
            try
            {
                MainThreadDispatcher.Enqueue(() => BuildAndSendNotification(notification));
            }
            catch (Exception e)
            {
                WinchCore.Log.Error(e);
            }

            await Task.Delay(750);
        }

        _isProcessing = false;
    }
}