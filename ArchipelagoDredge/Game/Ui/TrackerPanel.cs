using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Game.Tracker;
using ArchipelagoDredge.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArchipelagoDredge.Game.Ui;

/// <summary>
///     The journal's Archipelago tab: the slot's locations grouped by region with their logic status and hints, and
///     buttons to pin a location's spots on the map or open its encyclopedia page.
/// </summary>
internal class TrackerPanel : TabbedPanel
{
    private enum Filter
    {
        InLogic,
        Unchecked,
        All
    }

    private const float RowHeight = 90f;
    private const float PollSeconds = 1f;

    private JournalWindow journal;
    private TabbedPanelContainer tabbedPanel;
    private int tabIndex;
    private TrackerStyle style;
    private ControllerFocusGrabber focusGrabber;

    private ScrollRect scroll;
    private RectTransform listContent;
    private TextMeshProUGUI summaryText;
    private Card filterButton;
    private TextMeshProUGUI detailTitle;
    private TextMeshProUGUI detailBody;
    private Card pinButton;
    private Card encyclopediaButton;

    private readonly List<Card> rows = [];
    private IReadOnlyList<TrackerEntry> shownFrom;
    private Filter filter = Filter.InLogic;
    private string selectedName;
    private string actionMessage;
    private float nextPoll;

    internal void Init(GameObject container, JournalWindow journal, TabbedPanelContainer tabbedPanel, int tabIndex,
        TrackerStyle style)
    {
        this.container = container;
        this.journal = journal;
        this.tabbedPanel = tabbedPanel;
        this.tabIndex = tabIndex;
        this.style = style;
        focusGrabber = journal.GetComponent<ControllerFocusGrabber>();
        Build();
    }

    public override void ShowStart()
    {
        base.ShowStart();
        Rebuild(force: true);
        Focus();
    }

    private void Update()
    {
        if (!IsShowing() || Time.unscaledTime < nextPoll)
        {
            return;
        }

        nextPoll = Time.unscaledTime + PollSeconds;
        Rebuild(force: false);
    }

    private void Build()
    {
        var root = container.transform;

        var list = NewRect("List", root, new Vector2(0, 0), new Vector2(0.6f, 1), new Vector2(40, 40), new Vector2(-20, -90));
        filterButton = Card.Create(style, list);
        Place(filterButton.Rect, new Vector2(0, 1), Vector2.zero, new Vector2(380, 70));
        filterButton.Button.OnClick += CycleFilter;

        summaryText = NewText(list, 26);
        Place(summaryText.rectTransform, new Vector2(0, 1), new Vector2(400, 0), new Vector2(560, 70));
        summaryText.rectTransform.anchorMax = new Vector2(1, 1);
        summaryText.rectTransform.offsetMax = new Vector2(0, summaryText.rectTransform.offsetMax.y);
        summaryText.alignment = TextAlignmentOptions.Left;

        var scrollRect = NewRect("Scroll", list, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -90));
        scrollRect.gameObject.AddComponent<Image>().color = Color.clear;
        scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        var viewport = NewRect("Viewport", scrollRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        // A stencil mask, as the encyclopedia uses: RectMask2D doesn't clip the card graphics.
        viewport.gameObject.AddComponent<Image>();
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        listContent = NewRect("Content", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        listContent.pivot = new Vector2(0.5f, 1);
        var layout = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 15, 10);
        listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = listContent;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40;

        var details = NewRect("Details", root, new Vector2(0.6f, 0), new Vector2(1, 1), new Vector2(20, 40), new Vector2(-40, -90));
        detailTitle = NewText(details, 34);
        Place(detailTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 110));
        StretchWidth(detailTitle.rectTransform);
        detailBody = NewText(details, 26);
        Place(detailBody.rectTransform, new Vector2(0, 1), new Vector2(0, -120), new Vector2(0, 340));
        StretchWidth(detailBody.rectTransform);
        detailBody.alignment = TextAlignmentOptions.TopLeft;

