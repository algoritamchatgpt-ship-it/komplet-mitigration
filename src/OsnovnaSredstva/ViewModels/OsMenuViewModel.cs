using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class OsMenuViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;

    public event Action? OdjavaSeTrazena;
    public event Action? VratiseFirmaIzboru;

    public OsMenuViewModel(AppState appState, IPutanjaService putanjaService)
    {
        _appState = appState;
        _putanjaService = putanjaService;
        _ = PokreniProvjeruAmortizacijeAsync();
    }

    public string NazivFirme => _appState.AktivnaFirma?.Naziv ?? "—";
    public string FolderFirme => _appState.AktivnaFirma?.FolderIme ?? "—";
    public string KorisnikIme => _appState.TrenutniKorisnik?.KorisnikIme ?? "—";
    public string Godina => _appState.AktivnaGodina.ToString();

    public string NaslovHeader =>
        $"{NazivFirme}   |   {FolderFirme}   |   {KorisnikIme}   |   {Godina}";

    [ObservableProperty] private string _backupPoruka = string.Empty;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NapraviBackupCommand))]
    private bool _backupRadi;

    [ObservableProperty] private string _upozerenjePoruka = string.Empty;
    private List<UpozorenjeAmortizacije> _upozoreneKartice = [];

    [RelayCommand]
    private void OtvoriPodatkeOFirmi()
    {
        var vm = new FirmaPodaciViewModel(_appState);
        var win = new FirmaPodaciWindow(vm);
        win.ShowDialog();
        OnPropertyChanged(nameof(NazivFirme));
        OnPropertyChanged(nameof(NaslovHeader));
    }

    [RelayCommand]
    private void Odjava()
    {
        _appState.Odjavi();
        OdjavaSeTrazena?.Invoke();
    }

    [RelayCommand]
    private void PromenaFirme()
    {
        _appState.PostaviFirmu(null!);
        VratiseFirmaIzboru?.Invoke();
    }

    [RelayCommand]
    private void PromenaGodine()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "Unesite poslovnu godinu:", "Promena godine",
            _appState.AktivnaGodina.ToString());

        if (int.TryParse(input, out var godina) && godina >= 2000 && godina <= 2100)
        {
            _appState.AktivnaGodina = godina;
            OnPropertyChanged(nameof(Godina));
            OnPropertyChanged(nameof(NaslovHeader));
        }
    }

    [RelayCommand]
    private void OtvoriDashboard()
    {
        var vm = new OsDashboardViewModel(_appState);
        new OsDashboardWindow(vm) { Owner = System.Windows.Application.Current.MainWindow }.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriKartice()
    {
        var vm = new OsKarticeViewModel(_appState);
        var win = new OsKarticeWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriEvidenciju()
    {
        var vm = new OsEvidencijaViewModel(_appState, "os.dbf");
        var win = new OsEvidencijaWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriArhivu()
    {
        var vm = new OsArhivaViewModel(_appState);
        var win = new OsArhivaWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriStampa()
    {
        var vm = new OsObrazacOaViewModel(_appState);
        var win = new OsObrazacOaWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriSifarNici()
    {
        var vm = new OsSifarnikViewModel(_appState, _putanjaService);
        var win = new OsSifarnikWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriLozinke()
    {
        var vm = new FormiranjeLozinkiViewModel(_putanjaService);
        var win = new FormiranjeLozinkiWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriMesta()
    {
        var vm = new GradoviViewModel(_appState, _putanjaService);
        var win = new GradoviWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriPartnere()
    {
        if (_appState.AktivnaFirma is null)
        {
            MessageBox.Show("Nema aktivne firme.", "Partneri", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var vm = new PartneriViewModel(_appState);
        var win = new PartneriWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriIzvoz()
    {
        var vm = new IzvozTabelaViewModel(_appState, _putanjaService);
        var win = new IzvozTabelaWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriPrenosu()
    {
        if (_appState.AktivnaFirma is null)
        {
            MessageBox.Show("Nema aktivne firme.", "Prenos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var vm = new OsPrenosaViewModel(_appState);
        var win = new OsPrenosaWindow(vm);
        win.ShowDialog();
        OnPropertyChanged(nameof(Godina));
        OnPropertyChanged(nameof(NaslovHeader));
    }

    [RelayCommand]
    private void OtvoriPreglede()
    {
        if (_appState.AktivnaFirma is null)
        {
            MessageBox.Show("Nema aktivne firme.", "Pregledi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var vm = new OsPreglediViewModel(_appState);
        var win = new OsPreglediWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriGrupeAmortizacije()
    {
        var vm = new OsGrupeAmortizacijeViewModel(_appState);
        var win = new OsGrupeAmortizacijeWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriPodatkeOs()
    {
        if (_appState.AktivnaFirma is null)
        {
            MessageBox.Show("Nema aktivne firme.", "Podaci OS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var vm = new OsPodaciViewModel(_appState);
        var win = new OsPodaciWindow(vm);
        win.ShowDialog();
    }

    // Legacy: xml_ost.scx (forma XMLOST), poziva se kao "DO FORM XML_OST" — pozivni
    // ekran (koje dugme/koji parametar PLICA) nije pronađen u dostupnom OSIZFINA izvoru,
    // pa se ovde otvara sa mplica=0 (osnovna varijanta, koja ima i stvarne ost01.xml/
    // ost01_2022.xml šablone u repozitorijumu).
    [RelayCommand]
    private void OtvoriXmlOst()
    {
        if (_appState.AktivnaFirma is null)
        {
            MessageBox.Show("Nema aktivne firme.", "XML obrazac OST", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var vm = new OsXmlOstViewModel(_appState);
        var win = new OsXmlOstWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand(CanExecute = nameof(MozeBackup))]
    private async Task NapraviBackupAsync()
    {
        var firma = _appState.AktivnaFirma;
        if (firma is null)
        {
            BackupPoruka = "Nema aktivne firme.";
            return;
        }

        var arhivFolder = _putanjaService.DajArhivaPutanju();
        if (string.IsNullOrWhiteSpace(arhivFolder))
        {
            BackupPoruka = "Arhiv folder nije podešen — idite u Početni ekran i postavite ga.";
            return;
        }

        BackupRadi = true;
        BackupPoruka = "Backup u toku...";
        try
        {
            var rez = await BackupService.NapraviBackupAsync(
                firma.FolderPath, arhivFolder, firma.FolderIme);
            BackupPoruka =
                $"Backup završen: {rez.BrojFajlova} fajlova, " +
                $"{BackupService.FormatVelicine(rez.UkupnoBytes)}  →  {rez.OdredisteFolder}";
        }
        catch (Exception ex)
        {
            BackupPoruka = $"Greška pri backupu: {ex.Message}";
        }
        finally
        {
            BackupRadi = false;
        }
    }

    private bool MozeBackup() => !BackupRadi;

    private async Task PokreniProvjeruAmortizacijeAsync()
    {
        var path = DbfHelper.NadjiDbf(_appState, "os.dbf");
        if (path is null) return;

        try
        {
            var nadjeni = await Task.Run(() =>
            {
                var lista = new List<UpozorenjeAmortizacije>();
                var reader = new SimpleDbfReader(path);
                foreach (var r in reader.Zapisi())
                {
                    var nab = r.DajDecimal("NAB0");
                    var isp = r.DajDecimal("ISP0");
                    var sad = r.DajDecimal("SAD0");
                    if (nab > 0 && isp > nab)
                    {
                        lista.Add(new UpozorenjeAmortizacije(
                            r.DajString("OSIFRA").Trim(),
                            r.DajString("NAZ").Trim(),
                            nab, isp, sad));
                    }
                }
                return lista;
            });

            if (nadjeni.Count > 0)
            {
                _upozoreneKartice = nadjeni;
                UpozerenjePoruka  = $"Upozorenje: {nadjeni.Count} kartica ima amortizaciju veću od nabavne vrijednosti.";
            }
        }
        catch { /* provjera ne smije rušiti meni */ }
    }

    [RelayCommand]
    private void PrikaziUpozorenja()
    {
        if (_upozoreneKartice.Count == 0) return;
        new UpozorenjaAmortizacijeWindow(_upozoreneKartice).ShowDialog();
    }

}

public record UpozorenjeAmortizacije(
    string  Osifra,
    string  Naziv,
    decimal Nab0,
    decimal Isp0,
    decimal Sad0)
{
    public decimal PostoAmortizacije => Nab0 > 0 ? Math.Round(Isp0 / Nab0 * 100, 1) : 0m;
}
