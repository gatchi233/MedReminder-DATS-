using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MedReminder.Desktop.Services.Sync;
using MedReminder.Models;
using MedReminder.Services.Abstractions;

namespace MedReminder.ViewModels;

public class ResidentObservationsViewModel : INotifyPropertyChanged
{
    private readonly IObservationService _observations;
    private bool _hasLoadedOnce;
    public bool IsOffline => !ConnectivityHelper.IsOnline();
    public ICommand SyncCommand { get; }
    public enum ObservationRange
    {
        Today,
        Last3Days,
        Last7Days
    }

    private ObservationRange _currentRange = ObservationRange.Today;

    public void SetRange(ObservationRange range)
    {
        _currentRange = range;
        OnPropertyChanged(nameof(Subtitle));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public ResidentObservationsViewModel(IObservationService observations)
    {
        _observations = observations;

        RefreshCommand = new Command(async () => await LoadAsync());
        AddCommand = new Command(async () => await AddAsync());
        EditCommand = new Command<Observation>(async o => await EditAsync(o));
        DeleteCommand = new Command<Observation>(async o => await DeleteAsync(o));
        SyncCommand = new Command(async() => await SyncAsync(),() => !IsBusy);
    }

    private async Task SyncAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var synced = await _observations.SyncAsync(); // wrapper/coordinator
            StatusMessage = $"Synced {synced} item(s)";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Sync failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            (SyncCommand as Command)?.ChangeCanExecute();
        }
    }

    public Guid ResidentId { get; private set; } = Guid.Empty;
    public string ResidentName { get; private set; } = "";

    public ObservableCollection<Observation> Items { get; } = new();
    public ObservableCollection<Observation> Observations => Items;

    public bool HasLoadedOnce
    {
        get => _hasLoadedOnce;
        private set { _hasLoadedOnce = value; OnPropertyChanged(); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRefreshing));
            OnPropertyChanged(nameof(CanAdd));
            if (_isBusy)
                StatusMessage = "Loading...";
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public ICommand AddObservationCommand => AddCommand;
    public bool IsRefreshing => IsBusy;
    public bool CanAdd => !IsBusy && ResidentId != Guid.Empty;
    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }
    public string Subtitle => _currentRange switch
    {
        ObservationRange.Today => "Today",
        ObservationRange.Last3Days => "Last 3 days",
        ObservationRange.Last7Days => "Last 7 days",
        _ => ""
    };
    // StatusMessage is updated by callers and also when loading finishes.

    public void SetResident(Guid residentId, string residentName)
    {
        ResidentId = residentId;
        ResidentName = residentName ?? "";
        OnPropertyChanged(nameof(ResidentName));
        OnPropertyChanged(nameof(CanAdd));
    }

    public async Task InitializeAsync()
    {
        if (HasLoadedOnce) return;
        await LoadAsync();
        HasLoadedOnce = true;
    }

    public async Task LoadAsync()
    {
        if (IsBusy || ResidentId == Guid.Empty)
            return;

        try
        {
            IsBusy = true;
            Items.Clear();

            var list = await _observations.GetByResidentIdAsync(ResidentId);

            var filtered = ApplyDateFilter(list, _currentRange);

            foreach (var item in filtered.OrderByDescending(x => x.RecordedAt))
                Items.Add(item);

            StatusMessage = $"{Items.Count} observations";
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Load Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddAsync()
    {
        if (ResidentId == Guid.Empty)
            return;

        if (IsBusy)
            return;

        try
        {
            // Minimal: prompt for Type + Value + RecordedBy
            var type = await Shell.Current.DisplayPromptAsync("New Observation", "Type (e.g., Note / BP / Temp):", "OK", "Cancel", "Note");
            if (string.IsNullOrWhiteSpace(type)) return;

            var value = await Shell.Current.DisplayPromptAsync("New Observation", "Value:", "OK", "Cancel", "Example");
            if (string.IsNullOrWhiteSpace(value)) return;

            var by = await Shell.Current.DisplayPromptAsync("New Observation", "Recorded by:", "OK", "Cancel", "Staff");
            if (string.IsNullOrWhiteSpace(by)) by = "Staff";

            IsBusy = true;

            var obs = new Observation
            {
                Id = Guid.Empty,
                ResidentId = ResidentId,
                RecordedBy = by,
                Type = type,
                Value = value
            };

            await _observations.UpsertAsync(obs);
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Add Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }

    private async Task EditAsync(Observation? item)
    {
        if (item is null) return;

        // Simple edit: edit Notes
        var notes = await Shell.Current.DisplayPromptAsync("Edit Observation", "Notes:", "Save", "Cancel", item.Value);
        if (notes is null) return;

        item.Value = notes;
        await _observations.UpsertAsync(item);
        await LoadAsync();
    }

    private async Task DeleteAsync(Observation? item)
    {
        if (item is null) return;

        var ok = await Shell.Current.DisplayAlert("Delete", "Delete this observation?", "Delete", "Cancel");
        if (!ok) return;

        await _observations.DeleteAsync(item);
        await LoadAsync();
    }

    private IEnumerable<Observation> ApplyDateFilter(
    IEnumerable<Observation> source,
    ObservationRange range)
    {
        var todayLocal = DateTime.Now.Date;

        var fromLocal = range switch
        {
            ObservationRange.Today => todayLocal,
            ObservationRange.Last3Days => todayLocal.AddDays(-2),
            ObservationRange.Last7Days => todayLocal.AddDays(-6),
            _ => todayLocal
        };

        var fromUtc = fromLocal.ToUniversalTime();

        return source
            .Where(o => o.RecordedAt >= fromUtc)
            .OrderByDescending(o => o.RecordedAt);
    }
}
