namespace SpesnetTogglSync.Models;

/// <summary>
/// Daily tray schedule. Stored in autosync.json so sync watermark saves cannot wipe it.
/// Dates are South African calendar dates (yyyy-MM-dd).
/// </summary>
public class AutoSyncState
{
    public string? PromptDate { get; set; }

    public DateTime? DueUtc { get; set; }

    public string? CancelledDate { get; set; }

    public string? CompletedDate { get; set; }

    /// <summary>
    /// True after a sync stops or fails. Cleared by the next successful sync.
    /// The tray icon stays on the problem state until then.
    /// </summary>
    public bool SyncProblem { get; set; }

    /// <summary>Reason shown on the tray while <see cref="SyncProblem"/> is set.</summary>
    public string? SyncProblemMessage { get; set; }

    /// <summary>
    /// South African date (yyyy-MM-dd) of the cycle start whose previous period
    /// has already been written to the yyyy-MM report folder.
    /// </summary>
    public string? BillingReportCycleStart { get; set; }
}
