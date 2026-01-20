using MedReminder.Models;
using MedReminder.Pages;
using MedReminder.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MedReminder
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            var window = new Window(shell)
            {
                Title = "MedReminder Pro (DATS)"
            };

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await shell.GoToAsync($"//{nameof(LoginPage)}");

            });

            return window;
        }

    }
}
