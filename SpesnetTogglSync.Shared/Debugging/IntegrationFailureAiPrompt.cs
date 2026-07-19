using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SpesnetTogglSync.Debugging;

/// <summary>
/// Builds a ready-to-paste AI prompt from an integration HTTP failure.
/// </summary>
public static partial class IntegrationFailureAiPrompt
{
    public static string Build(
        string integration,
        string operation,
        string httpMethod,
        string requestUrl,
        string? requestPayload,
        HttpStatusCode? statusCode,
        string? reasonPhrase,
        string? rawResponse,
        Exception? exception)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"The {integration} integration in SpesnetTogglSync failed. Diagnose the failure and propose a concrete code or configuration fix in this repository.");
        sb.AppendLine();
        sb.AppendLine("## Failure details");
        sb.AppendLine($"- Integration: {integration}");
        sb.AppendLine($"- Operation: {operation}");
        sb.AppendLine($"- HTTP method: {httpMethod}");
        sb.AppendLine($"- Request URL: {requestUrl}");
        sb.AppendLine($"- Status: {(statusCode is null ? "(no HTTP response)" : $"{(int)statusCode.Value} {reasonPhrase}")}");
        sb.AppendLine();
        sb.AppendLine("## Request payload");
        sb.AppendLine(string.IsNullOrWhiteSpace(requestPayload)
            ? "(none — typically a GET)"
            : RedactSecrets(requestPayload));
        sb.AppendLine();
        sb.AppendLine("## Raw response body");
        sb.AppendLine(string.IsNullOrWhiteSpace(rawResponse) ? "(none)" : rawResponse);
        sb.AppendLine();
        sb.AppendLine("## Exception");
        sb.AppendLine(exception is null
            ? "(none — HTTP error status)"
            : $"{exception.GetType().FullName}: {exception.Message}{Environment.NewLine}{exception.StackTrace}");
        sb.AppendLine();
        sb.AppendLine("## Context");
        sb.AppendLine("- WinForms .NET app syncing Toggl Track → Spesnet/EvolveMed Timekeeping.");
        sb.AppendLine("- Toggl HTTP lives in SpesnetTogglSync.TogglApi; Spesnet HTTP in SpesnetTogglSync.SpesnetApi.");
        sb.AppendLine("- Prefer fixing the root cause (auth, mapping, payload shape, URL, deserialization) with minimal diffs.");
        sb.AppendLine("- Do not invent alternate Toggl↔Spesnet domain mappings; see AGENTS.md.");
        return sb.ToString();
    }

    private static string RedactSecrets(string payload) =>
        PasswordFieldRegex().Replace(payload, "$1\"***REDACTED***\"");

    [GeneratedRegex(
        """(?i)(\x22(?:password|passwd|pwd|api[_-]?token|access[_-]?token|secret|authorization)\x22\s*:\s*)\x22(?:\\.|[^\x22\\])*\x22""",
        RegexOptions.CultureInvariant)]
    private static partial Regex PasswordFieldRegex();
}
