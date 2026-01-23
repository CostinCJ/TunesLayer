namespace TunesLayer.Core.Models;

public class ThemeInfo
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ResourcePath { get; set; } = string.Empty;
    public bool IsPremium { get; set; }
    public string PreviewColor { get; set; } = "#8B5CF6";
}
