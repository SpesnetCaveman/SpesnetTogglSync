using SpesnetTogglSync.Models;

namespace SpesnetTogglSync.Services;

public interface ITogglClient : IDisposable
{
    Task<TogglMe> GetMeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TogglTimeEntry>> GetTimeEntriesSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TogglClient>> GetClientsAsync(long workspaceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TogglProject>> GetProjectsAsync(long workspaceId, CancellationToken cancellationToken = default);
}
