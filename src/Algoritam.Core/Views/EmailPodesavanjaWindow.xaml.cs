using Algoritam.Core.ViewModels;
using System.Windows;

namespace Algoritam.Core.Views;

public partial class EmailPodesavanjaWindow : Window
{
    public EmailPodesavanjaWindow(EmailPodesavanjaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        LozinkaBox.Password = vm.Lozinka;
        vm.ZatvoriAction = Close;
    }

    private void OnLozinkaChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is EmailPodesavanjaViewModel vm)
            vm.Lozinka = LozinkaBox.Password;
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
