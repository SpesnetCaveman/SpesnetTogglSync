using System.Text.Json;
using System.Text.Json.Serialization;
using SpesnetTogglSync.Models;

namespace SpesnetTogglSync.Services;

public class ConfigService
{
    public const string ConfigLocationFileName = "config-location.json";
    public const string InvoiceTemplateFileName = "invoice-template.docx";

    private static readonly string[] DataFileNames =
    [
        "appsettings.json",
        "mappings.json",
        "syncstate.json",
        "autosync.json",
        "invoice-numbers.json",
        InvoiceTemplateFileName
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private string _baseDirectory;
    private readonly string _installDirectory;

    public ConfigService(string? baseDirectory = null)
    {
        _installDirectory = AppContext.BaseDirectory;
        _baseDirectory = baseDirectory ?? ResolveDataDirectory();
    }

    /// <summary>Directory that holds appsettings, mappings, syncstate, and logs.</summary>
    public string DataDirectory => _baseDirectory;

    /// <summary>Directory containing the exe and optional config-location.json bootstrap.</summary>
    public string InstallDirectory => _installDirectory;

    private string AppSettingsPath => Path.Combine(_baseDirectory, "appsettings.json");
    private string SyncStatePath => Path.Combine(_baseDirectory, "syncstate.json");
    private string MappingsPath => Path.Combine(_baseDirectory, "mappings.json");
    private string AutoSyncStatePath => Path.Combine(_baseDirectory, "autosync.json");
    private string InvoiceLedgerPath => Path.Combine(_baseDirectory, "invoice-numbers.json");

    /// <summary>App-owned invoice template. Invoices are filled from this file, not from the original.</summary>
    public string AppInvoiceTemplatePath => Path.Combine(_baseDirectory, InvoiceTemplateFileName);
    private string ConfigLocationPath => Path.Combine(_installDirectory, ConfigLocationFileName);

    /// <summary>
    /// Reads <c>config-location.json</c> next to the exe (if present) to locate the
    /// data directory. Falls back to the install directory when the pointer is missing
    /// or empty. Relative paths are resolved against the install directory.
    /// </summary>
    public static string ResolveDataDirectory(string? installDirectory = null)
    {
        var install = installDirectory ?? AppContext.BaseDirectory;
        var pointerPath = Path.Combine(install, ConfigLocationFileName);
        if (!File.Exists(pointerPath))
        {
            return install;
        }

        try
        {
            var json = File.ReadAllText(pointerPath);
            var location = JsonSerializer.Deserialize<ConfigLocation>(json, JsonOptions);
            var configured = location?.DataDirectory?.Trim();
            if (string.IsNullOrWhiteSpace(configured))
            {
                return install;
            }

            return ResolvePath(configured, install);
        }
        catch
        {
            return install;
        }
    }

    /// <summary>
    /// Writes the bootstrap pointer, creates the directory, optionally copies missing
    /// data files from the previous directory, then ensures config files and logs/ exist.
    /// </summary>
    public string ApplyDataDirectory(string? configuredPath)
    {
        var previousDirectory = _baseDirectory;
        var trimmed = configuredPath?.Trim() ?? string.Empty;
        var resolved = string.IsNullOrWhiteSpace(trimmed)
            ? _installDirectory
            : ResolvePath(trimmed, _installDirectory);

        Directory.CreateDirectory(resolved);

        if (!string.Equals(previousDirectory, resolved, StringComparison.OrdinalIgnoreCase))
        {
            CopyDataFilesIfMissing(previousDirectory, resolved);
        }

        WriteConfigLocation(resolved);
        _baseDirectory = resolved;
        EnsureConfigFilesExist();
        return resolved;
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(AppSettingsPath))
        {
            var examplePath = Path.Combine(_installDirectory, "appsettings.example.json");
            if (!File.Exists(examplePath))
            {
                examplePath = Path.Combine(_baseDirectory, "appsettings.example.json");
            }

            if (File.Exists(examplePath))
            {
                Directory.CreateDirectory(_baseDirectory);
                File.Copy(examplePath, AppSettingsPath);
            }
            else
            {
                SaveSettings(new AppSettings());
            }
        }

        return ReadJson<AppSettings>(AppSettingsPath) ?? new AppSettings();
    }

    public void SaveSettings(AppSettings settings) => WriteJson(AppSettingsPath, settings);

    public SyncState LoadSyncState()
    {
        return ReadJson<SyncState>(SyncStatePath) ?? new SyncState();
    }

    public void SaveSyncState(SyncState state) => WriteJson(SyncStatePath, state);

    public AutoSyncState LoadAutoSyncState() =>
        ReadJson<AutoSyncState>(AutoSyncStatePath) ?? new AutoSyncState();

    public void SaveAutoSyncState(AutoSyncState state) => WriteJson(AutoSyncStatePath, state);

    public InvoiceNumberLedger LoadInvoiceLedger()
    {
        var ledger = ReadJson<InvoiceNumberLedger>(InvoiceLedgerPath) ?? new InvoiceNumberLedger();
        ledger.Periods ??= [];
        return ledger;
    }

