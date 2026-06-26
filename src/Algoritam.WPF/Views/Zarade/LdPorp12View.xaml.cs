using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdPorp12View : Window
{
    public LdPorp12View()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is LdPorp12ViewModel oldVm)
            oldVm.ZatvaranjeZatrazeno -= CloseNaZahtev;

        if (e.NewValue is LdPorp12ViewModel newVm)
            newVm.ZatvaranjeZatrazeno += CloseNaZahtev;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is LdPorp12ViewModel vm)
            vm.ZatvaranjeZatrazeno -= CloseNaZahtev;

        base.OnClosed(e);
    }

    private void CloseNaZahtev()
    {
        Dispatcher.Invoke(Close);
    }

    private void IzlazClick(object sender, RoutedEventArgs e) => Close();
}
