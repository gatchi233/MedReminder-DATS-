using CareHub.Models;
using CareHub.Services;
using CareHub.Services.Abstractions;

namespace CareHub.Pages.Desktop;

public partial class AiShiftHandoffPage : ContentPage
{
    private string _reportContent = "";

    public AiShiftHandoffPage()
    {
        InitializeComponent();
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

        await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        var ai = MauiProgram.Services.GetService<IAiService>();
        if (ai == null)
        {
            ReportLabel.Text = "AI service is not configured.";
            return;
        }

        PrintAction.IsEnabled = false;
        PrintAction.Opacity = 0.5;
        ReportLabel.Text = "Generating shift handoff report...";

        try
        {
            var result = await ai.ShiftHandoffAsync();
            if (result.Success)
            {
                _reportContent = result.Content;
                ReportLabel.Text = result.Content;
                DisclaimerLabel.Text = result.Disclaimer;
                DisclaimerLabel.IsVisible = true;
            }
            else
            {
                ReportLabel.Text = $"Error: {result.Content}";
                DisclaimerLabel.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            ReportLabel.Text = $"Could not generate report: {ex.Message}";
        }
        finally
        {
            PrintAction.IsEnabled = true;
            PrintAction.Opacity = 1.0;
        }
    }

    private async void OnPrintClicked(object sender, TappedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_reportContent))
        {
            await DisplayAlert("Nothing to Print", "Wait for the report to finish generating.", "OK");
            return;
        }

        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var fileName = $"ShiftHandoff_{timestamp}.html";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);

            var html = $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>Shift Handoff Report</title>
<style>
  body {{ font-family: 'Segoe UI', Arial, sans-serif; max-width: 800px; margin: 40px auto; padding: 0 20px; color: #1a1a1a; }}
  h1 {{ font-size: 22px; border-bottom: 2px solid #7C3AED; padding-bottom: 8px; }}
  .meta {{ font-size: 12px; color: #666; margin-bottom: 20px; }}
  .content {{ font-size: 14px; line-height: 1.6; white-space: pre-wrap; }}
  .disclaimer {{ margin-top: 30px; padding: 10px; background: #FEF3C7; border-radius: 6px; font-size: 11px; color: #92400E; font-style: italic; }}
  @media print {{ body {{ margin: 20px; }} }}
</style>
</head>
<body>
<h1>Shift Handoff Report</h1>
<div class=""meta"">Generated: {DateTime.Now:MMMM dd, yyyy h:mm tt}</div>
<div class=""content"">{System.Net.WebUtility.HtmlEncode(_reportContent)}</div>
<div class=""disclaimer"">AI-Generated — For Informational Purposes Only. This report must be reviewed by qualified staff.</div>
</body>
</html>";

            await File.WriteAllTextAsync(filePath, html);

            // Open in default browser for printing
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Print Error", $"Could not prepare report for printing: {ex.Message}", "OK");
        }
    }

    private async void OnCloseClicked(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
