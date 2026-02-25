using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedReminder.Desktop.Services.Sync;

public static class ConnectivityHelper
{
    public static bool IsOnline()
    {
        // For MAUI: this checks network access, not “API reachable”
        return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }
}
