using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using ArchipelagoDredge.Utils;

namespace ArchipelagoDredge.UI
{
    public class ArchipelagoNotificationUi : MonoBehaviour
    {
        private static GameObject notificationPanel;
        private static Text notificationText;
        private static Coroutine currentCoroutine;

        public static void Initialize()
        {
            if (notificationPanel != null) return;

            notificationPanel = new GameObject("NotificationPanel");
            var canvas = notificationPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Make sure it's on top

            var panel = new GameObject("Panel");
            panel.transform.SetParent(notificationPanel.transform);
            // Setup panel background, text, etc.
        }

        public static void ShowMessage(string message, float duration = 3f)
        {
            if (currentCoroutine != null)
            {
                MonoBehaviourInstance.Instance.StopCoroutine(currentCoroutine);
            }

            notificationText.text = message;
            notificationPanel.SetActive(true);
            currentCoroutine = MonoBehaviourInstance.Instance.StartCoroutine(HideAfterDelay(duration));
        }

        private static IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            notificationPanel.SetActive(false);
        }
    }
}