using System.Collections.Generic;
using MedReminder.ViewModels;
using Microsoft.Maui.Controls;

namespace MedReminder.Pages.Desktop
{
    public partial class ResidentReportPage : ContentPage, IQueryAttributable
    {
        private readonly ResidentReportViewModel _vm;

        public ResidentReportPage(ResidentReportViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query is null)
                return;

            if (query.TryGetValue("residentId", out var value) && value != null)
            {
                if (value is int id)
                {
                    _vm.ResidentId = id;
                    return;
                }

                if (int.TryParse(value.ToString(), out var parsed))
                    _vm.ResidentId = parsed;
            }
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"//{nameof(FloorPlanPage)}");
        }
    }
}
