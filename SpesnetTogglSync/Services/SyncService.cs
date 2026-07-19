using SpesnetTogglSync.Models;
using SpesnetTogglSync.SpesnetApi;
using SpesnetTogglSync.TogglApi;

namespace SpesnetTogglSync.Services;

public class SyncService
{
    private const double MaxHoursPerEntry = 8.0;

    private readonly ITogglClient _togglClient;
    private readonly ISpesnetTimekeepingClient _spesnetClient;
    private readonly ConfigService _configService;
    private readonly FileLogger _logger;

    public event EventHandler<SyncProgressEventArgs>? Progress;

    public SyncService(
        ITogglClient togglClient,
        ISpesnetTimekeepingClient spesnetClient,
        ConfigService configService,
        FileLogger logger)
    {
        _togglClient = togglClient;
        _spesnetClient = spesnetClient;
        _configService = configService;
        _logger = logger;
    }

    public async Task<SyncResult> SyncAsync(
        DateTime watermarkUtc,
        UserMappings mappings,
        SpesnetReferenceCache referenceCache,
        CancellationToken cancellationToken = default)
    {
        _logger.Info($"Sync started from watermark {watermarkUtc:u}");
        Report("Fetching Toggl time entries...");

        var entries = await _togglClient.GetTimeEntriesSinceAsync(watermarkUtc, cancellationToken);
        _logger.Info($"Fetched {entries.Count} Toggl entries after watermark");

        if (entries.Count == 0)
        {
            return new SyncResult
            {
                Success = true,
                Message = "No new time entries to sync.",
                LastSyncedStartTime = watermarkUtc
            };
        }

        var selectedClientIds = mappings.SelectedClients
            .Where(c => c.IsSelected)
            .Select(c => c.ClientId)
            .ToHashSet();

        if (selectedClientIds.Count == 0)
        {
            return Fail("No Toggl clients are selected for sync. Check at least one client on the Toggl Clients tab.");
        }

        var candidateEntries = new List<TogglTimeEntry>();
        var skippedCount = 0;

        foreach (var entry in entries)
        {
            if (entry.Duration < 0 || string.IsNullOrWhiteSpace(entry.Stop))
            {
                _logger.Warn($"Skipping running entry {entry.Id} starting {entry.StartUtc:u}");
                skippedCount++;
                continue;
            }

            if (!entry.ClientId.HasValue || string.IsNullOrWhiteSpace(entry.ClientName))
            {
                var message = $"Toggl entry at {entry.StartUtc:yyyy-MM-dd HH:mm} UTC is missing a client.";
                _logger.Error(message);
                return Fail(message);
            }

            if (!entry.ProjectId.HasValue || string.IsNullOrWhiteSpace(entry.ProjectName))
            {
                var message = $"Toggl entry at {entry.StartUtc:yyyy-MM-dd HH:mm} UTC is missing a project.";
                _logger.Error(message);
                return Fail(message);
            }

            if (!selectedClientIds.Contains(entry.ClientId.Value))
            {
                _logger.Info($"Skipping entry {entry.Id} for unselected client '{entry.ClientName}'");
                skippedCount++;
                continue;
            }

            candidateEntries.Add(entry);
        }

        var validationError = ValidateMappings(candidateEntries, mappings, referenceCache);
        if (validationError != null)
        {
            _logger.Error(validationError);
            return Fail(validationError);
        }

        if (candidateEntries.Count == 0)
        {
            var emptyMessage = $"No entries to sync. Skipped {skippedCount}.";
            _logger.Info(emptyMessage);
            return new SyncResult
            {
                Success = true,
                Message = emptyMessage,
                SkippedCount = skippedCount,
                LastSyncedStartTime = watermarkUtc
            };
        }

        Report("Logging in to Spesnet...");
        await _spesnetClient.LoginAsync(cancellationToken);
        var employeeId = referenceCache.EmployeeId > 0
            ? referenceCache.EmployeeId
            : await _spesnetClient.GetEmployeeIdAsync(cancellationToken);

        var syncedCount = 0;
        var currentWatermark = watermarkUtc;

        foreach (var entry in candidateEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mapping = FindEntryMapping(mappings, entry)!;
            var workDoneEntries = TransformEntry(entry, employeeId, mapping);
            var request = new SpesnetSaveWorkRequest { WorkDoneList = workDoneEntries };

            Report($"Syncing Toggl entry {entry.Id} ({entry.StartUtc:yyyy-MM-dd HH:mm})...");
            await _spesnetClient.SaveWorkEntriesAsync(request, cancellationToken);

            currentWatermark = entry.StartUtc;
            syncedCount++;

            var syncState = new SyncState { LastSyncedStartTime = currentWatermark };
            _configService.SaveSyncState(syncState);
            _logger.Info($"Synced entry {entry.Id}; watermark updated to {currentWatermark:u}");
            Report($"Synced entry {entry.Id}", currentWatermark);
        }

        var summary = $"Sync complete. Synced {syncedCount}, skipped {skippedCount}.";
        _logger.Info(summary);
        return new SyncResult
        {
            Success = true,
            Message = summary,
            SyncedCount = syncedCount,
            SkippedCount = skippedCount,
            LastSyncedStartTime = currentWatermark
        };
    }

