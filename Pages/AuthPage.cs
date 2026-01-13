using MedReminder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace MedReminder.Pages
{
    public class AuthPage : ContentPage
    {
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var auth = Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService<MedReminder.Services.AuthService>();

            // If not logged in, force redirect
            if (auth != null && !auth.IsLoggedIn)
            {
                await Shell.Current.GoToAsync("//login");
            }
        }

        protected async Task LogoutAsync()
        {
            var auth = Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService<MedReminder.Services.AuthService>();

            auth?.Logout();

            await Shell.Current.GoToAsync("//login");
        }

        // TODO (Optional):
        // Enforce role-based access control here once roles are added

    }
}
