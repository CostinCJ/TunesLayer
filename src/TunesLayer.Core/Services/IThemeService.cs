using TunesLayer.Core.Models;

namespace TunesLayer.Core.Services;

public interface IThemeService
{
    IEnumerable<ThemeInfo> GetAvailableThemes();
    ThemeInfo? GetCurrentTheme();
    void ApplyTheme(string themeId);
    Task ImportThemeAsync(string filePath);
    Task ExportThemeAsync(string themeId, string filePath);
}
