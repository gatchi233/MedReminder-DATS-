using CommunityToolkit.Maui.Views;
using MedReminder.Resources.Styles;
using Microsoft.Maui.Controls.Shapes;

namespace MedReminder.Pages.UI.Popups;

public partial class ActionPopup : Popup
{
    public enum PopupMode
    {
        List,
        Form
    }

    public record PopupResult(
        string? SelectedKey = null,
        string? Field1 = null,
        string? Field2 = null
    );

    private PopupMode _mode;

    // For Form mode validation (optional)
    private Func<string?, string?, (bool ok, string? error)>? _validator;

    public ActionPopup()
    {
        InitializeComponent();
    }

    // Sort
    public void ConfigureList(string title, IEnumerable<(string key, string text)> items)
    {
        _mode = PopupMode.List;

        TitleLabel.Text = title;

        ListContainer.IsVisible = true;
        FormContainer.IsVisible = false;

        ListStack.Children.Clear();

foreach (var (key, text) in items)
{
    var cardBg = (Color)Application.Current.Resources["Popup_Card_Background"];
    var cardBorder = (Color)Application.Current.Resources["Popup_Card_Border"];
    var fontPrimary = (Color)Application.Current.Resources["Popup_Font_Primary"];

    var row = new Border
    {
        BackgroundColor = cardBg,
        Stroke = cardBorder,
        StrokeThickness = 1,
        Padding = new Thickness(12, 10),
        StrokeShape = new RoundRectangle { CornerRadius = 10 },
        Content = new Label
        {
            Text = text,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = fontPrimary,
            VerticalOptions = LayoutOptions.Center
        }
    };

    var tap = new TapGestureRecognizer();
    tap.Tapped += (_, __) => Close(new PopupResult(SelectedKey: key));
    row.GestureRecognizers.Add(tap);

    ListStack.Children.Add(row);
}    }

    //Form
    public void ConfigureForm(
        string title,
        string? message,
        string primaryText,
        (string label, string placeholder, Keyboard keyboard)? field1,
        (string label, string placeholder, Keyboard keyboard)? field2,
        Func<string?, string?, (bool ok, string? error)>? validator = null)
    {
        _mode = PopupMode.Form;

        TitleLabel.Text = title;

        ListContainer.IsVisible = false;
        FormContainer.IsVisible = true;

        _validator = validator;

        if (!string.IsNullOrWhiteSpace(message))
        {
            MessageLabel.Text = message;
            MessageLabel.IsVisible = true;
        }
        else
        {
            MessageLabel.IsVisible = false;
        }

        PrimaryButton.Text = primaryText;

        // Field1
        if (field1.HasValue)
        {
            Field1Container.IsVisible = true;
            Field1Label.Text = field1.Value.label;
            Field1Entry.Placeholder = field1.Value.placeholder;
            Field1Entry.Keyboard = field1.Value.keyboard;
        }
        else
        {
            Field1Container.IsVisible = false;
            Field1Entry.Text = string.Empty;
        }

        // Field2
        if (field2.HasValue)
        {
            Field2Container.IsVisible = true;
            Field2Label.Text = field2.Value.label;
            Field2Entry.Placeholder = field2.Value.placeholder;
            Field2Entry.Keyboard = field2.Value.keyboard;
        }
        else
        {
            Field2Container.IsVisible = false;
            Field2Entry.Text = string.Empty;
        }
    }

    private void OnCloseTapped(object sender, EventArgs e) => Close(null);
    private void OnCancelClicked(object sender, EventArgs e) => Close(null);

    private async void OnPrimaryClicked(object sender, EventArgs e)
    {
        if (_mode != PopupMode.Form)
            return;

        var v1 = Field1Entry.Text?.Trim();
        var v2 = Field2Entry.Text?.Trim();

        if (_validator is not null)
        {
            var (ok, error) = _validator(v1, v2);
            if (!ok)
            {
                await Application.Current.MainPage.DisplayAlert("Invalid", error ?? "Invalid input.", "OK");
                return;
            }
        }

        Close(new PopupResult(Field1: v1, Field2: string.IsNullOrWhiteSpace(v2) ? null : v2));
    }
}
