using System.Text.Json;
using System.Text.Json.Serialization;
using SpesnetTogglSync.Models;

namespace SpesnetTogglSync.Services;

public class ConfigService
{
    public const string ConfigLocationFileName = "config-location.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _baseDirectory;
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

            var resolved = Path.IsPathRooted(configured)
                ? configured
                : Path.GetFullPath(Path.Combine(install, configured));

            Directory.CreateDirectory(resolved);
            return resolved;
        }
        catch
        {
            return install;
        }
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
