namespace ArchipelagoDredge.Game.Helpers;

/// <summary>
///     Fish received from the multiworld never rot. They're marked through <see cref="SpatialItemInstance.durability" />,
///     which the game never uses for fish but still saves with them and keeps when they move between grids.
/// </summary>
public static class MultiworldFish
{
    private const float Marker = -1f;

    public static void Mark(FishItemInstance fish)
    {
        fish.durability = Marker;
    }

    public static bool IsMarked(SpatialItemInstance item)
    {
        return item is FishItemInstance && item.durability == Marker;
    }
}
