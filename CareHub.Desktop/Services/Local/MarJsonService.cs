using System.Text.Json;
using System.Text.Json.Serialization;
using CareHub.Desktop.Models;
using CareHub.Services.Abstractions;

namespace CareHub.Services.Local;

public class MarJsonService : IMarService
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new UtcDateTimeConverter() }
    };

    public MarJsonService()
    {
        _filePath = Path.Combine(FileSystem.AppDataDirectory, "MarEntries.json");
    }

    public async Task<List<MarEntry>> LoadAsync(Guid? residentId, DateTime fromUtc, DateTime toUtc, bool includeVoided = false)
    {
        var all = await LoadAllAsync();

        return all
            .Where(m => includeVoided || !m.IsVoided)
            .Where(m => residentId == null || m.ResidentId == residentId.Value)
            .Where(m => m.AdministeredAtUtc >= new DateTimeOffset(fromUtc, TimeSpan.Zero)
                     && m.AdministeredAtUtc <= new DateTimeOffset(toUtc, TimeSpan.Zero))
            .OrderByDescending(m => m.AdministeredAtUtc)
            .ToList();
    }

    public async Task CreateAsync(MarEntry entry)
    {
        var list = await LoadAllAsync();

        // Idempotency check on ClientRequestId
        if (list.Any(m => m.ClientRequestId == entry.ClientRequestId))
            return;

        if (entry.Id == Guid.Empty)
            entry.Id = Guid.NewGuid();

        list.Add(entry);
        await SaveAsync(list);
    }

    public Task<int> SyncAsync() => Task.FromResult(0);

    public async Task VoidAsync(Guid id, string? reason)
    {
        var list = await LoadAllAsync();
        var entry = list.FirstOrDefault(m => m.Id == id);
        if (entry is null)
            throw new InvalidOperationException("MAR entry not found.");

        if (entry.IsVoided)
            throw new InvalidOperationException("Entry is already voided.");

        entry.IsVoided = true;
        entry.VoidedAtUtc = DateTimeOffset.UtcNow;
        entry.VoidReason = reason;
        entry.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await SaveAsync(list);
    }

    public async Task<MarReport> GetReportAsync(DateTime fromUtc, DateTime toUtc, Guid? residentId)
    {
        var entries = await LoadAsync(residentId, fromUtc, toUtc, false);

        var lines = entries
            .OrderBy(e => e.ResidentName)
            .ThenBy(e => e.AdministeredAtUtc)
            .Select(e => new MarReportLine
            {
                Id = e.Id,
                ResidentId = e.ResidentId,
                ResidentName = e.ResidentName,
                MedicationId = e.MedicationId,
                MedicationName = e.MedicationName,
                Status = e.Status,
                DoseQuantity = e.DoseQuantity,
                DoseUnit = e.DoseUnit,
                ScheduledForUtc = e.ScheduledForUtc,
                AdministeredAtUtc = e.AdministeredAtUtc,
                RecordedBy = e.RecordedBy,
                Notes = e.Notes
            })
            .ToList();

        return new MarReport
        {
            FromUtc = new DateTimeOffset(fromUtc, TimeSpan.Zero),
            ToUtc = new DateTimeOffset(toUtc, TimeSpan.Zero),
            Summary = new MarReportSummary
            {
                TotalEntries = lines.Count,
                GivenCount = lines.Count(l => l.Status == "Given"),
                RefusedCount = lines.Count(l => l.Status == "Refused"),
                MissedCount = lines.Count(l => l.Status == "Missed"),
                HeldCount = lines.Count(l => l.Status == "Held"),
                NotAvailableCount = lines.Count(l => l.Status == "NotAvailable")
            },
            Lines = lines
        };
    }

    public async Task<List<MarEntry>> LoadAllAsync()
    {
        if (!File.Exists(_filePath))
            return new List<MarEntry>();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new List<MarEntry>();

            return JsonSerializer.Deserialize<List<MarEntry>>(json, _jsonOptions)
                   ?? new List<MarEntry>();
        }
        catch (JsonException)
        {
            await WriteAllTextWithRetryAsync(_filePath, "[]");
            return new List<MarEntry>();
        }
        catch (IOException)
        {
            return new List<MarEntry>();
        }
    }

    public async Task DeleteByClientRequestIdAsync(Guid clientRequestId)
    {
        var list = await LoadAllAsync();
        list.RemoveAll(m => m.ClientRequestId == clientRequestId);
        await SaveAsync(list);
    }

    public async Task ReplaceAllAsync(List<MarEntry> items)
    {
        await SaveAsync(items ?? new List<MarEntry>());
    }

    public async Task UpsertAsync(MarEntry entry)
    {
        var list = await LoadAllAsync();
        var existing = list.FindIndex(m => m.Id == entry.Id || m.ClientRequestId == entry.ClientRequestId);
        if (existing >= 0)
            list[existing] = entry;
        else
            list.Add(entry);
        await SaveAsync(list);
    }

    private async Task SaveAsync(List<MarEntry> items)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await using var stream = new FileStream(
                    _filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read);

                await JsonSerializer.SerializeAsync(stream, items, _jsonOptions);
                return;
            }
            catch (IOException) when (attempt < 2)
            {
                await Task.Delay(50);
            }
        }
    }

    private static async Task WriteAllTextWithRetryAsync(string path, string content)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, content);
                return;
            }
            catch (IOException) when (attempt < 2)
            {
                await Task.Delay(50);
            }
        }
    }

    /// <summary>
    /// Safety net: forces all deserialized DateTime values to Kind=Utc.
    /// Prevents silent filter bugs if a JSON entry ever lacks the "Z" suffix.
    /// </summary>
    private sealed class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dt = reader.GetDateTime();
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }
    }
}
