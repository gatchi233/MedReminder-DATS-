using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedReminder.Models;

namespace MedMinder.Services.Abstractions
{
    public interface IStaffService
    {
        Task<List<StaffRecord>> GetAllAsync();
        Task SaveAllAsync(List<StaffRecord> staff);

        Task AddOrUpdateAsync(StaffRecord staff);
        Task SetEnabledAsync(string employeeId, bool isEnabled);
    }
}
