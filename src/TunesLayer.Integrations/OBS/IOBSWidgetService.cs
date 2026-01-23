namespace TunesLayer.Integrations.OBS;

public interface IOBSWidgetService
{
    bool IsRunning { get; }
    string WidgetUrl { get; }
    void Start();
    void Stop();
}
