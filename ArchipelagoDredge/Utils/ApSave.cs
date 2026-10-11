using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Network;

namespace ArchipelagoDredge.Utils;
public static class ApSave
{
    public static bool IsActive => ArchipelagoClient.IsConnected || ArchipelagoStateManager.IsBound;

    public static IReadOnlyList<string> ReceivedItemNames =>
        ArchipelagoClient.IsConnected
            ? ArchipelagoClient.Session.Items.AllItemsReceived
                .Where(item => item.ItemGame == ArchipelagoClient.GameName)
                .Select(item => item.ItemName)
                .ToList()
            : ArchipelagoStateManager.SavedReceivedItemNames;
}
