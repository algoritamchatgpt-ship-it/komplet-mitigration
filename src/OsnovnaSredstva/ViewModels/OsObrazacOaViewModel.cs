using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using KolDef = OsnovnaSredstva.Services.OsStampacHelper.KolDef;

namespace OsnovnaSredstva.ViewModels;

public partial class OsObrazacOaViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<OsObrazacOaViewModel>();
    private readonly AppState _appState;
    private List<OsOaStavka> _sveStavke = [];

    [ObservableProperty] private ObservableCollection<OsOaStavka> _stavke = [];
    [ObservableProperty] private OsOaStavka? _izabranaStavka;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _infoIzabrane = "";

    private bool _izmijenjeno;
    public bool ImaNeSnimljenih => _izmijenjeno;

    partial void OnIzabranaStavkaChanged(OsOaStavka? value)
        => InfoIzabrane = value == null ? "" : $"AG: {value.Ag}   Agstopa: {value.AgStopa:N3}   Neotpis: {value.Neotpis:N2}   Amort2: {value.Amort2:N2}   Sad2: {value.Sad2:N2}";

    public OsObrazacOaViewModel(AppState appState)
    {
        _appState = appState;
        Ucitaj();
    }

    private void Ucitaj()
    {
        var path = DbfHelper.NadjiDbf(_appState, "osoa.dbf");
        if (path == null) { Stavke = []; Poruka = "osoa.dbf nije pronađen u folderu firme."; return; }

        try
        {
            var reader = new SimpleDbfReader(path);
            var lista = new List<OsOaStavka>();

            foreach (var r in reader.Zapisi())
            {
                lista.Add(new OsOaStavka
                {
                    Ag      = r.DajString("AG"),
                    Pocetno = r.DajDecimal("POCETNO"),
                    Nabavka = r.DajDecimal("NABAVKA"),
                    Prodaja = r.DajDecimal("PRODAJA"),
                    Neotpis = r.DajDecimal("NEOTPIS"),
                    AgStopa = r.DajDecimal("AGSTOPA"),
                    Amort2  = r.DajDecimal("AMORT2"),
                    Sad2    = r.DajDecimal("SAD2"),
                    Preneto = r.DajString("PRENETO"),
                    Numred  = (int)r.DajDecimal("NUMRED"),
                    IDBr    = (int)r.DajDecimal("IDBR"),
                });
            }

            _sveStavke = lista;
            Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
            IzabranaStavka = Stavke.FirstOrDefault();
            Poruka = $"Učitano {_sveStavke.Count} zapisa iz osoa.dbf.";
            _log.Debug("osoa.dbf: učitano {Count} stavki", _sveStavke.Count);
            PretplatiSeNaIzmjene();
            _izmijenjeno = false;
        }
        catch (Exception ex)
        {
            _sveStavke = [];
            Stavke = [];
            Poruka = $"Greška: {ex.Message}";
            _log.Error(ex, "Greška pri čitanju osoa.dbf");
        }
    }

    [RelayCommand] private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Dodaj()
    {
        var max = _sveStavke.Select(s => s.IDBr).DefaultIfEmpty(0).Max();
        var nova = new OsOaStavka { IDBr = max + 1 };
        _sveStavke.Add(nova);
        Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
        IzabranaStavka = nova;
        nova.PropertyChanged += (_, _) => _izmijenjeno = true;
        Poruka = "Novi red dodan. Unesite podatke i kliknite Sačuvaj.";
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = DbfHelper.NadjiDbf(_appState, "osoa.dbf");
        if (path == null) { Poruka = "osoa.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, _sveStavke,
                (s, f) => f.ToUpperInvariant() switch
                {
                    "AG"      => (object?)s.Ag,
                    "POCETNO" => s.Pocetno,
                    "NABAVKA" => s.Nabavka,
                    "PRODAJA" => s.Prodaja,
                    "NEOTPIS" => s.Neotpis,
                    "AGSTOPA" => s.AgStopa,
                    "AMORT2"  => s.Amort2,
                    "SAD2"    => s.Sad2,
                    "PRENETO" => s.Preneto,
                    "NUMRED"  => (object?)s.Numred,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Sačuvano ({_sveStavke.Count} zapisa).";
            _izmijenjeno = false;
            _log.Information("osoa.dbf: sačuvano {Count} zapisa", _sveStavke.Count);
        }
        catch (Exception ex) { Poruka = $"Greška pri snimanju: {ex.Message}"; _log.Error(ex, "Greška pri snimanju osoa.dbf"); }
    }

    [RelayCommand]
    private void UcitajGrupe()
    {
        var path = DbfPutanjaZaGrupe("osag.dbf");
        if (path == null) { Poruka = "osag.dbf nije pronađen."; return; }

        try
        {
            var reader = new SimpleDbfReader(path);
            var lista = new List<OsOaStavka>();
            var idbr = 1;

            foreach (var r in reader.Zapisi())
            {
                var ag = r.DajString("AG").Trim();
                if (ag == "1") continue;   // FoxPro: DELETE ALL FOR AG='1'

                var postojeca = _sveStavke.FirstOrDefault(s => s.Ag.Trim() == ag);
                lista.Add(new OsOaStavka
                {
                    Ag      = ag,
                    AgStopa = r.DajDecimal("AGSTOPA"),
                    Pocetno = postojeca?.Pocetno ?? 0m,
                    Nabavka = postojeca?.Nabavka ?? 0m,
                    Prodaja = postojeca?.Prodaja ?? 0m,
                    Neotpis = postojeca?.Neotpis ?? 0m,
                    Amort2  = postojeca?.Amort2  ?? 0m,
                    Sad2    = postojeca?.Sad2    ?? 0m,
                    Preneto = postojeca?.Preneto ?? "",
                    Numred  = idbr,
                    IDBr    = idbr++,
                });
            }

            _sveStavke = lista;
            Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
            IzabranaStavka = Stavke.FirstOrDefault();
            Poruka = $"Učitano {_sveStavke.Count} amortizacionih grupa.";
            _log.Debug("osag.dbf: učitano {Count} amortizacionih grupa", _sveStavke.Count);
            PretplatiSeNaIzmjene();
            _izmijenjeno = true;
        }
        catch (Exception ex) { Poruka = $"Greška pri ucitavanju grupa: {ex.Message}"; _log.Error(ex, "Greška pri čitanju osag.dbf"); }
    }

    [RelayCommand]
    private void UcitajPodatke()
    {
        if (_sveStavke.Count == 0) { Poruka = "Nema grupa — prvo pokrenite UCITAJ GRUPE."; return; }

        var osPath = DbfHelper.NadjiDbf(_appState, "os.dbf");
        if (osPath == null) { Poruka = "os.dbf nije pronađen u folderu firme."; return; }

        try
        {
            // Čitamo period datume iz ospodaci.dbf ako postoji
            DateTime? edat0 = null, edat1 = null;
            var podaciPath = DbfHelper.NadjiDbf(_appState, "ospodaci.dbf");
            if (podaciPath != null)
            {
                var podaciReader = new SimpleDbfReader(podaciPath);
                foreach (var r in podaciReader.Zapisi())
                {
                    edat0 = r.DajDate("EDAT0");
                    edat1 = r.DajDate("EDAT1");
                    break;
                }
            }

            // Učitavamo OS zapise
            var osReader = new SimpleDbfReader(osPath);
            var osZapisi = osReader.Zapisi().ToList();

            // Nuliramo sve sume
            foreach (var s in _sveStavke)
            {
                s.Pocetno = 0m;
                s.Nabavka = 0m;
                s.Prodaja = 0m;
                s.Neotpis = 0m;
                s.Amort2  = 0m;
                s.Sad2    = 0m;
            }

            // Agregiramo po AG (filter: EMPTY(NACINOB))
            foreach (var r in osZapisi)
            {
                var nacinob = r.DajString("NACINOB").Trim();
                if (!string.IsNullOrEmpty(nacinob)) continue;

                var ag  = r.DajString("AG").Trim();
                var s   = _sveStavke.FirstOrDefault(x => (x.Ag?.Trim() ?? "") == ag);
                if (s == null) continue;

                var datum0 = r.DajDate("DATUM0");
                var datum1 = r.DajDate("DATUM1");
                var sad02  = r.DajDecimal("SAD02");
                var nab02  = r.DajDecimal("NAB02");
                var amort2 = r.DajDecimal("AMORT2");
                var sad2   = r.DajDecimal("SAD2");

                if (edat0.HasValue && datum0.HasValue && datum0.Value == edat0.Value)
                    s.Pocetno += sad02;

                if (edat0.HasValue && datum0.HasValue && datum0.Value > edat0.Value)
                    s.Nabavka += nab02;

                if (edat1.HasValue && datum1.HasValue && datum1.Value < edat1.Value)
                    s.Prodaja += sad02;

                s.Neotpis += sad02;
                s.Amort2  += amort2;
                s.Sad2    += sad2;
            }

            Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
            var datInfo = edat0.HasValue ? $" (period od {edat0:dd.MM.yyyy} do {edat1:dd.MM.yyyy})" : " (bez filtera datuma)";
            Poruka = $"Podaci učitani iz {Path.GetFileName(osPath)}{datInfo}.";
            _log.Debug("os.dbf: agregirano {Count} stavki za OA obrazac{DatInfo}", _sveStavke.Count, datInfo);
            PretplatiSeNaIzmjene();
            _izmijenjeno = true;
        }
        catch (Exception ex) { Poruka = $"Greška pri ucitavanju podataka: {ex.Message}"; _log.Error(ex, "Greška pri ucitavanju podataka iz os.dbf"); }
    }

    [RelayCommand]
    private void Preracun()
    {
        foreach (var s in _sveStavke)
        {
            s.Neotpis = s.Pocetno + s.Nabavka - s.Prodaja;
            s.Amort2  = Math.Round(s.Neotpis * s.AgStopa / 100m, 0);
            s.Sad2    = s.Neotpis - s.Amort2;
        }
        Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
        Poruka = "Preračun izvršeno.";
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void BrisanjePraznina()
    {
        _sveStavke.RemoveAll(s => string.IsNullOrWhiteSpace(s.Ag));
        Stavke = new ObservableCollection<OsOaStavka>(_sveStavke);
        Poruka = "Praznine obrisane.";
        _izmijenjeno = true;
    }

    // Legacy "\<BRISANJE" dugme na OSOA.scx: SELECT OSOA; ZAP — briše SVE zapise
    // bezuslovno (ne samo praznine). Odvojeno od BrisanjePraznina iznad (koja je
    // bezbednija reinterpretacija dodana ranije) zbog destruktivnosti — ista predostrožnost
    // kao kod OS.scx PrenosIzSifarnika.
    [RelayCommand]
    private void BrisanjeSve()
    {
        if (MessageBox.Show(
                $"Obrisati SVE zapise iz OA obrasca ({_sveStavke.Count})?\n\nOva radnja se ne može opozvati nakon Sačuvaj.",
                "BRISANJE", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        _sveStavke.Clear();
        Stavke = [];
        Poruka = "Svi zapisi obrisani. Kliknite Sačuvaj.";
        _izmijenjeno = true;
    }

    [RelayCommand] private void PreglediOa()  => IspisiIzvestaj("OA Obrazac");
    [RelayCommand] private void PreglediOa1() => IspisiIzvestaj("OA-1 Obrazac");

    private void PretplatiSeNaIzmjene()
    {
        foreach (var s in _sveStavke)
            s.PropertyChanged += (_, _) => _izmijenjeno = true;
    }

    private void IspisiIzvestaj(string naslov)
    {
        KolDef[] kol = [
            new("AG",       55, false), new("Pocetno",  95), new("Nabavka",  95),
            new("Prodaja",  95),        new("Neotpis",  95), new("AgStopa%", 70),
            new("Amort2",   95),        new("Sad2",     95)
        ];
        var redovi = _sveStavke.Select(s => new[] {
            s.Ag,
            s.Pocetno.ToString("N2"), s.Nabavka.ToString("N2"),
            s.Prodaja.ToString("N2"), s.Neotpis.ToString("N2"),
            s.AgStopa.ToString("N3"),
            s.Amort2.ToString("N2"),  s.Sad2.ToString("N2")
        }).ToList();
        string[] uk = [
            "UKUPNO",
            _sveStavke.Sum(s => s.Pocetno).ToString("N2"), _sveStavke.Sum(s => s.Nabavka).ToString("N2"),
            _sveStavke.Sum(s => s.Prodaja).ToString("N2"), _sveStavke.Sum(s => s.Neotpis).ToString("N2"), "",
            _sveStavke.Sum(s => s.Amort2).ToString("N2"),  _sveStavke.Sum(s => s.Sad2).ToString("N2")
        ];
        OsStampacHelper.Stampaj(naslov, kol, redovi, uk, landscape: false, m => Poruka = m);
    }

    [RelayCommand] private void Prvi()   { if (Stavke.Count > 0) IzabranaStavka = Stavke[0]; }
    [RelayCommand] private void Zadnji() { if (Stavke.Count > 0) IzabranaStavka = Stavke[^1]; }
    [RelayCommand] private void Dole()
    {
        var sel = IzabranaStavka;
        if (sel == null || Stavke.Count == 0) return;
        var idx = Stavke.IndexOf(sel);
        if (idx < Stavke.Count - 1) IzabranaStavka = Stavke[idx + 1];
    }
    [RelayCommand] private void Gore()
    {
        var sel = IzabranaStavka;
        if (sel == null || Stavke.Count == 0) return;
        var idx = Stavke.IndexOf(sel);
        if (idx > 0) IzabranaStavka = Stavke[idx - 1];
    }

    private string? DbfPutanjaZaGrupe(string ime)
    {
        var kandidatiFoldera = new List<string>();

        static bool IstaPutanja(string left, string right)
            => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        void DodajFolder(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return;
            if (kandidatiFoldera.Any(x => IstaPutanja(x, folder))) return;
            kandidatiFoldera.Add(folder);
        }

        var folderFirme = _appState.AktivnaFirma?.FolderPath;
        if (!string.IsNullOrWhiteSpace(folderFirme))
        {
            var root = FinWorkspaceResolver.NormalizeRootPath(folderFirme);
            DodajFolder(Path.Combine(root, "data00"));
            DodajFolder(Path.Combine(folderFirme, "data00"));
            DodajFolder(folderFirme);
            DodajFolder(root);
            DodajFolder(Path.Combine(root, "data01"));
        }

        DodajFolder(Path.Combine(Directory.GetCurrentDirectory(), "data00"));
        DodajFolder(Directory.GetCurrentDirectory());
        DodajFolder(Path.Combine(AppContext.BaseDirectory, "data00"));
        DodajFolder(AppContext.BaseDirectory);

        string? prviPronadjen = null;
        foreach (var folder in kandidatiFoldera)
        {
            var putanja = DbfHelper.NadjiDbfUFolderu(folder, ime);
            if (string.IsNullOrWhiteSpace(putanja)) continue;

            prviPronadjen ??= putanja;
            if (DbfHelper.ImaZapisaDbf(putanja)) return putanja;
        }

        return prviPronadjen;
    }

}
