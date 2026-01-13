using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedReminder.Services
{
    public class AuthService
    {
        private readonly Dictionary<string, string> _staff =
            new()
            {
                { "admin", "admin123" },
                { "nurse1", "password" },
                { "staff", "1234" }
            };

        public bool IsLoggedIn { get; private set; }
        public string? CurrentUser { get; private set; }

        public bool Login(string username, string password)
        {
            if (_staff.TryGetValue(username, out var pw) && pw == password)
            {
                IsLoggedIn = true;
                CurrentUser = username;
                return true;
            }
            return false;
        }

        public void Logout()
        {
            IsLoggedIn = false;
            CurrentUser = null;
        }

        // TODO (Optional):
        // Add session timeout support (e.g. auto logout after inactivity)
        // Add role-based authorization (e.g. Staff vs Admin)

    }
}
