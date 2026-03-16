using CareHub.Models;
using CareHub.Services;
using CareHub.Services.Abstractions;

namespace CareHub.Pages.Desktop;

public partial class AiCareQueryPage : ContentPage, IQueryAttributable
{
    private readonly IAiService _ai;
    private readonly IResidentService _residentService;
    private List<Resident> _residents = new();
    private string? ReturnTo { get; set; }

    public AiCareQueryPage(IAiService ai, IResidentService residentService)
    {
        InitializeComponent();
        _ai = ai;
        _residentService = residentService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null) return;
        if (query.TryGetValue("returnTo", out var val) && val != null)
            ReturnTo = val.ToString();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var auth = MauiProgram.Services.GetService<AuthService>();
        var canUseAi = auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false;
        if (!canUseAi)
        {
            await DisplayAlert("Access Denied", "AI features require Staff or Admin role.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        try
        {
            _residents = (await _residentService.LoadAsync()).OrderBy(r => r.ResidentName).ToList();
            ResidentPicker.Items.Clear();
            ResidentPicker.Items.Add("All residents (facility-wide)");
            foreach (var r in _residents)
                ResidentPicker.Items.Add($"{r.ResidentName} — Room {r.RoomNumber}");
            ResidentPicker.SelectedIndex = 0;
        }
        catch
        {
            // Offline — picker will just have "All residents"
        }
    }

    private async void OnAskClicked(object sender, TappedEventArgs e)
    {
        var query = QueryEditor.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            await DisplayAlert("Empty Query", "Please enter a question.", "OK");
            return;
        }

        AskButton.IsEnabled = false;
        AskButton.Opacity = 0.5;
        ResponseLabel.Text = "Analyzing...";

        try
        {
            Guid? residentId = null;
            if (ResidentPicker.SelectedIndex > 0)
                residentId = _residents[ResidentPicker.SelectedIndex - 1].Id;

            var result = await _ai.CareQueryAsync(query, residentId);

            if (result.Success)
            {
                ResponseLabel.Text = result.Content;
                DisclaimerLabel.Text = result.Disclaimer;
                DisclaimerLabel.IsVisible = true;
            }
            else
            {
                ResponseLabel.Text = result.Content;
                DisclaimerLabel.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            ResponseLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            AskButton.IsEnabled = true;
            AskButton.Opacity = 1.0;
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
