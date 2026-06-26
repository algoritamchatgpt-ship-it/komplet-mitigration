using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdNz1View : Window
{
    public LdNz1View()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is LdNz1ViewModel oldVm)
            oldVm.ZatvaranjeZatrazeno -= CloseNaZahtev;

        if (e.NewValue is LdNz1ViewModel newVm)
            newVm.ZatvaranjeZatrazeno += CloseNaZahtev;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is LdNz1ViewModel vm)
            vm.ZatvaranjeZatrazeno -= CloseNaZahtev;

        base.OnClosed(e);
    }

    private void CloseNaZahtev()
    {
        Dispatcher.Invoke(Close);
    }

    private void IzlazClick(object sender, RoutedEventArgs e) => Close();
}
