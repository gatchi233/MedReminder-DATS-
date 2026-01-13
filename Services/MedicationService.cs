using System.Text.Json;
using MedReminder.Models;
using Microsoft.Maui.Storage;

namespace MedReminder.Services
{
    public class MedicationService
    {
        private readonly string _filePath;

        public MedicationService()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "Medications.json");
        }

        private async Task EnsureSeedDataAsync()
        {
            if (File.Exists(_filePath))
                return;

            await using var inStream =
                await FileSystem.OpenAppPackageFileAsync("Medications.json");
            await using var outStream = File.Create(_filePath);
            await inStream.CopyToAsync(outStream);
        }

        public async Task<List<Medication>> LoadAsync()
        {
            await EnsureSeedDataAsync();

            await using var stream = File.OpenRead(_filePath);
            var items = await JsonSerializer.DeserializeAsync<List<Medication>>(stream);
            return items ?? new List<Medication>();
        }

        public async Task SaveAsync(List<Medication> items)
        {
            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(
                stream,
                items,
                new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task UpsertAsync(Medication item)
        {
            var list = await LoadAsync();

            if (item.Id == 0)
            {
                item.Id = list.Count == 0 ? 1 : list.Max(m => m.Id) + 1;
                list.Add(item);
            }
            else
            {
                var existing = list.FirstOrDefault(m => m.Id == item.Id);
                if (existing != null)
                    list.Remove(existing);

                list.Add(item);
            }

            await SaveAsync(list);
        }

        public async Task DeleteAsync(Medication item)
        {
            var list = await LoadAsync();
            list.RemoveAll(m => m.Id == item.Id);
            await SaveAsync(list);
        }
    }
}
