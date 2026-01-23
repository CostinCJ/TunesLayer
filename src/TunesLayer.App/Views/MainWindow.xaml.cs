using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TunesLayer.App.ViewModels;

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
}
