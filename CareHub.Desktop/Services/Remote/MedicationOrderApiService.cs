using System.Net.Http.Json;
using CareHub.Desktop.Services.Sync;
using CareHub.Models;
using CareHub.Services.Abstractions;
using System.Net;

namespace CareHub.Services.Remote;

public sealed class MedicationOrderApiService : IMedicationOrderService
{
    private readonly HttpClient _http;

    public MedicationOrderApiService(HttpClient http) => _http = http;

    public async Task<List<MedicationOrder>> LoadAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<MedicationOrder>>("api/medicationorders")
                   ?? new List<MedicationOrder>();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Load MedicationOrders).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Load MedicationOrders).", ex);
        }
    }

    public async Task<MedicationOrder> CreateAsync(Guid medicationId, int requestedQuantity, string? requestedBy, string? notes, string? medicationName = null)
    {
        try
        {
            var payload = new
            {
                medicationId,
                requestedQuantity,
                requestedBy,
                medicationName,
                notes
            };

            var resp = await _http.PostAsJsonAsync("api/medicationorders", payload);
            await EnsureSuccessAsync(resp);

            var created = await resp.Content.ReadFromJsonAsync<MedicationOrder>();
            return created ?? throw new InvalidOperationException("API returned null after creating order.");
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Create MedicationOrder).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Create MedicationOrder).", ex);
        }
    }

    public async Task UpdateStatusAsync(Guid orderId, MedicationOrderStatus newStatus, DateTimeOffset? expiryDate = null)
    {
        try
        {
            var payload = new
            {
                status = newStatus.ToString(),
                expiryDate
            };

            var resp = await _http.PutAsJsonAsync($"api/medicationorders/{orderId}/status", payload);
            await EnsureSuccessAsync(resp);
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Update MedicationOrder status).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Update MedicationOrder status).", ex);
        }
    }

    public async Task UpdateNameAsync(Guid orderId, string medicationName)
    {
        // Name updates are display-only, handled locally
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid orderId)
    {
        try
        {
            var resp = await _http.DeleteAsync($"api/medicationorders/{orderId}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return;
            await EnsureSuccessAsync(resp);
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Delete MedicationOrder).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Delete MedicationOrder).", ex);
        }
    }

    public async Task<List<MedicationOrder>> GetByMedicationIdAsync(Guid medicationId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<MedicationOrder>>($"api/medicationorders/by-medication/{medicationId}")
                   ?? new List<MedicationOrder>();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (GetByMedicationId).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (GetByMedicationId).", ex);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = await response.Content.ReadAsStringAsync();
        message = string.IsNullOrWhiteSpace(message)
            ? $"Request failed with status {(int)response.StatusCode}."
            : message.Trim().Trim('"');

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.NotFound)
            throw new InvalidOperationException(message);

        response.EnsureSuccessStatusCode();
    }
}
