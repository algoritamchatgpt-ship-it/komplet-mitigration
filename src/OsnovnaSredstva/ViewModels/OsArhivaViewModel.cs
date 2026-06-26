using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;

namespace OsnovnaSredstva.ViewModels;

public partial class OsArhivaViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<OsArhivaViewModel>();
    private readonly AppState _appState;
    private readonly string _dbfIme;

    private List<OsKartica> _sveKartice = [];

    [ObservableProperty] private ObservableCollection<OsKartica> _kartice = [];
    [ObservableProperty] private OsKartica? _izabranaKartica;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _filterText = "";

    private bool _izmijenjeno;
    public bool ImaNeSnimljenih => _izmijenjeno;

    public string NazivIzabrane =>
        IzabranaKartica == null ? "" : $"{IzabranaKartica.Osifra}   {IzabranaKartica.Naz}";

    public string InfoIzabrane =>
        IzabranaKartica == null ? "" :
        $"Konto: {IzabranaKartica.Konto}   Mesto: {IzabranaKartica.Mesto}   InvBroj: {IzabranaKartica.InvBroj}   AG: {IzabranaKartica.Ag}   AgPod: {IzabranaKartica.AgPod}";

    partial void OnIzabranaKarticaChanged(OsKartica? value)
    {
        OnPropertyChanged(nameof(NazivIzabrane));
        OnPropertyChanged(nameof(InfoIzabrane));
    }

    partial void OnFilterTextChanged(string value) => PrimeniFiIlter();

    private static readonly HashSet<string> PoznataPolja = new(StringComparer.OrdinalIgnoreCase)
    {
        "OSIFRA","NAZ","DATNAB","BRNAL","KONTO","VRSTA",
        "AG","AGPOD","INVBROJ","MESTO","NAB0","ISP0","SAD0",
        "KOM","CENA","STOPAOT","OSNOVKOR","IZVOR","PRENETO","IDBR"
    };

    public OsArhivaViewModel(AppState appState, string dbfIme = "osa.dbf")
    {
        _appState = appState;
        _dbfIme = dbfIme;
        Ucitaj();
    }

    private void PrimeniFiIlter()
    {
        var f = FilterText?.Trim() ?? "";
        Kartice = string.IsNullOrEmpty(f)
            ? new ObservableCollection<OsKartica>(_sveKartice)
            : new ObservableCollection<OsKartica>(
                _sveKartice.Where(k =>
                    (k.Osifra ?? "").ToLowerInvariant().Contains(f.ToLowerInvariant()) ||
                    (k.Naz    ?? "").ToLowerInvariant().Contains(f.ToLowerInvariant())));
    }

    private void Ucitaj()
    {
        var path = DbfPutanja(_dbfIme);
        if (path == null) { Kartice = []; Poruka = $"{_dbfIme} nije pronađen u folderu firme."; return; }

        try
        {
            var reader = new SimpleDbfReader(path);
            var stavke = new List<OsKartica>();

            foreach (var r in reader.Zapisi())
            {
                var k = new OsKartica
                {
                    Osifra   = r.DajString("OSIFRA"),
                    Naz      = r.DajString("NAZ"),
                    DatNab   = r.DajDate("DATNAB"),
                    BrNal    = r.DajString("BRNAL"),
                    Konto    = r.DajString("KONTO"),
                    Vrsta    = r.DajString("VRSTA"),
                    Ag       = r.DajString("AG"),
                    AgPod    = r.DajString("AGPOD"),
                    InvBroj  = r.DajString("INVBROJ"),
                    Mesto    = r.DajString("MESTO"),
                    Nab0     = r.DajDecimal("NAB0"),
                    Isp0     = r.DajDecimal("ISP0"),
                    Sad0     = r.DajDecimal("SAD0"),
                    Kom      = r.DajDecimal("KOM"),
                    Cena     = r.DajDecimal("CENA"),
                    StopaOt  = r.DajDecimal("STOPAOT"),
                    OsnovKor = r.DajString("OSNOVKOR"),
                    Izvor    = r.DajString("IZVOR"),
                    Preneto  = r.DajString("PRENETO"),
                    IDBr     = (int)r.DajDecimal("IDBR"),
                };

                foreach (var field in reader.Fields)
                {
                    if (!PoznataPolja.Contains(field.Name))
                    {
                        k.ExtraPolja[field.Name] = field.Type switch
                        {
                            'D'        => (object?)r.DajDate(field.Name),
                            'N' or 'F' => r.DajDecimal(field.Name),
                            'L'        => r.DajBool(field.Name),
                            _          => r.DajString(field.Name)
                        };
                    }
                }

                stavke.Add(k);
            }

            _sveKartice = stavke;
            PrimeniFiIlter();
            Poruka = $"Učitano {_sveKartice.Count} zapisa iz {_dbfIme}.";
            _log.Debug("OsArhiva — učitano {Count} zapisa iz {File}", _sveKartice.Count, path);
            _izmijenjeno = false;
        }
        catch (Exception ex)
        {
            _sveKartice = [];
            Kartice = [];
            Poruka = $"Greška: {ex.Message}";
            _log.Error(ex, "OsArhiva — greška pri učitavanju {File}", _dbfIme);
        }
    }

    [RelayCommand] private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Dodaj()
    {
        var max = _sveKartice.Select(k => k.IDBr).DefaultIfEmpty(0).Max();
        var nova = new OsKartica { IDBr = max + 1, Preneto = "N" };
        _sveKartice.Add(nova);
        PrimeniFiIlter();
        IzabranaKartica = nova;
        Poruka = "Novi red dodan. Unesite podatke i kliknite Sačuvaj.";
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = DbfPutanja(_dbfIme);
        if (path == null) { Poruka = $"{_dbfIme} nije pronađen."; return; }

        var duplikati = _sveKartice
            .Where(k => !string.IsNullOrWhiteSpace(k.Osifra))
            .GroupBy(k => k.Osifra!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplikati.Count > 0)
        {
            var lista = string.Join(", ", duplikati.Take(5));
            if (duplikati.Count > 5) lista += $" ... (+{duplikati.Count - 5})";
            System.Windows.MessageBox.Show(
                $"Duplikati šifara — snimanje onemogućeno:\n{lista}\n\nIspravite šifre prije snimanja.",
                "Duplikati", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            Poruka = $"Duplikati šifara: {lista}";
            return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, _sveKartice,
                (k, f) => f.ToUpperInvariant() switch
                {
                    "OSIFRA"   => (object?)k.Osifra,
                    "NAZ"      => k.Naz,
                    "DATNAB"   => k.DatNab,
                    "BRNAL"    => k.BrNal,
                    "KONTO"    => k.Konto,
                    "VRSTA"    => k.Vrsta,
                    "AG"       => k.Ag,
                    "AGPOD"    => k.AgPod,
                    "INVBROJ"  => k.InvBroj,
                    "MESTO"    => k.Mesto,
                    "NAB0"     => k.Nab0,
                    "ISP0"     => k.Isp0,
                    "SAD0"     => k.Sad0,
                    "KOM"      => k.Kom,
                    "CENA"     => k.Cena,
                    "STOPAOT"  => k.StopaOt,
                    "OSNOVKOR" => k.OsnovKor,
                    "IZVOR"    => k.Izvor,
                    "PRENETO"  => k.Preneto,
                    "IDBR"     => (object?)k.IDBr,
                    _          => k.ExtraPolja.TryGetValue(f, out var v) ? v : null
                });
            Poruka = $"Sačuvano ({_sveKartice.Count} zapisa).";
            _log.Information("OsArhiva — sačuvano {Count} zapisa u {File}", _sveKartice.Count, path);
            _izmijenjeno = false;
        }
        catch (Exception ex)
        {
            Poruka = $"Greška: {ex.Message}";
            _log.Error(ex, "OsArhiva — greška pri snimanju {File}", _dbfIme);
        }
    }

    [RelayCommand]
    private void BrisanjePraznina()
    {
        var count = _sveKartice.RemoveAll(k => string.IsNullOrWhiteSpace(k.Osifra));
        PrimeniFiIlter();
        Poruka = count > 0
            ? $"Obrisano {count} praznih kartica. Kliknite Sačuvaj."
            : "Nema praznih kartica.";
        if (count > 0) _izmijenjeno = true;
    }

    [RelayCommand]
    private void Obrisi()
    {
        if (IzabranaKartica == null) { Poruka = "Nije izabran red za brisanje."; return; }
        var naziv = $"{IzabranaKartica.Osifra?.Trim()} — {IzabranaKartica.Naz?.Trim()}";
        if (System.Windows.MessageBox.Show($"Obrisati karticu iz arhive:\n{naziv}?",
                "Brisanje kartice", System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes) return;
        _sveKartice.Remove(IzabranaKartica);
        IzabranaKartica = null;
        PrimeniFiIlter();
        Poruka = "Kartica obrisana iz arhive. Kliknite Sačuvaj.";
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void Sort()
    {
        _sveKartice = [.. _sveKartice.OrderBy(k => k.Osifra)];
        PrimeniFiIlter();
        Poruka = "Sortirano po sifri.";
    }

    [RelayCommand] private void Prvi()   { if (Kartice.Count > 0) IzabranaKartica = Kartice[0]; }
    [RelayCommand] private void Zadnji() { if (Kartice.Count > 0) IzabranaKartica = Kartice[^1]; }
    [RelayCommand] private void Dole()
    {
        if (IzabranaKartica == null || Kartice.Count == 0) return;
        var idx = Kartice.IndexOf(IzabranaKartica);
        if (idx < Kartice.Count - 1) IzabranaKartica = Kartice[idx + 1];
    }
    [RelayCommand] private void Gore()
    {
        if (IzabranaKartica == null || Kartice.Count == 0) return;
        var idx = Kartice.IndexOf(IzabranaKartica);
        if (idx > 0) IzabranaKartica = Kartice[idx - 1];
    }

    [RelayCommand]
    private void TrazenjeSifre()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox("Unesite šifru OS:", "Traženje po šifri", "");
        if (string.IsNullOrWhiteSpace(input)) return;
        var trag = input.Trim();
        var nadjeno = Kartice.FirstOrDefault(k =>
            (k.Osifra ?? "").Trim().Equals(trag, StringComparison.OrdinalIgnoreCase));
        if (nadjeno != null) { IzabranaKartica = nadjeno; Poruka = $"Pronađena: {nadjeno.Osifra?.Trim()} — {nadjeno.Naz}"; }
        else Poruka = $"Šifra '{trag}' nije pronađena.";
    }

    [RelayCommand]
    private void TrazenjeInventarnogBroja()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox("Unesite inventarski broj:", "Traženje po inv. broju", "");
        if (string.IsNullOrWhiteSpace(input)) return;
        var trag = input.Trim();
        var nadjeno = Kartice.FirstOrDefault(k =>
            (k.InvBroj ?? "").Trim().Equals(trag, StringComparison.OrdinalIgnoreCase));
        if (nadjeno != null) { IzabranaKartica = nadjeno; Poruka = $"Pronađena: {nadjeno.Osifra?.Trim()} — {nadjeno.Naz} (InvBroj: {nadjeno.InvBroj?.Trim()})"; }
        else Poruka = $"Inventarski broj '{trag}' nije pronađen.";
    }

    [RelayCommand] private void PregledKartica()
    {
        var kriterijumi = OsPopisFilterSupport.Ucitaj(_appState);
        var wnd = new OsPopisFilterWindow(OsPopisFilterMode.Kartica, kriterijumi);
        if (wnd.ShowDialog() != true)
            return;

        kriterijumi = wnd.ReadData();
        OsPopisFilterSupport.Sacuvaj(_appState, kriterijumi);
        var filtrirano = OsPopisFilterSupport.Primeni(_sveKartice, kriterijumi);

        var vm = OsMrsViewModel.KarticePregled(filtrirano);
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void ZadnjeUPocetno()
    {
        if (System.Windows.MessageBox.Show(
                $"Prenos tekućih u početne vrijednosti za SVE kartice ({_sveKartice.Count})?\n\n" +
                "Ovo ažurira NAB0, ISP0, SAD0 i resetuje period (NAB, ISP, AMORT = 0).",
                "Zadnje u početno",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        foreach (var k in _sveKartice)
        {
            var nav      = XDec(k, "NAB");
            var isp      = XDec(k, "ISP");
            var noviNab0 = nav > 0 ? nav : k.Nab0;
            var noviIsp0 = isp > 0 ? isp : k.Isp0;
            k.Nab0 = noviNab0;
            k.Isp0 = noviIsp0;
            k.Sad0 = noviNab0 - noviIsp0;
            k.ExtraPolja["NAB"]   = 0m;
            k.ExtraPolja["ISP"]   = 0m;
            k.ExtraPolja["SAD"]   = noviNab0 - noviIsp0;
            k.ExtraPolja["AMORT"] = 0m;

            var nab02 = XDec(k, "NAB02");
            var nab2  = XDec(k, "NAB2");
            var isp2  = XDec(k, "ISP2");
            var sad2  = XDec(k, "SAD2");
            k.ExtraPolja["ISP02"]  = isp2;
            k.ExtraPolja["SAD02"]  = sad2;
            k.ExtraPolja["NAB02"]  = nab2 > 0 ? nab2 : nab02;
            k.ExtraPolja["NAB2"]   = 0m;
            k.ExtraPolja["ISP2"]   = isp2;
            k.ExtraPolja["SAD2"]   = sad2;
            k.ExtraPolja["AMORT2"] = 0m;
            k.ExtraPolja["PAM"]    = 0m;
            k.ExtraPolja["RAM"]    = 0m;
        }
        Poruka = $"Zadnje preneseno u početne za {_sveKartice.Count} kartica. Kliknite Sačuvaj.";
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void SaldoKonta()
    {
        var izbor = new OsSaldoKontaIzborWindow();
        if (izbor.ShowDialog() != true) return;

        var (periodOd, _) = ProcitajPeriod();

        var vm = izbor.Action switch
        {
            OsSaldoKontaIzborAction.SaldoSintetika => OsSaldoViewModel.PoKontuSintetika(_sveKartice),
            OsSaldoKontaIzborAction.SaldoAnalitika => OsSaldoViewModel.PoKontu(_sveKartice),
            OsSaldoKontaIzborAction.SaldoNabavkePoAg => OsSaldoViewModel.SaldoNabavkePoAgrupama(_sveKartice, periodOd),
            OsSaldoKontaIzborAction.PocetnoStanje => OsSaldoViewModel.KarticaPoKontu(_sveKartice, periodOd),
            _ => null
        };

        if (vm == null) return;
        new OsSaldoWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void SaldoMesta()
    {
        var vm = OsSaldoViewModel.PoMestu(_sveKartice);
        new OsSaldoWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PregledMrs()
    {
        var vm = OsMrsViewModel.MrsPregled(_sveKartice);
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PreglPoreskaStara()
    {
        var vm = OsMrsViewModel.PoreskaStara(_sveKartice);
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PreglPoreskaNova()
    {
        var vm = OsMrsViewModel.PoreskaNova(_sveKartice);
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void Prenos()
    {
        var vm = new OsPrenosaViewModel(_appState);
        new OsPrenosaWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void Podaci()
    {
        // UNOS PODATAKA čita osa.dbf s diska — nesačuvane kartice neće biti ažurirane.
        if (_izmijenjeno)
        {
            var odg = System.Windows.MessageBox.Show(
                $"Arhiva ima {_sveKartice.Count} nesačuvanih kartica.\n\n" +
                "UNOS PODATAKA može ažurirati samo kartice snimljene u osa.dbf.\n\n" +
                "Sačuvati sada (preporučeno)?",
                "Nesačuvane izmjene",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);

            if (odg == System.Windows.MessageBoxResult.Cancel)
                return;

            if (odg == System.Windows.MessageBoxResult.Yes)
            {
                Sacuvaj();
                if (_izmijenjeno)
                {
                    Poruka = "PODACI otkazan — ispravite greške i sačuvajte arhivu.";
                    return;
                }
            }
        }

        var vm = new OsPodaciViewModel(_appState);
        var win = new OsPodaciWindow(vm);
        win.ShowDialog();
        Poruka = string.IsNullOrWhiteSpace(vm.Poruka) ? "" : $"Podaci: {vm.Poruka}";
    }

    [RelayCommand]
    private void Obracun()
    {
        if (_sveKartice.Count == 0) { Poruka = "Nema kartica za obračun."; return; }

        if (System.Windows.MessageBox.Show(
                $"Izračunati amortizaciju za sve kartice ({_sveKartice.Count})?\n\n" +
                "Ažurira AMORT, ISP, SAD (MRS) i AMORT2, ISP2, SAD2 (PP).",
                "Obračun amortizacije",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        int obradjeno = 0;
        foreach (var k in _sveKartice)
        {
            var nacinob = k.ExtraPolja.TryGetValue("NACINOB", out var n) ? n?.ToString()?.Trim() : null;
            if (!string.IsNullOrWhiteSpace(nacinob)) continue;

            if (k.StopaOt > 0 && k.Sad0 > 0)
            {
                var amort = Math.Round(k.Sad0 * k.StopaOt / 100m, 2);
                if (amort > k.Sad0) amort = k.Sad0;
                k.ExtraPolja["AMORT"] = amort;
                k.ExtraPolja["NAB"]   = k.Nab0;
                k.ExtraPolja["ISP"]   = k.Isp0 + amort;
                k.ExtraPolja["SAD"]   = k.Nab0 - (k.Isp0 + amort);
            }

            var stopaot2 = XDec(k, "STOPAOT2");
            var nab02    = XDec(k, "NAB02");
            var isp02    = XDec(k, "ISP02");
            var sad02    = XDec(k, "SAD02");
            if (stopaot2 > 0 && sad02 > 0)
            {
                var amort2 = Math.Round(sad02 * stopaot2 / 100m, 2);
                if (amort2 > sad02) amort2 = sad02;
                k.ExtraPolja["AMORT2"] = amort2;
                k.ExtraPolja["NAB2"]   = nab02;
                k.ExtraPolja["ISP2"]   = isp02 + amort2;
                k.ExtraPolja["SAD2"]   = nab02 - (isp02 + amort2);
            }
            obradjeno++;
        }
        Poruka = $"Obračun završen — {obradjeno} kartica obrađeno. Kliknite Sačuvaj.";
        if (obradjeno > 0) _izmijenjeno = true;
    }

    [RelayCommand]
    private void PoaObrazac()
    {
        var (_, periodDo) = ProcitajPeriod();
        var vm = new OsPoaIzvestajViewModel(_sveKartice, OsPoaIzvestajViewModel.TipIzvestaja.PoaObrazac, periodDo);
        new OsPoaIzvestajWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void EvidencijaPoa()
    {
        var vm = new OsPoaIzvestajViewModel(_sveKartice, OsPoaIzvestajViewModel.TipIzvestaja.EvidencijaPoa, null);
        new OsPoaIzvestajWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (_sveKartice.Count == 0) { Poruka = "Nema podataka za izvoz."; return; }
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title    = "Izvoz u CSV",
            Filter   = "CSV (*.csv)|*.csv|Svi fajlovi (*.*)|*.*",
            FileName = $"{_dbfIme.Replace(".dbf", "")}_{DateTime.Today:yyyyMMdd}.csv"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            using var sw = new StreamWriter(dlg.FileName, false, new System.Text.UTF8Encoding(true));
            sw.WriteLine("Sifra;Naziv;DatNab;BrNal;Konto;Vrsta;AG;AgPod;InvBroj;Mesto;Nab0;Isp0;Sad0;Kom;Cena;StopaOt;OsnovKor;Izvor;Preneto");
            foreach (var k in _sveKartice)
                sw.WriteLine(string.Join(";",
                    k.Osifra, k.Naz, k.DatNab?.ToString("dd.MM.yyyy") ?? "",
                    k.BrNal, k.Konto, k.Vrsta, k.Ag, k.AgPod, k.InvBroj, k.Mesto,
                    k.Nab0.ToString("N2"), k.Isp0.ToString("N2"), k.Sad0.ToString("N2"),
                    k.Kom.ToString("N2"), k.Cena.ToString("N2"), k.StopaOt.ToString("N3"),
                    k.OsnovKor, k.Izvor, k.Preneto));
            Poruka = $"CSV izvoz završen: {Path.GetFileName(dlg.FileName)} ({_sveKartice.Count} zapisa).";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { Poruka = $"Greška pri izvozu: {ex.Message}"; }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (_sveKartice.Count == 0) { Poruka = "Nema podataka za izvoz."; return; }
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title    = "Izvoz u Excel",
            Filter   = "Excel (*.xlsx)|*.xlsx|Svi fajlovi (*.*)|*.*",
            FileName = $"{_dbfIme.Replace(".dbf", "")}_{DateTime.Today:yyyyMMdd}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            ExcelExportHelper.SnimiKartice(dlg.FileName, _sveKartice, "OS Arhiva");
            Poruka = $"Excel izvoz završen: {Path.GetFileName(dlg.FileName)} ({_sveKartice.Count} zapisa).";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { Poruka = $"Greška pri izvozu: {ex.Message}"; }
    }

    private (DateTime? od, DateTime? @do) ProcitajPeriod()
    {
        var path = DbfPutanja("ospodaci.dbf");
        if (path == null) return (null, null);

        try
        {
            var reader = new SimpleDbfReader(path);
            foreach (var r in reader.Zapisi())
                return (r.DajDate("EDAT0"), r.DajDate("EDAT1"));
        }
        catch { }

        return (null, null);
    }

    private static decimal XDec(OsKartica k, string polje)
        => OsSaldoViewModel.DajDec(k, polje);

    private string? DbfPutanja(string ime) => DbfHelper.NadjiDbf(_appState, ime);
}
