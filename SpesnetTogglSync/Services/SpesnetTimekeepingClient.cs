using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SpesnetTogglSync.Models;

namespace SpesnetTogglSync.Services;

public class SpesnetTimekeepingClient : ISpesnetTimekeepingClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;
    private readonly FileLogger _logger;
    private int _employeeId;

    public SpesnetTimekeepingClient(AppSettings settings, FileLogger logger)
    {
        _settings = settings;
        _logger = logger;
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true
        };
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(NormalizeDomain(settings.SpesnetDomain))
        };
    }

    public async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Spesnet: logging in");
        var payload = new SpesnetLoginRequest
        {
            Username = _settings.SpesnetUsername,
            Password = _settings.SpesnetPassword,
            RememberMe = false
        };

        var response = await _httpClient.PostAsJsonAsync("api/Account/Login", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Spesnet login failed: {(int)response.StatusCode}. {body}");
        }

        _logger.Info("Spesnet: login successful");
    }

    public async Task<SpesnetUserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/User/GetUserInfo", cancellationToken);
        await EnsureSuccessAsync(response, "get user info");
        return await response.Content.ReadFromJsonAsync<SpesnetUserInfo>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Spesnet user info was empty.");
    }

    public async Task<int> GetEmployeeIdAsync(CancellationToken cancellationToken = default)
    {
        if (_employeeId > 0)
        {
            return _employeeId;
        }

        var workDate = DateTime.Now.ToString("ddd MMM dd yyyy");
        var url = $"api/employee/GetEmployeeByDate?workDate={Uri.EscapeDataString(workDate)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, "get employee by date");
        var employeeResponse = await response.Content.ReadFromJsonAsync<SpesnetEmployeeResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Spesnet employee response was empty.");
        _employeeId = employeeResponse.CurrentUser?.Id
            ?? throw new InvalidOperationException("Spesnet current user was not found.");
        _logger.Info($"Spesnet: resolved employee id {_employeeId}");
        return _employeeId;
    }

    public async Task<IReadOnlyList<SpesnetProject>> GetProjectsForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/Project/GetProjectForEmployee?employeeId={employeeId}", cancellationToken);
        await EnsureSuccessAsync(response, "get projects for employee");
        return await response.Content.ReadFromJsonAsync<List<SpesnetProject>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<SpesnetClient>> GetClientsByProjectAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/Client/GetClientsByProject?projectId={projectId}", cancellationToken);
        await EnsureSuccessAsync(response, "get clients by project");
        var payload = await response.Content.ReadFromJsonAsync<SpesnetClientsByProjectResponse>(cancellationToken: cancellationToken);
        return payload?.Clients ?? [];
    }

    public async Task<IReadOnlyList<SpesnetWorkTask>> GetWorkTasksAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/worktask", cancellationToken);
        await EnsureSuccessAsync(response, "get work tasks");
        return await response.Content.ReadFromJsonAsync<List<SpesnetWorkTask>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task SaveWorkEntriesAsync(SpesnetSaveWorkRequest request, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Spesnet: saving {request.WorkDoneList.Count} work entries");
        var response = await _httpClient.PostAsJsonAsync("api/worktask/save", request, cancellationToken);
        await EnsureSuccessAsync(response, "save work entries");
    }

    public async Task<SpesnetReferenceCache> RefreshReferenceDataAsync(CancellationToken cancellationToken = default)
    {
        await LoginAsync(cancellationToken);
        var employeeId = await GetEmployeeIdAsync(cancellationToken);
        var projects = (await GetProjectsForEmployeeAsync(employeeId, cancellationToken)).ToList();
        var workTasks = (await GetWorkTasksAsync(cancellationToken)).ToList();
        var clientsByProject = new Dictionary<int, List<SpesnetClient>>();

        foreach (var project in projects)
        {
            var clients = (await GetClientsByProjectAsync(project.Id, cancellationToken)).ToList();
            clientsByProject[project.Id] = clients;
        }

        return new SpesnetReferenceCache
        {
            EmployeeId = employeeId,
            Projects = projects,
            WorkTasks = workTasks,
            ClientsByProject = clientsByProject
        };
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        var message = $"Spesnet failed to {operation}: {(int)response.StatusCode} {response.ReasonPhrase}. {body}";
        _logger.Error(message);
        throw new HttpRequestException(message);
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
