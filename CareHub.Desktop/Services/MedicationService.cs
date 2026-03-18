using CareHub.Desktop.Services.Sync;
using CareHub.Models;
using CareHub.Services.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CareHub.Desktop.Services;

public class MedicationService : IMedicationService
{
    private readonly IMedicationService _apiMed;
    private readonly IMedicationService _localMed;
    private readonly ISyncQueue _queue;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public MedicationService(
        IMedicationService apiService,
        IMedicationService localService,
        ISyncQueue queue)
    {
        _apiMed = apiService;
        _localMed = localService;
        _queue = queue;
    }

    public async Task<List<Medication>> LoadAsync()
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var apiItems = await _apiMed.LoadAsync();
                ConnectivityHelper.MarkOnline();

                // Merge: keep local-only items that are not in the API yet.
                var localItems = await _localMed.LoadAsync();

                // If API payload omits global inventory rows, keep local inventory rows
                // so Inventory page does not go empty after a refresh.
                var apiHasInventory = apiItems.Any(m => m.ResidentId == null || m.ResidentId == Guid.Empty);
                if (!apiHasInventory)
                {
                    var localInventory = localItems
                        .Where(m => m.ResidentId == null || m.ResidentId == Guid.Empty)
                        .ToList();

                    foreach (var inv in localInventory)
                    {
                        if (!apiItems.Any(m => m.Id == inv.Id))
                            apiItems.Add(inv);
                    }
                }

                var apiIds = new HashSet<Guid>(apiItems.Select(m => m.Id));
                var localOnly = localItems.Where(m => !apiIds.Contains(m.Id)).ToList();

                if (localOnly.Count > 0)
                {
                    // Build a lookup of API items by (MedName, ResidentId) to avoid duplicates
                    var apiKeys = new HashSet<string>(
                        apiItems.Select(m => $"{(m.MedName ?? "").ToLowerInvariant()}|{m.ResidentId}"),
                        StringComparer.Ordinal);

                    foreach (var med in localOnly)
                    {
                        var key = $"{(med.MedName ?? "").ToLowerInvariant()}|{med.ResidentId}";
                        if (apiKeys.Contains(key))
                            continue;

                        try
                        {
                            await _apiMed.UpsertAsync(med);
                            apiItems.Add(med);
                        }
                        catch (OfflineException)
                        {
                            break;
                        }
                        catch
                        {
                            // Ignore non-offline upsert failures for local-only items.
                        }
                    }
                }

                // Merge local fields into API items where API is missing them (non-blocking)
                var localById = localItems
                    .GroupBy(m => m.Id)
                    .ToDictionary(g => g.Key, g => g.First());
                var toSync = new List<Medication>();
                foreach (var apiItem in apiItems)
                {
                    if (!localById.TryGetValue(apiItem.Id, out var localItem))
                        continue;

                    if (!apiItem.PurchaseDate.HasValue && localItem.PurchaseDate.HasValue)
                    {
                        apiItem.PurchaseDate = localItem.PurchaseDate;
                        toSync.Add(apiItem);
                    }
                }

                if (toSync.Count > 0)
                {
                    _ = Task.Run(async () =>
                    {
                        foreach (var med in toSync)
                        {
                            try { await _apiMed.UpsertAsync(med); }
                            catch { break; }
                        }
                    });
                }

