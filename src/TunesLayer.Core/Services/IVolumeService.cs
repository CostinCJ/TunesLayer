namespace TunesLayer.Core.Services;

/// <summary>
/// Service for controlling system volume.
/// </summary>
public interface IVolumeService
{
    /// <summary>
    /// Gets the current system volume (0-100).
    /// </summary>
    double GetVolume();

    /// <summary>
    /// Sets the system volume (0-100).
    /// </summary>
    void SetVolume(double volume);

    /// <summary>
    /// Increases volume by the specified step.
    /// </summary>
    void IncreaseVolume(double step = 5);

    /// <summary>
    /// Decreases volume by the specified step.
    /// </summary>
    void DecreaseVolume(double step = 5);

    /// <summary>
    /// Gets whether the system is muted.
    /// </summary>
    bool IsMuted();

    /// <summary>
    /// Toggles mute state.
    /// </summary>
    void ToggleMute();
}
