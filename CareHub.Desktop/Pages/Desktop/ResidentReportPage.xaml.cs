using System.Collections.Generic;
using CareHub.Models;
using CareHub.Services;
using CareHub.Services.Abstractions;
using CareHub.ViewModels;
using Microsoft.Maui.Controls;

namespace CareHub.Pages.Desktop
{
    public partial class ResidentReportPage : ContentPage, IQueryAttributable
    {
        private readonly ResidentReportViewModel _vm;
        private string? ReturnTo { get; set; }

        public ResidentReportPage(ResidentReportViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            var auth = MauiProgram.Services.GetService<AuthService>();
            var canUseAi = auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false;
            if (AiDraftAction != null) AiDraftAction.IsVisible = canUseAi;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query is null)
                return;

            if (query.TryGetValue("returnTo", out var returnValue) && returnValue != null)
                ReturnTo = returnValue.ToString();

            if (query.TryGetValue("residentId", out var value) && value != null)
            {
                if (value is Guid gid)
                {
                    _vm.ResidentId = gid;
                    return;
                }

                var s = value.ToString();
                if (!string.IsNullOrWhiteSpace(s) && Guid.TryParse(s, out var parsedGuid))
                {
                    _vm.ResidentId = parsedGuid;
                    return;
                }
            }
        }

        private async void OnAiDraftClicked(object sender, TappedEventArgs e)
        {
            if (_vm.ResidentId == Guid.Empty)
            {
                await DisplayAlert("No Resident", "Load a resident report first.", "OK");
                return;
            }

            var ai = MauiProgram.Services.GetService<IAiService>();
            if (ai == null)
            {
                await DisplayAlert("Unavailable", "AI service is not configured.", "OK");
                return;
            }

            AiDraftAction.IsEnabled = false;
            AiDraftAction.Opacity = 0.5;
            AiDraftSection.IsVisible = true;
            AiDraftLabel.Text = "Generating AI draft...";

            try
            {
                var result = await ai.ReportDraftAsync(_vm.ResidentId);
                AiDraftLabel.Text = result.Success
                    ? result.Content
                    : $"Error: {result.Content}";
            }
            catch (Exception ex)
            {
                AiDraftLabel.Text = $"Could not generate draft: {ex.Message}";
            }
            finally
            {
                AiDraftAction.IsEnabled = true;
                AiDraftAction.Opacity = 1.0;
            }
        }

        private async void OnCloseClicked(object sender, TappedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ReturnTo))
            {
                await Shell.Current.GoToAsync(ReturnTo);
                return;
            }

            await Shell.Current.GoToAsync($"//{nameof(FloorPlanPage)}");
        }

    }
}
