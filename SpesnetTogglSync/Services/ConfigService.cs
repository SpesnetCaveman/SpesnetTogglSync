using System.Text.Json;
using System.Text.Json.Serialization;
using SpesnetTogglSync.Models;

namespace SpesnetTogglSync.Services;

public class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _baseDirectory;

    public ConfigService(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ?? AppContext.BaseDirectory;
    }

    private string AppSettingsPath => Path.Combine(_baseDirectory, "appsettings.json");
    private string SyncStatePath => Path.Combine(_baseDirectory, "syncstate.json");
    private string MappingsPath => Path.Combine(_baseDirectory, "mappings.json");

    public AppSettings LoadSettings()
    {
        if (!File.Exists(AppSettingsPath))
        {
            var examplePath = Path.Combine(_baseDirectory, "appsettings.example.json");
            if (File.Exists(examplePath))
            {
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
