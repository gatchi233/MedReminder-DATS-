using MedReminder.Models;

namespace MedReminder.Services.Abstractions
{
    public interface IMedicationOrderService
    {
        Task<List<MedicationOrder>> LoadAsync();

        Task<MedicationOrder> CreateAsync(
            int medicationId,
            int requestedQuantity,
            string? requestedBy,
            string? notes);

        Task UpdateStatusAsync(int orderId, MedicationOrderStatus newStatus);

        Task DeleteAsync(int orderId);

        Task<List<MedicationOrder>> GetByMedicationIdAsync(int medicationId);
    }
}
