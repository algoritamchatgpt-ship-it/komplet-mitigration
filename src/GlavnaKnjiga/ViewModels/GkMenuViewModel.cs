using Algoritam.Core.Services;
using Algoritam.Core.Services.Dbf;
using Algoritam.Core.ViewModels;
using Algoritam.Core.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Views;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class GkMenuViewModel : ObservableObject
{
    private readonly AppState _appState;

    public event Action? OdjavaSeTrazena;
    public event Action? VratiseFirmaIzboru;

    public GkMenuViewModel(AppState appState)
    {
        _appState = appState;
    }

    public string NazivFirme  => _appState.AktivnaFirma?.Naziv     ?? "—";
    public string FolderFirme => _appState.AktivnaFirma?.FolderIme ?? "—";
    public string KorisnikIme => _appState.TrenutniKorisnik?.KorisnikIme ?? "—";
    public string Godina      => _appState.AktivnaGodina.ToString();

    public string NaslovHeader =>
        $"{NazivFirme}   |   {FolderFirme}   |   {KorisnikIme}   |   {Godina}";

    [RelayCommand]
    private void OtvoriPodatkeOFirmi()
    {
        var vm  = new FirmaPodaciViewModel(_appState);
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

    /// <summary>NALANPAR — Parametri knjiženja analitike</summary>
    [RelayCommand]
    private void OtvoriNalanpar()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new NalanparViewModel(firma.FolderPath);
        var win = new Views.NalanparWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALBROJ — Evidencija naloga</summary>
    [RelayCommand]
    private void OtvoriNalbroj()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new NalbrojViewModel(firma.FolderPath);
        var win = new Views.NalbrojWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALVRSTA — Vrste naloga za knjiženje</summary>
    [RelayCommand]
    private void OtvoriNalvrsta()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var dbfPath = System.IO.Path.Combine(firma.FolderPath, "nalvrsta.dbf");
        var vm  = new NalvrstaViewModel(dbfPath);
        var win = new Views.NalvrstaWindow(vm);
        win.ShowDialog();
    }

    /// <summary>IZRADA NALOGA ZA KNJIZENJE (NALP2)</summary>
    [RelayCommand]
    private void OtvoriNalp2()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }
        var firmPath = firma.FolderPath;

        var brnal = IzaberiNalog(firmPath);
        if (brnal == null) return;

        var vm  = new Nalp2ViewModel(_appState, firmPath, brnal);
        var win = new Nalp2Window(vm);
        win.ShowDialog();
    }

    /// <summary>NALPDEFK — Definicija konta analitike</summary>
    [RelayCommand]
    private void OtvoriNalpDefk()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new NalpDefkViewModel(
            firma.FolderPath,
            _appState.TrenutniKorisnik?.KorisnikIme ?? string.Empty,
            firma.Naziv ?? string.Empty);
        var win = new Views.NalpDefkWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NAL — knjiženi dnevnik glavne knjige</summary>
    [RelayCommand]
    private void OtvoriNal()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm = new NalViewModel(firma.FolderPath, _appState.AktivnaGodina);
        var win = new Views.NalWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALDNEV — Dnevnik GK (filter za štampu)</summary>
    [RelayCommand]
    private void OtvoriNaldnev()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm = new NaldnevViewModel(firma.FolderPath, _appState.AktivnaGodina);
        var win = new Views.NaldnevWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALKARTICE — Štampa kartica GK (filter)</summary>
    [RelayCommand]
    private void OtvoriNalkartice()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm = new NalkarticeViewModel(firma.FolderPath, _appState.AktivnaGodina);
        var win = new Views.NalkarticeWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALMTR — Evidencija mesta troškova</summary>
    [RelayCommand]
    private void OtvoriNalmtr()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new NalmtrViewModel(firma.FolderPath, _appState.AktivnaGodina);
        var win = new Views.NalmtrWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALPN — Nedovršeni nalozi (pregled nalpn.dbf)</summary>
    [RelayCommand]
    private void OtvoriNalpN()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new NalpNViewModel(firma.FolderPath);
        var win = new Views.NalpNWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALRASP — Raspodela klase 9</summary>
    [RelayCommand]
    private void OtvoriNalrasp()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new NalraspViewModel(firma.FolderPath);
        var win = new Views.NalraspWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALRAZNO — servisne operacije glavne knjige</summary>
    [RelayCommand]
    private void OtvoriNalrazno()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm = new NalraznoViewModel(firma.FolderPath, _appState.AktivnaGodina);
        var win = new Views.NalraznoWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALGK10 — Izrada naloga za knjiženje klase 9 (10-cifara konta)</summary>
    [RelayCommand]
    private void OtvoriNalgk10()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new Nalgk10ViewModel(firma.FolderPath);
        var win = new Views.Nalgk10Window(vm);
        win.ShowDialog();
    }

    /// <summary>NALKOPI — Kopiranje naloga za knjiženje</summary>
    [RelayCommand]
    private void OtvoriNalkop()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var brnal = IzaberiNalog(firma.FolderPath);
        if (brnal == null) return;

        var win = new Views.NalkopWindow(firma.FolderPath, brnal);
        win.ShowDialog();
    }

    /// <summary>NALIZV — GK Izveštaji</summary>
    [RelayCommand]
    private void OtvoriNaliziv()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new NalizivViewModel(firma.FolderPath, _appState.AktivnaGodina);
        var win = new Views.NalizivWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALUNIOR0 — Prijem računa iz STRIP-a</summary>
    [RelayCommand]
    private void OtvoriNaluniorPrijem()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new NaluniorPrijemViewModel(firma.FolderPath);
        var win = new Views.NaluniorPrijemWindow(vm);
        if (win.ShowDialog() == true || true)
        {
            var vm2  = new NaluniorViewModel(firma.FolderPath);
            var win2 = new Views.NaluniorWindow(vm2);
            win2.ShowDialog();
        }
    }

    /// <summary>NALPRKON — Pregled po kontima</summary>
    [RelayCommand]
    private void OtvoriNalprkon()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new NalprkonViewModel(
            firma.FolderPath,
            _appState.TrenutniKorisnik?.KorisnikIme ?? string.Empty,
            firma.Naziv ?? string.Empty);
        var win = new Views.NalprkonWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALPRNAL — Pregled po nalozima</summary>
    [RelayCommand]
    private void OtvoriNalprNal()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new NalprNalViewModel(
            firma.FolderPath,
            _appState.TrenutniKorisnik?.KorisnikIme ?? string.Empty,
            firma.Naziv ?? string.Empty);
        var win = new Views.NalprNalWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALZAMENAKONTA0 — Zamena konta</summary>
    [RelayCommand]
    private void OtvoriNalzamenakonta()
    {
        var firma = _appState.AktivnaFirma;
        if (firma == null) { MessageBox.Show("Nije izabrana firma."); return; }

        var vm  = new NalzamenakontaViewModel(firma.FolderPath);
        var win = new Views.NalzamenakontaWindow(vm);
        win.ShowDialog();
    }

    /// <summary>NALZAKLJ — Osnovni zaključni list</summary>
    [RelayCommand]
    private void OtvoriNalzaklj()
    {
        var win = new Views.NalzakljWindow();
        win.ShowDialog();
    }

    /// <summary>NALZAKLJ10 — Osnovni zaključni list (nalp.dbf)</summary>
    [RelayCommand]
    private void OtvoriNalzaklj10()
    {
        var win = new Views.Nalzaklj10Window();
        win.ShowDialog();
    }

    /// <summary>NALZAKLJB — Brzi zaključni list</summary>
    [RelayCommand]
    private void OtvoriNalzakljb()
    {
        var win = new Views.NalzakljbWindow();
        win.ShowDialog();
    }

    private static string? IzaberiNalog(string firmPath)
    {
        var nbPath = Path.Combine(firmPath, "nalbroj.dbf");
        var nalozi = new List<string>();

        if (File.Exists(nbPath))
        {
            try
            {
                var r = new SimpleDbfReader(nbPath);
                foreach (var rec in r.Zapisi())
                {
                    var brnal  = rec.DajString("BRNAL").Trim();
                    var vrnal  = rec.DajString("VRNAL").Trim();
                    var datnal = rec.DajDate("DATNAL");
                    if (!string.IsNullOrEmpty(brnal))
                        nalozi.Add($"{brnal}  {vrnal}  {datnal:dd.MM.yyyy}");
                }
            }
            catch { }
        }

        var dlg = new NalogIzborWindow(nalozi);
        if (dlg.ShowDialog() != true) return null;
        return dlg.IzabraniKod;
    }
}
