using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views;

public partial class FirmaIzborWindow : Window
{
    private readonly FirmaIzborViewModel _vm;

    public event Action? FirmaIzabrana;
    public event Action? OtkacenoPrijavljeni;

    public FirmaIzborWindow(FirmaIzborViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.FirmaIzabrana += () => FirmaIzabrana?.Invoke();
        vm.OtkacenoPrijavljeni += () => OtkacenoPrijavljeni?.Invoke();
        vm.GradoviTrazeni += OtvoriGradove;
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await _vm.UcitajAsync();
        GridFirmi.Focus();
    }

    private void OtvoriGradove(string finRootFolder)
    {
        var vm = new GradoviViewModel(finRootFolder);
        var view = new Zarade.GradoviView
        {
            DataContext = vm,
            Owner = this
        };

        view.ShowDialog();
    }
}
