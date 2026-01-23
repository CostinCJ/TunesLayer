using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using TunesLayer.Core.Models;
using TunesLayer.Core.Services;

namespace TunesLayer.App.Views;

public partial class AnalyticsWindow : Window
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsWindow()
    {
        InitializeComponent();
        _analyticsService = App.Services.GetRequiredService<IAnalyticsService>();
        LoadAnalytics();
    }

    private void LoadAnalytics()
    {
        // Load today's stats
        var todayStats = _analyticsService.GetTodayStats();
        TodayTimeText.Text = FormatDuration(todayStats.TotalListeningTime);
        TracksPlayedText.Text = todayStats.TracksPlayed.ToString();
        SessionsText.Text = todayStats.SessionsCount.ToString();

        // Load weekly stats
        var weeklyStats = _analyticsService.GetWeeklyStats().ToList();
        var totalWeeklyTime = weeklyStats.Sum(s => s.TotalListeningTime.TotalHours);
        WeeklyAvgText.Text = $"{totalWeeklyTime / 7:F1}h";

        // Build chart
        BuildWeeklyChart(weeklyStats);

        // Load recent tracks
        LoadRecentTracks();
    }

    private void BuildWeeklyChart(List<DailyStats> weeklyStats)
    {
        // Clear existing chart elements (except grid definitions)
        var toRemove = ChartGrid.Children.Cast<UIElement>()
            .Where(e => e is Border || e is TextBlock)
            .ToList();
        foreach (var element in toRemove)
        {
            ChartGrid.Children.Remove(element);
        }

        // Find max for scaling
        var maxTime = weeklyStats.Max(s => s.TotalListeningTime.TotalMinutes);
        if (maxTime == 0) maxTime = 60; // Default to 1 hour if no data

        for (int i = 0; i < 7; i++)
        {
            var stats = weeklyStats[i];
            var percentage = stats.TotalListeningTime.TotalMinutes / maxTime;
            var dayName = stats.Date.ToString("ddd");

            // Bar
            var bar = new Border
            {
                Background = i == 6 
                    ? (Brush)FindResource("PrimaryBrush") 
                    : new SolidColorBrush(Color.FromArgb(100, 139, 92, 246)),
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = Math.Max(4, percentage * 200),
                Margin = new Thickness(8, 0, 8, 0)
            };
            
            bar.ToolTip = $"{dayName}: {FormatDuration(stats.TotalListeningTime)}\n{stats.TracksPlayed} tracks";
            
            Grid.SetColumn(bar, i);
            Grid.SetRow(bar, 0);
            ChartGrid.Children.Add(bar);

            // Day label
            var label = new TextBlock
            {
                Text = dayName,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)FindResource("SecondaryTextBrush"),
                FontSize = 11,
                Margin = new Thickness(0, 5, 0, 0)
            };
            
            Grid.SetColumn(label, i);
            Grid.SetRow(label, 1);
            ChartGrid.Children.Add(label);
        }
    }

    private void LoadRecentTracks()
    {
        var recentTracks = _analyticsService.GetRecentTracks(20)
            .Select(t => new RecentTrackDisplay
            {
                TrackTitle = t.TrackTitle ?? "Unknown",
                Artist = t.Artist ?? "Unknown",
                PlayedAt = t.PlayedAt,
                PlayedAtFormatted = FormatRelativeTime(t.PlayedAt)
            })
            .ToList();

        RecentTracksList.ItemsSource = recentTracks;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        if (duration.TotalMinutes >= 1)
            return $"{duration.Minutes}m {duration.Seconds}s";
        return $"{duration.Seconds}s";
    }

    private static string FormatRelativeTime(DateTime time)
    {
        var diff = DateTime.Now - time;
        
        if (diff.TotalMinutes < 1)
            return "Just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";
        
        return time.ToString("MMM d");
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = ".json",
            FileName = $"tuneslayer_analytics_{DateTime.Now:yyyy-MM-dd}"
        };

        if (dialog.ShowDialog() == true)
        {
            await _analyticsService.ExportDataAsync(dialog.FileName);
            MessageBox.Show("Analytics data exported successfully!", "Export Complete", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class RecentTrackDisplay
{
    public string TrackTitle { get; set; } = "";
    public string Artist { get; set; } = "";
    public DateTime PlayedAt { get; set; }
    public string PlayedAtFormatted { get; set; } = "";
}
