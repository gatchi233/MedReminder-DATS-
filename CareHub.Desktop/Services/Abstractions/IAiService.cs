using CareHub.Services.Remote;

namespace CareHub.Services.Abstractions;

public interface IAiService
{
    Task<AiResult> ShiftSummaryAsync(Guid residentId);
    Task<AiResult> MedicationExplainAsync(string medicationName, string? dosage);
    Task<AiResult> DetectTrendsAsync(Guid residentId);
    Task<AiResult> ReportDraftAsync(Guid residentId);
    Task<AiResult> ShiftHandoffAsync();
    Task<AiResult> CareQueryAsync(string query, Guid? residentId = null);
    Task<AiResult> TrendExplainAsync(Guid residentId, int days);
}
