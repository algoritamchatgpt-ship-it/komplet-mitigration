using OsnovnaSredstva.Services;
using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class PosaljiMailWindow : Window
{
    private readonly EmailPodesavanjaService _podesavanjaService;

    public PosaljiMailWindow(PosaljiMailViewModel vm, EmailPodesavanjaService podesavanjaService)
    {
        InitializeComponent();
        _podesavanjaService = podesavanjaService;
        DataContext = vm;
        vm.ZatvoriAction = Close;
        vm.OtvoriPodesavanjaAction = OtvoriPodesavanja;
    }

    private void OtvoriPodesavanja()
    {
        var vm = new EmailPodesavanjaViewModel(_podesavanjaService);
        new EmailPodesavanjaWindow(vm) { Owner = this }.ShowDialog();
        if (DataContext is PosaljiMailViewModel posaljiVm)
            posaljiVm.OsveziStatusPoruku();
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
