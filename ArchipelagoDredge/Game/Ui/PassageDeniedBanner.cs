using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Winch.Core;

internal static class PassageDeniedBanner
{
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

    /// <summary>
    /// Returns false when DREDGE is already showing a normal banner.
    /// In that case, do not overwrite it.
    /// </summary>
    public static bool TryShow(BannersUI bannersUi, string passageName)
    {
        if ((UnityEngine.Object)bannersUi == null)
        {
            WinchCore.Log.Warn("Cannot show passage warning: BannersUI is null.");
            return false;
        }

        var bannerUi = BannerUiField(bannersUi);

        if ((UnityEngine.Object)bannerUi == null ||
            bannerUi.isShowing ||
            bannerUi.isHiding)
        {
            return false;
        }

        var titleLocalized = TitleLocalizedField(bannerUi);
        var subtitleLocalized = SubtitleLocalizedField(bannerUi);

        // Disable the existing localization event before assigning raw prototype text.
        // Replace these with your own localized string references later.
        titleLocalized.enabled = false;
        subtitleLocalized.enabled = false;

        var titleText = TitleTextField(bannerUi);
        titleText.color = GameManager.Instance.LanguageManager.GetColor(
            DredgeColorTypeEnum.WARNING);
        titleText.text = "THE CURRENT REJECTS YOU";

        var subtitleText = SubtitleTextField(bannerUi);
        subtitleText.text = $"Requires: {passageName}";

        var image = ImageField(bannerUi);
        image.sprite = BookSpriteField(bannerUi);
        image.transform.localScale = Vector3.one;

        bannerUi.isShowing = true;
        AnimatorField(bannerUi).SetBool("showing", true);

        // BannersUI.Update() now owns the normal hide animation.
        HoldTimeField(bannersUi) = 4f;

        return true;
    }
}