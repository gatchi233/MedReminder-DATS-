using CareHub.Api.Data;
using CareHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Roles = $"{Roles.Staff},{Roles.Admin}")]
public sealed class AiController : ControllerBase
{
    private readonly CareHubDbContext _db;
    private readonly GroqAiService _ai;
    private readonly AiRateLimiter _limiter;

    public AiController(CareHubDbContext db, GroqAiService ai, AiRateLimiter limiter)
    {
        _db = db;
        _ai = ai;
        _limiter = limiter;
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User.Identity?.Name
        ?? "anonymous";

    // POST api/ai/shift-summary
    [HttpPost("shift-summary")]
    public async Task<IActionResult> ShiftSummary(
        [FromBody] AiResidentRequest request, CancellationToken ct)
    {
        var rateLimitMsg = _limiter.TryAcquire(GetUserId());
        if (rateLimitMsg is not null)
            return StatusCode(429, new { error = rateLimitMsg });

        var resident = await _db.Residents.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResidentId, ct);
        if (resident is null) return NotFound("Resident not found.");

        var since = DateTimeOffset.UtcNow.AddHours(-24);

        var observations = await _db.Observations.AsNoTracking()
            .Where(o => o.ResidentId == request.ResidentId && o.RecordedAt >= since.UtcDateTime)
            .OrderByDescending(o => o.RecordedAt)
            .Take(20)
            .ToListAsync(ct);

        var marEntries = await _db.MarEntries.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId && !m.IsVoided
                        && m.AdministeredAtUtc >= since)
            .OrderByDescending(m => m.AdministeredAtUtc)
            .Take(30)
            .ToListAsync(ct);

