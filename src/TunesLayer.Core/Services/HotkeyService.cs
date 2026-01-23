using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace TunesLayer.Core.Services;

public class HotkeyService : IHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly Dictionary<int, Action> _hotkeyCallbacks = new();
    private readonly Dictionary<string, int> _hotkeyIds = new();
    private int _nextHotkeyId = 9000;
    private HwndSource? _hwndSource;
    private IntPtr _windowHandle;
    private bool _disposed;

    public void Initialize()
    {
        // Create a message-only window for hotkey messages
        var parameters = new HwndSourceParameters("TunesLayerHotkeyWindow")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = 0,
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
        _windowHandle = _hwndSource.Handle;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            if (_hotkeyCallbacks.TryGetValue(hotkeyId, out var callback))
            {
                callback.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void RegisterHotkey(string name, string keyCombo, Action callback)
    {
        if (_disposed) return;

        // Parse the key combination
        var (modifiers, key) = ParseKeyCombo(keyCombo);
        if (key == 0) return;

        // Unregister existing hotkey with same name
        if (_hotkeyIds.TryGetValue(name, out var existingId))
        {
            UnregisterHotKey(_windowHandle, existingId);
            _hotkeyCallbacks.Remove(existingId);
        }

        // Register new hotkey
        int id = _nextHotkeyId++;
        if (RegisterHotKey(_windowHandle, id, modifiers, key))
        {
            _hotkeyIds[name] = id;
            _hotkeyCallbacks[id] = callback;
        }
    }

    public void UnregisterHotkey(string name)
    {
        if (_hotkeyIds.TryGetValue(name, out var id))
        {
            UnregisterHotKey(_windowHandle, id);
            _hotkeyCallbacks.Remove(id);
            _hotkeyIds.Remove(name);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _hotkeyIds.Values)
        {
            UnregisterHotKey(_windowHandle, id);
        }
        _hotkeyCallbacks.Clear();
        _hotkeyIds.Clear();
    }

    private static (uint modifiers, uint key) ParseKeyCombo(string keyCombo)
    {
        uint modifiers = 0;
        uint key = 0;

        var parts = keyCombo.Split('+').Select(p => p.Trim()).ToArray();
        
        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= 0x0002; // MOD_CONTROL
                    break;
                case "alt":
                    modifiers |= 0x0001; // MOD_ALT
                    break;
                case "shift":
                    modifiers |= 0x0004; // MOD_SHIFT
                    break;
                case "win":
                case "windows":
                    modifiers |= 0x0008; // MOD_WIN
                    break;
                case "space":
                    key = 0x20; // VK_SPACE
                    break;
                case "left":
                    key = 0x25; // VK_LEFT
                    break;
                case "up":
                    key = 0x26; // VK_UP
                    break;
                case "right":
                    key = 0x27; // VK_RIGHT
                    break;
                case "down":
                    key = 0x28; // VK_DOWN
                    break;
                default:
                    // Try to parse as a single character key
                    if (part.Length == 1 && char.IsLetterOrDigit(part[0]))
                    {
                        key = (uint)char.ToUpperInvariant(part[0]);
                    }
                    break;
            }
        }

        return (modifiers, key);
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        UnregisterAll();
        _hwndSource?.Dispose();
        _disposed = true;
    }
}
