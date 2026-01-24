namespace TunesLayer.Overlay;

public interface IOverlayManager
{
    bool IsVisible { get; }
    bool IsClickThrough { get; }
    void Initialize();
    void Show();
    void Hide();
    void Toggle();
    void UpdateOpacity(double opacity);
    void UpdateSize(double size);
    void SetClickThrough(bool enabled);
    void ToggleClickThrough();
    void SetPosition(double x, double y);
}
