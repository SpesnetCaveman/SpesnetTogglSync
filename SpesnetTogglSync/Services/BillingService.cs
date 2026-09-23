using System.Globalization;
using SpesnetTogglSync.Models;
using SpesnetTogglSync.TogglApi;

namespace SpesnetTogglSync.Services;

/// <summary>
/// After a sync, builds the hours/earnings notice and, once the current cycle has started,
/// saves the previous cycle's Toggl summary if it has not been saved yet.
/// </summary>
internal sealed class BillingService
{
    private readonly ConfigService _configService;
    private readonly FileLogger _logger;

    public BillingService(ConfigService configService, FileLogger logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task ApplyAsync(ITogglClient toggl, SyncResult result, CancellationToken cancellationToken)
    {
        var settings = _configService.LoadSettings();
        var startDay = BillingCycle.NormalizeStartDay(settings.BillingCycleStartDay);
        var today = SouthAfricaClock.Today();
        var open = BillingCycle.OpenCycle(today, startDay);
        var lines = new List<string>();

        try
        {
            var cycleSeconds = await SumSecondsAsync(toggl, open.Start, open.End, cancellationToken);
            lines.Add(FormatEarnings(result.SyncedSeconds, cycleSeconds, open, settings.HourlyRate));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Billing summary failed: {ex.Message}");
            lines.Add("Billing summary could not be loaded.");
        }

        try
        {
            var reportLine = await TrySaveDueReportAsync(
                toggl, settings, startDay, today, result, force: false, cancellationToken);
            if (reportLine != null)
            {
                lines.Add(reportLine);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Billing report failed: {ex.Message}");
            lines.Add("Billing report was not saved.");
        }

        result.BillingNotice = string.Join(" ", lines);
        _logger.Info(result.BillingNotice);
    }

    /// <summary>Creates the finished cycle's Toggl PDF now, even if the daily sync has not reached it yet.</summary>
    public async Task<string> CreateReportNowAsync(ITogglClient toggl, CancellationToken cancellationToken)
    {
        var settings = _configService.LoadSettings();
        var startDay = BillingCycle.NormalizeStartDay(settings.BillingCycleStartDay);
        return await TrySaveDueReportAsync(
            toggl,
            settings,
            startDay,
            SouthAfricaClock.Today(),
            new SyncResult(),
            force: true,
            cancellationToken) ?? "Billing report was not saved.";
    }

    private async Task<string?> TrySaveDueReportAsync(
        ITogglClient toggl,
        AppSettings settings,
        int startDay,
        DateOnly today,
        SyncResult result,
        bool force,
        CancellationToken cancellationToken)
    {
        var due = BillingCycle.ReportDue(today, startDay);
        var state = _configService.LoadAutoSyncState();
        if (!force && string.Equals(state.BillingReportCycleStart, due.CycleKey, StringComparison.Ordinal))
        {
            return null;
        }

        var me = await toggl.GetMeAsync(cancellationToken);
        var workspaceId = settings.TogglWorkspaceId > 0 ? settings.TogglWorkspaceId : me.DefaultWorkspaceId;
        if (workspaceId <= 0)
        {
            throw new InvalidOperationException("Toggl workspace id is missing.");
        }

        if (settings.TogglWorkspaceId != workspaceId)
        {
            settings.TogglWorkspaceId = workspaceId;
            _configService.SaveSettings(settings);
        }

        var entries = await toggl.GetTimeEntriesBetweenAsync(
            SouthAfricaClock.StartOfDayUtc(due.Period.Start),
            SouthAfricaClock.StartOfDayUtc(due.Period.End.AddDays(1)),
            cancellationToken);
        var periodEntries = entries
            .Where(entry =>
            {
                var date = SouthAfricaClock.Date(entry.StartUtc);
                return date >= due.Period.Start && date <= due.Period.End;
            })
            .ToList();

        var mappings = _configService.GetOrCreateUserMappings(_configService.LoadMappings(), me.Id);
        var watermark = ResolveWatermark(result);
        var unsynced = periodEntries.Count(entry => SyncService.StillNeedsSync(mappings, entry, watermark));
        if (unsynced > 0)
        {
            var waiting =
                $"Billing report is waiting until {unsynced} time entr{(unsynced == 1 ? "y" : "ies")} " +
                $"in {FormatRange(due.Period.Start, due.Period.End)} ha{(unsynced == 1 ? "s" : "ve")} synced.";
            _logger.Info(waiting);
            return waiting;
        }

        var templatePath = ResolveTemplatePath(settings);
        if (settings.HourlyRate <= 0)
        {
            var rateMessage = "Billing report is waiting until an hourly rate is set.";
            _logger.Info(rateMessage);
            return rateMessage;
        }

        if (!File.Exists(templatePath))
        {
            throw new InvalidOperationException(
                $"Invoice template was not found: {templatePath}. Import a Word template on the Settings tab.");
        }

        long periodSeconds = 0;
        foreach (var entry in periodEntries)
        {
            if (entry.Duration > 0)
            {
                periodSeconds += entry.Duration;
            }
        }

        var hours = Hours(periodSeconds);
        var invoiceNumber = InvoiceNumbers.Resolve(_configService, settings, templatePath, due.Period);
        var pdf = await toggl.GetSummaryReportPdfAsync(
            workspaceId,
            due.Period.Start,
            due.Period.End,
            me.Id,
            cancellationToken);

        var folder = Path.Combine(ResolveReportRoot(settings), due.FolderName);
        Directory.CreateDirectory(folder);
        var timesheetName = TimesheetFileName(due.Period);
        var invoiceName = InvoiceFileName(due.Period);
        var timesheetPath = Path.Combine(folder, timesheetName);
        var invoicePath = Path.Combine(folder, invoiceName);
        await File.WriteAllBytesAsync(timesheetPath, pdf, cancellationToken);
        InvoiceDocument.CreatePdf(
            templatePath,
            invoicePath,
            invoiceNumber,
            due.Period,
            hours,
            settings.HourlyRate);

        state = _configService.LoadAutoSyncState();
        state.BillingReportCycleStart = due.CycleKey;
        _configService.SaveAutoSyncState(state);
        var periodText = FormatRange(due.Period.Start, due.Period.End);
        var numberText = invoiceNumber.ToString("000", CultureInfo.InvariantCulture);
        _logger.Info($"Billing report saved to {timesheetPath} and invoice {numberText} to {invoicePath} ({periodText}).");
        return $"Billing report saved to {due.FolderName} ({periodText}), invoice {numberText}.";
    }

    public static string TimesheetFileName(BillingPeriod period) =>
        $"toggle timesheet report {period.Start:yyyy-MM-dd} to {period.End:yyyy-MM-dd}.pdf";

    public static string InvoiceFileName(BillingPeriod period) =>
        "Gerrie Pretorius  Invoice - " +
        period.End.ToString("yyyy-MM", CultureInfo.InvariantCulture) + " " +
        period.End.ToString("MMMM", CultureInfo.InvariantCulture) + ".pdf";

    private string ResolveTemplatePath(AppSettings settings)
    {
        var appCopy = _configService.AppInvoiceTemplatePath;
        InvoiceDocument.EnsureAppCopy(appCopy, settings.InvoiceTemplatePath);
        return appCopy;
    }

    private DateTime? ResolveWatermark(SyncResult result)
    {
        DateTime? latest = null;
        Consider(result.LastSyncedStartTime);
        Consider(_configService.LoadSyncState().LastSyncedStartTime);
        return latest;

        void Consider(DateTime? value)
        {
            if (value is not DateTime stamp)
            {
                return;
            }

            var utc = SouthAfricaClock.ToUtc(stamp);
            if (latest is not DateTime current || utc > current)
            {
                latest = utc;
            }
        }
    }

    private string ResolveReportRoot(AppSettings settings)
    {
        var configured = settings.BillingReportDirectory?.Trim();
        if (string.IsNullOrEmpty(configured))
        {
            return _configService.DataDirectory;
        }

        return Path.GetFullPath(configured);
    }

    private static async Task<long> SumSecondsAsync(
        ITogglClient toggl,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken)
    {
        var entries = await toggl.GetTimeEntriesBetweenAsync(
            SouthAfricaClock.StartOfDayUtc(start),
            SouthAfricaClock.StartOfDayUtc(end.AddDays(1)),
            cancellationToken);

        long seconds = 0;
        foreach (var entry in entries)
        {
            if (entry.Duration <= 0)
            {
                continue;
            }

            var date = SouthAfricaClock.Date(entry.StartUtc);
            if (date >= start && date <= end)
            {
                seconds += entry.Duration;
            }
        }

        return seconds;
    }

    private static string FormatEarnings(long syncedSeconds, long cycleSeconds, BillingPeriod open, decimal hourlyRate)
    {
        var syncedHours = Hours(syncedSeconds);
        var cycleHours = Hours(cycleSeconds);
        return
            $"Synced {FormatHours(syncedHours)} ({FormatMoney(Money(syncedHours, hourlyRate))}). " +
            $"This cycle {FormatRange(open.Start, open.End)}: {FormatHours(cycleHours)} ({FormatMoney(Money(cycleHours, hourlyRate))}).";
    }

    private static decimal Hours(long seconds) =>
        decimal.Round(seconds / 3600m, 2, MidpointRounding.AwayFromZero);

    private static decimal Money(decimal hours, decimal hourlyRate) =>
        decimal.Round(hours * hourlyRate, 2, MidpointRounding.AwayFromZero);

    private static string FormatHours(decimal hours) =>
        hours.ToString("0.00", CultureInfo.InvariantCulture) + "h";

    private static string FormatMoney(decimal amount) =>
        "R" + amount.ToString("#,##0.00", CultureInfo.InvariantCulture);

    private static string FormatRange(DateOnly start, DateOnly end)
    {
        if (start.Year == end.Year)
        {
            return string.Concat(
                start.ToString("d MMM", CultureInfo.InvariantCulture),
                "–",
                end.ToString("d MMM yyyy", CultureInfo.InvariantCulture));
        }

        return string.Concat(
            start.ToString("d MMM yyyy", CultureInfo.InvariantCulture),
            "–",
            end.ToString("d MMM yyyy", CultureInfo.InvariantCulture));
    }

}
