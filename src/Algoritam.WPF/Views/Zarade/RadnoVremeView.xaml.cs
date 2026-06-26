using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class RadnoVremeView : Window
{
    private RadnoVremeViewModel Vm => (RadnoVremeViewModel)DataContext;

    public RadnoVremeView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();

    // DODAJ SVE RADNIKE → DO FORM LDRADVREDOD
    private void DodajSveRadnikeClick(object sender, RoutedEventArgs e)
    {
        var dlg = new LdRadvreDodajView { Owner = this };
        if (dlg.ShowDialog() == true)
            Vm.DodajSveRadnike(dlg.IzabraniDatum, dlg.IzabraniPocSat);
    }

    // PREGLED → DO FORM LDRADVREPREG
    private void PregledClick(object sender, RoutedEventArgs e)
    {
        var dlg = new LdRadvrePregledView(Vm.Stavke) { Owner = this };
        dlg.ShowDialog();
    }

    // RADNICI → DO FORM LDRADVRER
    private void RadniciClick(object sender, RoutedEventArgs e)
    {
        var view = new LdRadvrerView(Vm.FolderPath) { Owner = this };
        view.ShowDialog();
    }
}
