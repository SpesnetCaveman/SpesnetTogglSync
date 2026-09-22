namespace SpesnetTogglSync.Services;

/// <summary>South African time (GMT+2, no DST) for the daily midday prompt.</summary>
internal static class SouthAfricaClock
{
    public static readonly TimeSpan Midday = new(12, 0, 0);

    private static readonly TimeZoneInfo Zone = Resolve();

    public static DateTime Now() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public static string TodayString() => Now().ToString("yyyy-MM-dd");

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
