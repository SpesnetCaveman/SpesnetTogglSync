using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        var end = DateTime.UtcNow;
        _logger.Info($"Toggl: fetching time entries after {FormatUtc(since)} through {FormatUtc(end)}");
        var entries = await FetchTimeEntriesAsync(since, end, cancellationToken);
        return entries
            .Where(e => ToUtc(e.StartUtc) > since)
            .OrderBy(e => ToUtc(e.StartUtc))
            .ToList();
    }

    public async Task<IReadOnlyList<TogglTimeEntry>> GetTimeEntriesBetweenAsync(
        DateTime startUtcInclusive,
        DateTime endUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var start = ToUtc(startUtcInclusive);
        var end = ToUtc(endUtcExclusive);
        if (end <= start)
        {
            return [];
        }

        _logger.Info($"Toggl: fetching time entries from {FormatUtc(start)} through {FormatUtc(end)}");
        var entries = await FetchTimeEntriesAsync(start, end, cancellationToken);
        return entries
            .Where(e =>
            {
                var entryStart = ToUtc(e.StartUtc);
                return entryStart >= start && entryStart < end;
            })
            .OrderBy(e => ToUtc(e.StartUtc))
            .ToList();
    }

    public async Task<byte[]> GetSummaryReportPdfAsync(
        long workspaceId,
        DateOnly startDate,
        DateOnly endDate,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api.track.toggl.com/reports/api/v3/workspace/{workspaceId}/summary/time_entries.pdf";
        _logger.Info($"Toggl: summary PDF {startDate:yyyy-MM-dd} through {endDate:yyyy-MM-dd} for user {userId}");
        var response = await _http.SendAsync(
            "fetch summary report PDF",
            HttpMethod.Post,
            url,
            new SummaryReportRequest
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                UserIds = [userId],
                DateFormat = "YYYY-MM-DD",
                DurationFormat = "decimal"
            },
            cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task<List<TogglTimeEntry>> FetchTimeEntriesAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var startDate = FormatUtc(startUtc);
        var endDate = FormatUtc(endUtc);
        var url =
            $"me/time_entries?start_date={Uri.EscapeDataString(startDate)}" +
            $"&end_date={Uri.EscapeDataString(endDate)}&meta=true";
        var response = await _http.SendAsync(
            "fetch time entries",
            HttpMethod.Get,
            url,
            cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<TogglTimeEntry>>(cancellationToken: cancellationToken)
            ?? [];
    }

    private sealed class SummaryReportRequest
    {
        [JsonPropertyName("start_date")]
        public string StartDate { get; init; } = string.Empty;

        [JsonPropertyName("end_date")]
        public string EndDate { get; init; } = string.Empty;

        [JsonPropertyName("user_ids")]
        public long[] UserIds { get; init; } = [];

        [JsonPropertyName("grouping")]
        public string Grouping { get; init; } = "projects";

        [JsonPropertyName("sub_grouping")]
        public string SubGrouping { get; init; } = "time_entries";

        [JsonPropertyName("date_format")]
        public string? DateFormat { get; init; }

        [JsonPropertyName("duration_format")]
        public string? DurationFormat { get; init; }
    }

    private static string FormatUtc(DateTime value) =>
        ToUtc(value).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'");

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
