using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Algoritam.WPF.ViewModels;

public partial class PppIzborViewModel : ObservableObject
{
    private readonly string _folderPath;

    public PppIzborViewModel(string folderPath)
    {
        _folderPath = folderPath ?? string.Empty;
    }

    public Action? ZatvoriAction { get; set; }

    [RelayCommand]
    private void OtvoriParametreA()
    {
        var vm = new PppViewModel(_folderPath);
        var view = new Views.Zarade.PppView { DataContext = vm };
        view.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriPreuzimanjePppPd()
    {
        var vm = new PppPreuzimanjeViewModel(_folderPath);
        vm.OtvoriParametrePrenosaAction = () =>
        {
            var pvm = new PppPrenosParametriViewModel(_folderPath);
            var pvw = new Views.Zarade.PppPrenosParametriView { DataContext = pvm };
            pvw.ShowDialog();
        };

        var view = new Views.Zarade.PppPreuzimanjeView { DataContext = vm };
        view.ShowDialog();
    }

    [RelayCommand]
    private void Zatvori() => ZatvoriAction?.Invoke();
}

