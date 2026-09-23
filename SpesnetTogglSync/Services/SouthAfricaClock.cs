namespace SpesnetTogglSync.Services;

/// <summary>South African time (GMT+2, no DST).</summary>
internal static class SouthAfricaClock
{
    private static readonly TimeZoneInfo Zone = Resolve();

    public static DateTime Now() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public static DateOnly Today() => DateOnly.FromDateTime(Now());

    public static string TodayString() => Now().ToString("yyyy-MM-dd");

    public static DateOnly Date(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(ToUtc(utc), Zone));

    public static DateTime StartOfDayUtc(DateOnly date)
    {
        var unspecified = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, Zone);
    }

    public static string FormatTime(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(ToUtc(utc), Zone).ToString("HH:mm");

    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static TimeZoneInfo Resolve()
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

        return TimeZoneInfo.CreateCustomTimeZone(
            "South Africa Standard Time",
            TimeSpan.FromHours(2),
            "South Africa Standard Time",
            "South Africa Standard Time");
    }
}
