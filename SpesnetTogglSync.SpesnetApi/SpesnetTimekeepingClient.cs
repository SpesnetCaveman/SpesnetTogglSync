using System.Net.Http.Json;
using SpesnetTogglSync.Logging;
using SpesnetTogglSync.Models;

namespace SpesnetTogglSync.SpesnetApi;

public class SpesnetTimekeepingClient : ISpesnetTimekeepingClient
{
    private readonly SpesnetApiHttp _http;
    private readonly AppSettings _settings;
    private readonly IApiLogger _logger;
    private int _employeeId;

    public SpesnetTimekeepingClient(AppSettings settings, IApiLogger logger)
    {
        _settings = settings;
        _logger = logger;
        _http = new SpesnetApiHttp(SpesnetApiHttp.CreateHttpClient(settings.SpesnetDomain), logger);
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

        await _http.SendAsync(
            "login",
            HttpMethod.Post,
            "api/Account/Login",
            payload,
            cancellationToken);

        _logger.Info("Spesnet: login successful");
    }

    public async Task<SpesnetUserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.SendAsync(
            "get user info",
            HttpMethod.Get,
            "api/User/GetUserInfo",
            cancellationToken: cancellationToken);
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
        var response = await _http.SendAsync(
            "get employee by date",
            HttpMethod.Get,
            url,
            cancellationToken: cancellationToken);
        var employeeResponse = await response.Content.ReadFromJsonAsync<SpesnetEmployeeResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Spesnet employee response was empty.");
        _employeeId = employeeResponse.CurrentUser?.Id
            ?? throw new InvalidOperationException("Spesnet current user was not found.");
        _logger.Info($"Spesnet: resolved employee id {_employeeId}");
        return _employeeId;
    }

    public async Task<IReadOnlyList<SpesnetProject>> GetProjectsForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var response = await _http.SendAsync(
            "get projects for employee",
            HttpMethod.Get,
            $"api/Project/GetProjectForEmployee?employeeId={employeeId}",
            cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<SpesnetProject>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<SpesnetClient>> GetClientsByProjectAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var response = await _http.SendAsync(
            "get clients by project",
            HttpMethod.Get,
            $"api/Client/GetClientsByProject?projectId={projectId}",
            cancellationToken: cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<SpesnetClientsByProjectResponse>(cancellationToken: cancellationToken);
        return payload?.Clients ?? [];
    }

    public async Task<IReadOnlyList<SpesnetWorkTask>> GetWorkTasksAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.SendAsync(
            "get work tasks",
            HttpMethod.Get,
            "api/worktask",
            cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<SpesnetWorkTask>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task SaveWorkEntriesAsync(SpesnetSaveWorkRequest request, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Spesnet: saving {request.WorkDoneList.Count} work entries");
        await _http.SendAsync(
            "save work entries",
            HttpMethod.Post,
            "api/worktask/save",
            request,
            cancellationToken);
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

    public void Dispose() => _http.Dispose();
}
