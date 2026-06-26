using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Algoritam.WPF.ViewModels;

public partial class LdPrenosiViewModel : ObservableObject
{
    private readonly PlatniSpisakViewModel _platniSpisak;

    [ObservableProperty] private string _naslov = "PRENOS PODATAKA";
    [ObservableProperty] private string _statusPoruka = "Izaberite vrstu prenosa.";

    public event Action? ZatvaranjeZahtevano;

    public LdPrenosiViewModel(PlatniSpisakViewModel platniSpisak)
    {
        _platniSpisak = platniSpisak;
    }

    // ── PRENOS ZARADE II ISPLATA ──────────────────────────────────────
    // Čita NETO iz LD fajla za isplatu=1 i upisuje u AKONTAC tekućih stavki.
    [RelayCommand]
    private void PrenosZaradeIIIsplata()
    {
        var res = MessageBox.Show(
            "Preneti NETO iz isplate 1 kao AKONTAC u tekući obračun?\n\n" +
            "Program čita LD fajl za isplatu 1 (LD.DBF) i vrednost NETO\n" +
            "upisuje u kolonu AKONTAC za svakog radnika.",
            "Prenos zarade II isplata",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (res != MessageBoxResult.Yes) return;
        StatusPoruka = _platniSpisak.IzvrsiPrenosZaradeIIIsplata();
    }

    // ── PRENOS AKONTACIJE ─────────────────────────────────────────────
    [RelayCommand]
    private void PrenosAkontacije()
    {
        StatusPoruka = _platniSpisak.IzvrsiPrenosKredita(zaAkontaciju: true);
    }

    // ── PRENOS KREDITA ────────────────────────────────────────────────
    [RelayCommand]
    private void PrenosKredita()
    {
        var vm = new LdPrenosKreditaViewModel(_platniSpisak);
        var view = new Views.Zarade.LdPrenosKreditaView { DataContext = vm };

        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null)
            view.Owner = aktivniProzor;

        if (view.ShowDialog() == true)
            StatusPoruka = vm.StatusPoruka;
    }

    // ── DOTACIJA DO MINIMALNE ZARADE ──────────────────────────────────
    // Za radnike čiji NETO < MINNAC upisuje razliku u kolonu DOTACIJA.
    [RelayCommand]
    private void Dotacija()
    {
        var res = MessageBox.Show(
            "Dotirati radnike do minimalne neto zarade (MINNAC iz Parametri 1)?\n\n" +
            "Za svakog radnika čiji neto < minimum, biće upisana\n" +
            "razlika u kolonu DOTACIJA.",
            "Dotacija do minimuma",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (res != MessageBoxResult.Yes) return;
        StatusPoruka = _platniSpisak.IzvrsiDotaciju();
    }

    // ── TOPLI OBROK I REGRES ──────────────────────────────────────────
    [RelayCommand]
    private void TopliObrokIRegres()
    {
        var vm = new LdTopliObrokViewModel();
        var view = new Views.Zarade.LdTopliObrokView { DataContext = vm };

        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null)
            view.Owner = aktivniProzor;

        view.ShowDialog();

        if (vm.Potvrdjeno)
            StatusPoruka = _platniSpisak.IzvrsiTopliObrokPrenosInternal(vm.TopliIznos, vm.RegresIznos);
    }

    // ── TROŠKOVI RESTORANA ────────────────────────────────────────────
    // Upisuje iznos troškova restorana (TERENSKI) svim radnicima.
    [RelayCommand]
    private void TroskoviRestorana()
    {
        var iznos = PitajIznos(
            "TROŠKOVI RESTORANA",
            "Unesite iznos troškova restorana po radniku:");

        if (iznos == null) return;
        StatusPoruka = _platniSpisak.IzvrsiTroskoviRestorana(iznos.Value);
    }

    // ── EXPORT U EXCEL ────────────────────────────────────────────────
    // Izvozi platni spisak u CSV i otvara u Excel-u.
    [RelayCommand]
    private void ExportUExcel()
    {
        StatusPoruka = _platniSpisak.ExportUExcelInternal();
    }

    // ── KOPIRANJE ZARADE ──────────────────────────────────────────────
    // Kopira časove i dodatke iz izabranog LD fajla u tekuće stavke.
    [RelayCommand]
    private void KopiranjeZarade()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Izaberite izvorni LD fajl za kopiranje",
            Filter = "DBF fajlovi (*.dbf)|*.dbf|Svi fajlovi (*.*)|*.*",
            DefaultExt = ".dbf"
        };

        if (dlg.ShowDialog() != true) return;

        var res = MessageBox.Show(
            $"Kopirati časove i dodatke iz:\n{dlg.FileName}\n\nu tekući platni spisak?\n\n" +
            "Biće kopirani: svi časovi, topli obrok, regres, terenski, fiksna, stimulacije.",
            "Kopiranje zarade",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (res != MessageBoxResult.Yes) return;
        StatusPoruka = _platniSpisak.IzvrsiKopiranjeZarade(dlg.FileName);
    }

    // ── PRENOS ZA REGISTAR ────────────────────────────────────────────
    // Izvozi platni spisak u CSV za budžetski registar zaposlenih.
    [RelayCommand]
    private void PrenosZaRegistar()
    {
        StatusPoruka = _platniSpisak.IzvrsiPrenosZaRegistar();
    }

    // ── IZLAZ ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void Izlaz()
    {
        ZatvaranjeZahtevano?.Invoke();
    }

    // ── HELPER: programatski dialog za unos iznosa ────────────────────
    private static decimal? PitajIznos(string naslov, string poruka)
    {
        var info = new TextBlock
        {
            Text = poruka,
            Margin = new Thickness(0, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 12
        };

        var polje = new TextBox
        {
            Width = 200,
            Margin = new Thickness(0, 0, 0, 12),
            Text = "0,00",
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 12
        };

        var ok = new Button
        {
            Content = "U REDU",
            Width = 90,
            Height = 30,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };
        var odustani = new Button
        {
            Content = "ODUSTANI",
            Width = 90,
            Height = 30,
            IsCancel = true
        };

        var dugmad = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        dugmad.Children.Add(ok);
        dugmad.Children.Add(odustani);

        var panel = new StackPanel { Margin = new Thickness(14), MinWidth = 360 };
        panel.Children.Add(info);
        panel.Children.Add(polje);
        panel.Children.Add(dugmad);

        decimal? rezultat = null;

        var dialog = new Window
        {
            Title = naslov,
            Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize,
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 12
        };

        ok.Click += (_, _) =>
        {
            var txt = polje.Text.Trim().Replace(',', '.');
            if (decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ||
                decimal.TryParse(polje.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out val))
            {
                rezultat = val;
                dialog.DialogResult = true;
            }
            else
            {
                polje.Focus();
                polje.SelectAll();
            }
        };

        var akt = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (akt != null) dialog.Owner = akt;

        dialog.ShowDialog();
        return rezultat;
    }
}
