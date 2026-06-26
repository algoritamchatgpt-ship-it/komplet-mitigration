using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class EmailListiciView : Window
{
    public EmailListiciView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is EmailListiciViewModel oldVm)
            oldVm.ZatvaranjeZahtevano -= Close;
        if (e.NewValue is EmailListiciViewModel newVm)
            newVm.ZatvaranjeZahtevano += Close;
    }
}
