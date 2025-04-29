using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ArchipelagoDredge.Ui;

public class ArchipelagoNotificationUi : MonoBehaviour
{
    private static ArchipelagoNotificationUi _instance;

    private Text _text;
    private GameObject _panelObj;
    private float _timeSinceLastMessage = 0f;
    private bool _fading = false;

    private const float VisibleDuration = 10f; // Seconds before fade starts
    private const float FadeDuration = 2f;

    public static void Initialize()
    {
        if (_instance != null) return;

        GameObject obj = new GameObject("ArchipelagoNotificationUi");
        _instance = obj.AddComponent<ArchipelagoNotificationUi>();
        DontDestroyOnLoad(obj);
    }

    private void Awake()
    {
        CreateCanvasAndPanel();
    }

    private void CreateCanvasAndPanel()
    {
        // Canvas
        GameObject canvasObj = new GameObject("SimpleCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObj);

        // Panel
        _panelObj = new GameObject("Panel");
        _panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = _panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(200, 10);
        panelRect.sizeDelta = new Vector2(600, 200); 

        Image panelImage = _panelObj.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.5f);
        panelImage.raycastTarget = true;

        _panelObj.AddComponent<ArchipelagoNotificationUiDragHandler>();

        // Text
        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(_panelObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        _text = textObj.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _text.fontSize = 14;
        _text.color = Color.white;
        _text.alignment = TextAnchor.LowerLeft;
        _text.horizontalOverflow = HorizontalWrapMode.Wrap;
        _text.verticalOverflow = VerticalWrapMode.Overflow;
        _text.text = "";

        _text.raycastTarget = false;
    }

    private void Update()
    {
        if (_text == null || _panelObj == null)
            return;

        if (!_fading && _panelObj.activeSelf)
        {
            _timeSinceLastMessage += Time.deltaTime;

            if (_timeSinceLastMessage >= VisibleDuration)
            {
                StartCoroutine(FadeOutAndHide());
                _fading = true;
            }
        }
    }

    public static void ShowMessage(string message)
    {
        if (_instance == null)
        {
            Debug.LogWarning("ArchipelagoNotificationUi not initialized.");
            return;
        }

        _instance._text.text += message + "\n";
        _instance._text.color = Color.white;
        _instance._timeSinceLastMessage = 0f;
        _instance._fading = false;
        _instance._panelObj.SetActive(true); 
    }

    private System.Collections.IEnumerator FadeOutAndHide()
    {
        float elapsed = 0f;
        Color originalColor = _text.color;

        while (elapsed < FadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / FadeDuration);
            _text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        _text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        _panelObj.SetActive(false); 
    }
}

public class ArchipelagoNotificationUiDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private Vector2 _dragOffset;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out _dragOffset);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            _rectTransform.localPosition = localPoint - _dragOffset;
        }
    }
}