using SpesnetTogglSync.Models;
using SpesnetTogglSync.SpesnetApi;
using SpesnetTogglSync.TogglApi;

namespace SpesnetTogglSync.Services;

public class SyncService
{
    private const double MaxHoursPerEntry = 8.0;

    /// <summary>Spesnet work dates are South African (GMT+2); SA has no DST.</summary>
    private static readonly TimeZoneInfo SouthAfricaTimeZone = ResolveSouthAfricaTimeZone();

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
        watermarkUtc = ToUtc(watermarkUtc);
        _logger.Info($"Sync started from watermark {watermarkUtc:o} (exclusive; only starts after this)");
        Report("Fetching Toggl time entries...");

        var unresolved = FindUnresolvedMappings(mappings);
        if (unresolved.Count > 0)
        {
            var sample = string.Join("; ", unresolved.Take(5).Select(FormatMappingLabel));
            var more = unresolved.Count > 5 ? $" (+{unresolved.Count - 5} more)" : string.Empty;
            var message =
                $"Resolve mapping status for {unresolved.Count} row(s) still set to New before syncing: {sample}{more}. " +
                "Set each to Active (with Spesnet mappings) or Ignore.";
            _logger.Error(message);
            return Fail(message);
        }

        var entries = await _togglClient.GetTimeEntriesSinceAsync(watermarkUtc, cancellationToken);
        _logger.Info($"Fetched {entries.Count} Toggl entries after watermark");

        if (entries.Count == 0)
        {
            var noEntriesMessage = "Sync complete. Synced 0, skipped 0 (no new time entries after watermark).";
            _logger.Info(noEntriesMessage);
            return new SyncResult
            {
                Success = true,
                Message = noEntriesMessage,
                LastSyncedStartTime = watermarkUtc
            };
        }

        var candidateEntries = new List<TogglTimeEntry>();
        var skippedCount = 0;
        var ignoredByMappingCount = 0;
        var deferredForRunningTimer = false;

