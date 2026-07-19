using System.Text.Json;
using SpesnetTogglSync.Logging;
using SpesnetTogglSync.Models;

namespace SpesnetTogglSync.SpesnetApi;

public class MockSpesnetTimekeepingClient : ISpesnetTimekeepingClient
{
    private readonly IApiLogger _logger;
    private readonly SpesnetReferenceCache _referenceCache;

    public MockSpesnetTimekeepingClient(IApiLogger logger)
    {
        _logger = logger;
        _referenceCache = LoadReferenceData();
    }

    public Task LoginAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Mock Spesnet: login (no-op)");
        return Task.CompletedTask;
    }

    public Task<SpesnetUserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SpesnetUserInfo
        {
            Username = "GerriePre",
            AspNetUserId = "72ad9b5b-3840-46b8-ac38-a5501278e166"
        });
    }

    public Task<int> GetEmployeeIdAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_referenceCache.EmployeeId);
    }

    public Task<IReadOnlyList<SpesnetProject>> GetProjectsForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<SpesnetProject>>(_referenceCache.Projects);
    }

    public Task<IReadOnlyList<SpesnetClient>> GetClientsByProjectAsync(int projectId, CancellationToken cancellationToken = default)
    {
        if (_referenceCache.ClientsByProject.TryGetValue(projectId, out var clients))
        {
            return Task.FromResult<IReadOnlyList<SpesnetClient>>(clients);
        }

        return Task.FromResult<IReadOnlyList<SpesnetClient>>([]);
    }

    public Task<IReadOnlyList<SpesnetWorkTask>> GetWorkTasksAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<SpesnetWorkTask>>(_referenceCache.WorkTasks);
    }

    public Task SaveWorkEntriesAsync(SpesnetSaveWorkRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
        _logger.Info($"Mock Spesnet: accepted save payload:{Environment.NewLine}{json}");
        return Task.CompletedTask;
    }

    public Task<SpesnetReferenceCache> RefreshReferenceDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Mock Spesnet: returning embedded reference data");
        return Task.FromResult(CloneReferenceCache(_referenceCache));
    }

    private static SpesnetReferenceCache LoadReferenceData()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "mock-spesnet-reference.json");
        if (!File.Exists(path))
        {
            return new SpesnetReferenceCache();
        }

        var json = File.ReadAllText(path);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var cache = new SpesnetReferenceCache
        {
            EmployeeId = root.GetProperty("employeeId").GetInt32(),
            Projects = JsonSerializer.Deserialize<List<SpesnetProject>>(root.GetProperty("projects").GetRawText()) ?? [],
            WorkTasks = JsonSerializer.Deserialize<List<SpesnetWorkTask>>(root.GetProperty("workTasks").GetRawText()) ?? []
        };

        if (root.TryGetProperty("clientsByProject", out var clientsByProject))
        {
            foreach (var property in clientsByProject.EnumerateObject())
            {
                if (int.TryParse(property.Name, out var projectId))
                {
                    cache.ClientsByProject[projectId] =
                        JsonSerializer.Deserialize<List<SpesnetClient>>(property.Value.GetRawText()) ?? [];
                }
            }
        }

        return cache;
    }

    private static SpesnetReferenceCache CloneReferenceCache(SpesnetReferenceCache source)
    {
        return new SpesnetReferenceCache
        {
            EmployeeId = source.EmployeeId,
            Projects = source.Projects.Select(p => new SpesnetProject { Id = p.Id, ProjName = p.ProjName }).ToList(),
            WorkTasks = source.WorkTasks.Select(w => new SpesnetWorkTask
            {
                Id = w.Id,
                Description = w.Description,
                Code = w.Code
            }).ToList(),
            ClientsByProject = source.ClientsByProject.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(c => new SpesnetClient { Id = c.Id, Name = c.Name, Code = c.Code }).ToList())
        };
    }

    public void Dispose()
    {
    }
}
