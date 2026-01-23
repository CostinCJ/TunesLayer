using System.Windows;
using TunesLayer.Core.Models;

namespace TunesLayer.Core.Services;

public class ThemeService : IThemeService
{
    private static readonly List<ThemeInfo> BuiltInThemes = new()
    {
        new ThemeInfo
        {
            Id = "midnight",
            Name = "Midnight",
            Description = "Dark theme with purple accents",
            ResourcePath = "Themes/Midnight.xaml",
            IsPremium = false,
            PreviewColor = "#8B5CF6"
        },
        new ThemeInfo
        {
            Id = "neon",
            Name = "Neon",
            Description = "Cyberpunk aesthetic with glow effects",
            ResourcePath = "Themes/Neon.xaml",
            IsPremium = false,
            PreviewColor = "#00FF88"
        },
        new ThemeInfo
        {
            Id = "minimal",
            Name = "Minimal",
            Description = "Clean white/black, no decorations",
            ResourcePath = "Themes/Minimal.xaml",
            IsPremium = false,
            PreviewColor = "#FFFFFF"
        },
        new ThemeInfo
        {
            Id = "glassmorphism",
            Name = "Glassmorphism",
            Description = "Frosted glass with blur effect",
            ResourcePath = "Themes/Glassmorphism.xaml",
            IsPremium = true,
            PreviewColor = "#60A5FA"
        },
        new ThemeInfo
        {
            Id = "retro",
            Name = "Retro",
            Description = "Pixel art inspired nostalgic vibes",
            ResourcePath = "Themes/Retro.xaml",
            IsPremium = true,
            PreviewColor = "#F97316"
        }
    };

    private string _currentThemeId = "midnight";

    public IEnumerable<ThemeInfo> GetAvailableThemes()
    {
        return BuiltInThemes;
    }

    public ThemeInfo? GetCurrentTheme()
    {
        return BuiltInThemes.FirstOrDefault(t => t.Id == _currentThemeId);
    }

    public void ApplyTheme(string themeId)
    {
        var theme = BuiltInThemes.FirstOrDefault(t => t.Id == themeId);
        if (theme == null) return;

        _currentThemeId = themeId;

        try
        {
            var app = Application.Current;
            if (app == null) return;

            // Remove existing theme dictionary
            var existingTheme = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.OriginalString.Contains("Themes/") == true);
            
            if (existingTheme != null)
            {
                app.Resources.MergedDictionaries.Remove(existingTheme);
            }

            // Add new theme dictionary
            var newTheme = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/{theme.ResourcePath}", UriKind.Absolute)
            };
            app.Resources.MergedDictionaries.Add(newTheme);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply theme: {ex.Message}");
        }
    }

    public async Task ImportThemeAsync(string filePath)
    {
        // TODO: Implement theme import from .tuneslayer-theme files
        await Task.CompletedTask;
    }

    public async Task ExportThemeAsync(string themeId, string filePath)
    {
        // TODO: Implement theme export to .tuneslayer-theme files
        await Task.CompletedTask;
    }
}
