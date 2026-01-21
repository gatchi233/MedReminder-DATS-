using MedReminder.Pages;
using MedReminder.Pages.Desktop;
using MedReminder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace MedReminder
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Routes
            Routing.RegisterRoute(nameof(EditMedicationPage), typeof(EditMedicationPage));
            Routing.RegisterRoute(nameof(EditResidentPage), typeof(EditResidentPage));
            Routing.RegisterRoute(nameof(FloorPlanPage), typeof(FloorPlanPage));
            Routing.RegisterRoute(nameof(MedicationInventoryPage), typeof(MedicationInventoryPage));
            Routing.RegisterRoute(nameof(MedicationOrdersPage), typeof(MedicationOrdersPage));
            Routing.RegisterRoute(nameof(ResidentMedicationsPage), typeof(ResidentMedicationsPage));
            Routing.RegisterRoute(nameof(ResidentReportPage), typeof(ResidentReportPage));
            Routing.RegisterRoute(nameof(ResidentsPage), typeof(ResidentsPage));
            Routing.RegisterRoute(nameof(ViewResidentPage), typeof(ViewResidentPage));
            Routing.RegisterRoute(nameof(StaffManagementPage), typeof(StaffManagementPage)); 

            // Login is not under Desktop namespace
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            var auth = Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService<AuthService>();

            if (auth == null)
                return;

            // Allow navigation to login page when not logged in
            var target = args.Target?.Location?.OriginalString ?? string.Empty;

            if (!auth.IsLoggedIn && !target.Contains("login", StringComparison.OrdinalIgnoreCase))
            {
                args.Cancel();
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.GoToAsync("//login");
                });
            }
        }
    }
}
