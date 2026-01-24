using AudioSwitcher.AudioApi.CoreAudio;

namespace TunesLayer.Core.Services;

/// <summary>
/// Service for controlling system volume using CoreAudio API.
/// </summary>
public class VolumeService : IVolumeService
{
    private readonly CoreAudioController _audioController;

    public VolumeService()
    {
        _audioController = new CoreAudioController();
    }

    public double GetVolume()
    {
        try
        {
            var device = _audioController.DefaultPlaybackDevice;
            return device?.Volume ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public void SetVolume(double volume)
    {
        try
        {
            // Clamp volume to 0-100 range
            volume = Math.Max(0, Math.Min(100, volume));
            
            var device = _audioController.DefaultPlaybackDevice;
            if (device != null)
            {
                device.Volume = volume;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set volume: {ex.Message}");
        }
    }

    public void IncreaseVolume(double step = 5)
    {
        var currentVolume = GetVolume();
        SetVolume(currentVolume + step);
    }

    public void DecreaseVolume(double step = 5)
    {
        var currentVolume = GetVolume();
        SetVolume(currentVolume - step);
    }

    public bool IsMuted()
    {
        try
        {
            var device = _audioController.DefaultPlaybackDevice;
            return device?.IsMuted ?? false;
        }
        catch
        {
            return false;
        }
    }

    public void ToggleMute()
    {
        try
        {
            var device = _audioController.DefaultPlaybackDevice;
            if (device != null)
            {
                device.ToggleMute();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to toggle mute: {ex.Message}");
        }
    }
}
