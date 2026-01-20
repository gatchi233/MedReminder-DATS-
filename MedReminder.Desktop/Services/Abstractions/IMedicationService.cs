using MedReminder.Models;

namespace MedReminder.Services.Abstractions
{
    public interface IMedicationService
    {
        Task<List<Medication>> LoadAsync();
        Task UpsertAsync(Medication item);
        Task DeleteAsync(Medication item);

        // Inventory helpers
        Task<List<Medication>> GetLowStockAsync();
        Task AdjustStockAsync(int medicationId, int delta);
    }
}