        pinButton = Card.Create(style, details);
        Place(pinButton.Rect, new Vector2(0, 1), new Vector2(0, -480), new Vector2(0, RowHeight));
        StretchWidth(pinButton.Rect);
        pinButton.Button.OnClick += TogglePins;

        encyclopediaButton = Card.Create(style, details);
        Place(encyclopediaButton.Rect, new Vector2(0, 1), new Vector2(0, -590), new Vector2(0, RowHeight));
        StretchWidth(encyclopediaButton.Rect);
        encyclopediaButton.Button.OnClick += OpenEncyclopedia;
    }

    private void Rebuild(bool force)
    {
        var entries = TrackerService.Entries();
        if (!force && ReferenceEquals(entries, shownFrom))
        {
            return;
        }

        shownFrom = entries;
        filterButton.SetText(filter switch
        {
            Filter.InLogic => "Showing: In logic",
            Filter.Unchecked => "Showing: Unchecked",
            _ => "Showing: All"
        }, "Change filter");

        foreach (Transform child in listContent.Cast<Transform>().ToList())
        {
            DestroyImmediate(child.gameObject);
        }

        rows.Clear();

        if (entries == null)
        {
            summaryText.text = "Connect to the Archipelago server to see your locations.";
            RefreshDetails();
            return;
        }

        var inLogic = entries.Count(e => e.Status == TrackerStatus.InLogic);
        var uncheckedCount = entries.Count(e => e.Status != TrackerStatus.Checked);
        summaryText.text = $"{inLogic} in logic, {uncheckedCount} unchecked, {entries.Count - uncheckedCount}/{entries.Count} checked";

        var shown = entries.Where(e => filter switch
        {
            Filter.InLogic => e.Status == TrackerStatus.InLogic,
            Filter.Unchecked => e.Status != TrackerStatus.Checked,
            _ => true
        }).ToList();

        foreach (var group in shown.GroupBy(e => e.Group))
        {
            var groupEntries = entries.Where(e => e.Group == group.Key).ToList();
            var header = NewText(listContent, 30);
            header.text = $"{group.Key}  ({groupEntries.Count(e => e.Status == TrackerStatus.InLogic)} in logic, " +
                          $"{groupEntries.Count(e => e.Status != TrackerStatus.Checked)} unchecked)";
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;

            foreach (var entry in group)
            {
                AddRow(entry);
            }
        }

        if (shown.Count == 0)
        {
            var empty = NewText(listContent, 26);
            empty.text = filter == Filter.InLogic
                ? "Nothing in logic right now. Switch the filter to see everything else."
                : "Nothing to show.";
            empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
        }

        RefreshDetails();
    }

    private void AddRow(TrackerEntry entry)
    {
        var row = Card.Create(style, listContent);
        row.Root.AddComponent<LayoutElement>().preferredHeight = RowHeight;
        row.SetText(entry.Name, Subtitle(entry));
        row.SetCompleted(entry.Status == TrackerStatus.Checked, style);
        row.SetDimmed(entry.Status == TrackerStatus.OutOfLogic);
        row.Button.gameObject.AddComponent<ScrollIntoViewOnSelect>().Init(scroll, row.Rect);
        var name = entry.Name;
        row.Button.OnClick += () => Select(name);
        row.Button.OnSelectAction += () => Select(name);
        rows.Add(row);
    }

    private static string Subtitle(TrackerEntry entry)
    {
        var status = entry.Status switch
        {
            TrackerStatus.InLogic => "In logic",
            TrackerStatus.OutOfLogic => "Out of logic",
            _ => "Checked"
        };
        return entry.HintText == null ? status : $"{status} - {entry.HintText}";
    }

    private void Select(string name)
    {
        if (selectedName == name)
        {
            return;
        }

        selectedName = name;
        actionMessage = null;
        RefreshDetails();
    }

    private TrackerEntry Selected => shownFrom?.FirstOrDefault(e => e.Name == selectedName);

    private void RefreshDetails()
    {
        var entry = Selected;
        if (entry == null)
        {
            detailTitle.text = "";
            detailBody.text = shownFrom == null ? "" : "Select a location to see more.";
            pinButton.Root.SetActive(false);
            encyclopediaButton.Root.SetActive(false);
            return;
        }

        detailTitle.text = entry.Name;
        var lines = new List<string>
        {
            entry.LocationGroup switch
            {
                "Dredge" => "Dredge spot",
                "Encyclopedia" => "Fish",
                _ => entry.LocationGroup
            } + $" in {entry.Region}",
            entry.Status switch
            {
                TrackerStatus.InLogic => "In logic",
                TrackerStatus.OutOfLogic => "Out of logic: you're missing something it needs",
                _ => "Checked"
            }
        };
        if (entry.HintText != null)
        {
            lines.Add($"Hint: {entry.HintText}");
        }

        if (actionMessage != null)
        {
            lines.Add("");
            lines.Add(actionMessage);
        }

        detailBody.text = string.Join("\n", lines);

        var spots = entry.Status == TrackerStatus.Checked ? [] : TrackerTargets.GetSpots(entry);
        pinButton.Root.SetActive(spots.Count > 0);
        if (spots.Count > 0)
        {
            var allPinned = TrackerTargets.AreAllPinned(spots);
            pinButton.SetText(allPinned ? "Unpin from map" : "Pin on map",
                spots.Count == 1 ? "1 spot" : $"{spots.Count} spots");
        }

        var fish = TrackerTargets.GetFish(entry);
        encyclopediaButton.Root.SetActive(fish != null);
        encyclopediaButton.SetText("Open in encyclopedia", "Turn to its page");
    }

    private void CycleFilter()
    {
        filter = (Filter) (((int) filter + 1) % 3);
        Rebuild(force: true);
        scroll.verticalNormalizedPosition = 1;
    }

    private void TogglePins()
    {
        var entry = Selected;
        if (entry == null)
        {
            return;
        }

        actionMessage = TrackerTargets.TogglePins(TrackerTargets.GetSpots(entry));
        RefreshDetails();
    }

    private void OpenEncyclopedia()
    {
        var entry = Selected;
        var fish = entry == null ? null : TrackerTargets.GetFish(entry);
        if (fish == null)
        {
            return;
        }

        // The encyclopedia opens on the last unseen catch, so this is how to turn it to a page.
        GameManager.Instance.SaveData.LastUnseenCaughtSpecies = fish.id;
        var encyclopedia = GameManager.Instance.UI.EncyclopediaWindow;
        journal.Hide(PopupWindow.WindowHideMode.SWITCH);
        encyclopedia.OnHideComplete += OnEncyclopediaClosed;
        encyclopedia.Show();
    }

    private void OnEncyclopediaClosed()
    {
        GameManager.Instance.UI.EncyclopediaWindow.OnHideComplete -= OnEncyclopediaClosed;
        // Wait a frame so the back press that closed the encyclopedia doesn't also close the journal.
        MainThreadDispatcher.Enqueue(() =>
        {
            if (journal == null || journal.IsShowing)
            {
                return;
            }

            journal.Show();
            tabbedPanel.ShowNewPanel(tabIndex, showImmediate: true);
        });
    }

    private void Focus()
    {
        var target = rows.FirstOrDefault(r => r.Name == selectedName) ?? rows.FirstOrDefault() ?? filterButton;
        focusGrabber.SetSelectable(target.Button.Button);
        focusGrabber.SelectSelectable();
    }

    private TextMeshProUGUI NewText(Transform parent, float fontSize)
    {
        var text = Instantiate(style.TextPrefab, parent, false);
        text.gameObject.SetActive(true);
        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Left;
        text.text = "";
        return text;
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    // Anchors to the given corner with the pivot there too, so position is the offset from that corner.
    private static void Place(RectTransform rect, Vector2 corner, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = corner;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void StretchWidth(RectTransform rect)
    {
        var y = rect.anchoredPosition.y;
        var height = rect.sizeDelta.y;
        rect.anchorMin = new Vector2(0, rect.anchorMin.y);
        rect.anchorMax = new Vector2(1, rect.anchorMax.y);
        rect.sizeDelta = new Vector2(0, height);
        rect.anchoredPosition = new Vector2(0, y);
    }

    /// <summary>A clone of the journal's pursuit card, used for list rows and buttons.</summary>
    private class Card
    {
        public GameObject Root;
        public RectTransform Rect;
        public BasicButtonWrapper Button;
        public string Name;
        private TextMeshProUGUI title;
        private TextMeshProUGUI subtitle;
        private Image tick;
        private Image header;
        private Image step;
        private Color headerColor;
        private Color stepColor;
        private Color titleColor;
        private Color subtitleColor;

        public static Card Create(TrackerStyle style, Transform parent)
        {
            var root = Instantiate(style.CardPrefab, parent, false);
            root.SetActive(true);
            var t = root.transform;
            var card = new Card
            {
                Root = root,
                Rect = (RectTransform) t,
                Button = t.Find("Container").GetComponent<BasicButtonWrapper>(),
                step = t.Find("Container/ActiveStepContainer").GetComponent<Image>(),
                header = t.Find("Container/ActiveStepContainer/TitleContainer").GetComponent<Image>(),
                title = t.Find("Container/ActiveStepContainer/TitleContainer/TitleText").GetComponent<TextMeshProUGUI>(),
                tick = t.Find("Container/ActiveStepContainer/TitleContainer/TickIcon").GetComponent<Image>(),
                subtitle = t.Find("Container/ActiveStepContainer/ActiveStepShortText").GetComponent<TextMeshProUGUI>()
            };
            card.headerColor = card.header.color;
            card.stepColor = card.step.color;
            card.titleColor = card.title.color;
            card.subtitleColor = card.subtitle.color;
            foreach (var text in new[] { card.title, card.subtitle })
            {
                text.enableAutoSizing = false;
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }

            card.title.fontSize = 28;
            card.subtitle.fontSize = 24;
            card.tick.enabled = false;
            return card;
        }

        public void SetText(string titleText, string subtitleText)
        {
            Name = titleText;
            title.text = titleText;
            subtitle.text = subtitleText ?? "";
        }

        public void SetCompleted(bool completed, TrackerStyle style)
        {
            tick.enabled = completed;
            header.color = completed ? style.CompletedHeaderColor : headerColor;
            step.color = completed ? style.CompletedStepColor : stepColor;
            title.color = completed ? style.CompletedTextColor : titleColor;
            subtitle.color = completed ? style.CompletedTextColor : subtitleColor;
        }

        public void SetDimmed(bool dimmed)
        {
            var alpha = dimmed ? 0.55f : 1f;
            title.alpha = alpha;
            subtitle.alpha = alpha;
        }
    }

    /// <summary>
    ///     Scrolls the list just enough to show a row picked with a controller, leaving room above for its region
    ///     header. The game's ScrollRectMagnet centres on the scroll view's pivot, which doesn't suit a top-anchored list.
    /// </summary>
    private class ScrollIntoViewOnSelect : MonoBehaviour, ISelectHandler
    {
        private const float TopMargin = 60f;
        private const float BottomMargin = 10f;

        private ScrollRect scroll;
        private RectTransform row;

        public void Init(ScrollRect scroll, RectTransform row)
        {
            this.scroll = scroll;
            this.row = row;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!GameManager.Instance.Input.IsUsingController)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            var viewport = scroll.viewport;
            var corners = new Vector3[4];
            row.GetWorldCorners(corners);
            var bottom = viewport.InverseTransformPoint(corners[0]).y;
            var top = viewport.InverseTransformPoint(corners[1]).y;
            var view = viewport.rect;

            var delta = 0f;
            if (top > view.yMax - TopMargin)
            {
                delta = top - (view.yMax - TopMargin);
            }
            else if (bottom < view.yMin + BottomMargin)
            {
                delta = bottom - (view.yMin + BottomMargin);
            }

            var content = scroll.content;
            var maxScroll = Mathf.Max(0, content.rect.height - view.height);
            var position = content.anchoredPosition;
            position.y = Mathf.Clamp(position.y - delta, 0, maxScroll);
            scroll.StopMovement();
            content.anchoredPosition = position;
        }
    }
}
