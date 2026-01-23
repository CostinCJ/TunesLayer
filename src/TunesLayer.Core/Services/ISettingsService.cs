using TunesLayer.Core.Models;

namespace TunesLayer.Core.Services;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    Task LoadAsync();
    Task SaveAsync();
    event EventHandler<AppSettings>? SettingsChanged;
}
