using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using TunesLayer.App.ViewModels;
using TunesLayer.Core.Models;

namespace TunesLayer.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Hide to tray instead of closing
        e.Cancel = true;
        Hide();
    }
    
    private void ThemeItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is ThemeInfo theme)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.SelectedTheme = theme;
            }
        }
    }

    
    private void AnalyticsTab_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer && scrollViewer.IsVisible)
        {
            System.Diagnostics.Debug.WriteLine("Analytics tab became visible, refreshing...");
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.RefreshAnalytics();
            }
        }
    }

    private void RefreshAnalytics_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Refresh button clicked");
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.RefreshAnalytics();
        }
    }
}
