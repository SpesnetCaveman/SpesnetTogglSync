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
        // Exclusive watermark: only entries with start strictly after this instant.
        var since = ToUtc(sinceUtc);
        var startDate = since.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'");
        var url =
            $"me/time_entries?start_date={Uri.EscapeDataString(startDate)}" +
            $"&end_date={Uri.EscapeDataString(endDate)}&meta=true";
        _logger.Info($"Toggl: fetching time entries after {startDate} through {endDate}");
        var response = await _http.SendAsync(
            "fetch time entries",
            HttpMethod.Get,
            url,
            cancellationToken: cancellationToken);
        var entries = await response.Content.ReadFromJsonAsync<List<TogglTimeEntry>>(cancellationToken: cancellationToken)
            ?? [];

        return entries
            .Where(e => ToUtc(e.StartUtc) > since)
            .OrderBy(e => ToUtc(e.StartUtc))
            .ToList();
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        // Sync state JSON and DateTimePicker round-trips often lose Kind; treat as already-UTC.
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

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
