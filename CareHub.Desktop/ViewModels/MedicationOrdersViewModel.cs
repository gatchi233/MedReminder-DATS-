using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CareHub.Models;
using CareHub.Services;
using CareHub.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CareHub.ViewModels
{
    public class MedicationOrdersViewModel : INotifyPropertyChanged
    {
        private readonly IMedicationService _medicationService;
        private readonly IMedicationOrderService _orderService;
        private readonly AuthService? _authService;

        private bool _isBusy;
        private string _statusMessage = "";
        private Medication? _selectedMedication;

        public MedicationOrdersViewModel(IMedicationService medicationService, IMedicationOrderService orderService)
        {
            _medicationService = medicationService;
            _orderService = orderService;
            _authService = MauiProgram.Services.GetService<AuthService>();

            RefreshCommand = new Command(async () => await LoadAsync());
            ClearMedicationFilterCommand = new Command(() =>
            {
                SelectedMedication = null;
                _ = LoadAsync();
            });

            MarkOrderedCommand = new Command<MedicationOrderRow>(async row =>
                await UpdateStatusAsync(row, MedicationOrderStatus.Ordered));

            MarkReceivedCommand = new Command<MedicationOrderRow>(async row =>
                await UpdateStatusAsync(row, MedicationOrderStatus.Received));

            CancelCommand = new Command<MedicationOrderRow>(async row =>
                await UpdateStatusAsync(row, MedicationOrderStatus.Cancelled));
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
            }
        }

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

        public bool IsNurseOrAdmin => _authService?.HasRole(StaffRole.Nurse, StaffRole.Admin) ?? false;

        public ObservableCollection<Medication> InventoryMedications { get; } = new();
        public ObservableCollection<MedicationOrderRow> Orders { get; } = new();

        public Medication? SelectedMedication
        {
            get => _selectedMedication;
            set
            {
                if (_selectedMedication?.Id == value?.Id) return;
                _selectedMedication = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMedicationFilterActive));
                OnPropertyChanged(nameof(SelectedMedicationName));
            }
        }

        public bool IsMedicationFilterActive => SelectedMedication != null;
        public string SelectedMedicationName => SelectedMedication?.MedName ?? "All medications";

        public ICommand RefreshCommand { get; }
        public ICommand ClearMedicationFilterCommand { get; }
        public ICommand MarkOrderedCommand { get; }
        public ICommand MarkReceivedCommand { get; }
        public ICommand CancelCommand { get; }

        private static bool IsInventoryMedication(Medication m)
        {
            return !m.ResidentId.HasValue || m.ResidentId.Value == Guid.Empty;
        }

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var meds = await _medicationService.LoadAsync();

                var inventory = meds
                    .Where(IsInventoryMedication)
                    .OrderBy(m => m.MedName)
                    .ToList();

                InventoryMedications.Clear();
                foreach (var med in inventory)
                    InventoryMedications.Add(med);

                if (SelectedMedication != null)
                {
                    var stillExists = inventory.FirstOrDefault(m => m.Id == SelectedMedication.Id);
                    if (stillExists == null)
                        SelectedMedication = null;
                    else if (!ReferenceEquals(SelectedMedication, stillExists))
                        SelectedMedication = stillExists;
                }

                var orders = SelectedMedication == null
                    ? await _orderService.LoadAsync()
                    : await _orderService.GetByMedicationIdAsync(SelectedMedication.Id);

                var medNameMap = meds.ToDictionary(m => m.Id, m => m.MedName);
                var needsSave = false;

                foreach (var order in orders)
                {
                    if (string.IsNullOrWhiteSpace(order.MedicationName) &&
                        medNameMap.TryGetValue(order.MedicationId, out var resolved) &&
                        !string.IsNullOrWhiteSpace(resolved))
                    {
                        order.MedicationName = resolved;
                        needsSave = true;
                    }
                }

                if (needsSave)
                {
                    foreach (var order in orders.Where(o => !string.IsNullOrWhiteSpace(o.MedicationName)))
                        await _orderService.UpdateNameAsync(order.Id, order.MedicationName!);
                }

                Orders.Clear();
                foreach (var order in orders.OrderByDescending(x => x.RequestedAt))
                {
                    medNameMap.TryGetValue(order.MedicationId, out var medName);
                    medName ??= order.MedicationName ?? "Unknown Medication";
                    Orders.Add(new MedicationOrderRow(order, medName));
                }

                StatusMessage = SelectedMedication == null
                    ? $"{Orders.Count} order(s)"
                    : $"{Orders.Count} order(s) for {SelectedMedicationName}";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CreateOrderAsync(Guid medicationId, int qty, string? requestedBy, string? notes, string? medicationName = null)
        {
            if (qty <= 0)
                throw new InvalidOperationException("Quantity must be greater than 0.");

            await _orderService.CreateAsync(medicationId, qty, requestedBy, notes, medicationName);
            await LoadAsync();
        }

        public async Task<Guid> CreateInventoryMedicationAsync(string medName, int reorderLevel, string? quantityUnit = null, string? usage = null)
        {
            medName = (medName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(medName))
                throw new ArgumentException("Medication name is required.", nameof(medName));

            var list = await _medicationService.LoadAsync();

            var existing = list.FirstOrDefault(m => IsInventoryMedication(m) &&
                string.Equals((m.MedName ?? "").Trim(), medName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
                return existing.Id;

            var item = new Medication
            {
                ResidentId = null,
                MedName = medName,
                ReorderLevel = reorderLevel,
                StockQuantity = 0,
                QuantityUnit = quantityUnit ?? "",
                Usage = usage
            };

            await _medicationService.UpsertAsync(item);
            return item.Id;
        }

        public async Task UpdateStatusAsync(MedicationOrderRow? row, MedicationOrderStatus newStatus, DateTimeOffset? expiryDate = null)
        {
            if (row == null)
                return;

            await _orderService.UpdateStatusAsync(row.OrderId, newStatus, expiryDate);
            await LoadAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MedicationOrderRow
    {
        private readonly MedicationOrder _o;

        public MedicationOrderRow(MedicationOrder o, string medName)
        {
            _o = o;
            MedicationName = medName;
        }

        public Guid OrderId => _o.Id;
        public string MedicationName { get; }
        public string QuantityText => $"Qty: {_o.RequestedQuantity}";
        public string? Notes => _o.Notes;

        private static string Who(string? name) => string.IsNullOrWhiteSpace(name) ? "(unknown)" : name;

        public string StatusDisplay => _o.Status switch
        {
            MedicationOrderStatus.Requested => "Requested",
            MedicationOrderStatus.Ordered => "Ordered",
            MedicationOrderStatus.Received => "Received",
            MedicationOrderStatus.Cancelled => "Cancelled",
            _ => _o.Status.ToString()
        };

        public Color StatusBadgeColor => _o.Status switch
        {
            MedicationOrderStatus.Requested => Color.FromArgb("#F0AD4E"),
            MedicationOrderStatus.Ordered => Color.FromArgb("#5BC0DE"),
            MedicationOrderStatus.Received => Color.FromArgb("#5CB85C"),
            MedicationOrderStatus.Cancelled => Color.FromArgb("#777777"),
            _ => Color.FromArgb("#777777")
        };

        public string RequestedLine => $"{_o.RequestedAt.ToLocalTime():yyyy-MM-dd HH:mm} by {Who(_o.RequestedBy)}";
        public string OrderedLine => _o.OrderedAt == null ? "Not ordered yet" : $"{_o.OrderedAt.Value.ToLocalTime():yyyy-MM-dd HH:mm} by {Who(_o.OrderedBy)}";
        public string ReceivedLine => _o.ReceivedAt == null ? "Not received yet" : $"{_o.ReceivedAt.Value.ToLocalTime():yyyy-MM-dd HH:mm} by {Who(_o.ReceivedBy)}";
        public string CancelledLine => _o.CancelledAt == null ? "Not cancelled" : $"{_o.CancelledAt.Value.ToLocalTime():yyyy-MM-dd HH:mm} by {Who(_o.CancelledBy)}";
        public string ExpiryLine => _o.ReceivedExpiryDate == null ? "" : $"Expiry: {_o.ReceivedExpiryDate.Value:yyyy-MM-dd}";

        public Color RequestedTagColor => Color.FromArgb("#F0AD4E");
        public Color OrderedTagColor => Color.FromArgb("#5BC0DE");
        public Color ReceivedTagColor => Color.FromArgb("#5CB85C");
        public Color CancelledTagColor => Color.FromArgb("#777777");

        public bool IsCancelled => _o.Status == MedicationOrderStatus.Cancelled;
        public bool ShowOrderedRow => _o.OrderedAt != null || _o.Status is MedicationOrderStatus.Ordered or MedicationOrderStatus.Received or MedicationOrderStatus.Cancelled;
        public bool ShowThirdRow => _o.Status is MedicationOrderStatus.Cancelled or MedicationOrderStatus.Received;
        public string ThirdTagText => IsCancelled ? "Cancelled" : "Received";
        public Color ThirdTagColor => IsCancelled ? CancelledTagColor : ReceivedTagColor;
        public string ThirdLineText => IsCancelled ? CancelledLine : ReceivedLine;
        public bool ShowExpiry => !string.IsNullOrWhiteSpace(ExpiryLine);
        public bool ShowCancelRequest => _o.Status == MedicationOrderStatus.Requested;
        public bool ShowMarkOrdered => _o.Status == MedicationOrderStatus.Requested;
        public bool ShowCancelOrder => _o.Status == MedicationOrderStatus.Ordered;
        public bool ShowMarkReceived => _o.Status == MedicationOrderStatus.Ordered;
    }
}
