using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.Services.Remote;

public sealed class ObservationApiService : IObservationService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = CreateJsonOptions();

    public ObservationApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Observation>> LoadAsync()
    {
        var items = await _http.GetFromJsonAsync<List<Observation>>("api/Observations");
        return items ?? new List<Observation>();
    }

    public async Task<List<Observation>> GetByResidentIdAsync(Guid residentId)
    {
        var items = await _http.GetFromJsonAsync<List<Observation>>(
            $"api/Observations/by-resident/{residentId}");
        return items ?? new List<Observation>();
    }

    public Task AddAsync(Observation observation)
    {
        if (observation is null)
            throw new ArgumentNullException(nameof(observation));

        return UpsertAsync(observation);
    }

    public Task<int> SyncAsync()
    {
        return Task.FromResult(0);
    }

    public async Task UpsertAsync(Observation item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        // Create
        if (item.Id == Guid.Empty)
        {
            var resp = await _http.PostAsJsonAsync("api/Observations", item, _jsonOptions);
            resp.EnsureSuccessStatusCode();

            // If API returns the created entity, update local object
            var created = await resp.Content.ReadFromJsonAsync<Observation>();
            if (created is not null)
            {
                item.Id = created.Id;
                item.RecordedAt = created.RecordedAt;
            }

            return;
        }

        // Update
        var putResp = await _http.PutAsJsonAsync($"api/Observations/{item.Id}", item, _jsonOptions);
        putResp.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(Observation item)
    {
        if (item is null || item.Id == Guid.Empty)
            return;

        var resp = await _http.DeleteAsync($"api/Observations/{item.Id}");

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return;

        resp.EnsureSuccessStatusCode();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new UtcDateTimeConverter());
        options.Converters.Add(new UtcNullableDateTimeConverter());
        return options;
    }

    private sealed class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetDateTime();
            return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            writer.WriteStringValue(utc.ToString("O"));
        }
    }

    private sealed class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var value = reader.GetDateTime();
            return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var utc = value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime();
            writer.WriteStringValue(utc.ToString("O"));
        }
    }
}
