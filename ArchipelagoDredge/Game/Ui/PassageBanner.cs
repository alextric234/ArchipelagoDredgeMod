using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Winch.Core;

namespace ArchipelagoDredge.Game.Ui;

internal static class PassageBanner
{
    private const float BannerHoldSeconds = 4f;

    private static readonly AccessTools.FieldRef<BannersUI, BannerUI>
        BannerUiField =
            AccessTools.FieldRefAccess<BannersUI, BannerUI>("bannerUI");

    private static readonly AccessTools.FieldRef<BannersUI, float>
        HoldTimeField =
            AccessTools.FieldRefAccess<BannersUI, float>("timeUntilHideBanner");

    private static readonly AccessTools.FieldRef<BannerUI, TextMeshProUGUI>
        TitleTextField =
            AccessTools.FieldRefAccess<BannerUI, TextMeshProUGUI>("titleText");

    private static readonly AccessTools.FieldRef<BannerUI, LocalizeStringEvent>
        TitleLocalizedField =
            AccessTools.FieldRefAccess<BannerUI, LocalizeStringEvent>(
                "titleTextLocalized");

    private static readonly AccessTools.FieldRef<BannerUI, TextMeshProUGUI>
        SubtitleTextField =
            AccessTools.FieldRefAccess<BannerUI, TextMeshProUGUI>("subtitleText");

    private static readonly AccessTools.FieldRef<BannerUI, LocalizeStringEvent>
        SubtitleLocalizedField =
            AccessTools.FieldRefAccess<BannerUI, LocalizeStringEvent>(
                "subtitleTextLocalized");

    private static readonly AccessTools.FieldRef<BannerUI, Sprite>
        BookSpriteField =
            AccessTools.FieldRefAccess<BannerUI, Sprite>("bookSprite");

    private static readonly AccessTools.FieldRef<BannerUI, Image>
        ImageField =
            AccessTools.FieldRefAccess<BannerUI, Image>("image");

    private static readonly AccessTools.FieldRef<BannerUI, Animator>
        AnimatorField =
            AccessTools.FieldRefAccess<BannerUI, Animator>("animator");

    public static bool TryShow(BannersUI bannersUi, string titleText, string subtitleText, DredgeColorTypeEnum color)
    {
        if ((Object)bannersUi == null)
        {
            WinchCore.Log.Warn("Cannot show passage warning: BannersUI is null.");
            return false;
        }

        var bannerUi = BannerUiField(bannersUi);

        if ((Object)bannerUi == null ||
            bannerUi.isShowing ||
            bannerUi.isHiding)
        {
            return false;
        }

        var titleLocalized = TitleLocalizedField(bannerUi);
        var subtitleLocalized = SubtitleLocalizedField(bannerUi);

        titleLocalized.enabled = false;
        subtitleLocalized.enabled = false;

        var titleTextField = TitleTextField(bannerUi);
        titleTextField.color = GameManager.Instance.LanguageManager.GetColor(color);
        titleTextField.text = titleText;

        var subtitleTextField = SubtitleTextField(bannerUi);
        subtitleTextField.text = subtitleText;

        var image = ImageField(bannerUi);
        image.sprite = BookSpriteField(bannerUi);
        image.transform.localScale = Vector3.one;

        bannerUi.isShowing = true;
        AnimatorField(bannerUi).SetBool("showing", true);

        HoldTimeField(bannersUi) = BannerHoldSeconds;

        return true;
    }
}