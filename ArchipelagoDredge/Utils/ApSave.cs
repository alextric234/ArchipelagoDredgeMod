using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Network;

namespace ArchipelagoDredge.Utils;

/// <summary>
///     Whether the loaded save plays by AP rules. True while connected, and also offline once the save
///     has been bound to a multiworld. Saves that have never connected behave like vanilla.
/// </summary>
public static class ApSave
{
    public static bool IsActive => ArchipelagoClient.IsConnected || ArchipelagoStateManager.IsBound;

    /// <summary>
    ///     Names of the AP items this slot has received: live from the server when connected, otherwise
    ///     from the save's last snapshot.
    /// </summary>
    public static IReadOnlyList<string> ReceivedItemNames =>
        ArchipelagoClient.IsConnected
            ? ArchipelagoClient.Session.Items.AllItemsReceived
                .Where(item => item.ItemGame == ArchipelagoClient.GameName)
                .Select(item => item.ItemName)
                .ToList()
            : ArchipelagoStateManager.SavedReceivedItemNames;
}
