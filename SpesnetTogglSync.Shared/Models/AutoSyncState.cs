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
}
