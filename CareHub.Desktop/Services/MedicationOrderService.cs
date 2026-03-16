using CareHub.Desktop.Services.Sync;
using CareHub.Models;
using CareHub.Services.Abstractions;

namespace CareHub.Desktop.Services;

public class MedicationOrderService : IMedicationOrderService
{
    private readonly IMedicationOrderService _api;
    private readonly IMedicationOrderService _local;

    public MedicationOrderService(IMedicationOrderService api, IMedicationOrderService local)
    {
        _api = api;
        _local = local;
    }

    public async Task<List<MedicationOrder>> LoadAsync()
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var items = await _api.LoadAsync();
                ConnectivityHelper.MarkOnline();
                return items;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        return await _local.LoadAsync();
    }

    public async Task<MedicationOrder> CreateAsync(Guid medicationId, int requestedQuantity, string? requestedBy, string? notes, string? medicationName = null)
    {
        // Always save locally
        var localOrder = await _local.CreateAsync(medicationId, requestedQuantity, requestedBy, notes, medicationName);

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var apiOrder = await _api.CreateAsync(medicationId, requestedQuantity, requestedBy, notes, medicationName);
                ConnectivityHelper.MarkOnline();
                // Update local with server-generated ID
                if (apiOrder.Id != localOrder.Id)
                {
                    await _local.DeleteAsync(localOrder.Id);
                    localOrder.Id = apiOrder.Id;
                    // Re-create with server ID by saving locally again
                }
                return apiOrder;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        return localOrder;
    }

    public async Task UpdateStatusAsync(Guid orderId, MedicationOrderStatus newStatus, DateTimeOffset? expiryDate = null)
    {
        // Always update locally
        await _local.UpdateStatusAsync(orderId, newStatus, expiryDate);

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _api.UpdateStatusAsync(orderId, newStatus, expiryDate);
                ConnectivityHelper.MarkOnline();
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }
    }

    public async Task UpdateNameAsync(Guid orderId, string medicationName)
    {
        await _local.UpdateNameAsync(orderId, medicationName);
    }

    public async Task DeleteAsync(Guid orderId)
    {
        await _local.DeleteAsync(orderId);

        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                await _api.DeleteAsync(orderId);
                ConnectivityHelper.MarkOnline();
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }
    }

    public async Task<List<MedicationOrder>> GetByMedicationIdAsync(Guid medicationId)
    {
        if (ConnectivityHelper.IsOnline())
        {
            try
            {
                var items = await _api.GetByMedicationIdAsync(medicationId);
                ConnectivityHelper.MarkOnline();
                return items;
            }
            catch
            {
                ConnectivityHelper.MarkOffline();
            }
        }

        return await _local.GetByMedicationIdAsync(medicationId);
    }
}
