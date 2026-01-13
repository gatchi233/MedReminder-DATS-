using System.Text.Json;
using MedReminder.Models;
using Microsoft.Maui.Storage;

namespace MedReminder.Services
{
    public class ResidentService
    {
        private readonly string _filePath;

        public ResidentService()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "Residents.json");
        }

        private async Task EnsureSeedDataAsync()
        {
            if (File.Exists(_filePath))
                return;

            await using var inStream =
                await FileSystem.OpenAppPackageFileAsync("Residents.json");
            await using var outStream = File.Create(_filePath);
            await inStream.CopyToAsync(outStream);
        }

        private async Task<List<Resident>> LoadInternalAsync()
        {
            await EnsureSeedDataAsync();

            await using var stream = File.OpenRead(_filePath);
            var items = await JsonSerializer.DeserializeAsync<List<Resident>>(stream);
            return items ?? new List<Resident>();
        }

        private async Task SaveInternalAsync(List<Resident> items)
        {
            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(
                stream,
                items,
                new JsonSerializerOptions { WriteIndented = true });
        }

        public Task<List<Resident>> LoadAsync() => LoadInternalAsync();

        public async Task UpsertAsync(Resident item)
        {
            var list = await LoadInternalAsync();

            if (item.Id == 0)
            {
                item.Id = list.Count == 0 ? 1 : list.Max(r => r.Id) + 1;
                list.Add(item);
            }
            else
            {
                var existing = list.FirstOrDefault(r => r.Id == item.Id);
                if (existing != null)
                    list.Remove(existing);

                list.Add(item);
            }

            await SaveInternalAsync(list);
        }

        public async Task DeleteAsync(Resident item)
        {
            var list = await LoadInternalAsync();
            list.RemoveAll(r => r.Id == item.Id);
            await SaveInternalAsync(list);
        }
    }
}
