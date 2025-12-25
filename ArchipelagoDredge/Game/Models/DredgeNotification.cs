using Archipelago.MultiClient.Net.Colors;

namespace ArchipelagoDredge.Game.Models;

public class DredgeNotification
{
    public string Message { get; set; } = "NO MESSAGE SET";
    public PaletteColor MessageColor { get; set; } = PaletteColor.White;
}