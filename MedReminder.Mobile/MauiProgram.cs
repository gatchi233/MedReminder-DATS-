using MedReminder.Mobile.Pages;
using MedReminder.Mobile.Services;
using MedReminder.Mobile.Shell;
using MedReminder.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace MedReminder.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>();

        // DI
        builder.Services.AddSingleton<MobileAppShell>();

        builder.Services.AddSingleton<ResidentReadOnlyJsonService>();
        builder.Services.AddSingleton<MedicationReadOnlyJsonService>();

        builder.Services.AddTransient<ResidentsViewModel>();
        builder.Services.AddTransient<ResidentDetailViewModel>();

        builder.Services.AddTransient<ResidentsPage>();
        builder.Services.AddTransient<ResidentDetailPage>();

        return builder.Build();
    }
}
