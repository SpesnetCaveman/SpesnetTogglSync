using SpesnetTogglSync.Models;
using SpesnetTogglSync.SpesnetApi;
using SpesnetTogglSync.TogglApi;

namespace SpesnetTogglSync.Services;

public class SyncService
{
    private const decimal MaxHoursPerEntry = 8.0m;

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
        var skippedDetails = new List<string>();
        string? blockReason = null;
        var blockIsRunning = false;
        DateTime? blockStartUtc = null;
        var deferredCount = 0;

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            // Ignore is the only skip that may be passed. The watermark can move beyond
            // those entries because they should never sync.
            if (IsPermanentIgnore(mappings, entry))
            {
                NotePermanentIgnore(mappings, entry, skippedDetails, ref skippedCount, ref ignoredByMappingCount);
                continue;
            }

            var block = TryGetBlockReason(entry, mappings, referenceCache);
            if (block != null)
            {
                // Stop here. A later entry must not sync, or the start-based watermark
                // would move past this one and it would never be fetched again.
                blockReason = block;
                blockIsRunning = IsRunning(entry);
                blockStartUtc = ToUtc(entry.StartUtc);
                deferredCount = entries.Count - i - 1;
                break;
            }

            candidateEntries.Add(entry);
        }

        if (blockStartUtc is DateTime blockedAt)
        {
            var held = candidateEntries.Where(entry => ToUtc(entry.StartUtc) >= blockedAt).ToList();
            if (held.Count > 0)
            {
                foreach (var entry in held)
                {
                    _logger.Info(
                        $"Held {FormatEntryLabel(entry)} because it starts at the same time as an entry that cannot sync yet.");
                }

                candidateEntries.RemoveAll(entry => ToUtc(entry.StartUtc) >= blockedAt);
                deferredCount += held.Count;
            }
        }

        if (blockReason != null && deferredCount > 0)
        {
            _logger.Info(
                $"Deferred {deferredCount} later Toggl entr{(deferredCount == 1 ? "y" : "ies")} " +
                "so the watermark cannot move past the entry that cannot sync yet.");
        }

        LogOverlappingEntries(candidateEntries);

        var syncedCount = 0;
        var syncedSeconds = 0L;
        var currentWatermark = watermarkUtc;

        if (candidateEntries.Count == 0 && blockReason == null)
        {
            var emptySkippedSummary = BuildSkippedSummary(skippedCount, ignoredByMappingCount, skippedDetails);
            var emptyMessage =
                $"Sync complete. Synced 0, skipped {skippedCount}{emptySkippedSummary}.";
            _logger.Info(emptyMessage);
            return new SyncResult
            {
                Success = true,
                Message = emptyMessage,
                SkippedCount = skippedCount,
                LastSyncedStartTime = watermarkUtc
            };
        }

        if (candidateEntries.Count > 0)
        {
            Report("Logging in to Spesnet...");
            await _spesnetClient.LoginAsync(cancellationToken);
            var employeeId = referenceCache.EmployeeId > 0
                ? referenceCache.EmployeeId
                : await _spesnetClient.GetEmployeeIdAsync(cancellationToken);

            foreach (var entry in candidateEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mapping = FindEntryMapping(mappings, entry)!;
                var workDoneEntries = TransformEntry(entry, employeeId, mapping);
                if (workDoneEntries.Count == 0)
                {
                    _logger.Info($"Toggl entry {entry.Id} rounds to 0.00h; nothing sent to Spesnet.");
                }
                else
                {
                    var request = new SpesnetSaveWorkRequest { WorkDoneList = workDoneEntries };
                    Report($"Syncing Toggl entry {entry.Id} ({FormatSouthAfricaDateTime(entry.StartUtc)} SAST)...");
                    await _spesnetClient.SaveWorkEntriesAsync(request, cancellationToken);
                }

                // Exclusive watermark: next sync only takes entries with start > this value.
                // Never set this to the blocked entry's start (or an equal start), or that entry is skipped forever.
                currentWatermark = ToUtc(entry.StartUtc);
                syncedCount++;
                if (entry.Duration > 0)
                {
                    syncedSeconds += entry.Duration;
                }

                var syncState = new SyncState { LastSyncedStartTime = currentWatermark };
                _configService.SaveSyncState(syncState);
                _logger.Info($"Synced {FormatEntryLabel(entry)}; watermark updated to {currentWatermark:o} (next sync requires start > watermark)");
                Report($"Synced entry {entry.Id}", currentWatermark);
            }
        }

        var skippedSummary = BuildSkippedSummary(skippedCount, ignoredByMappingCount, skippedDetails);
        if (blockReason != null)
        {
            var deferredText = deferredCount > 0
                ? $" {deferredCount} later entr{(deferredCount == 1 ? "y" : "ies")} were not synced."
                : string.Empty;
            var stoppedMessage =
                $"{blockReason} Sync stopped here so this entry is included next time.{deferredText} " +
                $"Synced {syncedCount}, skipped {skippedCount}{skippedSummary}.";
            if (blockIsRunning)
            {
                _logger.Warn(stoppedMessage);
            }
            else
            {
                _logger.Error(stoppedMessage);
            }

            return new SyncResult
            {
                Success = false,
                Message = stoppedMessage,
                SyncedCount = syncedCount,
                SkippedCount = skippedCount,
                SyncedSeconds = syncedSeconds,
                LastSyncedStartTime = currentWatermark
            };
        }

        var summary =
            $"Sync complete. Synced {syncedCount}, skipped {skippedCount}{skippedSummary}.";
        _logger.Info(summary);
        return new SyncResult
        {
            Success = true,
            Message = summary,
            SyncedCount = syncedCount,
            SkippedCount = skippedCount,
            SyncedSeconds = syncedSeconds,
            LastSyncedStartTime = currentWatermark
        };
    }

    /// <summary>
    /// True when this entry still has to be written to Spesnet. Permanent Ignore does not.
    /// </summary>
    internal static bool StillNeedsSync(UserMappings mappings, TogglTimeEntry entry, DateTime? watermarkUtc)
    {
        if (IsPermanentIgnore(mappings, entry))
        {
            return false;
        }

        if (watermarkUtc is not DateTime watermark)
        {
            return true;
        }

        return SouthAfricaClock.ToUtc(entry.StartUtc) > SouthAfricaClock.ToUtc(watermark);
    }

    /// <summary>
    /// Client-level or entry Ignore. These entries are never synced, so the watermark may move past them.
    /// </summary>
    internal static bool IsPermanentIgnore(UserMappings mappings, TogglTimeEntry entry)
    {
        if (!entry.ClientId.HasValue || string.IsNullOrWhiteSpace(entry.ClientName))
        {
            return false;
        }

        if (HasClientLevelIgnore(mappings, entry))
        {
            return true;
        }

        if (!entry.ProjectId.HasValue || string.IsNullOrWhiteSpace(entry.ProjectName))
        {
            return false;
        }

        var mapping = FindEntryMapping(mappings, entry);
        return mapping?.Status == EntryMappingStatus.Ignore;
    }

    private void NotePermanentIgnore(
        UserMappings mappings,
        TogglTimeEntry entry,
        List<string> skippedDetails,
        ref int skippedCount,
        ref int ignoredByMappingCount)
    {
        var skippedEntry = FormatEntryLabel(entry);
        var clientLevel = HasClientLevelIgnore(mappings, entry);
        if (clientLevel)
        {
            _logger.Info(
                $"Ignored (mapping status=Ignore, client-level): {skippedEntry} — " +
                $"client '{entry.ClientName}'");
            skippedDetails.Add($"ignored by mapping (client-level): {skippedEntry}");
        }
        else
        {
            _logger.Info(
                $"Ignored (mapping status=Ignore): {skippedEntry} — " +
                $"'{entry.ClientName}' / '{entry.ProjectName}'");
            skippedDetails.Add($"ignored by mapping: {skippedEntry}");
        }

        skippedCount++;
        ignoredByMappingCount++;
    }

    /// <summary>
    /// Why this entry needs to sync but cannot yet. Null means it can sync now.
    /// </summary>
    private static string? TryGetBlockReason(
        TogglTimeEntry entry,
        UserMappings mappings,
        SpesnetReferenceCache referenceCache)
    {
        var when = FormatSouthAfricaDateTime(entry.StartUtc);
        if (IsRunning(entry))
        {
            return
                $"Toggl entry {entry.Id} at {when} SAST is still running. " +
                "Stop the timer, then sync again.";
        }

        if (!entry.ClientId.HasValue || string.IsNullOrWhiteSpace(entry.ClientName))
        {
            return
                $"Toggl entry {entry.Id} at {when} SAST is missing a client. " +
                "Assign a client in Toggl, then sync again.";
        }

        if (!entry.ProjectId.HasValue || string.IsNullOrWhiteSpace(entry.ProjectName))
        {
            return
                $"Toggl entry {entry.Id} at {when} SAST is missing a project. " +
                "Assign a project in Toggl, then sync again.";
        }

        var mapping = FindEntryMapping(mappings, entry);
        if (mapping == null)
        {
            return
                $"Missing mapping for Toggl client '{entry.ClientName}' and project '{entry.ProjectName}' " +
                $"(entry {entry.Id} at {when} SAST). " +
                "Refresh from Toggl on the Mapping tab so the row appears, then set Active or Ignore.";
        }

        if (mapping.Status == EntryMappingStatus.New)
        {
            return
                $"Mapping for Toggl client '{entry.ClientName}' and project '{entry.ProjectName}' is still New " +
                $"(entry {entry.Id} at {when} SAST). Set it to Active or Ignore before syncing.";
        }

        if (string.IsNullOrWhiteSpace(entry.Description))
        {
            return
                $"Toggl entry {entry.Id} at {when} SAST " +
                $"('{entry.ClientName}' / '{entry.ProjectName}') has no description. " +
                "Add a description in Toggl, then sync again.";
        }

        return ValidateEntryDestination(entry, mapping, referenceCache);
    }

    private static bool IsRunning(TogglTimeEntry entry) =>
        entry.Duration < 0 || string.IsNullOrWhiteSpace(entry.Stop);

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static DateTime ToSouthAfricaLocal(DateTime startUtc) =>
        TimeZoneInfo.ConvertTimeFromUtc(ToUtc(startUtc), SouthAfricaTimeZone);

    /// <summary>User-facing entry time in South African time (GMT+2).</summary>
    private static string FormatSouthAfricaDateTime(DateTime utc) =>
        ToSouthAfricaLocal(utc).ToString("yyyy-MM-dd HH:mm");

    /// <summary>
    /// Entry start in South African time (GMT+2), in the Spesnet API shape.
    /// </summary>
    private static string ToSpesnetTxDateTime(DateTime startUtc) =>
        ToSouthAfricaLocal(startUtc).ToString("yyyy-MM-dd'T'HH:mm:ss.fff");

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
                    $"(ends {FormatSouthAfricaDateTime(previousEnd)} SAST) overlaps {FormatEntryLabel(current)}. " +
                    "Both will sync because watermark tracks start time only.");
            }
        }
    }

    private static string FormatEntryLabel(TogglTimeEntry entry)
    {
        var description = string.IsNullOrWhiteSpace(entry.Description)
            ? "(no description)"
            : entry.Description!;
        return $"entry {entry.Id} at {FormatSouthAfricaDateTime(entry.StartUtc)} SAST '{description}'";
    }

    private static string BuildSkippedSummary(
        int skippedCount,
        int ignoredByMappingCount,
        IReadOnlyList<string> skippedDetails)
    {
        if (skippedCount == 0)
        {
            return $" ({ignoredByMappingCount} ignored by mapping status)";
        }

        var shownDetails = skippedDetails.Take(5).ToList();
        var detailText = shownDetails.Count > 0
            ? string.Join("; ", shownDetails)
            : "reason unavailable";
        var more = skippedDetails.Count > shownDetails.Count
            ? $" (+{skippedDetails.Count - shownDetails.Count} more)"
            : string.Empty;

        return
            $" ({ignoredByMappingCount} ignored by mapping status; skipped entries: {detailText}{more})";
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

    private static string? ValidateEntryDestination(
        TogglTimeEntry entry,
        EntryMapping mapping,
        SpesnetReferenceCache referenceCache)
    {
        var when = FormatSouthAfricaDateTime(entry.StartUtc);
        if (!mapping.HasSpesnetDestination)
        {
            return
                $"Active mapping for '{entry.ClientName}' / '{entry.ProjectName}' is missing Spesnet project, client, or work task " +
                $"(entry {entry.Id} at {when} SAST).";
        }

        if (!referenceCache.Projects.Any(p => p.Id == mapping.SpesnetProjectId))
        {
            return
                $"Mapped Spesnet project id {mapping.SpesnetProjectId} for '{entry.ClientName}' / '{entry.ProjectName}' was not found " +
                $"(entry {entry.Id} at {when} SAST).";
        }

        if (!referenceCache.ClientsByProject.TryGetValue(mapping.SpesnetProjectId, out var clients) ||
            clients.All(c => c.Id != mapping.SpesnetClientId))
        {
            return
                $"Mapped Spesnet client id {mapping.SpesnetClientId} for '{entry.ClientName}' / '{entry.ProjectName}' " +
                $"was not found for project {mapping.SpesnetProjectId} (entry {entry.Id} at {when} SAST).";
        }

        if (!referenceCache.WorkTasks.Any(w => w.Id == mapping.SpesnetWorkTaskId))
        {
            return
                $"Mapped Spesnet work task id {mapping.SpesnetWorkTaskId} for '{entry.ClientName}' / '{entry.ProjectName}' was not found " +
                $"(entry {entry.Id} at {when} SAST).";
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

    private static List<SpesnetWorkDoneEntry> TransformEntry(
        TogglTimeEntry entry,
        int employeeId,
        EntryMapping mapping)
    {
        // Spesnet only handles hours on a 0.05 grid (6.66 → 6.65, 6.68 → 6.70).
        var remaining = RoundHoursToFiveHundredths(entry.Duration);
        // Spesnet expects the start in South African time (GMT+2), not UTC.
        var txDateTime = ToSpesnetTxDateTime(entry.StartUtc);
        // Description is validated before write; whitespace-only is rejected earlier.
        var comment = (mapping.CommentPrefix ?? string.Empty) + entry.Description!.Trim();
        var result = new List<SpesnetWorkDoneEntry>();

        while (remaining > 0)
        {
            var chunkHours = remaining > MaxHoursPerEntry ? MaxHoursPerEntry : remaining;
            result.Add(new SpesnetWorkDoneEntry
            {
                Comment = comment,
                EmployeeId = employeeId,
                NormalHours = (double)chunkHours,
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

    /// <summary>Nearest 0.05 hour. 0.05h is 180 seconds. Halves round away from zero.</summary>
    private static decimal RoundHoursToFiveHundredths(long seconds)
    {
        const decimal secondsPerStep = 180m;
        var steps = decimal.Round(seconds / secondsPerStep, 0, MidpointRounding.AwayFromZero);
        return steps * 0.05m;
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
