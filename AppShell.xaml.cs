using MedReminder.Pages;
using MedReminder.Services;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;


namespace MedReminder
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("editMedication", typeof(MedReminder.Pages.EditMedicationPage));
            Routing.RegisterRoute("editResident", typeof(MedReminder.Pages.EditResidentPage));
            Routing.RegisterRoute("viewResident", typeof(MedReminder.Pages.ViewResidentPage));
            Routing.RegisterRoute("residentMedications", typeof(MedReminder.Pages.ResidentMedicationsPage));
            Routing.RegisterRoute("login", typeof(MedReminder.Pages.LoginPage));
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

            if (!auth.IsLoggedIn &&
                !args.Target.Location.OriginalString.Contains("login"))
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