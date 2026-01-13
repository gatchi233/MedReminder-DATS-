using System;
using System.Collections.Generic;
using MedReminder.Models;
using MedReminder.Services;
using MedReminder.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace MedReminder.Pages
{
    public partial class ResidentsPage : AuthPage
    {
        private readonly ResidentsPageViewModel _vm;

        // MUST be parameterless (Shell creates pages)
        public ResidentsPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException("MAUI services not available.");

            var residentService = services.GetRequiredService<ResidentService>();
            _vm = new ResidentsPageViewModel(residentService);

            BindingContext = _vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await _vm.LoadResidentsAsync();
            _vm.UpdateFilters(NameSearchBar?.Text ?? string.Empty);
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            _vm.UpdateFilters(NameSearchBar?.Text ?? string.Empty);
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            await LogoutAsync();
        }

        private async void OnAddResidentClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("editResident");
        }

        // CollectionView SelectionChanged (after you fix the XAML to call this)
        private async void OnResidentClicked(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0)
                return;

            if (e.CurrentSelection[0] is not Resident r)
                return;

            if (sender is CollectionView cv)
                cv.SelectedItem = null;

            await Shell.Current.GoToAsync($"viewResident?id={r.Id}");
        }

        // View button click
        private async void OnViewResidentClicked(object sender, EventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is not Resident r)
                return;

            await Shell.Current.GoToAsync($"viewResident?id={r.Id}");
        }

        private async void OnEditResidentClicked(object sender, EventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is not Resident r)
                return;

            await Shell.Current.GoToAsync($"editResident?id={r.Id}");
        }

        private async void OnViewMedClicked(object sender, EventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is not Resident r)
                return;

            var parameters = new Dictionary<string, object?>
            {
                ["residentId"] = r.Id,
                ["residentName"] = r.Name
            };

            await Shell.Current.GoToAsync("residentMedications", true, parameters);
        }

        private async void OnDeleteResidentClicked(object sender, EventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is not Resident r)
                return;

            bool confirm = await DisplayAlert(
                "Delete resident",
                $"Are you sure you want to delete {r.Name}?",
                "Delete", "Cancel");

            if (!confirm)
                return;

            await _vm.DeleteResidentAsync(r);
        }
    }
}
