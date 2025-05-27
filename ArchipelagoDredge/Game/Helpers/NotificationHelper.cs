using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Colors;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Game.Helpers
{
    internal static class NotificationHelper
    {
        private static readonly Queue<LogMessage> _messageQueue = new();
        private static bool _isProcessing = false;

        public static void ShowNotificationWithColour(NotificationType notificationType, string text, DredgeColorTypeEnum colour)
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

        public static void BuildAndSendNotification(string beginningOfString, MessagePart messageItem)
        {
            try
            {
                string fullText = $"{beginningOfString} {messageItem.Text}";
                NotificationType type = NotificationType.ITEM_ADDED;
                DredgeColorTypeEnum color = DredgeColorTypeEnum.NEUTRAL;

                switch (messageItem.PaletteColor)
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

                MainThreadDispatcher.Enqueue(() => ShowNotificationWithColour(type, fullText, color));
            }
            catch (Exception e)
            {
                WinchCore.Log.Error(e.Message);
            }
        }

        public static void TryToSendNotification(LogMessage message)
        {
            _messageQueue.Enqueue(message);
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
                var message = _messageQueue.Dequeue();
                try
                {
                    if (message.Parts.All(p => p.Type != MessagePartType.Player))
                    {
                        continue;
                    }

                    var currentPlayer = ArchipelagoClient.Session.Players.ActivePlayer.Name;
                    if (message.Parts.Count(p => p.Type == MessagePartType.Player) == 1)
                    {
                        var messagePlayer = message.Parts.FirstOrDefault(m => m.Type == MessagePartType.Player)?.Text;
                        if (messagePlayer != currentPlayer)
                        {
                            continue;
                        }

                        var messageItem = message.Parts.FirstOrDefault(m => m.Type == MessagePartType.Item);
                        if (messageItem == null)
                        {
                            continue;
                        }

                        MainThreadDispatcher.Enqueue(() => BuildAndSendNotification("Found", messageItem));
                        continue;
                    }

                    if (message.Parts.Count(p => p.Type == MessagePartType.Player) == 2)
                    {
                        if (message.Parts.Any(p => p.Type == MessagePartType.HintStatus))
                        {
                            continue;
                        }

                        var messageItem = message.Parts.FirstOrDefault(m => m.Type == MessagePartType.Item);
                        if (message.Parts.FirstOrDefault()!.Text == currentPlayer)
                        {
                            var receivingPlayer = message.Parts.Where(p => p.Type == MessagePartType.Player)
                                .ElementAtOrDefault(1);
                            MainThreadDispatcher.Enqueue(() => BuildAndSendNotification($"Found {receivingPlayer}'s", messageItem));
                        }
                        else
                        {
                            MainThreadDispatcher.Enqueue(() => BuildAndSendNotification("Received", messageItem));
                        }
                    }
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
}