                await _localMed.ReplaceAllAsync(apiItems);
                return apiItems;
            }
            catch (OfflineException)
            {
                ConnectivityHelper.MarkOffline();
                return await _localMed.LoadAsync();
            }
        }

        return await _localMed.LoadAsync();
    }

    public async Task UpsertAsync(Medication item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        // Medication.Id defaults to Guid.NewGuid(), so it's never empty.
        // Detect "create" by checking if the item already exists locally.
        var localList = await _localMed.LoadAsync();
        var isCreate = !localList.Any(m => m.Id == item.Id);

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _apiMed.UpsertAsync(item);
                ConnectivityHelper.MarkOnline();
                await _localMed.UpsertAsync(item);
                return;
            }
            catch (OfflineException)
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        await _localMed.UpsertAsync(item);

        var operation = isCreate ? SyncOperation.Create : SyncOperation.Update;
        var payloadJson = JsonSerializer.Serialize(item, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "Medication",
            Operation = operation,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public async Task DeleteAsync(Medication item)
    {
        if (item is null)
            return;

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _apiMed.DeleteAsync(item);
                ConnectivityHelper.MarkOnline();
                await _localMed.DeleteAsync(item);
                return;
            }
            catch (OfflineException)
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        await _localMed.DeleteAsync(item);

        var payloadJson = JsonSerializer.Serialize(item, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "Medication",
            Operation = SyncOperation.Delete,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public Task ReplaceAllAsync(List<Medication> items) => _localMed.ReplaceAllAsync(items);

    public async Task<List<Medication>> GetLowStockAsync()
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var items = await _apiMed.GetLowStockAsync();
                ConnectivityHelper.MarkOnline();
                return items;
            }
            catch (OfflineException)
            {
                ConnectivityHelper.MarkOffline();
                return await _localMed.GetLowStockAsync();
            }
        }

        return await _localMed.GetLowStockAsync();
    }

    public async Task AdjustStockFifoAsync(string medName, int delta)
    {
        var localList = await _localMed.LoadAsync();
        var batches = localList
            .Where(m => (m.ResidentId == null || m.ResidentId == Guid.Empty)
                && string.Equals(m.MedName, medName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.ExpiryDate)
            .ToList();

        var perBatchDeltas = new List<(Guid Id, int Delta)>();
        if (batches.Count > 0)
        {
            if (delta < 0)
            {
                int remaining = -delta;
                foreach (var b in batches)
                {
                    if (remaining <= 0) break;
                    int deduct = Math.Min(b.StockQuantity, remaining);
                    if (deduct > 0)
                        perBatchDeltas.Add((b.Id, -deduct));
                    remaining -= deduct;
                }
            }
            else if (delta > 0)
            {
                perBatchDeltas.Add((batches[0].Id, delta));
            }
        }

        await _localMed.AdjustStockFifoAsync(medName, delta);

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                foreach (var (id, d) in perBatchDeltas)
                    await _apiMed.AdjustStockAsync(id, d);
                ConnectivityHelper.MarkOnline();
                return;
            }
            catch (OfflineException)
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        foreach (var (id, d) in perBatchDeltas)
        {
            var payload = new { MedicationId = id, Delta = d };
            var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);

            await _queue.EnqueueAsync(new SyncQueueItem
            {
                EntityType = "StockAdjustment",
                Operation = SyncOperation.Update,
                PayloadJson = payloadJson,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    public async Task AdjustStockAsync(Guid medicationId, int delta)
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _apiMed.AdjustStockAsync(medicationId, delta);
                ConnectivityHelper.MarkOnline();
                await _localMed.AdjustStockAsync(medicationId, delta);
                return;
            }
            catch (OfflineException)
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        await _localMed.AdjustStockAsync(medicationId, delta);

        var payload = new { MedicationId = medicationId, Delta = delta };
        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);

        await _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = "StockAdjustment",
            Operation = SyncOperation.Update,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public async Task<int> SyncAsync()
    {
        System.Diagnostics.Debug.WriteLine("[SYNC] MedicationService.SyncAsync called");

        if (!ConnectivityHelper.IsOnline()) return 0;

        var items = await _queue.GetAllAsync();
        if (items.Count == 0) return 0;

        int success = 0;

        foreach (var item in items.Where(x => x.EntityType == "Medication").OrderBy(x => x.CreatedAtUtc))
        {
            try
            {
                var med = JsonSerializer.Deserialize<Medication>(item.PayloadJson, _jsonOptions);
                if (med is null)
                {
                    await _queue.RemoveAsync(item.Id);
                    continue;
                }

                switch (item.Operation)
                {
                    case SyncOperation.Create:
                    case SyncOperation.Update:
                        await _apiMed.UpsertAsync(med);
                        break;
                    case SyncOperation.Delete:
                        await _apiMed.DeleteAsync(med);
                        break;
                }

                await _queue.RemoveAsync(item.Id);
                success++;
            }
            catch
            {
                break;
            }
        }

        foreach (var item in items.Where(x => x.EntityType == "StockAdjustment").OrderBy(x => x.CreatedAtUtc))
        {
            try
            {
                var node = JsonNode.Parse(item.PayloadJson);
                var medicationId = node?["medicationId"]?.GetValue<Guid>() ?? Guid.Empty;
                var delta = node?["delta"]?.GetValue<int>() ?? 0;

                if (medicationId == Guid.Empty)
                {
                    await _queue.RemoveAsync(item.Id);
                    continue;
                }

                await _apiMed.AdjustStockAsync(medicationId, delta);
                await _queue.RemoveAsync(item.Id);
                success++;
            }
            catch
            {
                break;
            }
        }

        return success;
    }

    /// <summary>
    /// After a medication's temp ID is replaced by the server-assigned ID,
    /// update any pending StockAdjustment queue items that still reference
    /// the old MedicationId so they can sync successfully.
    /// </summary>
    private async Task RemapMedicationIdInQueue(Guid oldId, Guid newId)
    {
        var all = await _queue.GetAllAsync();
        var oldIdStr = oldId.ToString();
        var newIdStr = newId.ToString();

        foreach (var qi in all.Where(q => q.EntityType == "StockAdjustment" && q.PayloadJson.Contains(oldIdStr)))
        {
            try
            {
                var node = JsonNode.Parse(qi.PayloadJson);
                if (node is JsonObject obj && obj["medicationId"]?.ToString() == oldIdStr)
                {
                    obj["medicationId"] = newIdStr;
                    qi.PayloadJson = obj.ToJsonString(_jsonOptions);

                    await _queue.RemoveAsync(qi.Id);
                    await _queue.EnqueueAsync(qi);

                    System.Diagnostics.Debug.WriteLine(
                        $"[SYNC] Remapped MedicationId {oldId} -> {newId} in queued StockAdjustment");
                }
            }
            catch { }
        }
    }
}
