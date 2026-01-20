using MedReminder.Pages.Desktop;
using MedReminder.Services;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace MedReminder.ViewModels
{
    public class LoginViewModel
    {
        private readonly AuthService _auth;

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public Command LoginCommand { get; }

        public LoginViewModel(AuthService auth)
        {
            _auth = auth;
            LoginCommand = new Command(async () => await OnLoginAsync());
        }

        private async Task OnLoginAsync()
        {
            try
            {
                if (_auth.Login(Username.Trim(), Password))
                {
                    await Shell.Current.GoToAsync($"//{nameof(HomePage)}");

                    return;
                }

                await Application.Current.MainPage.DisplayAlert(
                    "Login failed",
                    "Invalid username or password.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Login error",
                    ex.Message,
                    "OK");
            }
        }
    }
}
