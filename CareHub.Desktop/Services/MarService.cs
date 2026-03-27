using CareHub.Desktop.Models;
using CareHub.Desktop.Services.Sync;
using CareHub.Services.Abstractions;
using System.Text.Json;

namespace CareHub.Desktop.Services;

public class MarService : IMarService
{
    private readonly IMarService _api;
    private readonly IMarService _local;
    private readonly ISyncQueue _queue;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public MarService(IMarService api, IMarService local, ISyncQueue queue)
    {
        _api = api;
        _local = local;
        _queue = queue;
    }

    public async Task<List<MarEntry>> LoadAsync(Guid? residentId, DateTime fromUtc, DateTime toUtc, bool includeVoided = false)
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var items = await _api.LoadAsync(residentId, fromUtc, toUtc, includeVoided);
                ConnectivityHelper.MarkOnline();

                if (_local is CareHub.Services.Local.MarJsonService localJson)
                    await localJson.ReplaceAllAsync(items);

                var pending = await _queue.GetAllAsync();
                var pendingMar = pending.Where(x => x.EntityType == "MarEntry").ToList();
                if (pendingMar.Count > 0)
                {
                    var existingIds = new HashSet<Guid>(items.Select(e => e.ClientRequestId));
                    foreach (var p in pendingMar)
                    {
                        var entry = JsonSerializer.Deserialize<MarEntry>(p.PayloadJson, _jsonOptions);
                        if (entry is not null && !existingIds.Contains(entry.ClientRequestId))
                            items.Add(entry);
                    }
                }

                return items;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
                return await _local.LoadAsync(residentId, fromUtc, toUtc, includeVoided);
            }
        }

        return await _local.LoadAsync(residentId, fromUtc, toUtc, includeVoided);
    }

    public async Task CreateAsync(MarEntry entry)
    {
        if (entry.ClientRequestId == Guid.Empty)
            entry.ClientRequestId = Guid.NewGuid();

        entry.CreatedAtUtc = DateTimeOffset.UtcNow;
        entry.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _api.CreateAsync(entry);
                ConnectivityHelper.MarkOnline();
                await _local.CreateAsync(entry);
                return;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        await _local.CreateAsync(entry);

        var payloadJson = JsonSerializer.Serialize(entry, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "MarEntry",
            Operation = SyncOperation.Create,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public async Task<int> SyncAsync()
    {
        if (!ConnectivityHelper.IsOnline()) return 0;

        var items = await _queue.GetAllAsync();
        if (items.Count == 0) return 0;

        int success = 0;

        foreach (var item in items.Where(x => x.EntityType == "MarEntry").OrderBy(x => x.CreatedAtUtc))
        {
            try
            {
                var entry = JsonSerializer.Deserialize<MarEntry>(item.PayloadJson, _jsonOptions);
                if (entry is null)
                {
                    await _queue.RemoveAsync(item.Id);
                    continue;
                }

                await _api.CreateAsync(entry);

                await _queue.RemoveAsync(item.Id);
                success++;
            }
            catch (Exception ex)
            {
                item.AttemptCount++;
                item.LastError = ex.Message;
                break;
            }
        }

        return success;
    }

    public async Task VoidAsync(Guid id, string? reason)
    {
        await _local.VoidAsync(id, reason);

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _api.VoidAsync(id, reason);
                ConnectivityHelper.MarkOnline();
            }
            catch (OfflineException)
            {
                ConnectivityHelper.MarkOffline();
            }
        }
    }

    public async Task<MarReport> GetReportAsync(DateTime fromUtc, DateTime toUtc, Guid? residentId)
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var report = await _api.GetReportAsync(fromUtc, toUtc, residentId);
                ConnectivityHelper.MarkOnline();
                return report;
            }
            catch (OfflineException)
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        return await _local.GetReportAsync(fromUtc, toUtc, residentId);
    }
}
