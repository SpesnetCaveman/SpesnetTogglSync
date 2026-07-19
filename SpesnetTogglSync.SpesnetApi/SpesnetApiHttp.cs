using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SpesnetTogglSync.Debugging;
using SpesnetTogglSync.Logging;

namespace SpesnetTogglSync.SpesnetApi;

/// <summary>
/// Single HTTP choke point for every Spesnet / EvolveMed Timekeeping request.
/// Set one breakpoint in <see cref="CreateFailure"/> to inspect any Spesnet API failure.
/// </summary>
internal sealed class SpesnetApiHttp : IDisposable
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly HttpClient _httpClient;
    private readonly IApiLogger _logger;

    public SpesnetApiHttp(HttpClient httpClient, IApiLogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public static HttpClient CreateHttpClient(string domain)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(NormalizeDomain(domain))
        };
    }

    public async Task<HttpResponseMessage> SendAsync(
        string operation,
        HttpMethod method,
        string relativeUrl,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        var requestUrl = new Uri(_httpClient.BaseAddress!, relativeUrl).ToString();
        var requestPayload = payload is null
            ? null
            : JsonSerializer.Serialize(payload, PayloadJsonOptions);

        using var request = new HttpRequestMessage(method, relativeUrl);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw CreateFailure(operation, method, requestUrl, requestPayload, response: null, rawResponse: null, exception: ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        throw CreateFailure(operation, method, requestUrl, requestPayload, response, rawResponse, exception: null);
    }

    /// <summary>
    /// CENTRAL SPESNET BREAKPOINT — place your breakpoint on the Debugger.Break() line below.
    /// Copy <c>aiPrompt</c> from Locals, or read it from the thrown exception message / error popup.
    /// </summary>
    private HttpRequestException CreateFailure(
        string operation,
        HttpMethod method,
        string requestUrl,
        string? requestPayload,
        HttpResponseMessage? response,
        string? rawResponse,
        Exception? exception)
    {
        var aiPrompt = IntegrationFailureAiPrompt.Build(
            integration: "Spesnet / EvolveMed Timekeeping",
            operation: operation,
            httpMethod: method.Method,
            requestUrl: requestUrl,
            requestPayload: requestPayload,
            statusCode: response?.StatusCode,
            reasonPhrase: response?.ReasonPhrase,
            rawResponse: rawResponse,
            exception: exception);

        _ = aiPrompt;
        _ = operation;
        _ = method;
        _ = requestUrl;
        _ = requestPayload;
        _ = response;
        _ = rawResponse;
        _ = exception;

        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }

        _logger.Error($"Spesnet failed to {operation}: {(response is null ? exception?.Message : $"{(int)response.StatusCode} {response.ReasonPhrase}")}. URL: {requestUrl}");
        return new HttpRequestException(aiPrompt, exception);
    }

    private static string NormalizeDomain(string domain)
    {
        var normalized = domain.Trim();
        if (!normalized.EndsWith('/'))
        {
            normalized += "/";
        }

        return normalized;
    }

    public void Dispose() => _httpClient.Dispose();
}
