using CareHub.Api.Data;
using CareHub.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Api.Services;

/// <summary>
/// Seeds 14 days of realistic MAR entries + observations for refused/missed.
/// Slot-based: all meds in the same (resident, day, hour) get the same status.
/// 98% Given, 1% Refused, 1% Missed — only on past days, never today.
/// </summary>
public static class MarSeeder
{
    private static readonly string[] Staff =
        { "Marcus Thompson", "James Wilson", "Linda Gomez", "Kevin Park", "Sarah Chen" };

    private static readonly string[] RefuseReasons =
    {
        "Resident refused all medications this round, stated feeling nauseous.",
        "Resident declined medications, reported upset stomach after breakfast.",
        "Resident refused medication round, says feeling dizzy this morning.",
        "Resident did not want to take medications, complained of headache.",
        "Resident refused, expressed concern about side effects.",
        "Resident was feeling too unwell to take medications this round.",
        "Resident declined all meds, stated they want to speak with doctor first.",
    };

    private static readonly string[] MissReasons =
    {
        "Resident was asleep during scheduled medication round.",
        "Resident was at physical therapy, missed entire medication window.",
        "Resident was away for family visit during scheduled time.",
        "Medication round delayed due to emergency on floor, time window passed.",
        "Resident was in radiology for scheduled X-ray, missed medication window.",
    };

    private static readonly string[] ObsRefused =
    {
        "Resident refused all {0} medications ({1}). Reported feeling nauseous. Vitals checked — within normal range. Doctor notified.",
        "Resident declined {0} medication round ({1}), citing dizziness. Monitored for 30 minutes, symptoms resolved.",
        "Resident refused {0} meds ({1}) due to upset stomach. Encouraged to eat first. Accepted medications with next meal.",
        "Resident expressed concerns about side effects and refused {0} medications ({1}). Nurse provided education. Care team to follow up.",
    };

