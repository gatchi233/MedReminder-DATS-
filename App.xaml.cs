using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using MedReminder.Services;
using MedReminder.Models;

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
                await shell.GoToAsync("//login");
            });

#if WINDOWS
            const int targetWidth = 600;
            const int targetHeight = 800;

            window.Created += (s, e) =>
            {
                var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
                var screenWidth = displayInfo.Width / displayInfo.Density;
                var screenHeight = displayInfo.Height / displayInfo.Density;

                window.X = (screenWidth - targetWidth) / 2;
                window.Y = (screenHeight - targetHeight) / 3;
                window.Width = targetWidth;
                window.Height = targetHeight;
            };
#endif

            return window;
        }

    }
}
