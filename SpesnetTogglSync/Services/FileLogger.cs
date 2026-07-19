namespace SpesnetTogglSync.Services;

public class FileLogger
{
    private readonly string _logDirectory;
    private readonly object _lock = new();
    public event EventHandler<string>? LogWritten;

    public FileLogger(string? baseDirectory = null)
    {
        _logDirectory = Path.Combine(baseDirectory ?? AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    public string GetTodayLogPath() =>
        Path.Combine(_logDirectory, $"sync-{DateTime.Now:yyyyMMdd}.log");

    public string ReadTodayLog()
    {
        var path = GetTodayLogPath();
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        lock (_lock)
        {
            File.AppendAllText(GetTodayLogPath(), line + Environment.NewLine);
        }

        LogWritten?.Invoke(this, line);
    }
}