    private static readonly string[] ObsMissed =
    {
        "Missed {0} medication round ({1}) — resident was sleeping. Per care plan, did not wake. Will administer at next window.",
        "Resident was at physiotherapy during {0} round. All scheduled medications ({1}) missed. Administered upon return.",
        "Resident away for family visit during {0} medication window. All meds ({1}) missed. Family educated about schedule.",
        "Emergency on floor delayed {0} medication round. Window passed for ({1}). Incident logged.",
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareHubDbContext>();
        var rng = new Random(777);
        var now = DateTimeOffset.Now;
        var todayLocal = now.Date;
        var localOffset = TimeZoneInfo.Local.GetUtcOffset(now);

        // If it's before 7 AM, shift "today" back by 1 day so we always have a full day of data
        if (now.Hour < 7)
        {
            todayLocal = todayLocal.AddDays(-1);
            Console.WriteLine($"[seed-mar] Early morning detected — treating {todayLocal:yyyy-MM-dd} as today for seeding.");
        }

        Console.WriteLine("[seed-mar] Clearing existing MAR entries and ledgers...");
        await db.Database.ExecuteSqlRawAsync("""DELETE FROM "MedicationInventoryLedgers" """);
        await db.Database.ExecuteSqlRawAsync("""DELETE FROM "MarEntries" """);

        // Load resident medications
        var meds = await db.Medications.AsNoTracking()
            .Where(m => m.ResidentId != null && m.ResidentId != Guid.Empty)
            .ToListAsync();

        var residents = await db.Residents.AsNoTracking().ToListAsync();
        var resNameMap = residents.ToDictionary(r => r.Id, r => $"{r.ResidentFName} {r.ResidentLName}".Trim());

        Console.WriteLine($"[seed-mar] Found {meds.Count} resident medications across {resNameMap.Count} residents.");

        // Build slots: (residentId, date, hour) -> list of (medication, scheduledMinute)
        var slots = new Dictionary<(Guid resId, DateTime date, int hour), List<(Medication med, int minute)>>();

        for (int dayOffset = 14; dayOffset >= 0; dayOffset--)
        {
            var day = todayLocal.AddDays(-dayOffset);
            var dayOfWeek = day.DayOfWeek;

            foreach (var med in meds)
            {
                if (!IsReminderDay(med, dayOfWeek)) continue;

                var times = GetScheduleTimes(med, dayOfWeek);
                foreach (var (h, m) in times)
                {
                    // For today, skip future slots (but if it's early morning, include all past days fully)
                    if (day == todayLocal && h > now.Hour) continue;

                    var key = (med.ResidentId!.Value, day, h);
                    if (!slots.ContainsKey(key)) slots[key] = new();
                    slots[key].Add((med, m));
                }
            }
        }

        // Select ~1% of PAST slots as refused, ~1% as missed
        var pastKeys = slots.Keys.Where(k => k.date < todayLocal).ToList();
        var todayKeys = slots.Keys.Where(k => k.date == todayLocal).ToList();

        int numRefused = Math.Max(1, (int)Math.Round(pastKeys.Count * 0.01));
        int numMissed = Math.Max(1, (int)Math.Round(pastKeys.Count * 0.01));

        var shuffled = pastKeys.OrderBy(_ => rng.Next()).ToList();
        var refusedSet = new HashSet<(Guid, DateTime, int)>(shuffled.Take(numRefused));
        var missedSet = new HashSet<(Guid, DateTime, int)>(shuffled.Skip(numRefused).Take(numMissed));

        int totalDoses = slots.Values.Sum(v => v.Count);
        int refusedDoses = refusedSet.Sum(k => slots[k].Count);
        int missedDoses = missedSet.Sum(k => slots[k].Count);
        Console.WriteLine($"[seed-mar] Slots: {slots.Count} total ({pastKeys.Count} past, {todayKeys.Count} today)");
        Console.WriteLine($"[seed-mar] Doses: {totalDoses} total, {totalDoses - refusedDoses - missedDoses} Given ({(totalDoses - refusedDoses - missedDoses) * 100.0 / totalDoses:F1}%), {refusedDoses} Refused, {missedDoses} Missed");

        // Create MAR entries
        var marEntries = new List<MarEntry>();
        var observations = new List<Observation>();

        foreach (var (key, medList) in slots)
        {
            var (resId, date, hour) = key;
            string status;
            string? notes;

            if (refusedSet.Contains(key))
            {
                status = "Refused";
                notes = RefuseReasons[rng.Next(RefuseReasons.Length)];
            }
            else if (missedSet.Contains(key))
            {
                status = "Missed";
                notes = MissReasons[rng.Next(MissReasons.Length)];
            }
            else
            {
                status = "Given";
                notes = null;
            }

            var staff = Staff[rng.Next(Staff.Length)];
            var medNames = new List<string>();

            foreach (var (med, schedMin) in medList)
            {
                var schedLocal = new DateTime(date.Year, date.Month, date.Day, hour, schedMin, 0);
                var schedUtc = new DateTimeOffset(schedLocal, localOffset).ToUniversalTime();

                var adminUtc = status == "Given"
                    ? schedUtc.AddMinutes(rng.Next(-3, 13))
                    : schedUtc;

                marEntries.Add(new MarEntry
                {
                    Id = Guid.NewGuid(),
                    ClientRequestId = Guid.NewGuid(),
                    ResidentId = resId,
                    MedicationId = med.Id,
                    Status = status,
                    DoseQuantity = Math.Max(1, med.Quantity),
                    DoseUnit = string.IsNullOrWhiteSpace(med.QuantityUnit) ? "tablet" : med.QuantityUnit,
                    ScheduledForUtc = schedUtc,
                    AdministeredAtUtc = adminUtc,
                    RecordedBy = staff,
                    Notes = notes,
                    CreatedAtUtc = adminUtc,
                    UpdatedAtUtc = adminUtc,
                });

                if (status != "Given")
                    medNames.Add(med.MedName);
            }

            // Create one observation per refused/missed slot
            if (status != "Given" && medNames.Count > 0)
            {
                var medsStr = string.Join(", ", medNames);
                var timeStr = $"{hour:D2}:00";
                var templates = status == "Refused" ? ObsRefused : ObsMissed;
                var obsText = string.Format(templates[rng.Next(templates.Length)], timeStr, medsStr);

                var obsTime = new DateTime(date.Year, date.Month, date.Day,
                    Math.Min(hour + 1, 23), rng.Next(0, 60), 0);

                observations.Add(new Observation
                {
                    Id = Guid.NewGuid(),
                    ResidentId = resId,
                    RecordedAt = obsTime.ToUniversalTime(),
                    Type = "Medication",
                    Value = obsText,
                    RecordedBy = staff,
                    ResidentName = resNameMap.GetValueOrDefault(resId, ""),
                });
            }
        }

        // Bulk insert
        Console.WriteLine($"[seed-mar] Inserting {marEntries.Count} MAR entries...");
        db.MarEntries.AddRange(marEntries);
        await db.SaveChangesAsync();

        Console.WriteLine($"[seed-mar] Inserting {observations.Count} medication observations...");
        db.Observations.AddRange(observations);
        await db.SaveChangesAsync();

        // Bump stock for all resident medications to 999
        await db.Database.ExecuteSqlRawAsync(
            """UPDATE "Medications" SET "StockQuantity" = 999 WHERE "ResidentId" IS NOT NULL AND "ResidentId" != '00000000-0000-0000-0000-000000000000' """);

        // Verification: check the DB to confirm
        var dbCount = await db.MarEntries.CountAsync();
        var dbGiven = await db.MarEntries.CountAsync(m => m.Status == "Given");
        var dbRefused = await db.MarEntries.CountAsync(m => m.Status == "Refused");
        var dbMissed = await db.MarEntries.CountAsync(m => m.Status == "Missed");
        Console.WriteLine($"[seed-mar] VERIFIED in DB: {dbCount} total — {dbGiven} Given, {dbRefused} Refused, {dbMissed} Missed");

        // Spot check: find any slot where meds have mixed statuses (should be 0)
        var mixedSlots = marEntries
            .GroupBy(e => (e.ResidentId, e.ScheduledForUtc?.Date, e.ScheduledForUtc?.Hour))
            .Where(g => g.Select(e => e.Status).Distinct().Count() > 1)
            .Count();
        Console.WriteLine($"[seed-mar] Slots with mixed statuses: {mixedSlots} (should be 0)");
        Console.WriteLine("[seed-mar] Done! Stock bumped to 999 for all resident medications.");
    }

