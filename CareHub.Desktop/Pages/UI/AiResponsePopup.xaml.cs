using CommunityToolkit.Maui.Views;

namespace CareHub.Pages.UI;

public partial class AiResponsePopup : Popup
{
    public AiResponsePopup(string title, string content, string disclaimer)
    {
        InitializeComponent();
        TitleLabel.Text = title;
        ContentLabel.Text = content;
        DisclaimerLabel.Text = $"--- {disclaimer} ---";
    }

    private void OnCloseTapped(object? sender, EventArgs e)
    {
        Close();
    }
}
