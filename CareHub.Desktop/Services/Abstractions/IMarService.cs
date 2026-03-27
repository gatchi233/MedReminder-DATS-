using CareHub.Desktop.Models;

namespace CareHub.Services.Abstractions;

public interface IMarService
{
    Task<List<MarEntry>> LoadAsync(Guid? residentId, DateTime fromUtc, DateTime toUtc, bool includeVoided = false);
    Task CreateAsync(MarEntry entry);
    Task VoidAsync(Guid id, string? reason);
    Task<MarReport> GetReportAsync(DateTime fromUtc, DateTime toUtc, Guid? residentId);
    Task<int> SyncAsync();
}
