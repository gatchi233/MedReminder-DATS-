using MedReminder.Pages.Desktop;
using MedReminder.Services;
using MedReminder.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MedReminder.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services;
            var vm = services?.GetService<LoginViewModel>();
            BindingContext = vm;
        }

        public LoginPage(LoginViewModel vm) : this()
        {
            BindingContext = vm;
        }

        protected override bool OnBackButtonPressed()
        {
            // Disable back navigation on Login page
            return true;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var auth = Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService<AuthService>();

            if (auth?.IsLoggedIn == true)
            {
                await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            }
        }

    }
}
