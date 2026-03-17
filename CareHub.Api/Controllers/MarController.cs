using CareHub.Api.Data;
using CareHub.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MarController : ControllerBase
{
    private readonly CareHubDbContext _db;

    public MarController(CareHubDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Admin},{Roles.Resident}")]
    public async Task<ActionResult<List<MarEntry>>> GetAll(
        [FromQuery] Guid? residentId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] bool includeVoided = false,
        CancellationToken ct = default)
    {
        var query = _db.MarEntries.AsNoTracking().AsQueryable();

        if (residentId.HasValue)
            query = query.Where(m => m.ResidentId == residentId.Value);

        if (fromUtc.HasValue)
            query = query.Where(m => m.AdministeredAtUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(m => m.AdministeredAtUtc <= toUtc.Value);

        if (!includeVoided)
            query = query.Where(m => !m.IsVoided);

        var list = await query
            .OrderByDescending(m => m.AdministeredAtUtc)
            .ToListAsync(ct);

        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Admin},{Roles.Resident}")]
    public async Task<ActionResult<MarEntry>> GetById(Guid id, CancellationToken ct)
    {
        var entry = await _db.MarEntries.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Staff)]
    public async Task<ActionResult<MarEntry>> Create(
        [FromBody] CreateMarEntryRequest request,
        CancellationToken ct)
    {
        var existing = await _db.MarEntries.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ClientRequestId == request.ClientRequestId, ct);

        if (existing is not null)
            return Ok(existing);

        if (!await _db.Residents.AnyAsync(r => r.Id == request.ResidentId, ct))
            return NotFound($"Resident {request.ResidentId} not found.");

        var med = await _db.Medications.FindAsync(new object[] { request.MedicationId }, ct);
        if (med is null)
            return NotFound($"Medication {request.MedicationId} not found.");

        var allowedStatuses = new[] { "Given", "Refused", "Held", "Missed", "NotAvailable" };
        if (!allowedStatuses.Contains(request.Status))
            return BadRequest($"Invalid status '{request.Status}'. Allowed: {string.Join(", ", allowedStatuses)}.");

        var now = DateTimeOffset.UtcNow;

        var entry = new MarEntry
        {
            ClientRequestId = request.ClientRequestId,
            ResidentId = request.ResidentId,
            MedicationId = request.MedicationId,
            Status = request.Status,
            DoseQuantity = request.DoseQuantity,
            DoseUnit = request.DoseUnit,
            AdministeredAtUtc = request.AdministeredAtUtc,
            ScheduledForUtc = request.ScheduledForUtc,
            Notes = request.Notes,
            RecordedBy = request.RecordedBy,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        if (entry.Status == "Given")
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var freshMed = await _db.Medications.FindAsync(new object[] { request.MedicationId }, ct);
                if (freshMed is null)
                {
                    await tx.RollbackAsync(ct);
                    return NotFound($"Medication {request.MedicationId} not found.");
                }

                if (freshMed.StockQuantity < entry.DoseQuantity)
                {
                    await tx.RollbackAsync(ct);
                    return Conflict($"Insufficient stock. Available: {freshMed.StockQuantity}, requested: {entry.DoseQuantity}.");
                }

                _db.MarEntries.Add(entry);

                _db.MedicationInventoryLedgers.Add(new MedicationInventoryLedger
                {
                    MedicationId = entry.MedicationId,
                    MarEntryId = entry.Id,
                    ChangeQty = -entry.DoseQuantity,
                    Unit = entry.DoseUnit,
                    Reason = "MAR_GIVEN",
                    CreatedAtUtc = now,
                });

                freshMed.StockQuantity -= entry.DoseQuantity;

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
        else
        {
            _db.MarEntries.Add(entry);
            await _db.SaveChangesAsync(ct);
        }

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
    }

    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Void(
        Guid id,
        [FromBody] VoidMarEntryRequest request,
        CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var entry = await _db.MarEntries.FindAsync(new object[] { id }, ct);
            if (entry is null)
            {
                await tx.RollbackAsync(ct);
                return NotFound();
            }

            if (entry.IsVoided)
            {
                await tx.RollbackAsync(ct);
                return BadRequest("Entry is already voided.");
            }

            var now = DateTimeOffset.UtcNow;

            entry.IsVoided = true;
            entry.VoidedAtUtc = now;
            entry.VoidReason = request.Reason;
            entry.UpdatedAtUtc = now;

            if (entry.Status == "Given")
            {
                var existingLedger = await _db.MedicationInventoryLedgers
                    .FirstOrDefaultAsync(l => l.MarEntryId == entry.Id, ct);

                if (existingLedger is null)
                {
                    await tx.RollbackAsync(ct);
                    return Conflict("Cannot void: no inventory ledger found for this entry.");
                }

                if (existingLedger.ChangeQty >= 0)
                {
                    await tx.RollbackAsync(ct);
                    return Conflict("Cannot void: inventory deduction already reversed.");
                }

                var med = await _db.Medications.FindAsync(new object[] { entry.MedicationId }, ct);
                if (med is null)
                {
                    await tx.RollbackAsync(ct);
                    return Conflict("Cannot void: medication record not found.");
                }

                med.StockQuantity += entry.DoseQuantity;
                existingLedger.ChangeQty = 0;
                existingLedger.Reason = "MAR_VOID_REVERSED";
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Ok(entry);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    [HttpGet("report")]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Admin}")]
    public async Task<ActionResult<MarReport>> GetReport(
        [FromQuery] DateTimeOffset fromUtc,
        [FromQuery] DateTimeOffset toUtc,
        [FromQuery] Guid? residentId,
        CancellationToken ct)
    {
        var query = _db.MarEntries.AsNoTracking()
            .Where(m => !m.IsVoided)
            .Where(m => m.AdministeredAtUtc >= fromUtc && m.AdministeredAtUtc <= toUtc);

        if (residentId.HasValue)
            query = query.Where(m => m.ResidentId == residentId.Value);

        var entries = await query.ToListAsync(ct);

        var residentIds = entries.Select(e => e.ResidentId).Distinct().ToList();
        var medIds = entries.Select(e => e.MedicationId).Distinct().ToList();

        var residentNames = await _db.Residents.AsNoTracking()
            .Where(r => residentIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => (r.ResidentFName + " " + r.ResidentLName).Trim(), ct);

        var medNames = await _db.Medications.AsNoTracking()
            .Where(m => medIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.MedName ?? "Unknown", ct);

        var lines = entries.Select(e => new MarReportLine
        {
            Id = e.Id,
            ResidentId = e.ResidentId,
            ResidentName = residentNames.GetValueOrDefault(e.ResidentId, "Unknown"),
            MedicationId = e.MedicationId,
            MedicationName = medNames.GetValueOrDefault(e.MedicationId, "Unknown"),
            Status = e.Status,
            DoseQuantity = e.DoseQuantity,
            DoseUnit = e.DoseUnit,
            ScheduledForUtc = e.ScheduledForUtc,
            AdministeredAtUtc = e.AdministeredAtUtc,
            RecordedBy = e.RecordedBy,
            Notes = e.Notes,
        }).OrderBy(l => l.ResidentName).ThenBy(l => l.AdministeredAtUtc).ToList();

        var summary = new MarReportSummary
        {
            TotalEntries = lines.Count,
            GivenCount = lines.Count(l => l.Status == "Given"),
            RefusedCount = lines.Count(l => l.Status == "Refused"),
            MissedCount = lines.Count(l => l.Status == "Missed"),
            HeldCount = lines.Count(l => l.Status == "Held"),
            NotAvailableCount = lines.Count(l => l.Status == "NotAvailable"),
        };

        return Ok(new MarReport
        {
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Summary = summary,
            Lines = lines,
        });
    }

    [HttpDelete("all")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteAll(CancellationToken ct)
    {
        var count = await _db.MarEntries.CountAsync(ct);
        if (count == 0) return Ok(new { deleted = 0 });

        _db.MedicationInventoryLedgers.RemoveRange(_db.MedicationInventoryLedgers);
        _db.MarEntries.RemoveRange(_db.MarEntries);
        await _db.SaveChangesAsync(ct);
        return Ok(new { deleted = count });
    }

    [HttpPost("seed-demo")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> SeedDemo(CancellationToken ct)
    {
        var todayLocal = DateTime.Now.Date;
        var localOffset = TimeZoneInfo.Local.GetUtcOffset(todayLocal);
        var todayStartUtc = new DateTimeOffset(todayLocal, localOffset).ToUniversalTime();
        var dayOfWeek = todayLocal.DayOfWeek;

        var meds = await _db.Medications.AsNoTracking()
            .Where(m => m.ResidentId != null && m.ResidentId != Guid.Empty)
            .ToListAsync(ct);

        if (meds.Count == 0)
            return BadRequest("No resident-assigned medications found to seed.");

        var existingToday = await _db.MarEntries
            .Where(m => m.AdministeredAtUtc >= todayStartUtc && m.AdministeredAtUtc < todayStartUtc.AddDays(1))
            .ToListAsync(ct);
        _db.MarEntries.RemoveRange(existingToday);

        var rng = new Random(42);
        var nurses = new[] { "Nurse Sarah", "Nurse James", "Nurse Emily" };
        var created = new List<object>();

        var slots = new Dictionary<(Guid resId, int hour), List<(Medication med, TimeSpan scheduledTime)>>();

        foreach (var med in meds)
        {
            var times = GetTimesForDay(med, dayOfWeek);
            int slotsToUse = Math.Max(1, Math.Min(3, med.TimesPerDay));

            for (int i = 0; i < slotsToUse && i < times.Count; i++)
            {
                var scheduledTime = times[i];
                if (scheduledTime == TimeSpan.Zero) continue;

                var scheduledLocal = todayLocal.Add(scheduledTime);
                if (scheduledLocal > DateTime.Now) continue;

                var key = (med.ResidentId!.Value, scheduledTime.Hours);
                if (!slots.ContainsKey(key)) slots[key] = new();
                slots[key].Add((med, scheduledTime));
            }
        }

        var slotKeys = slots.Keys.ToList();
        int numRefused = Math.Max(1, (int)Math.Round(slotKeys.Count * 0.01));
        int numMissed = Math.Max(1, (int)Math.Round(slotKeys.Count * 0.01));
        var shuffled = slotKeys.OrderBy(_ => rng.Next()).ToList();
        var refusedSlots = new HashSet<(Guid, int)>(shuffled.Take(numRefused));
        var missedSlots = new HashSet<(Guid, int)>(shuffled.Skip(numRefused).Take(numMissed));

        foreach (var (key, medList) in slots)
        {
            string status;
            if (refusedSlots.Contains(key)) status = "Refused";
            else if (missedSlots.Contains(key)) status = "Missed";
            else status = "Given";

            var nurse = nurses[rng.Next(nurses.Length)];

            foreach (var (med, scheduledTime) in medList)
            {
                var scheduledLocal = todayLocal.Add(scheduledTime);
                var scheduledOffset = TimeZoneInfo.Local.GetUtcOffset(scheduledLocal);
                var scheduledUtc = new DateTimeOffset(scheduledLocal, scheduledOffset).ToUniversalTime();

                var delayMinutes = status == "Given" ? rng.Next(1, 15) : 0;
                var administeredUtc = status == "Given"
                    ? scheduledUtc.AddMinutes(delayMinutes)
                    : scheduledUtc;

                var entry = new MarEntry
                {
                    ClientRequestId = Guid.NewGuid(),
                    ResidentId = med.ResidentId!.Value,
                    MedicationId = med.Id,
                    Status = status,
                    DoseQuantity = med.Quantity > 0 ? med.Quantity : 1,
                    DoseUnit = med.QuantityUnit ?? "tablet",
                    ScheduledForUtc = scheduledUtc,
                    AdministeredAtUtc = administeredUtc,
                    RecordedBy = nurse,
                    Notes = "SEED_DEMO",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                };

                _db.MarEntries.Add(entry);
                created.Add(new
                {
                    med.MedName,
                    Slot = scheduledTime.ToString(@"hh\:mm"),
                    status,
                    ScheduledUtc = scheduledUtc.ToString("yyyy-MM-dd HH:mm"),
                    AdminUtc = administeredUtc.ToString("yyyy-MM-dd HH:mm"),
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new { Message = $"Seeded {created.Count} MAR entries for today.", Entries = created });
    }

    private static List<TimeSpan> GetTimesForDay(Medication med, DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => new() { med.MonTime1, med.MonTime2, med.MonTime3 },
        DayOfWeek.Tuesday => new() { med.TueTime1, med.TueTime2, med.TueTime3 },
        DayOfWeek.Wednesday => new() { med.WedTime1, med.WedTime2, med.WedTime3 },
        DayOfWeek.Thursday => new() { med.ThuTime1, med.ThuTime2, med.ThuTime3 },
        DayOfWeek.Friday => new() { med.FriTime1, med.FriTime2, med.FriTime3 },
        DayOfWeek.Saturday => new() { med.SatTime1, med.SatTime2, med.SatTime3 },
        DayOfWeek.Sunday => new() { med.SunTime1, med.SunTime2, med.SunTime3 },
        _ => new()
    };
}
