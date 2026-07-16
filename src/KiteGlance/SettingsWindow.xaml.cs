using System.Windows;
using System.Windows.Input;
using KiteGlance.Interop;
using KiteGlance.Services;

namespace KiteGlance;

public partial class SettingsWindow : Window
{
    private readonly CredentialVault _vault = new();

    public SettingsWindow()
    {
        InitializeComponent();

        DragBar.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        };

        CloseButton.Click += (_, _) => { DialogResult = false; Close(); };
        SaveButton.Click += (_, _) => Save();

        var (key, secret) = _vault.GetCredentials();
        KeyBox.Text = key ?? "";
        SecretBox.Password = secret ?? "";
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        if (!WindowMaterial.Apply(this))
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x0E, 0x0F, 0x11));
        }
    }

    private void Save()
    {
        var key = KeyBox.Text.Trim();
        var secret = SecretBox.Password.Trim();

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
        {
            ErrorText.Text = "Both the API key and secret are required.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            _vault.SaveCredentials(key, secret);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
