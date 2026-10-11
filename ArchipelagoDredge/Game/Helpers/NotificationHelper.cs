using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net.Colors;
using ArchipelagoDredge.Game.Models;
using ArchipelagoDredge.Utils;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Game.Helpers;

internal static class NotificationHelper
{
    private const float SecondsBetweenNotifications = 0.75f;
    private static readonly Queue<DredgeNotification> _messageQueue = new();
    private static float _nextNotificationTime;

    public static void ShowNotificationWithColour(NotificationType notificationType, string text,
        DredgeColorTypeEnum colour)
    {
        MainThreadDispatcher.RunOnMainThread(() =>
            ShowNotificationWithColour(notificationType, text,
                GameManager.Instance.LanguageManager.GetColorCode(colour)));
    }

    public static void ShowNotificationWithColour(NotificationType notificationType, string text, string colourCode)
    {
        ShowNotification(notificationType, $"<color=#{colourCode}>{text}</color>");
    }

    public static void ShowNotification(NotificationType notificationType, string text)
    {
        MainThreadDispatcher.RunOnMainThread(() => GameEvents.Instance.TriggerNotification(notificationType, text));
    }

    private static void BuildAndSendNotification(DredgeNotification notification)
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

            ShowNotificationWithColour(type, notification.Message, color);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Failed to show notification: {e}");
        }
    }

    public static void TryToSendNotification(DredgeNotification notification)
    {
        MainThreadDispatcher.RunOnMainThread(() => _messageQueue.Enqueue(notification));
    }

    public static void Update()
    {
        if (_messageQueue.Count == 0 || Time.unscaledTime < _nextNotificationTime)
        {
            return;
        }

        BuildAndSendNotification(_messageQueue.Dequeue());
        _nextNotificationTime = Time.unscaledTime + SecondsBetweenNotifications;
    }
}