    private static string? ValidateMappings(
        IReadOnlyList<TogglTimeEntry> entries,
        UserMappings mappings,
        SpesnetReferenceCache referenceCache)
    {
        foreach (var entry in entries)
        {
            var mapping = FindEntryMapping(mappings, entry);
            if (mapping == null)
            {
                return $"Missing mapping for Toggl client '{entry.ClientName}' and project '{entry.ProjectName}'. Configure it on the Mapping tab.";
            }

            if (!referenceCache.Projects.Any(p => p.Id == mapping.SpesnetProjectId))
            {
                return $"Mapped Spesnet project id {mapping.SpesnetProjectId} for '{entry.ClientName}' / '{entry.ProjectName}' was not found.";
            }

            if (!referenceCache.ClientsByProject.TryGetValue(mapping.SpesnetProjectId, out var clients) ||
                clients.All(c => c.Id != mapping.SpesnetClientId))
            {
                return $"Mapped Spesnet client id {mapping.SpesnetClientId} for '{entry.ClientName}' / '{entry.ProjectName}' was not found for project {mapping.SpesnetProjectId}.";
            }

            if (!referenceCache.WorkTasks.Any(w => w.Id == mapping.SpesnetWorkTaskId))
            {
                return $"Mapped Spesnet work task id {mapping.SpesnetWorkTaskId} for '{entry.ClientName}' / '{entry.ProjectName}' was not found.";
            }
        }

        return null;
    }

    private static EntryMapping? FindEntryMapping(UserMappings mappings, TogglTimeEntry entry)
    {
        return mappings.EntryMappings.FirstOrDefault(m =>
            MatchesTogglClient(m, entry) && MatchesTogglProject(m, entry));
    }

    private static bool MatchesTogglClient(EntryMapping mapping, TogglTimeEntry entry) =>
        mapping.TogglClientId == entry.ClientId!.Value ||
        string.Equals(mapping.TogglClientName, entry.ClientName, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesTogglProject(EntryMapping mapping, TogglTimeEntry entry) =>
        mapping.TogglProjectId == entry.ProjectId!.Value ||
        string.Equals(mapping.TogglProjectName, entry.ProjectName, StringComparison.OrdinalIgnoreCase);

    private static List<SpesnetWorkDoneEntry> TransformEntry(
        TogglTimeEntry entry,
        int employeeId,
        EntryMapping mapping)
    {
        var totalHours = entry.Duration / 3600.0;
        var remaining = totalHours;
        var txDateTime = entry.StartUtc.Date.ToString("yyyy-MM-dd'T'00:00:00.000'Z'");
        var comment = string.IsNullOrWhiteSpace(entry.Description) ? "(no description)" : entry.Description!;
        var result = new List<SpesnetWorkDoneEntry>();

        while (remaining > 0)
        {
            var chunkHours = Math.Min(remaining, MaxHoursPerEntry);
            result.Add(new SpesnetWorkDoneEntry
            {
                Comment = comment,
                EmployeeId = employeeId,
                NormalHours = chunkHours,
                OvertimeHours = 0,
                ProjectId = mapping.SpesnetProjectId,
                ClientId = mapping.SpesnetClientId,
                TxDateTime = txDateTime,
                WorkTaskId = mapping.SpesnetWorkTaskId
            });
            remaining -= chunkHours;
        }

        return result;
    }

    private void Report(string message, DateTime? updatedWatermark = null)
    {
        Progress?.Invoke(this, new SyncProgressEventArgs
        {
            Message = message,
            UpdatedWatermark = updatedWatermark
        });
    }

    private SyncResult Fail(string message) =>
        new() { Success = false, Message = message };
}
