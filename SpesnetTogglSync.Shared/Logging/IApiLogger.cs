namespace SpesnetTogglSync.Logging;

/// <summary>
/// Minimal logging surface used by the Toggl and Spesnet API libraries.
/// </summary>
public interface IApiLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}
