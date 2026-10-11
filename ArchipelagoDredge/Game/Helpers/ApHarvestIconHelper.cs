using System.Collections.Generic;
using UnityEngine;
using Winch.Util;

namespace ArchipelagoDredge.Game.Helpers;

/// <summary>
///     Picks the AP icon to show for a harvest spot, and caches item copies that carry that icon.
/// </summary>
public static class ApHarvestIconHelper
{
    private const string ApIcon = "archipelago_icon";
    private const string AberrationApIcon = "aberration_archipelago_icon";

    private static readonly Dictionary<(string itemId, string iconName), HarvestableItemData> IconItemCopies = new();

    /// <summary>
    ///     The icon sprite name for this spot, or null when it has nothing left to check.
    /// </summary>
    /// <param name="item">The item the spot will give.</param>
    /// <param name="spotLocationId">The spot's id, used for non-fish spots; null if the spot can't be an AP location.</param>
    public static string GetIconName(HarvestableItemData item, string spotLocationId)
    {
        if (item == null)
        {
            return null;
        }

        if (item.itemSubtype == ItemSubtype.FISH)
        {
            if (IsUncheckedApLocation(item.id))
            {
                return ApIcon;
            }

            if (item is FishItemData fish && fish.Aberrations != null)
            {
                foreach (var aberration in fish.Aberrations)
                {
                    if (aberration != null && IsUncheckedApLocation(aberration.id))
                    {
                        return AberrationApIcon;
                    }
                }
            }

            return null;
        }

        return IsUncheckedApLocation(spotLocationId) ? ApIcon : null;
    }

    public static Sprite GetIcon(string iconName)
    {
        return TextureUtil.GetSprite(iconName);
    }

    /// <summary>
    ///     A cached copy of <paramref name="item" /> whose sprite is the given AP icon.
    /// </summary>
    public static HarvestableItemData GetItemWithIcon(HarvestableItemData item, string iconName)
    {
        var key = (item.id, iconName);
        if (!IconItemCopies.TryGetValue(key, out var copy) || copy == null)
        {
            copy = Object.Instantiate(item);
            copy.sprite = GetIcon(iconName);
            IconItemCopies[key] = copy;
        }

        return copy;
    }

    private static bool IsUncheckedApLocation(string id)
    {
        return id != null &&
               LocationHelper.TryParseLocation(id, out _) &&
               LocationHelper.DoesApWorldContainLocation(id) &&
               !LocationHelper.HasThisLocationBeenChecked(id);
    }
}
