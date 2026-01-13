using System;
using System.Linq;
using Microsoft.Maui.Controls;
using MedReminder.Models;
using MedReminder.Services;

namespace MedReminder.Pages
{
    [QueryProperty(nameof(ResidentId), "id")]
    public partial class ViewResidentPage : AuthPage
    {
        private readonly ResidentService _residentService;

        public int ResidentId { get; set; }

        private Resident? _resident;

        public ViewResidentPage(ResidentService residentService)
        {
            InitializeComponent();
            _residentService = residentService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var list = await _residentService.LoadAsync();
            _resident = list.FirstOrDefault(r => r.Id == ResidentId);

            if (_resident == null)
            {
                await DisplayAlert("Not found", "Resident not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            BindingContext = _resident;

            DobAgeLabel.Text = BuildDobAgeText(_resident.DOB);
        }

        private string BuildDobAgeText(string? dobString)
        {
            if (string.IsNullOrWhiteSpace(dobString))
                return "DOB: (not recorded)";

            if (!DateTime.TryParse(dobString, out var dob))
                return $"DOB: {dobString}";

            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age))
                age--;

            return $"DOB: {dob:yyyy-MM-dd}  (Age {age})";
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            if (_resident == null)
                return;

            await Shell.Current.GoToAsync($"editResident?id={_resident.Id}");
        }

        private async void OnViewMedicationsClicked(object sender, EventArgs e)
        {
            if (_resident == null)
                return;

            var parameters = new Dictionary<string, object?>
            {
                ["residentId"] = _resident.Id,
                ["residentName"] = _resident.Name
            };

            await Shell.Current.GoToAsync("residentMedications", true, parameters);
        }


        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