    private static bool IsReminderDay(Medication med, DayOfWeek dow) => dow switch
    {
        DayOfWeek.Monday => med.ReminderMon,
        DayOfWeek.Tuesday => med.ReminderTue,
        DayOfWeek.Wednesday => med.ReminderWed,
        DayOfWeek.Thursday => med.ReminderThu,
        DayOfWeek.Friday => med.ReminderFri,
        DayOfWeek.Saturday => med.ReminderSat,
        DayOfWeek.Sunday => med.ReminderSun,
        _ => true,
    };

    private static List<(int hour, int minute)> GetScheduleTimes(Medication med, DayOfWeek dow)
    {
        var times = new List<(int, int)>();
        var tpd = Math.Min(med.TimesPerDay, 3);
        for (int i = 1; i <= tpd; i++)
        {
            var ts = GetTimeSpan(med, dow, i);
            times.Add((ts.Hours, ts.Minutes));
        }
        return times;
    }

    private static TimeSpan GetTimeSpan(Medication med, DayOfWeek dow, int slot) => (dow, slot) switch
    {
        (DayOfWeek.Monday, 1) => med.MonTime1, (DayOfWeek.Monday, 2) => med.MonTime2, (DayOfWeek.Monday, 3) => med.MonTime3,
        (DayOfWeek.Tuesday, 1) => med.TueTime1, (DayOfWeek.Tuesday, 2) => med.TueTime2, (DayOfWeek.Tuesday, 3) => med.TueTime3,
        (DayOfWeek.Wednesday, 1) => med.WedTime1, (DayOfWeek.Wednesday, 2) => med.WedTime2, (DayOfWeek.Wednesday, 3) => med.WedTime3,
        (DayOfWeek.Thursday, 1) => med.ThuTime1, (DayOfWeek.Thursday, 2) => med.ThuTime2, (DayOfWeek.Thursday, 3) => med.ThuTime3,
        (DayOfWeek.Friday, 1) => med.FriTime1, (DayOfWeek.Friday, 2) => med.FriTime2, (DayOfWeek.Friday, 3) => med.FriTime3,
        (DayOfWeek.Saturday, 1) => med.SatTime1, (DayOfWeek.Saturday, 2) => med.SatTime2, (DayOfWeek.Saturday, 3) => med.SatTime3,
        (DayOfWeek.Sunday, 1) => med.SunTime1, (DayOfWeek.Sunday, 2) => med.SunTime2, (DayOfWeek.Sunday, 3) => med.SunTime3,
        _ => new TimeSpan(8, 0, 0),
    };
}
