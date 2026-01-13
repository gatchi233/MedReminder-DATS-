using CommunityToolkit.Maui;
using MedReminder.Pages;
using MedReminder.Services;
using MedReminder.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

namespace MedReminder
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<MedicationService>();
            builder.Services.AddSingleton<ResidentService>();
            builder.Services.AddSingleton<AuthService>();

            // ViewModels
            builder.Services.AddSingleton<MedicationViewModel>();
            builder.Services.AddSingleton<ResidentsPageViewModel>();
            builder.Services.AddSingleton<LoginViewModel>();

            // Pages
            builder.Services.AddTransient<EditMedicationPage>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<EditResidentPage>();
            builder.Services.AddTransient<ResidentMedicationsPage>();
            builder.Services.AddTransient<ResidentsPage>();
            builder.Services.AddTransient<ViewResidentPage>();
            builder.Services.AddTransient<LoginPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}
