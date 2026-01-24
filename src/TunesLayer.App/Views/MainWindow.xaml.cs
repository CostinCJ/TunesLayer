using System.Windows;
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
}
