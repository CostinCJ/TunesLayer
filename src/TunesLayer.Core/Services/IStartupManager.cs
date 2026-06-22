namespace TunesLayer.Core.Services;

public interface IStartupManager
{
    /// <summary>
    /// Enables or disables the application to start with Windows.
    /// </summary>
    /// <param name="enable">True to enable startup, false to disable.</param>
    void SetStartupEnabled(bool enable);

    /// <summary>
    /// Checks if the application is currently set to start with Windows.
    /// </summary>
    /// <returns>True if startup is enabled, false otherwise.</returns>
    bool IsStartupEnabled();
}
