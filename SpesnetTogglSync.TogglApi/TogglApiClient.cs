using System.Net.Http.Json;
using System.Text.Json;
using SpesnetTogglSync.Logging;
using SpesnetTogglSync.Models;

namespace SpesnetTogglSync.TogglApi;

public class TogglApiClient : ITogglClient
{
    private readonly TogglApiHttp _http;
    private readonly IApiLogger _logger;

    public TogglApiClient(string apiToken, IApiLogger logger)
    {
        _logger = logger;
        _http = new TogglApiHttp(TogglApiHttp.CreateHttpClient(apiToken), logger);
    }

    public async Task<TogglMe> GetMeAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Toggl: fetching /me");
        var response = await _http.SendAsync(
            "fetch user profile",
            HttpMethod.Get,
            "me",
            cancellationToken: cancellationToken);
        var me = await response.Content.ReadFromJsonAsync<TogglMe>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Toggl /me returned empty response.");
        return me;
    }

    public async Task<IReadOnlyList<TogglClient>> GetClientsAsync(long workspaceId, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Toggl: fetching clients for workspace {workspaceId}");
        var response = await _http.SendAsync(
            "fetch clients",
            HttpMethod.Get,
            $"workspaces/{workspaceId}/clients",
            cancellationToken: cancellationToken);
        return await ReadItemsAsync<TogglClient>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<TogglProject>> GetProjectsAsync(long workspaceId, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Toggl: fetching active projects for workspace {workspaceId}");
        var response = await _http.SendAsync(
            "fetch projects",
            HttpMethod.Get,
            $"workspaces/{workspaceId}/projects?active=true",
            cancellationToken: cancellationToken);
        return await ReadItemsAsync<TogglProject>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<TogglTimeEntry>> GetTimeEntriesSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        var startDate = sinceUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"me/time_entries?start_date={Uri.EscapeDataString(startDate)}&meta=true";
        _logger.Info($"Toggl: fetching time entries since {startDate}");
        var response = await _http.SendAsync(
            "fetch time entries",
            HttpMethod.Get,
            url,
            cancellationToken: cancellationToken);
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

    public void Dispose() => _http.Dispose();
}
