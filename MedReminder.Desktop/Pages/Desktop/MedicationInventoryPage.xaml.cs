using MedReminder.Models;
using MedReminder.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MedReminder.Pages.Desktop
{
    public partial class MedicationInventoryPage : AuthPage
    {
        private MedicationInventoryViewModel VM => (MedicationInventoryViewModel)BindingContext;

        public MedicationInventoryPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException("MAUI services not available.");

            BindingContext = services.GetRequiredService<MedicationInventoryViewModel>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await VM.LoadAsync();
        }

        private async void OnSortClicked(object sender, EventArgs e)
        {
            var choice = await DisplayActionSheet(
                "Sort Inventory",
                "Cancel",
                null,
                "Low stock first (default)",
                "Name (A–Z)",
                "Stock (low → high)",
                "Reorder level (high → low)");

            if (choice == null || choice == "Cancel")
                return;

            VM.SortMode = choice switch
            {
                "Name (A–Z)" => InventorySortMode.Name,
                "Stock (low → high)" => InventorySortMode.Stock,
                "Reorder level (high → low)" => InventorySortMode.ReorderLevel,
                _ => InventorySortMode.LowStockFirst
            };

            await VM.LoadAsync();
        }

        private async void OnAdjustClicked(object sender, EventArgs e)
        {
            if (sender is not Button { BindingContext: MedicationInventoryRow row })
                return;

            // TODO (later): Supervisor-only adjustment.

            var input = await DisplayPromptAsync(
                "Adjust Stock",
                $"Current stock: {row.StockQuantity}\n\nEnter adjustment for {row.MedName}.\nUse + to add, - to subtract (e.g., 10 or -5).",
                accept: "OK",
                cancel: "Cancel",
                placeholder: "e.g., 10 or -5",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(input))
                return;

            input = input.Trim();

            if (!int.TryParse(input, out var delta) || delta == 0)
            {
                await DisplayAlert("Invalid", "Please enter a non-zero integer (e.g., 10 or -5).", "OK");
                return;
            }

            // Step 1: Block negative stock (with a friendly fallback option)
            var newStock = row.StockQuantity + delta;
            if (newStock < 0)
            {
                var setToZero = await DisplayAlert(
                    "Invalid adjustment",
                    $"That would make stock negative.\n\nCurrent: {row.StockQuantity}\nAdjustment: {delta}\nResult: {newStock}\n\nDo you want to set stock to 0 instead?",
                    "Set to 0",
                    "Cancel");

                if (!setToZero)
                    return;

                // Adjust down to exactly 0
                delta = -row.StockQuantity;

                // If already 0, nothing to do
                if (delta == 0)
                    return;
            }

            VM.AdjustStockCommand.Execute(Tuple.Create(row.Med, delta));
        }

        private async void OnOrderClicked(object sender, EventArgs e)
        {
            if (sender is not Button { BindingContext: MedicationInventoryRow row })
                return;

            var qtyText = await DisplayPromptAsync(
                "Create Order",
                $"Enter quantity to order for {row.MedName}:",
                accept: "Create",
                cancel: "Cancel",
                placeholder: "e.g. 30",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(qtyText))
                return;

            if (!int.TryParse(qtyText, out var qty) || qty <= 0)
            {
                await DisplayAlert("Invalid", "Please enter a positive number.", "OK");
                return;
            }

            var notes = await DisplayPromptAsync(
                "Notes (optional)",
                "Add notes for this order (optional):",
                accept: "OK",
                cancel: "Skip",
                placeholder: "e.g., urgent / pharmacy call");

            VM.CreateOrderCommand.Execute(
                Tuple.Create(row.Med, qty, string.IsNullOrWhiteSpace(notes) ? null : notes));

            await DisplayAlert("Created", "Order created (Status: Requested).", "OK");
        }

        private async void OnOrdersClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(MedicationOrdersPage));
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            await LogoutAsync();
        }
    }
}
