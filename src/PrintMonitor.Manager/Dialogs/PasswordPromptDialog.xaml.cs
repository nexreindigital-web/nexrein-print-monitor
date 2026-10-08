using System.Windows;
using System.Windows.Input;
using PrintMonitor.Manager.Utilities;

namespace PrintMonitor.Manager.Dialogs;

public partial class PasswordPromptDialog : Window
{
    private readonly string? _expectedHash;

    public bool IsAuthenticated { get; private set; } = false;

    public PasswordPromptDialog(string actionReason, string? expectedHash = null)
    {
        InitializeComponent();
        TxtActionReason.Text = actionReason;
        _expectedHash = expectedHash;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PbPassword.Focus();
    }

    private void BtnAuthenticate_Click(object sender, RoutedEventArgs e)
    {
        ValidateAndSubmit();
    }

    private void PbPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ValidateAndSubmit();
        }
    }

    private void ValidateAndSubmit()
    {
        var input = PbPassword.Password;
        if (SecurityManager.VerifyPassword(input, _expectedHash))
        {
            IsAuthenticated = true;
            DialogResult = true;
            Close();
        }
        else
        {
            TxtErrorMessage.Text = "Incorrect password. Access denied.";
            TxtErrorMessage.Visibility = Visibility.Visible;
            PbPassword.SelectAll();
            PbPassword.Focus();
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
