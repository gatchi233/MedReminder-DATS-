using MedReminder.Desktop.Services.Sync;
using MedReminder.Models;
using MedReminder.Services.Abstractions;
using System.Text.Json;

namespace MedReminder.Desktop.Services;

public class ObservationService : IObservationService
{
    private readonly IObservationService _api;
    private readonly IObservationService _local;
    private readonly ISyncQueue _queue;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ObservationService(
        IObservationService apiObservationService,
        IObservationService localObservationService,
        ISyncQueue queue)
    {
        _api = apiObservationService;
        _local = localObservationService;
        _queue = queue;
    }

    public async Task<List<Observation>> LoadAsync()
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                return await _api.LoadAsync();
            }
            catch
            {
                return await _local.LoadAsync();
            }
        }

        return await _local.LoadAsync();
    }

    public async Task<List<Observation>> GetByResidentIdAsync(Guid residentId)
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                return await _api.GetByResidentIdAsync(residentId);
            }
            catch
            {
                return await _local.GetByResidentIdAsync(residentId);
            }
        }

        return await _local.GetByResidentIdAsync(residentId);
    }

    public async Task UpsertAsync(Observation item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        if (item.Id == Guid.Empty)
        {
            await AddAsync(item);
            return;
        }

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _api.UpsertAsync(item);
                await _local.UpsertAsync(item);
                return;
            }
            catch
            {
                // fall back to local
            }
        }

        await _local.UpsertAsync(item);
    }

    public async Task DeleteAsync(Observation item)
    {
        if (item is null)
            return;

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _api.DeleteAsync(item);
                await _local.DeleteAsync(item);
                return;
            }
            catch
            {
                // fall back to local
            }
        }

        await _local.DeleteAsync(item);
    }

    public async Task AddAsync(Observation observation)
    {
        // Always use UTC for recorded/observed timestamp
        observation.RecordedAt = DateTime.UtcNow;

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _api.AddAsync(observation);

                // Optional: also cache locally
                await _local.AddAsync(observation);
                return;
            }
            catch
            {
                // API failed -> queue + local
            }
        }

        // Offline (or API error): save local + enqueue
        await _local.AddAsync(observation);

        var payloadJson = JsonSerializer.Serialize(observation, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "Observation",
            Operation = SyncOperation.Create,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    // M2: Sync only supports CREATE replay
    public async Task<int> SyncAsync()
    {
        if (!ConnectivityHelper.IsOnline()) return 0;

        var items = await _queue.GetAllAsync();
        if (items.Count == 0) return 0;

        int success = 0;

        foreach (var item in items.OrderBy(x => x.CreatedAtUtc))
        {
            if (item.EntityType != "Observation" || item.Operation != SyncOperation.Create)
                continue;

            try
            {
                var obs = JsonSerializer.Deserialize<Observation>(item.PayloadJson, _jsonOptions);
                if (obs is null)
                {
                    await _queue.RemoveAsync(item.Id);
                    continue;
                }

                // Ensure UTC again (safe)
                obs.RecordedAt = DateTime.SpecifyKind(obs.RecordedAt, DateTimeKind.Utc);

                await _api.AddAsync(obs);
                await _queue.RemoveAsync(item.Id);
                success++;
            }
            catch (Exception ex)
            {
                // Stop at first failure to avoid hammering / repeating errors
                // (M2 simple behavior)
                item.AttemptCount++;
                item.LastError = ex.Message;

                // You *could* write back updated error info if you want,
                // but keeping it simple is OK for M2.
                break;
            }
        }

        return success;
    }
}
