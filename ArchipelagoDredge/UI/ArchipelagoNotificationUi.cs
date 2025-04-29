using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Winch.Core;

namespace ArchipelagoDredge.UI
{
    public class ArchipelagoNotificationUi : MonoBehaviour
    {
        // Configuration
        private const int MAX_MESSAGES = 5;
        private const float MESSAGE_DURATION = 5f;
        private const float FADE_DURATION = 0.5f;
        private const float MESSAGE_SPACING = 5f;
        private static readonly Vector2 PANEL_POSITION = new Vector2(20, 20);
        private static readonly Vector2 PANEL_SIZE = new Vector2(400, 200);

        private static ArchipelagoNotificationUi _instance;
        private static Canvas _notificationCanvas;
        private static GameObject _panel;
        private static RectTransform _contentPanel;
        private static CanvasGroup _panelCanvasGroup;
        private static List<NotificationMessage> _activeMessages = new List<NotificationMessage>();

        private class NotificationMessage
        {
            public GameObject container;
            public Text textComponent;
            public CanvasGroup canvasGroup;
            public RectTransform rectTransform;
        }

        public static void Initialize()
        {
            if (_instance != null) return;

            var obj = new GameObject("ArchipelagoNotificationManager");
            _instance = obj.AddComponent<ArchipelagoNotificationUi>();
            DontDestroyOnLoad(obj);

            CreateNotificationCanvas();
        }

        private static void CreateNotificationCanvas()
        {
            if (_notificationCanvas != null) return;

            // Create canvas
            var canvasObj = new GameObject("ArchipelagoNotificationCanvas");
            _notificationCanvas = canvasObj.AddComponent<Canvas>();
            _notificationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _notificationCanvas.sortingOrder = 9999;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasObj);

            // Create background panel
            _panel = new GameObject("NotificationPanel");
            _panel.transform.SetParent(canvasObj.transform);

            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.2f, 0.7f);

            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.sizeDelta = PANEL_SIZE;
            panelRect.anchoredPosition = PANEL_POSITION;

            // Add panel canvas group for fading
            _panelCanvasGroup = _panel.AddComponent<CanvasGroup>();
            _panelCanvasGroup.alpha = 0f;

            // Create content area
            var content = new GameObject("Content");
            content.transform.SetParent(_panel.transform);

            _contentPanel = content.AddComponent<RectTransform>();
            _contentPanel.anchorMin = new Vector2(0, 0);
            _contentPanel.anchorMax = new Vector2(1, 1);
            _contentPanel.sizeDelta = new Vector2(-20, -20);
            _contentPanel.anchoredPosition = Vector2.zero;
            _contentPanel.pivot = new Vector2(0, 0); // Critical for bottom-up growth

            // Add vertical layout group with bottom alignment
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = MESSAGE_SPACING;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        public static void ShowMessage(string message)
        {
            if (_instance == null)
            {
                WinchCore.Log.Error("NotificationUI not initialized!");
                return;
            }

            // Fade in panel if first message
            if (_activeMessages.Count == 0)
            {
                _panelCanvasGroup.DOFade(1f, 0.3f);
            }

            // Create new message object
            var messageObj = new GameObject("NotificationMessage");
            messageObj.transform.SetParent(_contentPanel);
            messageObj.transform.SetAsFirstSibling(); // Add to top of hierarchy

            var messageRect = messageObj.AddComponent<RectTransform>();

            var text = messageObj.AddComponent<Text>();
            text.text = $"• {message}";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 14;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var canvasGroup = messageObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            // Calculate preferred height after text is set
            Canvas.ForceUpdateCanvases();
            messageRect.sizeDelta = new Vector2(PANEL_SIZE.x - 40, text.preferredHeight);

            // Store reference
            var notification = new NotificationMessage
            {
                container = messageObj,
                textComponent = text,
                canvasGroup = canvasGroup,
                rectTransform = messageRect
            };
            _activeMessages.Insert(0, notification);

            // Start fade out sequence
            _instance.StartCoroutine(MessageLifecycle(notification));

            // Trim oldest message if needed
            if (_activeMessages.Count > MAX_MESSAGES)
            {
                var oldest = _activeMessages[_activeMessages.Count - 1];
                _activeMessages.RemoveAt(_activeMessages.Count - 1);
                Destroy(oldest.container);
            }

            // Force immediate layout update
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentPanel);
        }

        private static IEnumerator MessageLifecycle(NotificationMessage message)
        {
            // Wait for duration
            yield return new WaitForSecondsRealtime(MESSAGE_DURATION - FADE_DURATION);

            // Fade out message
            if (message.canvasGroup != null)
            {
                message.canvasGroup.DOFade(0f, FADE_DURATION)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true);

                yield return new WaitForSecondsRealtime(FADE_DURATION);
            }

            // Remove from active messages and destroy
            _activeMessages.Remove(message);
            if (message.container != null)
            {
                Destroy(message.container);
            }

            // Fade out panel if no more messages
            if (_activeMessages.Count == 0)
            {
                _panelCanvasGroup.DOFade(0f, FADE_DURATION);
            }
            else
            {
                // Update layout after removal
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentPanel);
            }
        }
    }
}