    public void SaveInvoiceLedger(InvoiceNumberLedger ledger) => WriteJson(InvoiceLedgerPath, ledger);

    /// <summary>
    /// Records today's automatic sync as done when the clock is already past the daily sync time,
    /// so a manual sync does not get repeated by the tray.
    /// </summary>
    public void MarkAutomaticSyncCompletedIfDue()
    {
        var syncTime = DailySyncTime.Parse(LoadSettings().DailySyncTime);
        if (SouthAfricaClock.Now().TimeOfDay < syncTime)
        {
            return;
        }

        var today = SouthAfricaClock.TodayString();
        var state = LoadAutoSyncState();
        if (state.CompletedDate == today || state.CancelledDate == today)
        {
            return;
        }

        state.CompletedDate = today;
        SaveAutoSyncState(state);
    }

    public void SetSyncProblem(string message)
    {
        var state = LoadAutoSyncState();
        if (state.SyncProblem && string.Equals(state.SyncProblemMessage, message, StringComparison.Ordinal))
        {
            return;
        }

        state.SyncProblem = true;
        state.SyncProblemMessage = message;
        SaveAutoSyncState(state);
    }

    public void ClearSyncProblem()
    {
        var state = LoadAutoSyncState();
        if (!state.SyncProblem && string.IsNullOrEmpty(state.SyncProblemMessage))
        {
            return;
        }

        state.SyncProblem = false;
        state.SyncProblemMessage = null;
        SaveAutoSyncState(state);
    }

    public MappingsFile LoadMappings()
    {
        return ReadJson<MappingsFile>(MappingsPath) ?? new MappingsFile();
    }

    public void SaveMappings(MappingsFile mappings) => WriteJson(MappingsPath, mappings);

    public UserMappings GetOrCreateUserMappings(MappingsFile file, long togglUserId)
    {
        var key = togglUserId.ToString();
        if (!file.Users.TryGetValue(key, out var userMappings))
        {
            userMappings = new UserMappings { TogglUserId = togglUserId };
            file.Users[key] = userMappings;
        }

        return userMappings;
    }

    public void EnsureConfigFilesExist()
    {
        Directory.CreateDirectory(_baseDirectory);
        Directory.CreateDirectory(Path.Combine(_baseDirectory, "logs"));

        if (!File.Exists(AppSettingsPath))
        {
            LoadSettings();
        }

        if (!File.Exists(MappingsPath))
        {
            SaveMappings(new MappingsFile());
        }

        if (!File.Exists(SyncStatePath))
        {
            SaveSyncState(new SyncState());
        }
    }

    private void WriteConfigLocation(string dataDirectory)
    {
        WriteJson(ConfigLocationPath, new ConfigLocation { DataDirectory = dataDirectory });
    }

    private static string ResolvePath(string configured, string installDirectory)
    {
        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(installDirectory, configured));
    }

    private static void CopyDataFilesIfMissing(string sourceDirectory, string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory) ||
            !Directory.Exists(sourceDirectory) ||
            string.Equals(sourceDirectory, targetDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var fileName in DataFileNames)
        {
            var sourcePath = Path.Combine(sourceDirectory, fileName);
            var targetPath = Path.Combine(targetDirectory, fileName);
            if (File.Exists(sourcePath) && !File.Exists(targetPath))
            {
                File.Copy(sourcePath, targetPath);
            }
        }

        var sourceLogs = Path.Combine(sourceDirectory, "logs");
        var targetLogs = Path.Combine(targetDirectory, "logs");
        if (Directory.Exists(sourceLogs))
        {
            Directory.CreateDirectory(targetLogs);
            foreach (var sourceLog in Directory.EnumerateFiles(sourceLogs))
            {
                var targetLog = Path.Combine(targetLogs, Path.GetFileName(sourceLog));
                if (!File.Exists(targetLog))
                {
                    File.Copy(sourceLog, targetLog);
                }
            }
        }

        foreach (var sourceFolder in Directory.EnumerateDirectories(sourceDirectory))
        {
            var name = Path.GetFileName(sourceFolder);
            if (!IsBillingReportFolder(name))
            {
                continue;
            }

            var targetFolder = Path.Combine(targetDirectory, name);
            foreach (var sourceFile in Directory.EnumerateFiles(sourceFolder))
            {
                var targetFile = Path.Combine(targetFolder, Path.GetFileName(sourceFile));
                if (File.Exists(targetFile))
                {
                    continue;
                }

                Directory.CreateDirectory(targetFolder);
                File.Copy(sourceFile, targetFile);
            }
        }
    }

    private static bool IsBillingReportFolder(string name)
    {
        if (name.Length != 7 || name[4] != '-')
        {
            return false;
        }

        return int.TryParse(name.AsSpan(0, 4), out var year) && year is >= 2000 and <= 2100
            && int.TryParse(name.AsSpan(5, 2), out var month) && month is >= 1 and <= 12;
    }

    private T? ReadJson<T>(string path) where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private void WriteJson<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(value, JsonOptions);
        File.WriteAllText(path, json);
    }
}
