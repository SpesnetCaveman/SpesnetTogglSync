namespace SpesnetTogglSync.Services;

internal readonly record struct BillingPeriod(DateOnly Start, DateOnly End);

/// <summary>
/// A billing cycle starts on <see cref="CycleStart"/> and the report for the period
/// that ended the day before is filed in <see cref="FolderName"/> (yyyy-MM of that start).
/// </summary>
internal readonly record struct DueBillingReport(DateOnly CycleStart, BillingPeriod Period, string FolderName)
{
    public string CycleKey => CycleStart.ToString("yyyy-MM-dd");
}

/// <summary>
/// Billing cycles are South African calendar dates. Start day 20 means the 20th through the 19th.
/// Months shorter than the start day use the last day of that month.
/// </summary>
internal static class BillingCycle
{
    public static int NormalizeStartDay(int startDay) =>
        startDay < 1 ? 1 : startDay > 31 ? 31 : startDay;

    public static BillingPeriod OpenCycle(DateOnly today, int startDay)
    {
        var start = StartContaining(today, startDay);
        return new BillingPeriod(start, today);
    }

    public static DueBillingReport ReportDue(DateOnly today, int startDay)
    {
        var cycleStart = StartContaining(today, startDay);
        var periodEnd = cycleStart.AddDays(-1);
        var periodStart = StartContaining(periodEnd, startDay);
        return new DueBillingReport(cycleStart, new BillingPeriod(periodStart, periodEnd), cycleStart.ToString("yyyy-MM"));
    }

    public static DateOnly StartContaining(DateOnly date, int startDay)
    {
        var day = DayInMonth(date.Year, date.Month, startDay);
        if (date.Day >= day)
        {
            return new DateOnly(date.Year, date.Month, day);
        }

        var previous = date.AddMonths(-1);
        var previousDay = DayInMonth(previous.Year, previous.Month, startDay);
        return new DateOnly(previous.Year, previous.Month, previousDay);
    }

    private static int DayInMonth(int year, int month, int startDay) =>
        Math.Min(NormalizeStartDay(startDay), DateTime.DaysInMonth(year, month));
}
