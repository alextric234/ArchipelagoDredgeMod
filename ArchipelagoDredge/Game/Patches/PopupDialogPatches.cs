using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Utils;
using TMPro;
using Winch.Core;

namespace ArchipelagoDredge.Game.Patches;

[HarmonyPatch(typeof(PopupDialog))]
internal static class PopupDialogPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PopupDialog.Show),
    new[] {typeof(DialogOptions), typeof(Action<DialogButtonOptions>)})]
    private static void Show_DeathLinkPrefix(
        DialogOptions dialogOptions,
        out string __state)
    {
        __state = null;

        if (!dialogOptions.useDeathScreenPopup ||
            !DeathLinkManager.IsDeathLinkEnabled ||
            !DeathLinkManager.TryConsumePendingDeathLink(out string deathLinkMessage))
        {
            return;
        }

        __state = deathLinkMessage;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(PopupDialog.Show))]
    private static void Show_DeathLinkPostfix(
        PopupDialog __instance,
        string __state)
    {
        if (string.IsNullOrWhiteSpace(__state))
            return;

        TMP_Text deathScreenText = __instance
            .GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => text.name == "Text");

        if (deathScreenText == null)
        {
            WinchCore.Log.Error("Could not find the death-screen text element.");
            return;
        }

        deathScreenText.text =
            "Your fate is linked to another.\n\n" +
            __state;
    }

    [HarmonyPrefix]
    [HarmonyPatch(
        "OnButtonPressComplete",
        new Type[] { typeof(int) })]
    private static void OnButtonPressComplete_DeathScreenPatch(
        int id,
        DialogOptions ___dialogOptions)
    {
        if (!___dialogOptions.useDeathScreenPopup)
            return;

        DialogButtonOptions selectedButton =
            ___dialogOptions.buttonOptions[id];

        switch (selectedButton.id)
        {
            case 0: // Load Last Savee

                ArchipelagoStateManager.RevertStateData();
                ArchipelagoStateManager.AwaitingDeathScreenChoice = false;
                break;

            case 1: // Return to Menu

                ArchipelagoStateManager.AwaitingDeathScreenChoice = false;
                ArchipelagoCommandManager.Disconnect();
                break;
        }
    }
}