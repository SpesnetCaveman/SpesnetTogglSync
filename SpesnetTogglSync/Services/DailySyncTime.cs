using System.Globalization;

namespace SpesnetTogglSync.Services;

/// <summary>South African clock time after which the daily sync runs as soon as the PC is on.</summary>
internal static class DailySyncTime
{
    public static readonly TimeSpan Default = new(7, 0, 0);

    public static TimeSpan Parse(string? value)
    {
        if (TimeSpan.TryParseExact(
                value?.Trim(),
                ["h\\:mm", "hh\\:mm", "h\\:mm\\:ss", "hh\\:mm\\:ss"],
                CultureInfo.InvariantCulture,
                out var parsed)
            && parsed >= TimeSpan.Zero
            && parsed < TimeSpan.FromDays(1))
        {
            return new TimeSpan(parsed.Hours, parsed.Minutes, 0);
        }

        return Default;
    }

    public static string Format(TimeSpan time) => $"{time.Hours:00}:{time.Minutes:00}";
}
