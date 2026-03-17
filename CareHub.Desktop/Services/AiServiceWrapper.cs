using CareHub.Services.Abstractions;
using CareHub.Services.Remote;

namespace CareHub.Desktop.Services;

public sealed class AiServiceWrapper : IAiService
{
    private readonly AiApiService _api;

    public AiServiceWrapper(AiApiService api)
    {
        _api = api;
    }

    public Task<AiResult> ShiftSummaryAsync(Guid residentId) =>
        _api.ShiftSummaryAsync(residentId);

    public Task<AiResult> MedicationExplainAsync(string medicationName, string? dosage) =>
        _api.MedicationExplainAsync(medicationName, dosage);

    public Task<AiResult> DetectTrendsAsync(Guid residentId) =>
        _api.DetectTrendsAsync(residentId);

    public Task<AiResult> ReportDraftAsync(Guid residentId) =>
        _api.ReportDraftAsync(residentId);

    public Task<AiResult> ShiftHandoffAsync() =>
        _api.ShiftHandoffAsync();

    public Task<AiResult> CareQueryAsync(string query, Guid? residentId = null) =>
        _api.CareQueryAsync(query, residentId);

    public Task<AiResult> TrendExplainAsync(Guid residentId, int days) =>
        _api.TrendExplainAsync(residentId, days);
}
