namespace TunesLayer.Core.Services;

public interface IHotkeyService : IDisposable
{
    void Initialize();
    void RegisterHotkey(string name, string keyCombo, Action callback);
    void UnregisterHotkey(string name);
    void UnregisterAll();
}

public enum HotkeyModifier
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}
