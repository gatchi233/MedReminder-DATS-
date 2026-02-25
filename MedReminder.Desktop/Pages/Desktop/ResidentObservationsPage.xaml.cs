using MedReminder.ViewModels;

namespace MedReminder.Pages.Desktop;

public partial class ResidentObservationsPage : ContentPage, IQueryAttributable
{
    private readonly ResidentObservationsViewModel _vm;

    public ResidentObservationsPage(ResidentObservationsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null)
            return;

        if (query.TryGetValue("residentId", out var value) && value != null)
        {
            Guid residentId = Guid.Empty;

            if (value is Guid g)
                residentId = g;
            else if (value is string s && Guid.TryParse(s, out var parsed))
                residentId = parsed;

            if (residentId != Guid.Empty)
            {
                _vm.SetResident(residentId, string.Empty);
                await _vm.LoadAsync();
            }
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
