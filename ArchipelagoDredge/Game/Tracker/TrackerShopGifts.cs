using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoDredge.Game.Tracker;

/// <summary>
///     Shopkeepers hand over their gift once you've traded enough with them (buying and selling both count). Money
///     isn't part of logic, so this is shown apart from what logic needs. Thresholds come from the vanilla dialogue.
/// </summary>
public static class TrackerShopGifts
{
    private sealed class Gift(string shopkeeper, decimal threshold, bool inclusive, params string[] shopIds)
    {
        public string Shopkeeper { get; } = shopkeeper;
        public decimal Threshold { get; } = threshold;
        public bool Inclusive { get; } = inclusive;
        public string[] ShopIds { get; } = shopIds;
    }

    private static readonly Dictionary<string, Gift> GiftsByLocation = new()
    {
        ["Fishmonger's Gift"] = new Gift("the Fishmonger", 400, false, "destination.gm-fishmonger"),
        ["Shipwright's Gift"] = new Gift("the Shipwright", 350, false, "destination.gm-shipwright"),
        ["Little Marrow Trader's Gift"] = new Gift("the Little Marrow Trader", 100, false, "destination.lm-trader"),
        ["Travelling Merchant's Gift"] = new Gift("the Travelling Merchant", 1000, true,
            "destination.tm-fish-market", "destination.tm-shipyard")
    };

    /// <summary>The trading a gift location still needs, or null for other locations.</summary>
    public static LogicRequirement Requirement(string locationName)
    {
        var saveData = GameManager.Instance?.SaveData;
        if (saveData == null || !GiftsByLocation.TryGetValue(locationName, out var gift))
        {
            return null;
        }

        var total = gift.ShopIds.Sum(saveData.GetShopTransactionTotalById);
        var met = gift.Inclusive ? total >= gift.Threshold : total > gift.Threshold;
        var amount = gift.Inclusive ? $"${gift.Threshold:#,0}" : $"over ${gift.Threshold:#,0}";
        return new LogicRequirement(
            $"Buy or sell {amount} with {gift.Shopkeeper}, then talk again (so far ${total:#,0})", met);
    }
}
