using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using SpesnetTogglSync.Logging;

namespace SpesnetTogglSync.TogglApi;

/// <summary>
/// Single HTTP choke point for every Toggl Track request.
/// Set one breakpoint in <see cref="OnFailedResponse"/> to inspect any Toggl API failure.
/// </summary>
internal sealed class TogglApiHttp : IDisposable
{
    private const string BaseUrl = "https://api.track.toggl.com/api/v9";

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly HttpClient _httpClient;
    private readonly IApiLogger _logger;

    public TogglApiHttp(HttpClient httpClient, IApiLogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public static HttpClient CreateHttpClient(string apiToken)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };

        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{apiToken}:api_token"));
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        return client;
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
            OnFailedResponse(operation, requestUrl, requestPayload, response: null, rawResponse: null, exception: ex);
            throw;
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        OnFailedResponse(operation, requestUrl, requestPayload, response, rawResponse, exception: null);

        var message =
            $"Toggl failed to {operation}: {(int)response.StatusCode} {response.ReasonPhrase}. URL: {requestUrl}. {rawResponse}";
        _logger.Error(message);
        throw new HttpRequestException(message);
    }

    /// <summary>
    /// CENTRAL TOGGL BREAKPOINT — place your breakpoint on the Debugger.Break() line below.
    /// Locals: <paramref name="operation"/>, <paramref name="requestUrl"/>, <paramref name="requestPayload"/>,
    /// <paramref name="response"/>, <paramref name="rawResponse"/>, <paramref name="exception"/>.
    /// </summary>
    private static void OnFailedResponse(
        string operation,
        string requestUrl,
        string? requestPayload,
        HttpResponseMessage? response,
        string? rawResponse,
        Exception? exception)
    {
        _ = operation;
        _ = requestUrl;
        _ = requestPayload;
        _ = response;
        _ = rawResponse;
        _ = exception;

        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
