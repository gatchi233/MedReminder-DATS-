using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MedReminder.Models;
using MedReminder.Services;

namespace MedReminder.ViewModels
{
    public class ResidentsPageViewModel
    {
        private readonly ResidentService _residentService;

        private List<Resident> _allResidents = new();

        public string NameFilter { get; private set; } = string.Empty;

        public ObservableCollection<Resident> Residents { get; } = new();

        public ResidentsPageViewModel(ResidentService residentService)
        {
            _residentService = residentService;
        }

        public async Task LoadResidentsAsync()
        {
            var residents = await _residentService.LoadAsync();

            _allResidents = residents?.ToList() ?? new List<Resident>();
            ApplyFilters();
        }

        public async Task DeleteResidentAsync(Resident resident)
        {
            if (resident == null)
                return;

            await _residentService.DeleteAsync(resident);

            _allResidents = _allResidents
                .Where(r => r.Id != resident.Id)
                .ToList();

            ApplyFilters();
        }

        public void UpdateFilters(string nameFilter)
        {
            NameFilter = nameFilter?.Trim() ?? string.Empty;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            IEnumerable<Resident> query = _allResidents;

            if (!string.IsNullOrWhiteSpace(NameFilter))
            {
                query = query.Where(r =>
                    !string.IsNullOrEmpty(r.Name) &&
                    r.Name.Contains(NameFilter, System.StringComparison.OrdinalIgnoreCase));
            }

            query = query
                .OrderBy(r => r.DOB)
                .ThenBy(r => r.Name);

            Residents.Clear();
            foreach (var r in query)
            {
                Residents.Add(r);
            }
        }
    }
}
