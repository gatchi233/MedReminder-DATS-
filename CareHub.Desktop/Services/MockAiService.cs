using CareHub.Services.Abstractions;
using CareHub.Services.Remote;

namespace CareHub.Desktop.Services;

public sealed class MockAiService : IAiService
{
    private static readonly string Disclaimer = "AI-Generated Draft (MOCK) - Review Required";

    public Task<AiResult> ShiftSummaryAsync(Guid residentId) =>
        Task.FromResult(new AiResult
        {
            Success = true,
            Disclaimer = Disclaimer,
            Content = """
                SHIFT SUMMARY (Mock)
                - Resident is stable with no acute concerns
                - Vital signs within normal parameters for the past 24 hours
                - All scheduled medications administered as ordered
                - No refusals or missed doses recorded
                - Continue routine monitoring per care plan

                Note: This is mock data. Connect to the AI service for real analysis.
                """
        });

    public Task<AiResult> MedicationExplainAsync(string medicationName, string? dosage) =>
        Task.FromResult(new AiResult
        {
            Success = true,
            Disclaimer = Disclaimer,
            Content = $"""
                MEDICATION INFO (Mock): {medicationName}
                - Purpose: Used for management of common conditions
                - Common side effects: drowsiness, nausea, headache
                - Staff notes: Monitor for adverse reactions, give with food if applicable

                Note: This is mock data. Connect to the AI service for real information.
                """
        });

    public Task<AiResult> DetectTrendsAsync(Guid residentId) =>
        Task.FromResult(new AiResult
        {
            Success = true,
            Disclaimer = Disclaimer,
            Content = """
                TREND ANALYSIS (Mock)
                - Vital signs: Stable over the analysis period
                - Medication compliance: Within expected range
                - No concerning patterns detected
                - Recommendation: Continue routine monitoring

                Note: This is mock data. Connect to the AI service for real analysis.
                """
        });

    public Task<AiResult> ReportDraftAsync(Guid residentId) =>
        Task.FromResult(new AiResult
        {
            Success = true,
            Disclaimer = Disclaimer,
            Content = """
                RESIDENT REPORT DRAFT (Mock)

                1. RESIDENT OVERVIEW
                Demographics and room assignment on file. Primary physician contacted as needed.

                2. CURRENT MEDICATIONS
                All medications active and administered per schedule. No recent changes to orders.

                3. RECENT OBSERVATIONS SUMMARY
                Vital signs recorded within normal ranges. No significant deviations noted.

                4. MEDICATION COMPLIANCE
                Compliance rate satisfactory. No pattern of refusals or missed doses.

                5. CARE NOTES
                Resident cooperative and engaged. No new concerns raised by staff.
                Continue current care plan. Next review scheduled per protocol.

                Note: This is mock data. Connect to the AI service for real report drafts.
                """
        });

    public Task<AiResult> ShiftHandoffAsync() =>
        Task.FromResult(new AiResult
        {
            Success = true,
            Disclaimer = Disclaimer,
            Content = """
                SHIFT HANDOFF SUMMARY (Mock)

                1. PRIORITY ITEMS
                - No urgent items requiring immediate attention

                2. RESIDENT UPDATES
                - All residents stable with no acute changes
                - Routine vitals completed for the shift

                3. MEDICATION COMPLIANCE
                - Overall compliance within normal range
                - No refusals or missed doses to report

                4. FOLLOW-UP ITEMS
                - Continue routine monitoring per care plans
                - No pending physician callbacks

                Note: This is mock data. Connect to the AI service for real handoff summaries.
                """
        });

    public Task<AiResult> CareQueryAsync(string query, Guid? residentId = null) =>
        Task.FromResult(new AiResult
        {
            Success = true,
            Disclaimer = Disclaimer,
            Content = $"""
                QUERY RESPONSE (Mock)

                Your question: "{query}"

                The AI care query service is currently using mock responses.
                When connected to the AI service, this feature will analyze facility
                data to answer natural-language questions about residents, medications,
                observations, and care operations.

                Examples of supported questions:
                - "Which residents refused medications this week?"
                - "What are the recent vital sign trends for Room 101?"
                - "How many missed doses were recorded today?"

                Note: Connect to the AI service for real query responses.
                """
        });

    public Task<AiResult> TrendExplainAsync(Guid residentId, int days) =>
        Task.FromResult(new AiResult
        {
            Success = true,
            Disclaimer = Disclaimer,
            Content = $"""
                {days}-DAY TREND EXPLANATION (Mock)

                1. VITAL SIGNS TRENDS
                - Temperature: Stable
                - Blood Pressure: Within normal range
                - Pulse: No significant changes
                - SpO2: Consistently normal

                2. MEDICATION PATTERNS
                - Compliance rate: Satisfactory
                - No refusals or missed doses in the period

                3. KEY OBSERVATIONS
                - No notable events recorded

                4. RECOMMENDATIONS
                - Continue routine monitoring
                - No immediate staff action required

                Note: This is mock data. Connect to the AI service for real trend analysis.
                """
        });
}
