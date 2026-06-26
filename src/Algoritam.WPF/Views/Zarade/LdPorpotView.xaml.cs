using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdPorpotView : Window
{
    public LdPorpotView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is LdPorpotViewModel oldVm)
            oldVm.ZatvaranjeZatrazeno -= CloseNaZahtev;

        if (e.NewValue is LdPorpotViewModel newVm)
            newVm.ZatvaranjeZatrazeno += CloseNaZahtev;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is LdPorpotViewModel vm)
            vm.ZatvaranjeZatrazeno -= CloseNaZahtev;

        base.OnClosed(e);
    }

    private void CloseNaZahtev()
    {
        Dispatcher.Invoke(Close);
    }

    private void IzlazClick(object sender, RoutedEventArgs e) => Close();
}
