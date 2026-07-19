using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SpesnetTogglSync.Models;

namespace SpesnetTogglSync.Services;

public class TogglApiClient : ITogglClient, IDisposable
{
    private const string BaseUrl = "https://api.track.toggl.com/api/v9";
    private readonly HttpClient _httpClient;
    private readonly FileLogger _logger;

    public TogglApiClient(string apiToken, FileLogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiToken}:api_token"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<TogglMe> GetMeAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Toggl: fetching /me");
        var response = await _httpClient.GetAsync("/me", cancellationToken);
        await EnsureSuccessAsync(response, "fetch user profile");
        var me = await response.Content.ReadFromJsonAsync<TogglMe>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Toggl /me returned empty response.");
        return me;
    }

    public async Task<IReadOnlyList<TogglClient>> GetClientsAsync(long workspaceId, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Toggl: fetching clients for workspace {workspaceId}");
        var response = await _httpClient.GetAsync($"/workspaces/{workspaceId}/clients", cancellationToken);
        await EnsureSuccessAsync(response, "fetch clients");
        return await ReadItemsAsync<TogglClient>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<TogglProject>> GetProjectsAsync(long workspaceId, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Toggl: fetching projects for workspace {workspaceId}");
        var response = await _httpClient.GetAsync($"/workspaces/{workspaceId}/projects", cancellationToken);
        await EnsureSuccessAsync(response, "fetch projects");
        return await ReadItemsAsync<TogglProject>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<TogglTimeEntry>> GetTimeEntriesSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        var startDate = sinceUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"/me/time_entries?start_date={Uri.EscapeDataString(startDate)}&meta=true";
        _logger.Info($"Toggl: fetching time entries since {startDate}");
        var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, "fetch time entries");
        var entries = await response.Content.ReadFromJsonAsync<List<TogglTimeEntry>>(cancellationToken: cancellationToken)
            ?? [];

        return entries
            .Where(e => e.StartUtc > sinceUtc.ToUniversalTime())
            .OrderBy(e => e.StartUtc)
            .ToList();
    }

    private static async Task<List<T>> ReadItemsAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<T>>(json) ?? [];
        }

        if (document.RootElement.TryGetProperty("items", out var items))
        {
            return JsonSerializer.Deserialize<List<T>>(items.GetRawText()) ?? [];
        }

        return [];
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        var message = $"Toggl failed to {operation}: {(int)response.StatusCode} {response.ReasonPhrase}. {body}";
        _logger.Error(message);
        throw new HttpRequestException(message);
    }

    public void Dispose() => _httpClient.Dispose();
}
