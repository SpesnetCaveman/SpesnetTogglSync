using SpesnetTogglSync.Models;

namespace SpesnetTogglSync.SpesnetApi;

public interface ISpesnetTimekeepingClient : IDisposable
{
    Task LoginAsync(CancellationToken cancellationToken = default);
    Task<SpesnetUserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default);
    Task<int> GetEmployeeIdAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpesnetProject>> GetProjectsForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpesnetClient>> GetClientsByProjectAsync(int projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpesnetWorkTask>> GetWorkTasksAsync(CancellationToken cancellationToken = default);
    Task SaveWorkEntriesAsync(SpesnetSaveWorkRequest request, CancellationToken cancellationToken = default);
    Task<SpesnetReferenceCache> RefreshReferenceDataAsync(CancellationToken cancellationToken = default);
}
