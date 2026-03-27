using CareHub.Desktop.Models;
using CareHub.Desktop.Services.Sync;
using CareHub.Models;
using CareHub.Services;
using CareHub.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CareHub.ViewModels;

public class MarPageViewModel : INotifyPropertyChanged
{
    private readonly IMarService _marService;
    private readonly IMedicationService _medicationService;
    private readonly IResidentService _residentService;
    private readonly AuthService? _authService;

    private bool _isBusy;
    private bool _includeVoided;
    private string _statusMessage = "";
    private Resident? _selectedResident;
    private Medication? _selectedMedication;
    private string _selectedStatus = "Given";
    private decimal _doseQuantity = 1m;
    private string _doseUnit = "";
    private string _notes = "";
    private string _voidReason = "";
    private DateTime _fromLocalDate = DateTime.Today;
    private DateTime _toLocalDate = DateTime.Today;
    private DateTime _administeredLocalDate = DateTime.Today;
    private TimeSpan _administeredLocalTime = DateTime.Now.TimeOfDay;
    private DateTime _scheduledLocalDate = DateTime.Today;
    private TimeSpan _scheduledLocalTime = TimeSpan.FromHours(8);
    private MarReport _report = new();

    public MarPageViewModel(IMarService marService, IMedicationService medicationService, IResidentService residentService)
    {
        _marService = marService;
        _medicationService = medicationService;
        _residentService = residentService;
        _authService = MauiProgram.Services.GetService<AuthService>();

        RefreshCommand = new Command(async () => await LoadAsync());
        CreateEntryCommand = new Command(async () => await CreateEntryAsync());
        RefreshReportCommand = new Command(async () => await LoadReportAsync());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Resident> Residents { get; } = new();
    public ObservableCollection<Medication> ResidentMedications { get; } = new();
    public ObservableCollection<MarEntryRow> Entries { get; } = new();
    public ObservableCollection<MarReportLineRow> ReportLines { get; } = new();

    public IReadOnlyList<string> StatusOptions { get; } = new[] { "Given", "Refused", "Held", "Missed", "NotAvailable" };

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRefreshing));
        }
    }

    public bool IsRefreshing => IsBusy;
    public bool IsOffline => !ConnectivityHelper.IsOnline();
    public bool IsNurse => _authService?.HasRole(StaffRole.Nurse) ?? false;
    public bool CanEditEntries => IsNurse;
    public bool CanViewReport => _authService?.HasRole(StaffRole.Nurse, StaffRole.Admin) ?? false;

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

    public Resident? SelectedResident
    {
        get => _selectedResident;
        set
        {
            if (_selectedResident?.Id == value?.Id) return;
            _selectedResident = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedResidentName));
            _ = ReloadResidentMedicationOptionsAsync();
        }
    }

    public string SelectedResidentName => SelectedResident?.ResidentName ?? "All residents";

    public Medication? SelectedMedication
    {
        get => _selectedMedication;
        set
        {
            if (_selectedMedication?.Id == value?.Id) return;
            _selectedMedication = value;
            OnPropertyChanged();
        }
    }

    public bool IncludeVoided
    {
        get => _includeVoided;
        set
        {
            if (_includeVoided == value) return;
            _includeVoided = value;
            OnPropertyChanged();
        }
    }

    public DateTime FromLocalDate
    {
        get => _fromLocalDate;
        set
        {
            if (_fromLocalDate == value) return;
            _fromLocalDate = value;
            OnPropertyChanged();
        }
    }

    public DateTime ToLocalDate
    {
        get => _toLocalDate;
        set
        {
            if (_toLocalDate == value) return;
            _toLocalDate = value;
            OnPropertyChanged();
        }
    }

    public DateTime AdministeredLocalDate
    {
        get => _administeredLocalDate;
        set
        {
            if (_administeredLocalDate == value) return;
            _administeredLocalDate = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan AdministeredLocalTime
    {
        get => _administeredLocalTime;
        set
        {
            if (_administeredLocalTime == value) return;
            _administeredLocalTime = value;
            OnPropertyChanged();
        }
    }

    public DateTime ScheduledLocalDate
    {
        get => _scheduledLocalDate;
        set
        {
            if (_scheduledLocalDate == value) return;
            _scheduledLocalDate = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan ScheduledLocalTime
    {
        get => _scheduledLocalTime;
        set
        {
            if (_scheduledLocalTime == value) return;
            _scheduledLocalTime = value;
            OnPropertyChanged();
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (_selectedStatus == value) return;
            _selectedStatus = value;
            OnPropertyChanged();
        }
    }

    public decimal DoseQuantity
    {
        get => _doseQuantity;
        set
        {
            if (_doseQuantity == value) return;
            _doseQuantity = value;
            OnPropertyChanged();
        }
    }

    public string DoseUnit
    {
        get => _doseUnit;
        set
        {
            if (_doseUnit == value) return;
            _doseUnit = value;
            OnPropertyChanged();
        }
    }

    public string Notes
    {
        get => _notes;
        set
        {
            if (_notes == value) return;
            _notes = value;
            OnPropertyChanged();
        }
    }

    public string VoidReason
    {
        get => _voidReason;
        set
        {
            if (_voidReason == value) return;
            _voidReason = value;
            OnPropertyChanged();
        }
    }

    public MarReportSummary ReportSummary => _report.Summary;
    public string ReportWindowText => $"{_report.FromUtc.LocalDateTime:yyyy-MM-dd} to {_report.ToUtc.LocalDateTime:yyyy-MM-dd}";

    public ICommand RefreshCommand { get; }
    public ICommand CreateEntryCommand { get; }
    public ICommand RefreshReportCommand { get; }

    public void SetResident(Guid residentId, string residentName)
    {
        SelectedResident = Residents.FirstOrDefault(r => r.Id == residentId) ?? new Resident
        {
            Id = residentId,
            ResidentFName = residentName ?? ""
        };
    }

    public async Task InitializeAsync()
    {
        var residents = await _residentService.LoadAsync();

        Residents.Clear();
        foreach (var resident in residents.OrderBy(r => r.ResidentName))
            Residents.Add(resident);

        if (SelectedResident != null)
        {
            SelectedResident = Residents.FirstOrDefault(r => r.Id == SelectedResident.Id) ?? SelectedResident;
        }

        await ReloadResidentMedicationOptionsAsync();
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            await InitializeAsync();

            var fromUtc = ToUtcStart(FromLocalDate);
            var toUtc = ToUtcEnd(ToLocalDate);

            var entries = await _marService.LoadAsync(SelectedResident?.Id, fromUtc.UtcDateTime, toUtc.UtcDateTime, IncludeVoided);
            var medications = await _medicationService.LoadAsync();
            var residents = Residents.ToDictionary(r => r.Id, r => r.ResidentName);
            var medNames = medications.ToDictionary(m => m.Id, m => m.MedName);

            Entries.Clear();
            foreach (var entry in entries.OrderByDescending(e => e.AdministeredAtUtc))
            {
                entry.ResidentName = residents.GetValueOrDefault(entry.ResidentId, "Unknown Resident");
                entry.MedicationName = medNames.GetValueOrDefault(entry.MedicationId, entry.MedicationName);

                if (SelectedMedication != null && entry.MedicationId != SelectedMedication.Id)
                    continue;

                Entries.Add(new MarEntryRow(entry));
            }

            StatusMessage = $"{Entries.Count} MAR record(s)";
            await LoadReportAsync();
        }
        catch (OfflineException)
        {
            StatusMessage = "Offline mode (API unreachable)";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsOffline));
        }
    }

    public async Task CreateEntryAsync()
    {
        if (!CanEditEntries)
            throw new InvalidOperationException("Only nurses can create MAR entries.");

        if (SelectedResident == null)
            throw new InvalidOperationException("Select a resident.");

        if (SelectedMedication == null)
            throw new InvalidOperationException("Select a medication.");

        if (DoseQuantity <= 0)
            throw new InvalidOperationException("Dose quantity must be greater than 0.");

        var entry = new MarEntry
        {
            ClientRequestId = Guid.NewGuid(),
            ResidentId = SelectedResident.Id,
            MedicationId = SelectedMedication.Id,
            Status = SelectedStatus,
            DoseQuantity = DoseQuantity,
            DoseUnit = string.IsNullOrWhiteSpace(DoseUnit) ? SelectedMedication.QuantityUnit : DoseUnit.Trim(),
            AdministeredAtUtc = ToUtc(AdministeredLocalDate, AdministeredLocalTime),
            ScheduledForUtc = ToUtc(ScheduledLocalDate, ScheduledLocalTime),
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            RecordedBy = GetCurrentUserLabel(),
            MedicationName = SelectedMedication.MedName,
            ResidentName = SelectedResident.ResidentName
        };

        await _marService.CreateAsync(entry);
        ClearCreateForm();
        await LoadAsync();
    }

    public async Task VoidAsync(MarEntryRow? row)
    {
        if (!CanEditEntries)
            throw new InvalidOperationException("Only nurses can void MAR entries.");

        if (row == null)
            return;

        await _marService.VoidAsync(row.Id, string.IsNullOrWhiteSpace(VoidReason) ? null : VoidReason.Trim());
        VoidReason = "";
        await LoadAsync();
    }

    public async Task LoadReportAsync()
    {
        if (!CanViewReport)
            return;

        var report = await _marService.GetReportAsync(
            ToUtcStart(FromLocalDate).UtcDateTime,
            ToUtcEnd(ToLocalDate).UtcDateTime,
            SelectedResident?.Id);

        _report = report;
        ReportLines.Clear();
        foreach (var line in report.Lines.Where(l => SelectedMedication == null || l.MedicationId == SelectedMedication.Id))
            ReportLines.Add(new MarReportLineRow(line));

        OnPropertyChanged(nameof(ReportSummary));
        OnPropertyChanged(nameof(ReportWindowText));
    }

    private async Task ReloadResidentMedicationOptionsAsync()
    {
        var all = await _medicationService.LoadAsync();
        var meds = SelectedResident == null
            ? all.Where(m => m.ResidentId.HasValue)
            : all.Where(m => m.ResidentId == SelectedResident.Id);

        ResidentMedications.Clear();
        foreach (var medication in meds.OrderBy(m => m.MedName))
            ResidentMedications.Add(medication);

        if (SelectedMedication != null)
        {
            SelectedMedication = ResidentMedications.FirstOrDefault(m => m.Id == SelectedMedication.Id);
        }
        else
        {
            SelectedMedication = ResidentMedications.FirstOrDefault();
        }

        if (SelectedMedication != null && string.IsNullOrWhiteSpace(DoseUnit))
            DoseUnit = SelectedMedication.QuantityUnit ?? "";
    }

    private void ClearCreateForm()
    {
        SelectedStatus = "Given";
        DoseQuantity = SelectedMedication?.Quantity > 0 ? SelectedMedication.Quantity : 1m;
        DoseUnit = SelectedMedication?.QuantityUnit ?? "";
        Notes = "";
        AdministeredLocalDate = DateTime.Today;
        AdministeredLocalTime = DateTime.Now.TimeOfDay;
        ScheduledLocalDate = DateTime.Today;
        ScheduledLocalTime = SelectedMedication == null ? TimeSpan.FromHours(8) : ScheduledLocalTime;
    }

    private string GetCurrentUserLabel()
    {
        if (_authService?.CurrentUser == null)
            return "Unknown";

        return $"{_authService.CurrentUser.StaffName} ({_authService.CurrentUser.Role})";
    }

    private static DateTimeOffset ToUtc(DateTime localDate, TimeSpan localTime)
    {
        var localDateTime = localDate.Date.Add(localTime);
        return new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime)).ToUniversalTime();
    }

    private static DateTimeOffset ToUtcStart(DateTime localDate)
    {
        var local = localDate.Date;
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
    }

    private static DateTimeOffset ToUtcEnd(DateTime localDate)
    {
        var local = localDate.Date.AddDays(1).AddTicks(-1);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class MarEntryRow
{
    public MarEntryRow(MarEntry entry) => Entry = entry;

    public MarEntry Entry { get; }

    public Guid Id => Entry.Id;
    public bool IsVoided => Entry.IsVoided;
    public string ResidentName => Entry.ResidentName;
    public string MedicationName => Entry.MedicationName;
    public string Status => Entry.Status;
    public string DoseDisplay => $"{Entry.DoseQuantity:0.##} {Entry.DoseUnit}".Trim();
    public string AdministeredDisplay => Entry.AdministeredAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string ScheduledDisplay => Entry.ScheduledForUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "Unscheduled";
    public string RecordedBy => string.IsNullOrWhiteSpace(Entry.RecordedBy) ? "Unknown" : Entry.RecordedBy;
    public string Notes => string.IsNullOrWhiteSpace(Entry.Notes) ? "" : Entry.Notes!;
    public string VoidDisplay => Entry.IsVoided
        ? $"Voided {Entry.VoidedAtUtc?.ToLocalTime():yyyy-MM-dd HH:mm} {Entry.VoidReason}".Trim()
        : "";
    public bool ShowVoidButton => !Entry.IsVoided;
}

public sealed class MarReportLineRow
{
    public MarReportLineRow(MarReportLine line) => Line = line;

    public MarReportLine Line { get; }

    public string Summary => $"{Line.ResidentName} | {Line.MedicationName} | {Line.Status}";
    public string Detail => $"{Line.AdministeredAtUtc.ToLocalTime():yyyy-MM-dd HH:mm} | {Line.DoseQuantity:0.##} {Line.DoseUnit}".Trim();
}
