using Algoritam.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Algoritam.WPF.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public event Action? PrijavaUspela;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.PrijavaUspela += () => PrijavaUspela?.Invoke();

        // Enter u korisnik polju → pređi na lozinku
        TxtKorisnik.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                TxtLozinka.Focus();
                e.Handled = true;
            }
        };

        // Enter u PasswordBox pokrće komandu
        TxtLozinka.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && _vm.PrijavaCommand.CanExecute(null))
                _vm.PrijavaCommand.Execute(null);
        };

        BtnIzlaz.Click += (_, _) => Close();

        Loaded += (_, _) => TxtKorisnik.Focus();
    }

    protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnPreviewGotKeyboardFocus(e);
        if (e.NewFocus is TextBox tb)
            tb.SelectAll();
    }

    private void TxtLozinka_PasswordChanged(object sender, RoutedEventArgs e)
        => _vm.Lozinka = ((PasswordBox)sender).Password;
}
