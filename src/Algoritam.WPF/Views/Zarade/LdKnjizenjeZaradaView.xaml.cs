using Algoritam.WPF.ViewModels;
using System;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdKnjizenjeZaradaView : Window
{
    private bool _autoSaveInProgress;

    public LdKnjizenjeZaradaView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void KnjizenjeGridCurrentCellChanged(object sender, EventArgs e)
    {
        if (_autoSaveInProgress)
            return;

        if (DataContext is not LdKnjizenjeZaradaViewModel vm)
            return;

        if (vm.Ucitava || vm.Stavke.Count == 0)
            return;

        try
        {
            _autoSaveInProgress = true;
            await vm.SacuvajBezPorukeAsync();
        }
        finally
        {
            _autoSaveInProgress = false;
        }
    }
}
