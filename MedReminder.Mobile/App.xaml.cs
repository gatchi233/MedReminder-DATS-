using MedReminder.Mobile.Shell;

namespace MedReminder.Mobile
{
    public partial class App : Application
    {
        public App(MobileAppShell shell)
        {
            InitializeComponent();
            MainPage = shell;
        }
    }
}
