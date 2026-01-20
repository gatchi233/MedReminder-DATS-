using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedReminder.Models;

namespace MedReminder.Services.Abstractions
{
    public interface IObservationService
    {
        Task<List<Observation>> LoadAsync();
        Task UpsertAsync(Observation item);
        Task DeleteAsync(Observation item);
    }
}
