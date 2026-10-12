using System;
using System.Linq;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;

/// <summary>
///     The minimum aberration chance granted by Progressive Aberration Chance and Guaranteed Aberrations items.
///     Players can switch the boost off per save; the mod menu toggle mirrors the loaded save's setting.
/// </summary>
public static class AberrationChanceManager
{
    public static void Initialize()
    {
        try
        {
            ApConfigHelper.Config.OnConfigValueChanged += OnConfigValueChanged;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error($"Couldn't watch the boosted aberrations setting: {e}");
        }
    }

    public static bool IsEnabled => ArchipelagoStateManager.StateData?.BoostedAberrationsEnabled ?? true;

    public static void SetEnabled(bool enabled)
    {
        ArchipelagoStateManager.SetBoostedAberrationsEnabled(enabled);
        if (ApConfigHelper.ReadBoostedAberrations() != enabled)
        {
            ApConfigHelper.SaveBoostedAberrationsValue(enabled);
        }
    }

    /// <summary>Shows the loaded save's setting in the mod menu.</summary>
    public static void SyncConfigFromSave()
    {
        if (ApConfigHelper.ReadBoostedAberrations() != IsEnabled)
        {
            ApConfigHelper.SaveBoostedAberrationsValue(IsEnabled);
        }
    }

    /// <summary>The chance the items grant, whether or not the boost is switched on.</summary>
    public static float GrantedChance
    {
        get
        {
            if (!ApSave.IsActive)
            {
                return 0f;
            }

            var receivedNames = ApSave.ReceivedItemNames;
            if (receivedNames.Contains(ItemNames.Names[Item.GUARANTEED_ABERRATIONS]))
            {
                return 1f;
            }

            var total = ArchipelagoClient.SlotData.ProgressiveAberrationChanceItems;
            if (total <= 0)
            {
                return 0f;
            }

            var progressiveName = ItemNames.Names[Item.PROGRESSIVE_ABERRATION_CHANCE];
            var count = receivedNames.Count(name => name == progressiveName);
            return Math.Min(1f, (float) count / total);
        }
    }

    public static float MinimumChance => IsEnabled ? GrantedChance : 0f;

    private static void OnConfigValueChanged(string key)
    {
        if (key != ApConfigHelper.BoostedAberrationsKey || ArchipelagoStateManager.StateData == null)
        {
            return;
        }

        var enabled = ApConfigHelper.ReadBoostedAberrations();
        if (enabled != IsEnabled)
        {
            ArchipelagoStateManager.SetBoostedAberrationsEnabled(enabled);
        }
    }
}