        var medications = await _db.Medications.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId)
            .ToListAsync(ct);

        var residentName = $"{resident.ResidentFName} {resident.ResidentLName}".Trim();

        var dataBlock = new StringBuilder();
        dataBlock.AppendLine($"Resident: {residentName}");
        dataBlock.AppendLine($"Room: {resident.RoomNumber}, DOB: {resident.DateOfBirth}, Gender: {resident.Gender}");
        dataBlock.AppendLine();

        dataBlock.AppendLine("== Active Medications ==");
        foreach (var m in medications)
            dataBlock.AppendLine($"- {m.MedName} ({m.Dosage}), {m.TimesPerDay}x/day");

        dataBlock.AppendLine();
        dataBlock.AppendLine("== Recent Observations (last 24h) ==");
        foreach (var o in observations)
            dataBlock.AppendLine($"- [{o.RecordedAt:yyyy-MM-dd HH:mm}] {o.Type}: {o.Value} (by {o.RecordedBy})");

        dataBlock.AppendLine();
        dataBlock.AppendLine("== Recent MAR Entries (last 24h) ==");
        foreach (var e in marEntries)
        {
            var medName = medications.FirstOrDefault(m => m.Id == e.MedicationId)?.MedName ?? "Unknown";
            dataBlock.AppendLine($"- [{e.AdministeredAtUtc:yyyy-MM-dd HH:mm}] {medName}: {e.Status} ({e.DoseQuantity} {e.DoseUnit}) by {e.RecordedBy}");
        }

        var systemPrompt = """
            You are a clinical assistant for a care facility. Generate a concise,
            plain-language shift summary for the incoming staff. Include:
            - Overall status of the resident
            - Key observations and any concerning trends
            - Medication administration summary (given, refused, missed)
            - Any items requiring follow-up
            Keep it under 200 words. Use bullet points. Do not make diagnoses.
            """;

        try
        {
            var result = await _ai.CompleteAsync(systemPrompt, dataBlock.ToString(), ct);
            return Ok(new AiResponse { Content = result, ResidentName = residentName });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "AI service unavailable.", detail = ex.Message });
        }
    }

    // POST api/ai/medication-explain
    [HttpPost("medication-explain")]
    public async Task<IActionResult> MedicationExplain(
        [FromBody] AiMedicationRequest request, CancellationToken ct)
    {
        var rateLimitMsg = _limiter.TryAcquire(GetUserId());
        if (rateLimitMsg is not null)
            return StatusCode(429, new { error = rateLimitMsg });

        if (string.IsNullOrWhiteSpace(request.MedicationName))
            return BadRequest("MedicationName is required.");

        var systemPrompt = """
            You are a helpful medication reference assistant for care facility staff.
            Given a medication name and dosage, provide:
            - What the medication is used for (1-2 sentences)
            - Common side effects (bullet list, max 5)
            - Important notes for care staff (e.g., take with food, monitor BP)
            Keep it simple and under 150 words. This is for informational purposes only.
            Do NOT provide dosage advice or make treatment recommendations.
            """;

        var userPrompt = $"Medication: {request.MedicationName}";
        if (!string.IsNullOrWhiteSpace(request.Dosage))
            userPrompt += $", Dosage: {request.Dosage}";

        try
        {
            var result = await _ai.CompleteAsync(systemPrompt, userPrompt, ct);
            return Ok(new AiResponse { Content = result, ResidentName = null });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "AI service unavailable.", detail = ex.Message });
        }
    }

    // POST api/ai/detect-trends
    [HttpPost("detect-trends")]
    public async Task<IActionResult> DetectTrends(
        [FromBody] AiResidentRequest request, CancellationToken ct)
    {
        var rateLimitMsg = _limiter.TryAcquire(GetUserId());
        if (rateLimitMsg is not null)
            return StatusCode(429, new { error = rateLimitMsg });

        var resident = await _db.Residents.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResidentId, ct);
        if (resident is null) return NotFound("Resident not found.");

        var since = DateTimeOffset.UtcNow.AddDays(-14);

        var observations = await _db.Observations.AsNoTracking()
            .Where(o => o.ResidentId == request.ResidentId && o.RecordedAt >= since.UtcDateTime)
            .OrderBy(o => o.RecordedAt)
            .Take(50)
            .ToListAsync(ct);

        var marEntries = await _db.MarEntries.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId && !m.IsVoided
                        && m.AdministeredAtUtc >= since)
            .OrderBy(m => m.AdministeredAtUtc)
            .Take(100)
            .ToListAsync(ct);

        var medications = await _db.Medications.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId)
            .ToListAsync(ct);

        var residentName = $"{resident.ResidentFName} {resident.ResidentLName}".Trim();

        var dataBlock = new StringBuilder();
        dataBlock.AppendLine($"Resident: {residentName}");
        dataBlock.AppendLine($"Analysis period: last 14 days");
        dataBlock.AppendLine();

        dataBlock.AppendLine("== Observations (chronological) ==");
        foreach (var o in observations)
            dataBlock.AppendLine($"- [{o.RecordedAt:yyyy-MM-dd HH:mm}] {o.Type}: {o.Value}");

        dataBlock.AppendLine();
        dataBlock.AppendLine("== MAR Entries (chronological) ==");
        var grouped = marEntries.GroupBy(e => e.Status);
        foreach (var g in grouped)
            dataBlock.AppendLine($"- {g.Key}: {g.Count()} entries");
        dataBlock.AppendLine();
        var refusals = marEntries.Where(e => e.Status == "Refused").ToList();
        if (refusals.Any())
        {
            dataBlock.AppendLine("Refused medications detail:");
            foreach (var r in refusals)
            {
                var medName = medications.FirstOrDefault(m => m.Id == r.MedicationId)?.MedName ?? "Unknown";
                dataBlock.AppendLine($"  - [{r.AdministeredAtUtc:yyyy-MM-dd HH:mm}] {medName}");
            }
        }

        var systemPrompt = """
            You are a clinical trend analysis assistant for a care facility.
            Analyze the resident's data over the past 14 days and identify:
            - Trends in vital signs (improving, stable, declining)
            - Patterns in medication refusals or missed doses
            - Any observations that warrant staff attention
            - Overall trajectory assessment
            If no concerning trends are found, say so clearly.
            Keep it under 200 words. Use bullet points. Do not make diagnoses.
            Flag items that may need follow-up with a doctor.
            """;

        try
        {
            var result = await _ai.CompleteAsync(systemPrompt, dataBlock.ToString(), ct);
            return Ok(new AiResponse { Content = result, ResidentName = residentName });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "AI service unavailable.", detail = ex.Message });
        }
    }

    // POST api/ai/report-draft
    [HttpPost("report-draft")]
    public async Task<IActionResult> ReportDraft(
        [FromBody] AiResidentRequest request, CancellationToken ct)
    {
        var rateLimitMsg = _limiter.TryAcquire(GetUserId());
        if (rateLimitMsg is not null)
            return StatusCode(429, new { error = rateLimitMsg });

        var resident = await _db.Residents.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResidentId, ct);
        if (resident is null) return NotFound("Resident not found.");

        var observations = await _db.Observations.AsNoTracking()
            .Where(o => o.ResidentId == request.ResidentId)
            .OrderByDescending(o => o.RecordedAt)
            .Take(30)
            .ToListAsync(ct);

        var marEntries = await _db.MarEntries.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId && !m.IsVoided)
            .OrderByDescending(m => m.AdministeredAtUtc)
            .Take(50)
            .ToListAsync(ct);

        var medications = await _db.Medications.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId)
            .ToListAsync(ct);

        var residentName = $"{resident.ResidentFName} {resident.ResidentLName}".Trim();

        var dataBlock = new StringBuilder();
        dataBlock.AppendLine($"Resident: {residentName}");
        dataBlock.AppendLine($"Room: {resident.RoomNumber}, DOB: {resident.DateOfBirth}, Gender: {resident.Gender}");
        var allergyList = new List<string>();
        if (resident.AllergyPeanuts) allergyList.Add("Peanuts");
        if (resident.AllergyTreeNuts) allergyList.Add("Tree nuts");
        if (resident.AllergyMilk) allergyList.Add("Milk");
        if (resident.AllergyLatex) allergyList.Add("Latex");
        if (resident.AllergyPenicillin) allergyList.Add("Penicillin");
        if (resident.AllergySulfa) allergyList.Add("Sulfa");
        if (resident.AllergyAspirin) allergyList.Add("Aspirin");
        if (resident.AllergyCodeine) allergyList.Add("Codeine");
        if (!string.IsNullOrWhiteSpace(resident.AllergyOtherItems)) allergyList.Add(resident.AllergyOtherItems);
        dataBlock.AppendLine($"Allergies: {(allergyList.Count > 0 ? string.Join(", ", allergyList) : "None recorded")}");
        dataBlock.AppendLine($"Doctor: {resident.DoctorName}, Contact: {resident.DoctorContact}");
        dataBlock.AppendLine();

        dataBlock.AppendLine("== Active Medications ==");
        foreach (var m in medications)
            dataBlock.AppendLine($"- {m.MedName} ({m.Dosage}), {m.TimesPerDay}x/day, Usage: {m.Usage}");

        dataBlock.AppendLine();
        dataBlock.AppendLine("== Recent Observations ==");
        foreach (var o in observations)
            dataBlock.AppendLine($"- [{o.RecordedAt:yyyy-MM-dd HH:mm}] {o.Type}: {o.Value} (by {o.RecordedBy})");

        dataBlock.AppendLine();
        dataBlock.AppendLine("== Recent MAR Entries ==");
        foreach (var e in marEntries)
        {
            var medName = medications.FirstOrDefault(m => m.Id == e.MedicationId)?.MedName ?? "Unknown";
            dataBlock.AppendLine($"- [{e.AdministeredAtUtc:yyyy-MM-dd HH:mm}] {medName}: {e.Status} ({e.DoseQuantity} {e.DoseUnit})");
        }

        var systemPrompt = """
            You are a clinical documentation assistant for a care facility.
            Generate a structured resident report draft with these sections:
            1. RESIDENT OVERVIEW — demographics, room, doctor, allergies
            2. CURRENT MEDICATIONS — list with dosage and purpose
            3. RECENT OBSERVATIONS SUMMARY — key vitals trends and notes
            4. MEDICATION COMPLIANCE — given/refused/missed patterns
            5. CARE NOTES — any items requiring attention or follow-up
            Write in professional clinical language. Keep each section concise.
            Total length: 200-300 words. Do not make diagnoses or prescribe.
            This is a DRAFT for staff review, not a final clinical document.
            """;

        try
        {
            var result = await _ai.CompleteAsync(systemPrompt, dataBlock.ToString(), ct);
            return Ok(new AiResponse { Content = result, ResidentName = residentName });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "AI service unavailable.", detail = ex.Message });
        }
    }

    // POST api/ai/shift-handoff
    [HttpPost("shift-handoff")]
    public async Task<IActionResult> ShiftHandoff(CancellationToken ct)
    {
        var rateLimitMsg = _limiter.TryAcquire(GetUserId());
        if (rateLimitMsg is not null)
            return StatusCode(429, new { error = rateLimitMsg });

        var since = DateTimeOffset.UtcNow.AddHours(-12);

        var residents = await _db.Residents.AsNoTracking().ToListAsync(ct);

        var recentObs = await _db.Observations.AsNoTracking()
            .Where(o => o.RecordedAt >= since.UtcDateTime)
            .OrderByDescending(o => o.RecordedAt)
            .Take(100)
            .ToListAsync(ct);

        var recentMar = await _db.MarEntries.AsNoTracking()
            .Where(m => !m.IsVoided && m.AdministeredAtUtc >= since)
            .OrderByDescending(m => m.AdministeredAtUtc)
            .Take(150)
            .ToListAsync(ct);

        var medications = await _db.Medications.AsNoTracking().ToListAsync(ct);

        var dataBlock = new StringBuilder();
        dataBlock.AppendLine($"Facility shift handoff — last 12 hours (since {since:yyyy-MM-dd HH:mm} UTC)");
        dataBlock.AppendLine($"Total residents: {residents.Count}");
        dataBlock.AppendLine();

        // Per-resident summary
        foreach (var r in residents)
        {
            var name = $"{r.ResidentFName} {r.ResidentLName}".Trim();
            var obs = recentObs.Where(o => o.ResidentId == r.Id).ToList();
            var mar = recentMar.Where(m => m.ResidentId == r.Id).ToList();

            if (obs.Count == 0 && mar.Count == 0) continue;

            dataBlock.AppendLine($"--- {name} (Room {r.RoomNumber}) ---");
            foreach (var o in obs.Take(5))
                dataBlock.AppendLine($"  OBS [{o.RecordedAt:HH:mm}] {o.Type}: {o.Value}");

            var given = mar.Count(m => m.Status == "Given");
            var refused = mar.Count(m => m.Status == "Refused");
            var missed = mar.Count(m => m.Status == "Missed");
            dataBlock.AppendLine($"  MAR: {given} given, {refused} refused, {missed} missed");

            if (refused > 0)
            {
                foreach (var rm in mar.Where(m => m.Status == "Refused"))
                {
                    var medName = medications.FirstOrDefault(m => m.Id == rm.MedicationId)?.MedName ?? "Unknown";
                    dataBlock.AppendLine($"    Refused: {medName} at {rm.AdministeredAtUtc:HH:mm}");
                }
            }
            dataBlock.AppendLine();
        }

        var systemPrompt = """
            You are a shift handoff assistant for a care facility.
            Generate a concise facility-wide shift handoff summary for the incoming team.
            IMPORTANT: Always refer to residents by their full name as provided in the data.
            Never use generic labels like "Resident 1", "Resident 12", etc.
            Structure:
            1. PRIORITY ITEMS — residents needing immediate attention (refusals, abnormal vitals, missed meds)
            2. RESIDENT UPDATES — brief per-resident status for those with activity
            3. MEDICATION COMPLIANCE — overall compliance rate, notable issues
            4. FOLLOW-UP ITEMS — anything the next shift should watch
            Keep it under 300 words. Use bullet points. Do not make diagnoses.
            If a resident had no events, do not mention them.
            """;

        try
        {
            var result = await _ai.CompleteAsync(systemPrompt, dataBlock.ToString(), ct);
            return Ok(new AiResponse { Content = result });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "AI service unavailable.", detail = ex.Message });
        }
    }

    // POST api/ai/care-query
    [HttpPost("care-query")]
    public async Task<IActionResult> CareQuery(
        [FromBody] AiCareQueryRequest request, CancellationToken ct)
    {
        var rateLimitMsg = _limiter.TryAcquire(GetUserId());
        if (rateLimitMsg is not null)
            return StatusCode(429, new { error = rateLimitMsg });

        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Query is required.");

        var since = DateTimeOffset.UtcNow.AddDays(-7);
        var dataBlock = new StringBuilder();

        if (request.ResidentId.HasValue && request.ResidentId.Value != Guid.Empty)
        {
            var resident = await _db.Residents.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.ResidentId.Value, ct);
            if (resident is null) return NotFound("Resident not found.");

            var residentName = $"{resident.ResidentFName} {resident.ResidentLName}".Trim();
            dataBlock.AppendLine($"Context: Resident {residentName}, Room {resident.RoomNumber}");

            var obs = await _db.Observations.AsNoTracking()
                .Where(o => o.ResidentId == request.ResidentId.Value && o.RecordedAt >= since.UtcDateTime)
                .OrderByDescending(o => o.RecordedAt).Take(30).ToListAsync(ct);

            var mar = await _db.MarEntries.AsNoTracking()
                .Where(m => m.ResidentId == request.ResidentId.Value && !m.IsVoided && m.AdministeredAtUtc >= since)
                .OrderByDescending(m => m.AdministeredAtUtc).Take(50).ToListAsync(ct);

            var meds = await _db.Medications.AsNoTracking()
                .Where(m => m.ResidentId == request.ResidentId.Value).ToListAsync(ct);

            dataBlock.AppendLine("== Medications ==");
            foreach (var m in meds)
                dataBlock.AppendLine($"- {m.MedName} ({m.Dosage}), {m.TimesPerDay}x/day");

            dataBlock.AppendLine("== Recent Observations (7d) ==");
            foreach (var o in obs)
                dataBlock.AppendLine($"- [{o.RecordedAt:yyyy-MM-dd HH:mm}] {o.Type}: {o.Value}");

            dataBlock.AppendLine("== Recent MAR (7d) ==");
            foreach (var e in mar)
            {
                var medName = meds.FirstOrDefault(m => m.Id == e.MedicationId)?.MedName ?? "Unknown";
                dataBlock.AppendLine($"- [{e.AdministeredAtUtc:yyyy-MM-dd HH:mm}] {medName}: {e.Status}");
            }
        }
        else
        {
            // Facility-wide context
            var residents = await _db.Residents.AsNoTracking().ToListAsync(ct);
            var allMeds = await _db.Medications.AsNoTracking().ToListAsync(ct);
            dataBlock.AppendLine($"Context: Facility-wide ({residents.Count} residents)");

            dataBlock.AppendLine("== Residents ==");
            foreach (var r in residents)
                dataBlock.AppendLine($"- {r.ResidentFName} {r.ResidentLName} (Room {r.RoomNumber})");

            dataBlock.AppendLine("== Medications ==");
            foreach (var m in allMeds.Where(m => m.ResidentId.HasValue && m.ResidentId != Guid.Empty))
            {
                var rName = residents.FirstOrDefault(r => r.Id == m.ResidentId);
                var name = rName != null ? $"{rName.ResidentFName} {rName.ResidentLName}" : "Unknown";
                dataBlock.AppendLine($"- {name}: {m.MedName} ({m.Dosage}), {m.TimesPerDay}x/day");
            }

            var recentObs = await _db.Observations.AsNoTracking()
                .Where(o => o.RecordedAt >= since.UtcDateTime)
                .OrderByDescending(o => o.RecordedAt).Take(60).ToListAsync(ct);

            dataBlock.AppendLine("== Recent Observations (7d) ==");
            foreach (var o in recentObs)
            {
                var rName = residents.FirstOrDefault(r => r.Id == o.ResidentId);
                var name = rName != null ? $"{rName.ResidentFName} {rName.ResidentLName}" : "Unknown";
                dataBlock.AppendLine($"- [{o.RecordedAt:yyyy-MM-dd HH:mm}] {name}: {o.Type}: {o.Value}");
            }

            var recentMar = await _db.MarEntries.AsNoTracking()
                .Where(m => !m.IsVoided && m.AdministeredAtUtc >= since)
                .OrderByDescending(m => m.AdministeredAtUtc).Take(80).ToListAsync(ct);

            dataBlock.AppendLine("== Recent MAR (7d) ==");
            foreach (var e in recentMar)
            {
                var rName = residents.FirstOrDefault(r => r.Id == e.ResidentId);
                var name = rName != null ? $"{rName.ResidentFName} {rName.ResidentLName}" : "Unknown";
                var medName = allMeds.FirstOrDefault(m => m.Id == e.MedicationId)?.MedName ?? "Unknown";
                dataBlock.AppendLine($"- [{e.AdministeredAtUtc:yyyy-MM-dd HH:mm}] {name}: {medName} {e.Status}");
            }
        }

        var systemPrompt = """
            You are a care facility query assistant. Staff will ask natural-language
            questions about residents, medications, observations, or facility operations.
            Answer based ONLY on the provided data context. If the data doesn't contain
            enough information to answer, say so clearly.
            Keep answers concise (under 200 words). Use bullet points where helpful.
            Do not make diagnoses, prescribe, or provide medical advice.
            If asked about a specific resident, focus on their data.
            """;

        var userPrompt = $"Staff question: {request.Query}\n\n{dataBlock}";

        try
        {
            var result = await _ai.CompleteAsync(systemPrompt, userPrompt, ct);
            return Ok(new AiResponse { Content = result });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "AI service unavailable.", detail = ex.Message });
        }
    }

    // POST api/ai/trend-explain
    [HttpPost("trend-explain")]
    public async Task<IActionResult> TrendExplain(
        [FromBody] AiTrendExplainRequest request, CancellationToken ct)
    {
        var rateLimitMsg = _limiter.TryAcquire(GetUserId());
        if (rateLimitMsg is not null)
            return StatusCode(429, new { error = rateLimitMsg });

        var days = request.Days is 3 or 7 ? request.Days : 7;

        var resident = await _db.Residents.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResidentId, ct);
        if (resident is null) return NotFound("Resident not found.");

        var since = DateTimeOffset.UtcNow.AddDays(-days);

        var observations = await _db.Observations.AsNoTracking()
            .Where(o => o.ResidentId == request.ResidentId && o.RecordedAt >= since.UtcDateTime)
            .OrderBy(o => o.RecordedAt)
            .Take(60)
            .ToListAsync(ct);

        var marEntries = await _db.MarEntries.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId && !m.IsVoided
                        && m.AdministeredAtUtc >= since)
            .OrderBy(m => m.AdministeredAtUtc)
            .Take(80)
            .ToListAsync(ct);

        var medications = await _db.Medications.AsNoTracking()
            .Where(m => m.ResidentId == request.ResidentId)
            .ToListAsync(ct);

        var residentName = $"{resident.ResidentFName} {resident.ResidentLName}".Trim();

        var dataBlock = new StringBuilder();
        dataBlock.AppendLine($"Resident: {residentName}");
        dataBlock.AppendLine($"Analysis period: last {days} days");
        dataBlock.AppendLine();

        dataBlock.AppendLine("== Observations (chronological) ==");
        foreach (var o in observations)
            dataBlock.AppendLine($"- [{o.RecordedAt:yyyy-MM-dd HH:mm}] {o.Type}: {o.Value}");

        dataBlock.AppendLine();
        dataBlock.AppendLine("== MAR Entries (chronological) ==");
        foreach (var e in marEntries)
        {
            var medName = medications.FirstOrDefault(m => m.Id == e.MedicationId)?.MedName ?? "Unknown";
            dataBlock.AppendLine($"- [{e.AdministeredAtUtc:yyyy-MM-dd HH:mm}] {medName}: {e.Status}");
        }

        var systemPrompt = $"""
            You are a clinical trend analysis assistant for a care facility.
            Analyze the resident's data over the past {days} days and provide:
            1. VITAL SIGNS TRENDS — direction for each recorded vital (improving/stable/declining)
            2. MEDICATION PATTERNS — compliance rate, any refusals or missed doses
            3. KEY OBSERVATIONS — notable events or changes
            4. RECOMMENDATIONS — items for staff attention (do NOT diagnose)
            If data is sparse, note that more observations would improve analysis.
            Keep it under 250 words. Use bullet points. Do not make diagnoses.
            """;

        try
        {
            var result = await _ai.CompleteAsync(systemPrompt, dataBlock.ToString(), ct);
            return Ok(new AiResponse { Content = result, ResidentName = residentName });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { error = "AI service unavailable.", detail = ex.Message });
        }
    }
}

public sealed class AiResidentRequest
{
    public Guid ResidentId { get; set; }
}

public sealed class AiMedicationRequest
{
    public string MedicationName { get; set; } = "";
    public string? Dosage { get; set; }
}

public sealed class AiCareQueryRequest
{
    public string Query { get; set; } = "";
    public Guid? ResidentId { get; set; }
}

public sealed class AiTrendExplainRequest
{
    public Guid ResidentId { get; set; }
    public int Days { get; set; } = 7;
}

public sealed class AiResponse
{
    public string Content { get; set; } = "";
    public string? ResidentName { get; set; }
    public string Disclaimer { get; set; } = "AI-Generated - For Informational Purposes Only";
}
