using System;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Winch.Core;
using Winch.Util;
using Object = UnityEngine.Object;

namespace ArchipelagoDredge.Game.Ui;

/// <summary>
///     Adds an Archipelago tab to the journal, after the pursuit tabs, built from clones of the journal's own tab and
///     pursuit card so it matches the game's look.
/// </summary>
[HarmonyPatch(typeof(JournalWindow))]
internal static class TrackerTab
{
    public const string TitleKey = "alextric234.archipelagodredge.tracker.tab";

    private static readonly AccessTools.FieldRef<JournalWindow, TabbedPanelContainer> TabbedPanelField =
        AccessTools.FieldRefAccess<JournalWindow, TabbedPanelContainer>("tabbedPanel");

    private static readonly AccessTools.FieldRef<TabUI, TabShowQuery> TabShowQueryField =
        AccessTools.FieldRefAccess<TabUI, TabShowQuery>("tabShowQuery");

    private static readonly AccessTools.FieldRef<QuestEntryUI, Color> CompletedHeaderColorField =
        AccessTools.FieldRefAccess<QuestEntryUI, Color>("completedHeaderBackplateColor");

    private static readonly AccessTools.FieldRef<QuestEntryUI, Color> CompletedStepColorField =
        AccessTools.FieldRefAccess<QuestEntryUI, Color>("completedActiveStepBackplateColor");

    private static readonly AccessTools.FieldRef<QuestEntryUI, Color> CompletedTextColorField =
        AccessTools.FieldRefAccess<QuestEntryUI, Color>("completedTextColor");

    // The tab list must be extended before TabbedPanelContainer.Init wires up the tab buttons, which happens the first
    // time the journal opens.
    [HarmonyPrefix]
    [HarmonyPatch("Awake")]
    private static void Awake_Prefix(JournalWindow __instance)
    {
        try
        {
            Inject(__instance);
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Couldn't add the Archipelago journal tab: {e}");
        }
    }

    private static void Inject(JournalWindow journal)
    {
        var tabbedPanel = TabbedPanelField(journal);
        var tabs = tabbedPanel.TabbedPanels;
        var source = tabs[0];

        var tabObject = Object.Instantiate(source.tab.gameObject, source.tab.transform.parent, false);
        tabObject.name = "ArchipelagoTab";
        var tab = tabObject.GetComponent<TabUI>();
        Object.DestroyImmediate(tabObject.GetComponent<TabShowQuery>());
        TabShowQueryField(tab) = tabObject.AddComponent<TrackerTabShowQuery>();
        var tabTitle = tab.TabLocalizedString != null
            ? tab.TabLocalizedString
            : tabObject.GetComponentInChildren<LocalizeStringEvent>(true);
        tabTitle.StringReference = LocalizationUtil.CreateStringsReference(TitleKey);

        var panelObject = new GameObject("ArchipelagoPanel", typeof(RectTransform));
        panelObject.transform.SetParent(source.panel.transform.parent, false);
        Stretch(panelObject.transform as RectTransform);

        var sourceContainer = source.panel.transform.Find("Container").gameObject;
        var container = Object.Instantiate(sourceContainer, panelObject.transform, false);
        container.name = "Container";
        foreach (Transform child in container.transform.Cast<Transform>().ToList())
        {
            Object.DestroyImmediate(child.gameObject);
        }

        container.SetActive(false);

        var card = source.panel.GetComponentsInChildren<QuestEntryUI>(true).First();
        var style = new TrackerStyle
        {
            CompletedHeaderColor = CompletedHeaderColorField(card),
            CompletedStepColor = CompletedStepColorField(card),
            CompletedTextColor = CompletedTextColorField(card)
        };

        var prefabs = new GameObject("Prefabs", typeof(RectTransform));
        prefabs.transform.SetParent(panelObject.transform, false);
        prefabs.SetActive(false);
        style.CardPrefab = MakeCardPrefab(card, prefabs.transform);
        style.TextPrefab = MakeTextPrefab(style.CardPrefab.GetComponentsInChildren<TextMeshProUGUI>(true).Last(),
            prefabs.transform);

        var panel = panelObject.AddComponent<TrackerPanel>();
        var tabIndex = tabs.Count;
        panel.Init(container, journal, tabbedPanel, tabIndex, style);

        tabs.Add(new TabConfig { panel = panel, tab = tab });
        tabbedPanel.showablePanelIndexes.Add(tabIndex);
    }

    private static GameObject MakeCardPrefab(QuestEntryUI source, Transform parent)
    {
        var card = Object.Instantiate(source.gameObject, parent, false);
        card.name = "Card";
        Object.DestroyImmediate(card.GetComponent<QuestEntryUI>());
        Object.DestroyImmediate(card.GetComponent<DredgeStringLookupHelper>());
        foreach (var localized in card.GetComponentsInChildren<LocalizeStringEvent>(true))
        {
            Object.DestroyImmediate(localized);
        }

        card.transform.localRotation = Quaternion.identity;
        card.transform.localScale = Vector3.one;
        foreach (Transform child in card.transform)
        {
            child.gameObject.SetActive(true);
        }

        return card;
    }

    private static TextMeshProUGUI MakeTextPrefab(TextMeshProUGUI source, Transform parent)
    {
        var text = Object.Instantiate(source.gameObject, parent, false).GetComponent<TextMeshProUGUI>();
        text.name = "Text";
        text.transform.localRotation = Quaternion.identity;
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        return text;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

internal class TrackerStyle
{
    public GameObject CardPrefab;
    public TextMeshProUGUI TextPrefab;
    public Color CompletedHeaderColor;
    public Color CompletedStepColor;
    public Color CompletedTextColor;
}

public class TrackerTabShowQuery : TabShowQuery
{
    public override bool GetCanNavigate() => Utils.ApSave.IsActive;

    public override bool GetCanShow() => Utils.ApSave.IsActive;
}
