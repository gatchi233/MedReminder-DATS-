using System.Net.Http.Json;
using CareHub.Desktop.Models;
using CareHub.Desktop.Services.Sync;
using CareHub.Services.Abstractions;
using System.Net;

namespace CareHub.Services.Remote;

public sealed class MarApiService : IMarService
{
    private readonly HttpClient _http;

    public MarApiService(HttpClient http) => _http = http;

    public async Task<List<MarEntry>> LoadAsync(Guid? residentId, DateTime fromUtc, DateTime toUtc, bool includeVoided = false)
    {
        try
        {
            var url = $"api/mar?fromUtc={fromUtc:O}&toUtc={toUtc:O}";
            if (residentId.HasValue)
                url += $"&residentId={residentId.Value}";
            if (includeVoided)
                url += "&includeVoided=true";

            var items = await _http.GetFromJsonAsync<List<MarEntry>>(url);
            return items ?? new List<MarEntry>();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Load MAR).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Load MAR).", ex);
        }
    }

    public async Task CreateAsync(MarEntry entry)
    {
        try
        {
            var payload = new
            {
                clientRequestId  = entry.ClientRequestId,
                residentId       = entry.ResidentId,
                medicationId     = entry.MedicationId,
                status           = entry.Status,
                doseQuantity     = entry.DoseQuantity,
                doseUnit         = entry.DoseUnit,
                administeredAtUtc = entry.AdministeredAtUtc,
                scheduledForUtc  = entry.ScheduledForUtc,
                notes            = entry.Notes,
                recordedBy       = entry.RecordedBy
            };

            var resp = await _http.PostAsJsonAsync("api/mar", payload);
            await EnsureSuccessAsync(resp);

            var created = await resp.Content.ReadFromJsonAsync<MarEntry>();
            if (created is not null)
            {
                entry.Id = created.Id;
                entry.CreatedAtUtc = created.CreatedAtUtc;
                entry.UpdatedAtUtc = created.UpdatedAtUtc;
            }
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Create MAR).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Create MAR).", ex);
        }
    }

    public async Task VoidAsync(Guid id, string? reason)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync($"api/mar/{id}/void", new { reason });
            await EnsureSuccessAsync(resp);
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (Void MAR).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (Void MAR).", ex);
        }
    }

    public async Task<MarReport> GetReportAsync(DateTime fromUtc, DateTime toUtc, Guid? residentId)
    {
        try
        {
            var url = $"api/mar/report?fromUtc={fromUtc:O}&toUtc={toUtc:O}";
            if (residentId.HasValue)
                url += $"&residentId={residentId.Value}";

            var report = await _http.GetFromJsonAsync<MarReport>(url);
            return report ?? new MarReport();
        }
        catch (HttpRequestException ex)
        {
            throw new OfflineException("API unreachable (MAR report).", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new OfflineException("API timeout (MAR report).", ex);
        }
    }

    public Task<int> SyncAsync() => Task.FromResult(0);

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
