using CareHub.ViewModels;

namespace CareHub.Pages.Desktop;

public partial class MarPage : AuthPage, IQueryAttributable
{
    private readonly MarPageViewModel _vm;

    public string? ReturnTo { get; private set; }

    public MarPage(MarPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null)
            return;

        if (query.TryGetValue("returnTo", out var returnValue) && returnValue != null)
            ReturnTo = returnValue.ToString();

        if (query.TryGetValue("residentId", out var value) && value != null)
        {
            Guid residentId = Guid.Empty;

            if (value is Guid g)
                residentId = g;
            else if (value is string s && Guid.TryParse(s, out var parsed))
                residentId = parsed;

            if (residentId != Guid.Empty)
                _vm.SetResident(residentId, string.Empty);
        }

        if (query.TryGetValue("residentName", out var nameValue) && nameValue != null &&
            query.TryGetValue("residentId", out var residentValue) &&
            residentValue != null)
        {
            Guid residentIdFromQuery = residentValue is Guid g ? g : Guid.Empty;
            if (residentIdFromQuery == Guid.Empty)
                Guid.TryParse(residentValue.ToString(), out residentIdFromQuery);

            if (residentIdFromQuery != Guid.Empty)
                _vm.SetResident(residentIdFromQuery, nameValue.ToString() ?? "");
        }

        await _vm.LoadAsync();
    }

    private async void OnVoidClicked(object sender, EventArgs e)
    {
        var row = (sender as BindableObject)?.BindingContext as MarEntryRow;
        if (row == null && sender is Button button)
            row = button.CommandParameter as MarEntryRow;
        if (row == null) return;

        try
        {
            await _vm.VoidAsync(row);
        }
        catch (Exception ex)
        {
            await DisplayAlert("MAR Error", ex.Message, "OK");
        }
    }

    private async void OnCloseClicked(object sender, TappedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ReturnTo))
        {
            await Shell.Current.GoToAsync(ReturnTo);
            return;
        }

        await Shell.Current.GoToAsync("..");
    }
}