        foreach (var entry in entries)
        {
            // Only completed entries can sync. Stop here so the watermark (entry start)
            // cannot advance past a still-running timer and orphan it.
            if (IsRunning(entry))
            {
                _logger.Warn(
                    $"Skipped (still running): {FormatEntryLabel(entry)}. " +
                    "Later entries will sync after this timer is stopped.");
                skippedCount++;
                deferredForRunningTimer = true;
                break;
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

            if (HasClientLevelIgnore(mappings, entry))
            {
                _logger.Info(
                    $"Ignored (mapping status=Ignore, client-level): {FormatEntryLabel(entry)} — " +
                    $"client '{entry.ClientName}'");
                skippedCount++;
                ignoredByMappingCount++;
                continue;
            }

            var mapping = FindEntryMapping(mappings, entry);
            if (mapping == null)
            {
                var message =
                    $"Missing mapping for Toggl client '{entry.ClientName}' and project '{entry.ProjectName}'. " +
                    "Refresh from Toggl on the Mapping tab so the row appears, then set Active or Ignore.";
                _logger.Error(message);
                return Fail(message);
            }

            if (mapping.Status == EntryMappingStatus.Ignore)
            {
                _logger.Info(
                    $"Ignored (mapping status=Ignore): {FormatEntryLabel(entry)} — " +
                    $"'{entry.ClientName}' / '{entry.ProjectName}'");
                skippedCount++;
                ignoredByMappingCount++;
                continue;
            }

            if (mapping.Status == EntryMappingStatus.New)
            {
                var message =
                    $"Mapping for Toggl client '{entry.ClientName}' and project '{entry.ProjectName}' is still New. " +
                    "Set it to Active or Ignore before syncing.";
                _logger.Error(message);
                return Fail(message);
            }

            candidateEntries.Add(entry);
        }

        if (deferredForRunningTimer)
        {
            var remaining = entries.Count - skippedCount - candidateEntries.Count;
            if (remaining > 0)
            {
                _logger.Info($"Deferred {remaining} later Toggl entr{(remaining == 1 ? "y" : "ies")} until the running timer stops.");
            }
        }

        LogOverlappingEntries(candidateEntries);

        var validationError = ValidateMappings(candidateEntries, mappings, referenceCache);
        if (validationError != null)
        {
            _logger.Error(validationError);
            return Fail(validationError);
        }

        if (candidateEntries.Count == 0)
        {
            var emptyMessage =
                $"Sync complete. Synced 0, skipped {skippedCount} " +
                $"({ignoredByMappingCount} ignored by mapping status).";
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

            // Exclusive watermark: next sync only takes entries with start > this value.
            currentWatermark = ToUtc(entry.StartUtc);
            syncedCount++;

            var syncState = new SyncState { LastSyncedStartTime = currentWatermark };
            _configService.SaveSyncState(syncState);
            _logger.Info($"Synced {FormatEntryLabel(entry)}; watermark updated to {currentWatermark:o} (next sync requires start > watermark)");
            Report($"Synced entry {entry.Id}", currentWatermark);
        }

        var summary =
            $"Sync complete. Synced {syncedCount}, skipped {skippedCount} " +
            $"({ignoredByMappingCount} ignored by mapping status).";
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

    private static bool IsRunning(TogglTimeEntry entry) =>
        entry.Duration < 0 || string.IsNullOrWhiteSpace(entry.Stop);

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    /// <summary>
    /// Entry start in South African time (GMT+2), in the Spesnet API shape.
    /// </summary>
    private static string ToSpesnetTxDateTime(DateTime startUtc)
    {
        var southAfricaLocal = TimeZoneInfo
            .ConvertTimeFromUtc(ToUtc(startUtc), SouthAfricaTimeZone);
        return southAfricaLocal.ToString("yyyy-MM-dd'T'HH:mm:ss.fff");
    }

    private static TimeZoneInfo ResolveSouthAfricaTimeZone()
    {
        foreach (var id in new[] { "Africa/Johannesburg", "South Africa Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        // South Africa is permanently GMT+2 (no DST).
        return TimeZoneInfo.CreateCustomTimeZone(
            "South Africa Standard Time",
            TimeSpan.FromHours(2),
            "South Africa Standard Time",
            "South Africa Standard Time");
    }

    /// <summary>
    /// Watermark is entry start only, so overlaps still sync when starts differ.
    /// Warn so overlapping Toggl ranges are visible in the log.
    /// </summary>
    private void LogOverlappingEntries(IReadOnlyList<TogglTimeEntry> entries)
    {
        for (var i = 1; i < entries.Count; i++)
        {
            var previous = entries[i - 1];
            var current = entries[i];
            var previousEnd = previous.StartUtc.AddSeconds(previous.Duration);
            if (current.StartUtc < previousEnd)
            {
                _logger.Warn(
                    $"Overlapping Toggl entries: {FormatEntryLabel(previous)} " +
                    $"(ends {previousEnd:yyyy-MM-dd HH:mm} UTC) overlaps {FormatEntryLabel(current)}. " +
                    "Both will sync because watermark tracks start time only.");
            }
        }
    }

    private static string FormatEntryLabel(TogglTimeEntry entry)
    {
        var description = string.IsNullOrWhiteSpace(entry.Description)
            ? "(no description)"
            : entry.Description!;
        return $"entry {entry.Id} at {entry.StartUtc:yyyy-MM-dd HH:mm} UTC '{description}'";
    }

    /// <summary>
    /// Rows still marked New block sync until explicitly set to Active or Ignore.
    /// Client-level Ignore covers every project for that client, so New project rows under
    /// an ignored client are not required and do not block.
    /// </summary>
    public static IReadOnlyList<EntryMapping> FindUnresolvedMappings(UserMappings mappings)
    {
        var ignoredClientIds = mappings.EntryMappings
            .Where(m => m.IsClientLevelIgnore)
            .Select(m => m.TogglClientId)
            .Where(id => id > 0)
            .ToHashSet();

        var ignoredClientNames = mappings.EntryMappings
            .Where(m => m.IsClientLevelIgnore)
            .Select(m => m.TogglClientName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return mappings.EntryMappings
            .Where(m => m.Status == EntryMappingStatus.New)
            .Where(m =>
                !ignoredClientIds.Contains(m.TogglClientId) &&
                !ignoredClientNames.Contains(m.TogglClientName))
            .ToList();
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

            if (!mapping.HasSpesnetDestination)
            {
                return $"Active mapping for '{entry.ClientName}' / '{entry.ProjectName}' is missing Spesnet project, client, or work task.";
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

    private static bool HasClientLevelIgnore(UserMappings mappings, TogglTimeEntry entry) =>
        mappings.EntryMappings.Any(m => m.IsClientLevelIgnore && MatchesTogglClient(m, entry));

    private static EntryMapping? FindEntryMapping(UserMappings mappings, TogglTimeEntry entry)
    {
        return mappings.EntryMappings.FirstOrDefault(m =>
            !m.IsClientLevelIgnore &&
            MatchesTogglClient(m, entry) &&
            MatchesTogglProject(m, entry));
    }

    private static bool MatchesTogglClient(EntryMapping mapping, TogglTimeEntry entry) =>
        mapping.TogglClientId == entry.ClientId!.Value ||
        string.Equals(mapping.TogglClientName, entry.ClientName, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesTogglProject(EntryMapping mapping, TogglTimeEntry entry) =>
        mapping.TogglProjectId == entry.ProjectId!.Value ||
        string.Equals(mapping.TogglProjectName, entry.ProjectName, StringComparison.OrdinalIgnoreCase);

    private static string FormatMappingLabel(EntryMapping mapping)
    {
        if (string.IsNullOrWhiteSpace(mapping.TogglProjectName))
        {
            return $"'{mapping.TogglClientName}' (client-level)";
        }

        return $"'{mapping.TogglClientName}' / '{mapping.TogglProjectName}'";
    }

    private static List<SpesnetWorkDoneEntry> TransformEntry(
        TogglTimeEntry entry,
        int employeeId,
        EntryMapping mapping)
    {
        var totalHours = entry.Duration / 3600.0;
        var remaining = totalHours;
        // Spesnet expects the start in South African time (GMT+2), not UTC.
        var txDateTime = ToSpesnetTxDateTime(entry.StartUtc);
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
