using Algoritam.Application;
using Algoritam.Application.Services;
using Algoritam.Infrastructure.Dbf;
using Algoritam.Infrastructure.Migration;
using Algoritam.Infrastructure.Services;
using Algoritam.WPF.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Algoritam.WPF.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using DomainRadnik = Algoritam.Domain.Entities.Radnik;
using LdObracunStavka = Algoritam.Domain.Entities.LdObracunStavka;
using LdParametar = Algoritam.Domain.Entities.LdParametar;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za Platni Spisak — ekvivalent FoxPro forme LD.SCX.
/// Čita/piše direktno u LD*.dbf fajl za tekući period.
/// </summary>
public partial class PlatniSpisakViewModel : ObservableObject
{
    private readonly record struct KreditPrenosZbir(decimal AktivnaRata, decimal AkontRata);
    private sealed class ProveraPodatakaIzvestaj
    {
        public string NazivKoraka { get; }
        public List<string> Info { get; } = [];
        public List<string> Greske { get; } = [];
        public List<string> Upozorenja { get; } = [];

        public ProveraPodatakaIzvestaj(string nazivKoraka)
        {
            NazivKoraka = nazivKoraka;
        }
    }

    private readonly AppState _appState;
    private readonly IObracunService _obracunService = new ObracunService();
    private string? _currentLdPath;
    private LdParametar? _parametar;
    private List<DomainRadnik> _sviRadnici = [];
    private DbfOptimisticConcurrency.FileSnapshot? _ldSnapshot;
    private string _lastSaveError = "";

    [ObservableProperty] private ObservableCollection<LdObracunStavka> _stavke = [];
    [ObservableProperty] private LdObracunStavka? _selektovana;
    [ObservableProperty] private string _filterTekst = "";

    public ICollectionView StavkeView { get; private set; } = CollectionViewSource.GetDefaultView(Array.Empty<LdObracunStavka>());
    [ObservableProperty] private string _statusInfo = "Učitavanje...";
    [ObservableProperty] private string _mesecGodina = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListaPrazan))]
    private int _ukupnoRadnika;
    [ObservableProperty] private decimal _ukupnoBruto;

    public bool IsListaPrazan => UkupnoRadnika == 0;

    // ── Vidljivost kolona ────────────────────────────────────────────────
    [ObservableProperty] private bool _prikaziCasove = true;
    [ObservableProperty] private bool _prikaziObustaveDetalj = false;
    [ObservableProperty] private bool _prikaziFirmskeStavke = false;
    [ObservableProperty] private bool _prikaziKrediteKolone = false;
    [ObservableProperty] private decimal _ukupnoNeto;
    [ObservableProperty] private decimal _ukupnoZaIsplatu;

    // Nazivi primanja (NAZP1-5) i nazivi obustava (NAZO1-6) iz Parametri 2.
    // Koriste se kao labele u kartici, na listicu i za dinamicke naslove kolona.
    [ObservableProperty] private string _nazpLabel1 = "PRIMANJE 1";
    [ObservableProperty] private string _nazpLabel2 = "PRIMANJE 2";
    [ObservableProperty] private string _nazpLabel3 = "PRIMANJE 3";
    [ObservableProperty] private string _nazpLabel4 = "PRIMANJE 4";
    [ObservableProperty] private string _nazpLabel5 = "TERENSKI";
    [ObservableProperty] private string _nazoLabel1 = "OBUSTAVA 1";
    [ObservableProperty] private string _nazoLabel2 = "OBUSTAVA 2";
    [ObservableProperty] private string _nazoLabel3 = "OBUSTAVA 3";
    [ObservableProperty] private string _nazoLabel4 = "OBUSTAVA 4";
    [ObservableProperty] private string _nazoLabel5 = "OBUSTAVA 5";
    [ObservableProperty] private string _nazoLabel6 = "OBUSTAVA 6";

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _busyPoruka = "Učitavanje...";

    public PlatniSpisakViewModel(AppState appState)
    {
        _appState = appState;
        StavkeView = CollectionViewSource.GetDefaultView(Stavke);
    }

    public async Task InitAsync()
    {
        await UcitajPodatkeAsync();
    }

    partial void OnFilterTekstChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            StavkeView.Filter = null;
        }
        else
        {
            var upit = value.Trim().ToLowerInvariant();
            StavkeView.Filter = obj =>
                obj is LdObracunStavka s &&
                (s.ImePrez.ToLowerInvariant().Contains(upit) ||
                 s.Evidbroj.ToLowerInvariant().Contains(upit) ||
                 s.Broj.ToString().Contains(upit));
        }
        StavkeView.Refresh();
    }

    private readonly record struct DbfRezultat(
        List<LdObracunStavka> Stavke,
        LdParametar? Parametar,
        List<DomainRadnik> SviRadnici,
        string? LdPath,
        string? Greska);

    private DbfRezultat CitajDbfPodatke()
    {
        var folder = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return new DbfRezultat([], null, [], null, "Folder firme nije pronađen.");

        var parametar = UcitajParametarIzFoxDbf(folder);
        var sviRadnici = UcitajRadnikeIzDbf();
        var ldPath = NadjiLdFajlZaPeriod(folder, parametar);
        var stavke = new List<LdObracunStavka>();
        string? greska = null;

        if (ldPath != null && File.Exists(ldPath))
        {
            try
            {
                var zapisi = DbfReader.CitajSveZapise(ldPath);
                var isplata = parametar?.Isplata ?? 1;
                var vrsta = isplata switch { 2 => "P", 3 => "B", _ => "A" };
                foreach (var z in zapisi.OrderBy(z => Int(z, "BROJ")))
                {
                    var s = MapirajZapis(z);
                    s.Isplata = isplata;
                    s.Vrsta = vrsta;
                    stavke.Add(s);
                }
            }
            catch (Exception ex)
            {
                greska = $"Greška pri čitanju LD fajla: {ex.Message}";
            }
        }

        return new DbfRezultat(stavke, parametar, sviRadnici, ldPath, greska);
    }

    private void PrimenjiDbfRezultat(DbfRezultat rez)
    {
        Stavke.Clear();
        _parametar = rez.Parametar;
        _sviRadnici = rez.SviRadnici;
        _currentLdPath = rez.LdPath;

        if (rez.Greska != null)
        {
            StatusInfo = rez.Greska;
            return;
        }

        foreach (var s in rez.Stavke) Stavke.Add(s);

        MesecGodina = _parametar != null
            ? $"Mesec: {_parametar.Mesec}  Isplata: {_parametar.Isplata}  Godina: {_parametar.Godina}"
            : "Parametri nisu podešeni";

        AzurirajNaziveKolona();
        OsveziLdSnapshot();
        AzurirajSumarno();
    }

    private void UcitajPodatke() => PrimenjiDbfRezultat(CitajDbfPodatke());

    private async Task UcitajPodatkeAsync()
    {
        IsBusy = true;
        BusyPoruka = "Učitavanje podataka iz arhive...";
        try
        {
            var rez = await Task.Run(CitajDbfPodatke);
            PrimenjiDbfRezultat(rez);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AzurirajNaziveKolona()
    {
        if (_parametar == null) return;
        NazpLabel1 = string.IsNullOrWhiteSpace(_parametar.Nazp1) ? "PRIMANJE 1" : _parametar.Nazp1.Trim();
        NazpLabel2 = string.IsNullOrWhiteSpace(_parametar.Nazp2) ? "PRIMANJE 2" : _parametar.Nazp2.Trim();
        NazpLabel3 = string.IsNullOrWhiteSpace(_parametar.Nazp3) ? "PRIMANJE 3" : _parametar.Nazp3.Trim();
        NazpLabel4 = string.IsNullOrWhiteSpace(_parametar.Nazp4) ? "PRIMANJE 4" : _parametar.Nazp4.Trim();
        NazpLabel5 = string.IsNullOrWhiteSpace(_parametar.Nazp5ter) ? "TERENSKI" : _parametar.Nazp5ter.Trim();
        NazoLabel1 = string.IsNullOrWhiteSpace(_parametar.Nazo1) ? "OBUSTAVA 1" : _parametar.Nazo1.Trim();
        NazoLabel2 = string.IsNullOrWhiteSpace(_parametar.Nazo2) ? "OBUSTAVA 2" : _parametar.Nazo2.Trim();
        NazoLabel3 = string.IsNullOrWhiteSpace(_parametar.Nazo3) ? "OBUSTAVA 3" : _parametar.Nazo3.Trim();
        NazoLabel4 = string.IsNullOrWhiteSpace(_parametar.Nazo4) ? "OBUSTAVA 4" : _parametar.Nazo4.Trim();
        NazoLabel5 = string.IsNullOrWhiteSpace(_parametar.Nazo5) ? "OBUSTAVA 5" : _parametar.Nazo5.Trim();
        NazoLabel6 = string.IsNullOrWhiteSpace(_parametar.Nazo6) ? "OBUSTAVA 6" : _parametar.Nazo6.Trim();
    }

    private bool SacuvajUDbf()
    {
        _lastSaveError = "";

        if (_currentLdPath == null)
        {
            _lastSaveError = "Putanja LD fajla nije postavljena.";
            return false;
        }

        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            _lastSaveError = "Folder aktivne firme nije pronađen.";
            StatusInfo = _lastSaveError;
            return false;
        }

        if (!File.Exists(_currentLdPath))
        {
            // Need schema template — locate LD template from firma/install workspace.
            var templatePath = NadjiLdTemplateZaKreiranje(folder, _currentLdPath);
            if (templatePath == null)
            {
                var templateF1 = FoxWorkspaceSupport.FindTemplateF1(Directory.GetParent(folder)?.FullName);
                if (!string.IsNullOrWhiteSpace(templateF1) && Directory.Exists(templateF1))
                {
                    FoxWorkspaceSupport.CopyLdTablesFromTemplate(templateF1, folder, overwrite: false);
                    templatePath = NadjiLdTemplateZaKreiranje(folder, _currentLdPath);
                }
            }

            if (templatePath == null)
            {
                _lastSaveError = "Nije moguće kreirati LD fajl — nema template-a. Proverite instalaciju.";
                StatusInfo = _lastSaveError;
                return false;
            }

            File.Copy(templatePath, _currentLdPath);
        }

        if (!ProveriKonfliktPreSnimanja())
        {
            _lastSaveError = "Fajl je promenjen od strane drugog korisnika. Podaci su osvezeni, molimo ponovite unos.";
            return false;
        }

        RecordLockHandle? lockHandle = null;

        try
        {
            if (!DbfOptimisticConcurrency.TryAcquireRecordLock(
                    _currentLdPath!,
                    "__FILE__",
                    Environment.UserName,
                    out lockHandle,
                    out var lockGreska))
            {
                _lastSaveError = lockGreska;
                StatusInfo = lockGreska;
                MessageBox.Show(lockGreska, "Zakljucano", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var schema = DbfTableWriter.LoadSchema(_currentLdPath!);
            var rows = Stavke.Select(MapirajStavkuURed).ToList();
            DbfTableWriter.WriteTable(
                _currentLdPath!,
                schema,
                rows,
                static (r, f) => r.TryGetValue(f, out var v) ? v : null);

            OsveziLdSnapshot();
            return true;
        }
        catch (Exception ex)
        {
            _lastSaveError = $"Greska pri snimanju LD fajla: {ex.Message}";
            StatusInfo = _lastSaveError;
            return false;
        }
        finally
        {
            lockHandle?.Dispose();
        }
    }

    private bool SacuvajIliVratiStanje()
    {
        if (SacuvajUDbf())
            return true;

        var greska = _lastSaveError;
        UcitajPodatke();
        if (!string.IsNullOrWhiteSpace(greska))
            StatusInfo = greska;
        return false;
    }

    private string? NadjiLdFajlZaPeriod(string folder, LdParametar? param)
    {
        if (param == null || string.IsNullOrWhiteSpace(folder)) return null;

        var prefix = param.Isplata switch { 2 => "LDP", 3 => "LDB", _ => "LD" };
        var trazeniMesec = param.Mesec;
        var trazenaGodina = (param.Godina ?? string.Empty).Trim();
        var trazenaIsplata = param.Isplata;
        string? fallbackMesec = null;
        var poseceni = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var kandidatiNaziva = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"{prefix}.DBF",
            $"{prefix}0.DBF",
            $"{prefix}00.DBF"
        };

        for (int i = 1; i <= 99; i++)
        {
            kandidatiNaziva.Add($"{prefix}{i}.DBF");
            kandidatiNaziva.Add($"{prefix}{i:00}.DBF");
        }

        foreach (var naziv in kandidatiNaziva)
        {
            var putanja = LdObracunDbfReader.PronadjiDbf(folder, naziv);
            if (string.IsNullOrWhiteSpace(putanja) || !poseceni.Add(putanja))
                continue;

            try
            {
                var zapisi = DbfReader.CitajSveZapise(putanja);
                var poMesecu = zapisi.Where(z => Int(z, "MESEC") == trazeniMesec).ToList();
                if (poMesecu.Count == 0)
                    continue;

                fallbackMesec ??= putanja;

                var imaTacanPeriod = poMesecu.Any(z =>
                    (trazenaIsplata <= 0 || Int(z, "ISPLATA") is 0 || Int(z, "ISPLATA") == trazenaIsplata)
                    && GodinaSePoklapa(Str(z, "GODINA"), trazenaGodina));

                if (imaTacanPeriod)
                    return putanja;
            }
            catch { }
        }

        // Not found — new period, use mesec as file index
        if (fallbackMesec != null)
            return fallbackMesec;

        var bazniPut = LdObracunDbfReader.PronadjiDbf(folder, $"{prefix}.DBF");
        if (!string.IsNullOrWhiteSpace(bazniPut))
            return bazniPut;

        var indeks = Math.Clamp(trazeniMesec, 1, 99);
        return Path.Combine(folder, $"{prefix}{indeks}.DBF");
    }

    private static bool GodinaSePoklapa(string godinaZapisa, string trazenaGodina)
    {
        if (string.IsNullOrWhiteSpace(trazenaGodina))
            return true;

        if (string.IsNullOrWhiteSpace(godinaZapisa))
            return true;

        var zapis = godinaZapisa.Trim();
        var trazena = trazenaGodina.Trim();
        if (zapis.Equals(trazena, StringComparison.OrdinalIgnoreCase))
            return true;

        if (int.TryParse(zapis, out var godinaZapisInt) && int.TryParse(trazena, out var godinaTrazenaInt))
        {
            if (godinaZapisInt == godinaTrazenaInt)
                return true;

            return (godinaZapisInt % 100) == (godinaTrazenaInt % 100);
        }

        return false;
    }

    private List<DomainRadnik> UcitajRadnikeIzDbf()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return [];

        var ldradDbf = PronadjiLdradDbf(folder);
        if (ldradDbf is null)
            return [];

        try
        {
            var zapisi = DbfReader.CitajSveZapise(ldradDbf);
            return zapisi
                .Where(z => !string.Equals(Str(z, "BRISANJE"), "D", StringComparison.OrdinalIgnoreCase))
                .Select(z => new DomainRadnik
                {
                    Broj = Int(z, "BROJ"),
                    ImePrezime = Str(z, "IME_PREZ"),
                    MaticniBroj = Str(z, "MATICNIBR"),
                    IdBroj = Str(z, "IDBROJ"),
                    EvidencijskiBroj = Str(z, "EVIDBROJ"),
                    Grupa = Int(z, "GRUPA"),
                    Grupa1 = Int(z, "GRUPA1"),
                    Mtr = Int(z, "MTR"),
                    StartniBodovi = Dec(z, "STARTBOD"),
                    BeneficiraniProcenat = Dec(z, "BENPROC"),
                    Stimulacija1 = Dec(z, "STIM1"),
                    Stimulacija2 = Dec(z, "STIM2"),
                    Stimulacija3 = Dec(z, "STIM3"),
                    PorskoUmanjenje = Dec(z, "PORUMANJ"),
                    DoprinosnoUmanjenje = Dec(z, "DOPUMANJ"),
                    PioUmanjenjeRadnik = Dec(z, "PIOUMANJR"),
                    PioUmanjenjeFirma = Dec(z, "PIOUMANJF"),
                    Vrsta = Str(z, "VRSTA"),
                    Neaktivan = Str(z, "NEAKTIVAN"),
                    Brisanje = Str(z, "BRISANJE"),
                    Staz = Int(z, "STAZ"),
                    Ropnr = Str(z, "ROPNR"),
                    Kasa = Dec(z, "KASA"),
                    KasaRata = Dec(z, "KASARATA"),
                    SamodoprProcenat = Dec(z, "SAMOPROC"),
                    SindikatProcenat1 = Dec(z, "SIND1PROC"),
                    SindikatProcenat2 = Dec(z, "SIND2PROC"),
                    SolidarnostProcenat = Dec(z, "SOLPROC"),
                    AlimentacijaProcenat = Dec(z, "ALIMPROC"),
                    Partija = Str(z, "PARTIJA"),
                    ZiroRacun = Str(z, "ZIRORAC"),
                })
                .OrderBy(r => r.Broj)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string? PronadjiLdradDbf(string folder)
    {
        var kandidati = new[]
        {
            Path.Combine(folder, "ldrad.dbf"),
            Path.Combine(folder, "LDRAD.DBF")
        };

        foreach (var kandidat in kandidati)
        {
            if (File.Exists(kandidat))
                return kandidat;
        }

        return Directory.GetFiles(folder, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => string.Equals(Path.GetFileName(f), "ldrad.dbf", StringComparison.OrdinalIgnoreCase));
    }

    private static string Str(Dictionary<string, object?> z, string key)
        => z.TryGetValue(key, out var v) && v is string s ? s.Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> z, string key)
    {
        if (!z.TryGetValue(key, out var v) || v is null) return 0;
        return v switch
        {
            int i => i,
            long l => (int)l,
            decimal d => (int)d,
            double d => (int)d,
            float f => (int)f,
            string s when int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) => i,
            string s when int.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out var i) => i,
            _ => 0
        };
    }

    private static decimal Dec(Dictionary<string, object?> z, string key)
    {
        if (!z.TryGetValue(key, out var v) || v is null) return 0m;
        return v switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double d => (decimal)d,
            float f => (decimal)f,
            string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
            string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out var d) => d,
            _ => 0m
        };
    }

    private static LdParametar? UcitajParametarIzFoxDbf(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return null;

        var putanja = LdObracunDbfReader.PronadjiDbf(folderPath, "ldparam.dbf");
        if (putanja == null) return null;

        try
        {
            var zapisi = DbfReader.CitajSveZapise(putanja);
            var z = zapisi.FirstOrDefault();
            if (z is null) return null;

            return new LdParametar
            {
                Id = 1,
                Mesec = Int(z, "MESEC"),
                Isplata = Int(z, "ISPLATA"),
                Godina = Str(z, "GODINA"),
                Nazmes = Str(z, "NAZMES"),
                Cenarada = Dec(z, "CENARADA"),
                Czakon = Int(z, "CZAKON"),
                Ekoefs = Dec(z, "EKOEFS"),
                Procnoc = Dec(z, "PROCNOC"),
                Procprod = Dec(z, "PROCPROD"),
                Procned = Dec(z, "PROCNED"),
                Procbol = Dec(z, "PROCBOL"),
                Procpraz = Dec(z, "PROCPRAZ"),
                Procplac = Dec(z, "PROCPLAC"),
                Procsus = Dec(z, "PROCSUS"),
                Procmin = Dec(z, "PROCMIN"),
                Minnac = Int(z, "MINNAC"),
                Procpor = Dec(z, "PROCPOR"),
                Neoporez = Dec(z, "NEOPOREZ"),
                Neoporezp = Dec(z, "NEOPOREZP"),
                Srazpor = Str(z, "SRAZPOR"),
                Sdin1 = Dec(z, "SDIN1"),
                Prosbruto = Dec(z, "PROSBRUTO"),
                Redispl = Int(z, "REDISPL"),
                Nakpos = Str(z, "NAKPOS"),
                Decimale = Str(z, "DECIMALE"),
                Bkproc = Dec(z, "BKPROC"),
                Bkzastita = Dec(z, "BKZASTITA"),
                Priprav = Dec(z, "PRIPRAV"),
                Solporod1 = Dec(z, "SOLPOROD1"),
                Solpordo1 = Dec(z, "SOLPORDO1"),
                Solporod2 = Dec(z, "SOLPOROD2"),
                Solproc1 = Dec(z, "SOLPROC1"),
                Solproc2 = Dec(z, "SOLPROC2"),
                Doppr1 = Dec(z, "DOPPR1"),
                Dopzr1 = Dec(z, "DOPZR1"),
                Dopnr1 = Dec(z, "DOPNR1"),
                Doppf1 = Dec(z, "DOPPF1"),
                Dopzf1 = Dec(z, "DOPZF1"),
                Dopnf1 = Dec(z, "DOPNF1"),
                Doppr2 = Dec(z, "DOPPR2"),
                Dopzr2 = Dec(z, "DOPZR2"),
                Dopnr2 = Dec(z, "DOPNR2"),
                Doppf2 = Dec(z, "DOPPF2"),
                Dopzf2 = Dec(z, "DOPZF2"),
                Dopnf2 = Dec(z, "DOPNF2"),
                Doppr3 = Dec(z, "DOPPR3"),
                Dopzr3 = Dec(z, "DOPZR3"),
                Dopnr3 = Dec(z, "DOPNR3"),
                Doppf3 = Dec(z, "DOPPF3"),
                Dopzf3 = Dec(z, "DOPZF3"),
                Dopnf3 = Dec(z, "DOPNF3"),
                Doppr4 = Dec(z, "DOPPR4"),
                Dopzr4 = Dec(z, "DOPZR4"),
                Dopnr4 = Dec(z, "DOPNR4"),
                Doppf4 = Dec(z, "DOPPF4"),
                Dopzf4 = Dec(z, "DOPZF4"),
                Dopnf4 = Dec(z, "DOPNF4"),
                Doppr5 = Dec(z, "DOPPR5"),
                Dopzr5 = Dec(z, "DOPZR5"),
                Dopnr5 = Dec(z, "DOPNR5"),
                Doppf5 = Dec(z, "DOPPF5"),
                Dopzf5 = Dec(z, "DOPZF5"),
                Dopnf5 = Dec(z, "DOPNF5"),
                Komoraj = Dec(z, "KOMORAJ"),
                Komoras = Dec(z, "KOMORAS"),
                Komorar = Dec(z, "KOMORAR"),
                Dat1 = z.TryGetValue("DAT1", out var d1) && d1 is DateTime dt1 ? dt1 : null,
                Dat2 = z.TryGetValue("DAT2", out var d2) && d2 is DateTime dt2 ? dt2 : null,
                Dat3 = z.TryGetValue("DAT3", out var d3) && d3 is DateTime dt3 ? dt3 : null,
                Dat4 = z.TryGetValue("DAT4", out var d4) && d4 is DateTime dt4 ? dt4 : null,
                Nazp1 = Str(z, "NAZP1"), Nazp2 = Str(z, "NAZP2"), Nazp3 = Str(z, "NAZP3"),
                Nazp4 = Str(z, "NAZP4"), Nazp5 = Str(z, "NAZP5"), Nazp5ter = Str(z, "NAZP5TER"),
                Nazo1 = Str(z, "NAZO1"), Nazo2 = Str(z, "NAZO2"), Nazo3 = Str(z, "NAZO3"),
                Nazo4 = Str(z, "NAZO4"), Nazo5 = Str(z, "NAZO5"), Nazo6 = Str(z, "NAZO6"),
                Konacna = Str(z, "KONACNA"),
                Vrstaplate = Str(z, "VRSTAPLATE"),
            };
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? DatumIsplate(LdParametar? parametar)
    {
        if (parametar == null) return null;
        return parametar.Redispl switch
        {
            1 => parametar.Dat1,
            2 => parametar.Dat2,
            3 => parametar.Dat3,
            _ => parametar.Dat4
        };
    }

    private void AzurirajSumarno()
    {
        UkupnoRadnika = Stavke.Count;
        UkupnoBruto = Stavke.Sum(s => s.Bruto);
        UkupnoNeto = Stavke.Sum(s => s.Neto);
        UkupnoZaIsplatu = Stavke.Sum(s => s.Zaisplatu);
        StatusInfo = $"Radnika: {UkupnoRadnika}  |  Bruto: {UkupnoBruto:N2}  |  Neto: {UkupnoNeto:N2}  |  Za isplatu: {UkupnoZaIsplatu:N2}";
    }

    private static string? NadjiLdTemplateZaKreiranje(string firmaFolder, string ciljLdPath)
    {
        var ciljFajl = Path.GetFileName(ciljLdPath);
        if (string.IsNullOrWhiteSpace(ciljFajl))
            return null;

        foreach (var folder in EnumerirajLdTemplateFoldere(firmaFolder))
        {
            var template = NadjiLdTemplateUNeposrednomFolderu(folder, ciljFajl);
            if (template != null)
                return template;
        }

        return null;
    }

    private static IEnumerable<string> EnumerirajLdTemplateFoldere(string firmaFolder)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void DodajAkoPostoji(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return;

            var normalized = Path.GetFullPath(folder);
            if (seen.Add(normalized))
                result.Add(normalized);
        }

        DodajAkoPostoji(firmaFolder);

        var firmaRoot = Directory.GetParent(firmaFolder)?.FullName;
        DodajAkoPostoji(FoxWorkspaceSupport.FindTemplateF1(firmaRoot));

        var appDir = AppContext.BaseDirectory;
        var kandidati = new[]
        {
            Path.Combine(appDir, "instalacije", "AlgoritamOffice", "templates", "F1"),
            Path.Combine(appDir, "instalacije", "AlgoritamOffice", "templates", "F1", "zarade"),
            Path.Combine(appDir, "..", "instalacije", "AlgoritamOffice", "templates", "F1"),
            Path.Combine(appDir, "..", "instalacije", "AlgoritamOffice", "templates", "F1", "zarade"),
            Path.Combine(appDir, "templates", "F1"),
            Path.Combine(appDir, "templates", "F1", "zarade"),
            Path.Combine(appDir, "..", "templates", "F1"),
            Path.Combine(appDir, "..", "templates", "F1", "zarade"),
        };

        foreach (var kandidat in kandidati)
            DodajAkoPostoji(kandidat);

        return result;
    }

    private static string? NadjiLdTemplateUNeposrednomFolderu(string folder, string ciljFajl)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return null;

        var nazivBezExt = Path.GetFileNameWithoutExtension(ciljFajl) ?? string.Empty;
        var prefiks = UcitajLdPrefiks(nazivBezExt);

        var kandidati = new List<string> { ciljFajl };
        if (!string.IsNullOrWhiteSpace(prefiks))
        {
            kandidati.Add($"{prefiks}1.DBF");
            kandidati.Add($"{prefiks}.DBF");
        }

        kandidati.Add("LD1.DBF");
        kandidati.Add("LD.DBF");

        foreach (var kandidat in kandidati.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var putanja = LdObracunDbfReader.PronadjiDbf(folder, kandidat);
            if (putanja != null)
                return putanja;
        }

        return Directory.GetFiles(folder, "*.dbf", SearchOption.TopDirectoryOnly)
            .Where(f => JeLdObracunNaziv(Path.GetFileNameWithoutExtension(f)))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string UcitajLdPrefiks(string nazivBezEkstenzije)
    {
        if (nazivBezEkstenzije.StartsWith("LDP", StringComparison.OrdinalIgnoreCase)) return "LDP";
        if (nazivBezEkstenzije.StartsWith("LDB", StringComparison.OrdinalIgnoreCase)) return "LDB";
        if (nazivBezEkstenzije.StartsWith("LDI", StringComparison.OrdinalIgnoreCase)) return "LDI";
        if (nazivBezEkstenzije.StartsWith("LDR", StringComparison.OrdinalIgnoreCase)) return "LDR";
        return "LD";
    }

    private static bool JeLdObracunNaziv(string? nazivBezEkstenzije)
    {
        if (string.IsNullOrWhiteSpace(nazivBezEkstenzije))
            return false;

        var naziv = nazivBezEkstenzije.Trim();
        var prefiksi = new[] { "LDP", "LDB", "LDI", "LDR", "LD" };
        foreach (var prefiks in prefiksi)
        {
            if (!naziv.StartsWith(prefiks, StringComparison.OrdinalIgnoreCase))
                continue;

            var ostatak = naziv[prefiks.Length..];
            return ostatak.Length == 0 || ostatak.All(char.IsDigit);
        }

        return false;
    }

    private void OsveziGrid()
    {
        var selBroj = Selektovana?.Broj;
        var temp = Stavke.ToList();
        Stavke.Clear();
        foreach (var s in temp) Stavke.Add(s);
        if (selBroj.HasValue)
            Selektovana = Stavke.FirstOrDefault(s => s.Broj == selBroj.Value);
    }

    private void OsveziLdSnapshot()
    {
        if (string.IsNullOrWhiteSpace(_currentLdPath) || !File.Exists(_currentLdPath))
        {
            _ldSnapshot = null;
            return;
        }

        _ldSnapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(_currentLdPath);
    }

    private bool ProveriKonfliktPreSnimanja()
    {
        if (string.IsNullOrWhiteSpace(_currentLdPath))
            return false;

        if (_ldSnapshot == null)
        {
            OsveziLdSnapshot();
            return true;
        }

        if (!DbfOptimisticConcurrency.HasFileChanged(_currentLdPath, _ldSnapshot))
            return true;

        const string poruka = "Neko je vec sačuvao izmene u ovoj tabeli. Osveži podatke i ponovi unos.";
        StatusInfo = poruka;
        MessageBox.Show(poruka, "Konflikt pri snimanju", MessageBoxButton.OK, MessageBoxImage.Warning);
        UcitajPodatke();
        return false;
    }

    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private void OcistiFilter() => FilterTekst = "";

    //  UNOS RADNIKA
    // ══════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void UnosRadnika()
    {
        if (_parametar == null)
        {
            MessageBox.Show("Parametri zarada nisu podešeni!", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var radniciUObracu = Stavke.Select(s => s.Broj).ToHashSet();
        var vrstaplate = (_parametar.Vrstaplate ?? string.Empty).Trim();
        var noviRadnici = _sviRadnici
            .Where(r => !radniciUObracu.Contains(r.Broj)
                     && r.Neaktivan != "D"
                     && r.Brisanje != "D"
                     && VrstaRadnikaOdgovara(r.Vrsta, vrstaplate))
            .ToList();

        if (noviRadnici.Count == 0)
        {
            MessageBox.Show("Svi aktivni radnici su već u obračunu.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!ConfirmDialog.Pitaj(
            $"Dodati {noviRadnici.Count} radnika u obračun za mesec {_parametar.Mesec}/{_parametar.Godina}?",
            "Unos radnika")) return;

        var kreditMapa = UcitajMapuKreditnihRata();

        foreach (var r in noviRadnici)
        {
            kreditMapa.TryGetValue(r.Broj, out var zbir);
            var akontacionaIsplata = JeAkontacionaIsplata(_parametar.Isplata);

            Stavke.Add(new LdObracunStavka
            {
                Broj = r.Broj,
                ImePrez = r.ImePrezime,
                Maticnibr = r.MaticniBroj,
                Idbroj = r.IdBroj,
                Evidbroj = r.EvidencijskiBroj,
                Grupa = r.Grupa,
                Grupa1 = r.Grupa1,
                Mtr = r.Mtr,
                Mesec = _parametar.Mesec,
                Godina = _parametar.Godina,
                Isplata = _parametar.Isplata,
                Casuc = _parametar.Czakon,
                Startbod = r.StartniBodovi,
                Cenarada = _parametar.Cenarada,
                Benproc = r.BeneficiraniProcenat,
                Stim1proc = r.Stimulacija1,
                Stim2proc = r.Stimulacija2,
                Stim3proc = r.Stimulacija3,
                Porumanj = r.PorskoUmanjenje,
                Dopumanj = r.DoprinosnoUmanjenje,
                Doposlob = _parametar.Neoporez,
                Pioumanjr = r.PioUmanjenjeRadnik,
                Pioumanjf = r.PioUmanjenjeFirma,
                Krediti = akontacionaIsplata ? 0m : zbir.AktivnaRata,
                Kreditia = akontacionaIsplata ? zbir.AkontRata : 0m
            });
        }

        if (!SacuvajIliVratiStanje())
            return;
        OsveziGrid();
        AzurirajSumarno();
    }

    // ══════════════════════════════════════════════════════════════════
    //  UNOS ČASOVA
    // ══════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void UnosCasova()
    {
        if (_parametar == null)
        {
            Toast.Pokazi("Parametri zarada nisu podešeni.", ToastTip.Upozorenje);
            return;
        }

        var sveStavke = Stavke.OrderBy(s => s.Broj).ToList();
        if (sveStavke.Count == 0)
        {
            Toast.Pokazi("Nema radnika u obračunu.", ToastTip.Upozorenje);
            return;
        }

        var selektovani = Selektovana;
        var predlozak = selektovani != null ? sveStavke.FirstOrDefault(s => s.Broj == selektovani.Broj) : sveStavke[0];
        if (predlozak == null) return;

        var vm = new UnosCasovaViewModel(predlozak, _parametar, $"SVI RADNICI ({sveStavke.Count})");
        var view = new Views.Zarade.UnosCasovaView { DataContext = vm };
        if (view.ShowDialog() != true) return;

        var odlukaMasovnogUnosa = MessageBox.Show(
            $"Primeniti unete casove i dodatke na svih {sveStavke.Count} radnika?\n\nDa = svi radnici\nNe = samo izabrani radnik\nOtkazi = bez izmene",
            "Masovni unos casova",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (odlukaMasovnogUnosa == MessageBoxResult.Cancel)
            return;

        if (odlukaMasovnogUnosa == MessageBoxResult.Yes)
        {
            var poljaZaKopiranje = typeof(LdObracunStavka).GetProperties()
                .Where(p => p.CanRead && p.CanWrite && (
                    p.Name.StartsWith("Cas", StringComparison.OrdinalIgnoreCase) ||
                    p.Name is "Cslput" or "Topli" or "Regres" or "Terenski" or "Fiksna" or "Prevoz" or "Stim1proc" or "Stim2proc" or "Stim3proc"))
                .ToList();

            foreach (var stavka in sveStavke)
            {
                if (stavka.Broj == predlozak.Broj) continue;
                foreach (var polje in poljaZaKopiranje)
                    polje.SetValue(stavka, polje.GetValue(predlozak));
            }
        }

        if (!SacuvajIliVratiStanje())
            return;
        OsveziGrid();
        AzurirajSumarno();
        if (selektovani != null)
            Selektovana = Stavke.FirstOrDefault(s => s.Broj == selektovani.Broj);

        var porukaUspeha = odlukaMasovnogUnosa == MessageBoxResult.Yes
            ? $"Casovi su azurirani za {sveStavke.Count} radnika."
            : "Casovi su azurirani za izabranog radnika.";
        Toast.Pokazi(porukaUspeha, ToastTip.Uspeh);
    }

    // ══════════════════════════════════════════════════════════════════
    //  OBRAČUN
    // ══════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void ObracunBruto()
    {
        if (!ProveriPodatkePreKoraka("obracun BRUTO", strogaProveraIsplate: false))
            return;

        if (_parametar == null || Stavke.Count == 0)
        {
            Toast.Pokazi("Nema podataka za obračun.", ToastTip.Upozorenje);
            return;
        }

        var dlgVm = new ObracunBrutoViewModel(_parametar);
        var dlg = new Views.Zarade.ObracunBrutoView { DataContext = dlgVm };
        if (dlg.ShowDialog() != true || !dlgVm.Potvrdjen) return;

        _parametar.Cenarada = dlgVm.CenaRada;

        int obracunato = 0;
        foreach (var s in Stavke)
        {
            if (dlgVm.Grupa != 0 && s.Grupa != dlgVm.Grupa) continue;
            if (s.Casuc == 0 && s.Dinvr == 0 && s.Casbol == 0) continue;

            var radnik = _sviRadnici.FirstOrDefault(r => r.Broj == s.Broj);
            if (radnik == null) continue;

            _obracunService.Obracunaj(s, radnik, _parametar, dlgVm.ObracunatiNaknade, dlgVm.ObracunatiOdUkupneObaveze);
            obracunato++;
        }

        if (!SacuvajIliVratiStanje())
            return;
        OsveziGrid();
        AzurirajSumarno();
        Toast.Pokazi($"Obračun završen za {obracunato} radnika.", ToastTip.Uspeh);
    }

    [RelayCommand]
    private void ObracunSve() => ObracunBruto();

    [RelayCommand]
    private void ObracunNeto() => ObracunNetoInterno(stari: false);

    [RelayCommand]
    private void ObracunNetoStari() => ObracunNetoInterno(stari: true);

    private void ObracunNetoInterno(bool stari)
    {
        var nazivKoraka = stari ? "obracun NETO (stari)" : "obracun NETO";
        if (!ProveriPodatkePreKoraka(nazivKoraka, strogaProveraIsplate: false))
            return;

        if (_parametar == null || Stavke.Count == 0)
        {
            Toast.Pokazi("Nema podataka za obračun.", ToastTip.Upozorenje);
            return;
        }

        var dlgVm = new ObracunBrutoViewModel(_parametar);
        var dlg = new Views.Zarade.ObracunBrutoView { DataContext = dlgVm };
        if (dlg.ShowDialog() != true || !dlgVm.Potvrdjen) return;

        _parametar.Cenarada = dlgVm.CenaRada;

        int obracunato = 0;
        foreach (var s in Stavke)
        {
            if (dlgVm.Grupa != 0 && s.Grupa != dlgVm.Grupa) continue;
            if (s.Casuc == 0 && s.Dinvr == 0 && s.Casbol == 0) continue;

            var radnik = _sviRadnici.FirstOrDefault(r => r.Broj == s.Broj);
            if (radnik == null) continue;

            _obracunService.ObracunajOdNeta(s, radnik, _parametar, stari);
            obracunato++;
        }

        if (!SacuvajIliVratiStanje())
            return;
        OsveziGrid();
        AzurirajSumarno();
        var naslov = stari ? "Obračun NETO (stari)" : "Obračun NETO";
        Toast.Pokazi($"{naslov} završen za {obracunato} radnika.", ToastTip.Uspeh);
    }

    [RelayCommand]
    private void ProveraPreFinala()
    {
        var izvestaj = KreirajIzvestajProvere("provera pre finala", strogaProveraIsplate: true);
        var tekst = SastaviTekstIzvestaja(izvestaj);

        var icon = izvestaj.Greske.Count > 0
            ? MessageBoxImage.Warning
            : MessageBoxImage.Information;

        MessageBox.Show(tekst, "Provera pre finala", MessageBoxButton.OK, icon);

        var pitanjeIzvoza = MessageBox.Show(
            "Da li zelite izvoz izvestaja u CSV ili PDF?",
            "Provera pre finala",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (pitanjeIzvoza != MessageBoxResult.Yes)
            return;

        var poruka = IzveziIzvestajProvere(izvestaj);
        Toast.Pokazi(poruka, ToastTip.Uspeh);
    }

    [RelayCommand]
    private void ObracunJedan()
    {
        if (Selektovana == null || _parametar == null)
        {
            Toast.Pokazi("Izaberite radnika.", ToastTip.Upozorenje);
            return;
        }

        var radnik = _sviRadnici.FirstOrDefault(r => r.Broj == Selektovana.Broj);
        if (radnik == null) return;

        _obracunService.Obracunaj(Selektovana, radnik, _parametar, obracunatiNaknade: true);
        if (!SacuvajIliVratiStanje())
            return;
        OsveziGrid();
        AzurirajSumarno();
    }

    [RelayCommand]
    private void Prenosi()
    {
        var vm = new LdPrenosiViewModel(this);
        var view = new Views.Zarade.LdPrenosiView { DataContext = vm };

        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null)
            view.Owner = aktivniProzor;

        view.ShowDialog();
    }

    [RelayCommand]
    private void PrenosKredita()
    {
        var vm = new LdPrenosKreditaViewModel(this);
        var view = new Views.Zarade.LdPrenosKreditaView { DataContext = vm };

        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null)
            view.Owner = aktivniProzor;

        view.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriKredite()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            Toast.Pokazi("Folder aktivne firme nije pronađen.", ToastTip.Upozorenje);
            return;
        }

        var vm = new KreditiViewModel(folderPath, _parametar);
        var view = new Views.Zarade.KreditiView { DataContext = vm };

        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null)
            view.Owner = aktivniProzor;

        view.ShowDialog();
        UcitajPodatke();

        if (_parametar == null || Stavke.Count == 0)
            return;

        var zaAkontaciju = JeAkontacionaIsplata(_parametar.Isplata);
        var pitanje = zaAkontaciju
            ? "Da li zelite da odmah prenesete AKONTACIJE kredita u aktivni platni spisak?"
            : "Da li zelite da odmah prenesete KREDITE u aktivni platni spisak?";

        var odgovor = MessageBox.Show(
            pitanje,
            "Krediti -> Platni spisak",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (odgovor != MessageBoxResult.Yes)
            return;

        var rezultatPrenosa = IzvrsiPrenosKredita(zaAkontaciju);
        Toast.Pokazi(rezultatPrenosa, ToastTip.Info);
    }

    // ══════════════════════════════════════════════════════════════════
    //  DODAJ JEDNOG RADNIKA PO SIFRI
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fox DODAJ dugme u LD.SCX: korisnik unosi sifru (BROJ) radnika i taj radnik
    /// se dodaje u tekuci obracun ako vec nije unutra.
    /// </summary>
    [RelayCommand]
    private void DodajRadnikaPoSifri()
    {
        if (_parametar == null)
        {
            MessageBox.Show("Parametri zarada nisu podeseni.", "Dodaj radnika",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var input = PitajTekst("Unesite šifru (broj) radnika:", "Dodaj radnika");
        if (string.IsNullOrWhiteSpace(input)) return;
        if (!int.TryParse(input.Trim(), out var trazeniiBroj) || trazeniiBroj <= 0)
        {
            MessageBox.Show("Neispravna šifra radnika.", "Dodaj radnika",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Stavke.Any(s => s.Broj == trazeniiBroj))
        {
            MessageBox.Show($"Radnik sa šifrom {trazeniiBroj} je već u obračunu.", "Dodaj radnika",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var radnik = _sviRadnici.FirstOrDefault(r => r.Broj == trazeniiBroj);
        if (radnik == null)
        {
            MessageBox.Show($"Radnik sa šifrom {trazeniiBroj} nije pronađen u evidenciji (LDRAD).", "Dodaj radnika",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (radnik.Brisanje == "D" || radnik.Neaktivan == "D")
        {
            MessageBox.Show($"Radnik {radnik.ImePrezime} je obrisan/neaktivan — ne može u obračun.", "Dodaj radnika",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var kreditMapa = UcitajMapuKreditnihRata();
        kreditMapa.TryGetValue(radnik.Broj, out var zbir);
        var akontacionaIsplata = JeAkontacionaIsplata(_parametar.Isplata);

        Stavke.Add(new LdObracunStavka
        {
            Broj = radnik.Broj,
            ImePrez = radnik.ImePrezime,
            Maticnibr = radnik.MaticniBroj,
            Idbroj = radnik.IdBroj,
            Evidbroj = radnik.EvidencijskiBroj,
            Grupa = radnik.Grupa,
            Grupa1 = radnik.Grupa1,
            Mtr = radnik.Mtr,
            Mesec = _parametar.Mesec,
            Godina = _parametar.Godina,
            Isplata = _parametar.Isplata,
            Casuc = _parametar.Czakon,
            Startbod = radnik.StartniBodovi,
            Cenarada = _parametar.Cenarada,
            Benproc = radnik.BeneficiraniProcenat,
            Stim1proc = radnik.Stimulacija1,
            Stim2proc = radnik.Stimulacija2,
            Stim3proc = radnik.Stimulacija3,
            Porumanj = radnik.PorskoUmanjenje,
            Dopumanj = radnik.DoprinosnoUmanjenje,
            Doposlob = _parametar.Neoporez,
            Pioumanjr = radnik.PioUmanjenjeRadnik,
            Pioumanjf = radnik.PioUmanjenjeFirma,
            Krediti = akontacionaIsplata ? 0m : zbir.AktivnaRata,
            Kreditia = akontacionaIsplata ? zbir.AkontRata : 0m
        });

        if (!SacuvajIliVratiStanje()) return;
        OsveziGrid();
        AzurirajSumarno();
        Selektovana = Stavke.FirstOrDefault(s => s.Broj == trazeniiBroj);
        MessageBox.Show($"Radnik {radnik.ImePrezime} (br. {radnik.Broj}) dodat u obračun.", "Dodaj radnika",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    internal string IzvrsiPrenosKredita(bool zaAkontaciju)
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return "Folder aktivne firme nije pronađen.";

        if (KreditiDbfSupport.PronadjiDbf(folderPath, "ldkred.dbf") == null)
            return "Tabela kredita nije pronađena (ldkred.dbf).";

        List<KreditStavka> krediti;
        try
        {
            krediti = KreditiDbfSupport.UcitajKredite(folderPath);
        }
        catch (Exception ex)
        {
            return $"Greska pri citanju ldkred.dbf: {ex.Message}";
        }

        // Fox logika: samo krediti sa ZAODBITAK='*' (aktivni), identično ldprenkred.scx
        var zbirPoBroju = krediti
            .Where(k => k.Broj > 0 && k.ZaObaviti.Trim() == "*")
            .GroupBy(k => k.Broj)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(k => zaAkontaciju ? k.AkontRata : k.AktivnaRata));

        var stavkeZaPrenos = Stavke.Where(PripadaAktivnomObracunu).ToList();
        if (stavkeZaPrenos.Count == 0)
        {
            var period = _parametar == null
                ? "tekuceg obracuna"
                : $"mesec {_parametar.Mesec}, godina {_parametar.Godina}, isplata {_parametar.Isplata}";
            return $"Nema stavki za prenos kredita u periodu {period}.";
        }

        int azuriranoPolja = 0;
        int azuriranoObustave = 0;
        int preskoceno = Stavke.Count - stavkeZaPrenos.Count;
        int mdec = string.IsNullOrWhiteSpace(_parametar?.Decimale) ? 0 : 2;

        foreach (var stavka in stavkeZaPrenos)
        {
            var iznos = zbirPoBroju.TryGetValue(stavka.Broj, out var sumirano) ? sumirano : 0m;
            var staraUkobust = stavka.Ukobust;
            var staraZaIsplatu = stavka.Zaisplatu;
            var akontacionaIsplata = _parametar != null && JeAkontacionaIsplata(_parametar.Isplata);

            if (zaAkontaciju)
            {
                if (stavka.Kreditia != iznos) { stavka.Kreditia = iznos; azuriranoPolja++; }
                if (akontacionaIsplata && stavka.Krediti != 0m) { stavka.Krediti = 0m; azuriranoPolja++; }
            }
            else
            {
                if (stavka.Krediti != iznos) { stavka.Krediti = iznos; azuriranoPolja++; }
                if (!akontacionaIsplata && stavka.Kreditia != 0m) { stavka.Kreditia = 0m; azuriranoPolja++; }
            }

            ReizracunajObustave(stavka, mdec);
            if (stavka.Ukobust != staraUkobust || stavka.Zaisplatu != staraZaIsplatu)
                azuriranoObustave++;
        }

        if (!SacuvajIliVratiStanje())
            return _lastSaveError.Length > 0 ? _lastSaveError : "Snimanje nije uspelo.";
        OsveziGrid();
        AzurirajSumarno();

        return zaAkontaciju
            ? $"PRENOS KREDITA ZA AKONTACIJU JE IZVRSEN. Azurirano polja: {azuriranoPolja}. Reizracunate obustave: {azuriranoObustave}. Preskoceno van tekuceg perioda: {preskoceno}."
            : $"PRENOS KREDITA JE IZVRSEN. Azurirano polja: {azuriranoPolja}. Reizracunate obustave: {azuriranoObustave}. Preskoceno van tekuceg perioda: {preskoceno}.";
    }

    /// <summary>
    /// Fox ldtopli.scx / ldtop.prg logika:
    /// Upisuje isti iznos toplog obroka i/ili regresa svim radnicima u tekućem periodu.
    /// </summary>
    internal string IzvrsiTopliObrokPrenosInternal(decimal topliIznos, decimal regresIznos)
    {
        var stavkeZaPrenos = Stavke.Where(PripadaAktivnomObracunu).ToList();
        if (stavkeZaPrenos.Count == 0)
            return "Nema radnika u tekucem periodu za prenos.";

        int azuriranoTopli = 0, azuriranoRegres = 0;

        foreach (var stavka in stavkeZaPrenos)
        {
            if (topliIznos != 0m && stavka.Topli != topliIznos)
            {
                stavka.Topli = topliIznos;
                azuriranoTopli++;
            }

            if (regresIznos != 0m && stavka.Regres != regresIznos)
            {
                stavka.Regres = regresIznos;
                azuriranoRegres++;
            }
        }

        if (!SacuvajIliVratiStanje())
            return _lastSaveError.Length > 0 ? _lastSaveError : "Snimanje nije uspelo.";
        OsveziGrid();
        AzurirajSumarno();

        var delovi = new List<string>();
        if (azuriranoTopli > 0) delovi.Add($"Topli obrok: {azuriranoTopli} radnika ({topliIznos:N2})");
        if (azuriranoRegres > 0) delovi.Add($"Regres: {azuriranoRegres} radnika ({regresIznos:N2})");
        return delovi.Count > 0 ? string.Join(", ", delovi) + "." : "Vrednosti su vec postavljene.";
    }

    /// <summary>
    /// Fox ldpopunikol.scx logika — F8:
    /// Popunjava navedenu kolonu istom vrednošću za sve radnike u tekućem periodu.
    /// </summary>
    [RelayCommand]
    private void PopuniKolonu()
    {
        var vm = new LdPopuniKolonuViewModel();
        var view = new Views.Zarade.LdPopuniKolonuView { DataContext = vm };

        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null)
            view.Owner = aktivniProzor;

        view.ShowDialog();

        if (!vm.Potvrdjeno) return;

        var stavkeZaPrenos = Stavke.Where(PripadaAktivnomObracunu).ToList();
        if (stavkeZaPrenos.Count == 0)
        {
            StatusInfo = "Nema radnika u tekucem periodu.";
            return;
        }

        var kolona = vm.IzabranaKolona.ToUpperInvariant();
        var vrednost = vm.Vrednost;
        int azurirano = 0;

        var prop = typeof(LdObracunStavka).GetProperties()
            .FirstOrDefault(p => p.CanRead && p.CanWrite &&
                string.Equals(p.Name, kolona, StringComparison.OrdinalIgnoreCase));

        if (prop == null)
        {
            StatusInfo = $"Kolona '{kolona}' nije pronađena.";
            return;
        }

        foreach (var stavka in stavkeZaPrenos)
        {
            prop.SetValue(stavka, vrednost);
            azurirano++;
        }

        if (!SacuvajIliVratiStanje())
            return;
        OsveziGrid();
        AzurirajSumarno();
        StatusInfo = $"Kolona {kolona} = {vrednost:N2} upisana za {azurirano} radnika.";
    }

    private static string? PitajTekst(string poruka, string naslov)
    {
        var polje = new System.Windows.Controls.TextBox
        {
            Width = 220, Margin = new Thickness(0, 0, 0, 10),
            FontFamily = new System.Windows.Media.FontFamily("Tahoma"), FontSize = 12
        };
        var ok = new System.Windows.Controls.Button
        {
            Content = "U REDU", IsDefault = true, Width = 80, Margin = new Thickness(0, 0, 6, 0),
            FontFamily = new System.Windows.Media.FontFamily("Tahoma")
        };
        var otk = new System.Windows.Controls.Button
        {
            Content = "Otkaži", IsCancel = true, Width = 80,
            FontFamily = new System.Windows.Media.FontFamily("Tahoma")
        };
        var btns = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        btns.Children.Add(ok);
        btns.Children.Add(otk);
        var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(14) };
        sp.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = poruka, Margin = new Thickness(0, 0, 0, 8),
            FontFamily = new System.Windows.Media.FontFamily("Tahoma"), FontSize = 12, TextWrapping = System.Windows.TextWrapping.Wrap
        });
        sp.Children.Add(polje);
        sp.Children.Add(btns);
        var dlg = new Window { Title = naslov, Content = sp, SizeToContent = SizeToContent.WidthAndHeight, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        string? rezultat = null;
        ok.Click += (_, _) => { rezultat = polje.Text; dlg.DialogResult = true; };
        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null) dlg.Owner = aktivniProzor;
        dlg.ShowDialog();
        return rezultat;
    }

    private bool PripadaAktivnomObracunu(LdObracunStavka stavka)
    {
        if (_parametar == null) return true;
        if (stavka.Mesec != _parametar.Mesec) return false;

        var trazenaGodina = (_parametar.Godina ?? string.Empty).Trim();
        var godinaStavke = (stavka.Godina ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(trazenaGodina) && !string.IsNullOrWhiteSpace(godinaStavke) &&
            !godinaStavke.Equals(trazenaGodina, StringComparison.OrdinalIgnoreCase))
            return false;

        if (_parametar.Isplata > 0 && stavka.Isplata > 0 && stavka.Isplata != _parametar.Isplata)
            return false;

        return true;
    }
    private bool ProveriPodatkePreKoraka(string nazivKoraka, bool strogaProveraIsplate)
    {
        var izvestaj = KreirajIzvestajProvere(nazivKoraka, strogaProveraIsplate);
        if (izvestaj.Greske.Count > 0)
        {
            MessageBox.Show(
                SastaviTekstIzvestaja(izvestaj),
                "Provera podataka",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        if (izvestaj.Upozorenja.Count == 0)
            return true;

        var potvrda = MessageBox.Show(
            $"{SastaviTekstIzvestaja(izvestaj)}\n\nNastaviti ipak?",
            "Provera podataka",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return potvrda == MessageBoxResult.Yes;
    }

    private ProveraPodatakaIzvestaj KreirajIzvestajProvere(string nazivKoraka, bool strogaProveraIsplate)
    {
        var izvestaj = new ProveraPodatakaIzvestaj(nazivKoraka);

        if (_parametar == null || Stavke.Count == 0)
        {
            izvestaj.Greske.Add("Nema podataka za obradu.");
            return izvestaj;
        }

        var aktivneStavke = Stavke.Where(PripadaAktivnomObracunu).ToList();
        if (aktivneStavke.Count == 0)
        {
            izvestaj.Greske.Add(
                $"Nema stavki za aktivni period (mesec {_parametar.Mesec}, godina {_parametar.Godina}, isplata {_parametar.Isplata}).");
            return izvestaj;
        }

        izvestaj.Info.Add($"Aktivne stavke: {aktivneStavke.Count}.");

        if (aktivneStavke.All(s => s.Arhiva == "*"))
        {
            izvestaj.Greske.Add("Tekuci obracun je arhiviran. Uradite Dearhiviranje pre novog obracuna.");
            return izvestaj;
        }

        var radniciPoBroju = _sviRadnici
            .GroupBy(r => r.Broj)
            .ToDictionary(g => g.Key, g => g.First());

        var bezSifreRadnika = aktivneStavke
            .Where(s => !radniciPoBroju.ContainsKey(s.Broj))
            .Select(s => s.Broj)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (bezSifreRadnika.Count > 0)
            izvestaj.Greske.Add($"Nedostaju podaci u LDRAD za sifre: {FormatirajSifre(bezSifreRadnika)}.");

        if (!strogaProveraIsplate)
            return izvestaj;

        var stavkeZaIsplatu = aktivneStavke
            .Where(s => s.Zaisplatu != 0m || s.Neto != 0m || s.Bruto != 0m || s.Casuk != 0m || s.Casuc != 0m || s.Casbol != 0m)
            .ToList();

        izvestaj.Info.Add($"Stavke sa iznosom za proveru isplate: {stavkeZaIsplatu.Count}.");

        DodajMatematickeProvere(izvestaj, stavkeZaIsplatu);

        var bezMaticnog = stavkeZaIsplatu
            .Where(s => string.IsNullOrWhiteSpace(s.Maticnibr))
            .Select(s => s.Broj)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        if (bezMaticnog.Count > 0)
            izvestaj.Greske.Add($"Nedostaje MATICNIBR za radnike: {FormatirajSifre(bezMaticnog)}.");

        var neispravanMaticni = stavkeZaIsplatu
            .Where(s => !string.IsNullOrWhiteSpace(s.Maticnibr) && !Ima13Cifara(s.Maticnibr))
            .Select(s => s.Broj)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        if (neispravanMaticni.Count > 0)
            izvestaj.Upozorenja.Add($"Maticni broj nije 13 cifara za radnike: {FormatirajSifre(neispravanMaticni)}.");

        var bezPartije = stavkeZaIsplatu
            .Where(s => radniciPoBroju.TryGetValue(s.Broj, out var r) && string.IsNullOrWhiteSpace(r.Partija))
            .Select(s => s.Broj)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        if (bezPartije.Count > 0)
            izvestaj.Greske.Add($"Nedostaje PARTIJA u LDRAD za radnike: {FormatirajSifre(bezPartije)}.");

        var bezZiroRacuna = stavkeZaIsplatu
            .Where(s => radniciPoBroju.TryGetValue(s.Broj, out var r) && string.IsNullOrWhiteSpace(r.ZiroRacun))
            .Select(s => s.Broj)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        if (bezZiroRacuna.Count > 0)
            izvestaj.Greske.Add($"Nedostaje ZIRORAC u LDRAD za radnike: {FormatirajSifre(bezZiroRacuna)}.");

        return izvestaj;
    }

    private void DodajMatematickeProvere(ProveraPodatakaIzvestaj izvestaj, List<LdObracunStavka> stavkeZaIsplatu)
    {
        if (stavkeZaIsplatu.Count == 0)
            return;

        int mdec = string.IsNullOrWhiteSpace(_parametar?.Decimale) ? 0 : 2;

        DodajGreskuZaOdstupanja(
            izvestaj,
            stavkeZaIsplatu,
            s => s.Casuk,
            IzracunajCasukFox,
            "CASUK ne odgovara FOX SABERICAS zbiru za radnike");

        DodajGreskuZaOdstupanja(
            izvestaj,
            stavkeZaIsplatu,
            s => s.Dinuk,
            IzracunajDinukFox,
            "DINUK ne odgovara FOX SABERIDIN zbiru za radnike");

        DodajGreskuZaOdstupanja(
            izvestaj,
            stavkeZaIsplatu,
            s => s.Naknade,
            IzracunajNaknadeFox,
            "NAKNADE ne odgovaraju FOX SABERINAK zbiru za radnike");

        DodajGreskuZaOdstupanja(
            izvestaj,
            stavkeZaIsplatu,
            s => s.Ldodaci,
            IzracunajLdodaciFox,
            "LDODACI ne odgovaraju FOX SABERIDOD zbiru za radnike");

        DodajGreskuZaOdstupanja(
            izvestaj,
            stavkeZaIsplatu,
            s => s.Ukobust,
            IzracunajUkupneObustaveFox,
            "UKOBUST ne odgovara FOX SABERIOB zbiru za radnike");

        DodajGreskuZaOdstupanja(
            izvestaj,
            stavkeZaIsplatu,
            s => s.Zaisplatu,
            s => Math.Round(s.Neto - IzracunajUkupneObustaveFox(s) + s.Netoprev, mdec),
            "ZAISPLATU ne odgovara NETO - UKOBUST + NETOPREV za radnike");

        DodajGreskuZaOdstupanja(
            izvestaj,
            stavkeZaIsplatu,
            s => s.Netosve,
            s => s.Neto + s.Netoprev,
            "NETOSVE ne odgovara NETO + NETOPREV za radnike");
    }

    private decimal IzracunajCasukFox(LdObracunStavka s)
    {
        var casuk = s.Casuc + s.Caspraz + s.Casbol + s.Casbol2
                  + s.Casplac + s.Casplac2 + s.Casgod + s.Casdor + s.Cslput
                  + s.Cas1 + s.Cas2 + s.Cas3 + s.Casneplac;

        if (string.Equals((_parametar?.Nakpos ?? string.Empty).Trim(), "D", StringComparison.OrdinalIgnoreCase))
            casuk += s.Casnoc + s.Casprod + s.Casned;

        return casuk;
    }

    private static decimal IzracunajDinukFox(LdObracunStavka s)
        => s.Dinuc + s.Dinnoc + s.Dinpriprav + s.Dinprod + s.Dinradnap
         + s.Dinned + s.Dinpraz + s.Dinbol + s.Dinbol2
         + s.Dinplac + s.Dinplac2 + s.Dingod + s.Dindor + s.Dinsl
         + s.Din1 + s.Din2 + s.Din3 + s.Dinmin;

    private static decimal IzracunajNaknadeFox(LdObracunStavka s)
        => s.Dinpraz + s.Dinbol + s.Dinbol2 + s.Dinplac + s.Dinplac2 + s.Dingod;

    private static decimal IzracunajLdodaciFox(LdObracunStavka s)
        => s.Dinnoc + s.Dinpriprav + s.Dinprod + s.Dinradnap
         + s.Dinned + s.Dinmin + s.Topli + s.Regres + s.Terenski;

    private static decimal IzracunajUkupneObustaveFox(LdObracunStavka s)
        => s.Krediti + s.Kreditia + s.Akontac + s.Prevoz + s.Aliment
         + s.Kasa + s.Kasarata + s.Samodopr + s.Sindikat1 + s.Sindikat2
         + s.Solidarn + s.Obust1 + s.Obust2 + s.Obust3 + s.Obust4
         + s.Obustto + s.Solpor;

    private static void DodajGreskuZaOdstupanja(
        ProveraPodatakaIzvestaj izvestaj,
        IEnumerable<LdObracunStavka> stavke,
        Func<LdObracunStavka, decimal> stvarnaVrednost,
        Func<LdObracunStavka, decimal> ocekivanaVrednost,
        string poruka)
    {
        var sifre = stavke
            .Where(s => Odstupa(stvarnaVrednost(s), ocekivanaVrednost(s)))
            .Select(s => s.Broj)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (sifre.Count > 0)
            izvestaj.Greske.Add($"{poruka}: {FormatirajSifre(sifre)}.");
    }

    private static bool Odstupa(decimal stvarno, decimal ocekivano)
        => Math.Abs(stvarno - ocekivano) > 0.01m;

    private static string SastaviTekstIzvestaja(ProveraPodatakaIzvestaj izvestaj)
        => string.Join("\n", SastaviLinijeIzvestaja(izvestaj));

    private static List<string> SastaviLinijeIzvestaja(ProveraPodatakaIzvestaj izvestaj)
    {
        var linije = new List<string>
        {
            $"PROVERA: {izvestaj.NazivKoraka.ToUpperInvariant()}",
            $"Vreme: {DateTime.Now:dd.MM.yyyy HH:mm}",
            string.Empty
        };

        if (izvestaj.Info.Count > 0)
        {
            linije.Add("INFO:");
            linije.AddRange(izvestaj.Info.Select(i => $"- {i}"));
            linije.Add(string.Empty);
        }

        if (izvestaj.Greske.Count > 0)
        {
            linije.Add("GRESKE:");
            linije.AddRange(izvestaj.Greske.Select(g => $"- {g}"));
            linije.Add(string.Empty);
        }

        if (izvestaj.Upozorenja.Count > 0)
        {
            linije.Add("UPOZORENJA:");
            linije.AddRange(izvestaj.Upozorenja.Select(u => $"- {u}"));
            linije.Add(string.Empty);
        }

        var status = izvestaj.Greske.Count > 0
            ? "STATUS: NIJE SPREMNO ZA FINALIZACIJU."
            : izvestaj.Upozorenja.Count > 0
                ? "STATUS: SPREMNO UZ UPOZORENJA."
                : "STATUS: SPREMNO ZA FINALIZACIJU.";
        linije.Add(status);

        return linije;
    }

    private string IzveziIzvestajProvere(ProveraPodatakaIzvestaj izvestaj)
    {
        var suffix = _parametar != null
            ? $"{_parametar.Mesec}_{_parametar.Godina}_i{_parametar.Isplata}"
            : DateTime.Now.ToString("yyyyMMdd_HHmm");

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Sacuvaj izvestaj provere",
            Filter = "CSV fajl (*.csv)|*.csv|PDF fajl (*.pdf)|*.pdf|Tekst fajl (*.txt)|*.txt|Svi fajlovi (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = $"ProveraPreFinala_{suffix}"
        };

        if (dlg.ShowDialog() != true)
            return "Izvoz otkazan.";

        try
        {
            var ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
            if (ext == ".pdf")
            {
                Algoritam.WPF.Utilities.SimplePdfWriter.WriteTextPdf(
                    dlg.FileName,
                    "Provera pre finala",
                    SastaviLinijeIzvestaja(izvestaj));
            }
            else if (ext == ".csv")
            {
                File.WriteAllLines(
                    dlg.FileName,
                    SastaviCsvRedoveIzvestaja(izvestaj),
                    new System.Text.UTF8Encoding(true));
            }
            else
            {
                File.WriteAllLines(
                    dlg.FileName,
                    SastaviLinijeIzvestaja(izvestaj),
                    new System.Text.UTF8Encoding(true));
            }

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });

            return $"Izvestaj je sacuvan: {Path.GetFileName(dlg.FileName)}.";
        }
        catch (Exception ex)
        {
            return $"Greska pri izvozu izvestaja: {ex.Message}";
        }
    }

    private static IEnumerable<string> SastaviCsvRedoveIzvestaja(ProveraPodatakaIzvestaj izvestaj)
    {
        const string sep = ";";
        yield return string.Join(sep, "TIP", "PORUKA");
        yield return string.Join(sep, "KORAK", CsvEscape(izvestaj.NazivKoraka));

        foreach (var info in izvestaj.Info)
            yield return string.Join(sep, "INFO", CsvEscape(info));
        foreach (var greska in izvestaj.Greske)
            yield return string.Join(sep, "GRESKA", CsvEscape(greska));
        foreach (var upozorenje in izvestaj.Upozorenja)
            yield return string.Join(sep, "UPOZORENJE", CsvEscape(upozorenje));
    }

    private static bool Ima13Cifara(string? vrednost)
    {
        if (string.IsNullOrWhiteSpace(vrednost))
            return false;

        return vrednost.Count(char.IsDigit) == 13;
    }

    private static string FormatirajSifre(IEnumerable<int> sifre, int maxPrikaz = 12)
    {
        var lista = sifre.Distinct().OrderBy(x => x).Take(maxPrikaz + 1).ToList();
        if (lista.Count <= maxPrikaz)
            return string.Join(", ", lista);

        return $"{string.Join(", ", lista.Take(maxPrikaz))}, ...";
    }

    private Dictionary<int, KreditPrenosZbir> UcitajMapuKreditnihRata()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return [];

        if (KreditiDbfSupport.PronadjiDbf(folderPath, "ldkred.dbf") == null)
            return [];

        try
        {
            return KreditiDbfSupport.UcitajKredite(folderPath)
                .Where(k => k.Broj > 0
                            && k.ZaObaviti.Trim() == "*"
                            && k.Arhiva.Trim() != "*")
                .GroupBy(k => k.Broj)
                .ToDictionary(
                    g => g.Key,
                    g => new KreditPrenosZbir(g.Sum(x => x.AktivnaRata), g.Sum(x => x.AkontRata)));
        }
        catch
        {
            return [];
        }
    }

    private static void ReizracunajObustave(LdObracunStavka stavka, int mdec)
    {
        stavka.Ukobust = IzracunajUkupneObustaveFox(stavka);
        stavka.Zaisplatu = Math.Round(stavka.Neto - stavka.Ukobust + stavka.Netoprev, mdec);
    }

    // FoxPro logika: VRSTAPLATE u Parametri 2 određuje koji zaposleni ulaze u obračun.
    // VRSTA u LDRAD: blank=redovan, B=bolovanje>30, P=porodilje, R=penzioner, I=invalid, U=ugovor.
    private static bool VrstaRadnikaOdgovara(string vrstaRadnika, string vrstaplate)
    {
        var v = vrstaRadnika.Trim().ToUpperInvariant();
        return vrstaplate switch
        {
            "2" => v == "P",
            "3" => v == "B",
            "4" => v == "I",
            "5" => v == "R",
            "6" => v == "U",
            _   => v == string.Empty
        };
    }

    private static bool JeAkontacionaIsplata(int isplata)
        => isplata == 2;

    [RelayCommand]
    private void JedanListic()
    {
        if (Selektovana == null)
        {
            Toast.Pokazi("Izaberite radnika za isplatni listić.", ToastTip.Upozorenje);
            return;
        }

        var radnik = _sviRadnici.FirstOrDefault(r => r.Broj == Selektovana.Broj);
        var view = new Views.Zarade.JedanListicReportView(
            Selektovana, radnik, _parametar, _appState.AktivnaFirma, DatumIsplate(_parametar));
        view.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriPreglede()
    {
        var view = new Views.Zarade.PlatniSpisakPreglediView { DataContext = this };
        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null) view.Owner = aktivniProzor;
        view.ShowDialog();
    }

    [RelayCommand]
    private void PregledJedanListic() => JedanListic();

    [RelayCommand]
    private void PregledSviListici()
    {
        if (Stavke.Count == 0)
        {
            Toast.Pokazi("Nema podataka za pregled listića.", ToastTip.Upozorenje);
            return;
        }

        var view = new Views.Zarade.SviListiciPregledView(this);
        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null) view.Owner = aktivniProzor;
        view.ShowDialog();
    }

    [RelayCommand]
    private void PregledNetoBrutoListic()
    {
        if (Selektovana == null)
        {
            Toast.Pokazi("Izaberite radnika za NETO-BRUTO listić.", ToastTip.Upozorenje);
            return;
        }

        var radnik = _sviRadnici.FirstOrDefault(r => r.Broj == Selektovana.Broj);
        var view = new Views.Zarade.ListicNetoBrutoView(
            Selektovana, radnik, _parametar, _appState.AktivnaFirma, DatumIsplate(_parametar));
        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null) view.Owner = aktivniProzor;
        view.ShowDialog();
    }

    [RelayCommand]
    private void PregledKolone()
    {
        if (Stavke.Count == 0)
        {
            Toast.Pokazi("Nema podataka za pregled kolone.", ToastTip.Upozorenje);
            return;
        }

        var view = new Views.Zarade.PregledKoloneView(Stavke);
        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null) view.Owner = aktivniProzor;
        view.ShowDialog();
    }

    [RelayCommand]
    private void OtvoriFoxPregled(string pregledSifra)
    {
        if (string.IsNullOrWhiteSpace(pregledSifra)) return;

        if (string.Equals(pregledSifra, "ZBIRNA_REKAPITULACIJA", StringComparison.OrdinalIgnoreCase))
        {
            OtvoriZbirnuRekapitulacijuFox();
            return;
        }

        if (Stavke.Count == 0)
        {
            Toast.Pokazi("Nema podataka za pregled.", ToastTip.Upozorenje);
            return;
        }

        FoxPregledDefinicija? def = pregledSifra switch
        {
            "SPISAK_1" => NapraviSpisakDef("SPISAK 1", "Neto po radniku", s => s.Neto),
            "SPISAK_2" => NapraviSpisakDef("SPISAK 2", "Bruto po radniku", s => s.Bruto),
            "SPISAK_3" => NapraviSpisakDef("SPISAK 3", "Za isplatu po radniku", s => s.Zaisplatu),
            "SPISAK_4" => NapraviSpisakDef("SPISAK 4", "Porez po radniku", s => s.Porez),
            "SPISAK_5" => NapraviSpisakDef("SPISAK 5", "Doprinosi radnika po radniku", s => s.Dopsocr),
            "SPISAK_6" => NapraviSpisakDef("SPISAK 6", "Doprinosi firme po radniku", s => s.Dopsocf),
            "SPISAK_7" => NapraviSpisakDef("SPISAK 7", "Ukupne obustave po radniku", s => s.Ukobust),
            "SPISAK_8" => NapraviSpisakDef("SPISAK 8", "Prevoz po radniku", s => s.Prevoz),
            "SPISAK_9" => NapraviSpisakDef("SPISAK 9", "Akontacija po radniku", s => s.Akontac),
            "SAMODOPRINOSI" => NapraviSamodoprinoseDef(),
            "PREGLED_MTR" => NapraviGrupniDef("PREGLED PO MESTIMA TROSKOVA", "MTR", s => s.Mtr.ToString(), s => s.Bruto, s => s.Neto),
            "PREGLED_GRUPE" => NapraviGrupniDef("PREGLED PO GRUPAMA", "GRUPA", s => s.Grupa.ToString(), s => s.Bruto, s => s.Zaisplatu),
            "MINI_REKAPITULACIJA" => NapraviMiniRekapDef(),
            "SALDO_MTR" => NapraviGrupniDef("SALDO PO MESTIMA TROSKOVA", "MTR", s => s.Mtr.ToString(), s => s.Zaisplatu, s => s.Ukobust),
            "SALDO_GRUPE" => NapraviGrupniDef("SALDO PO GRUPAMA", "GRUPA", s => s.Grupa.ToString(), s => s.Zaisplatu, s => s.Ukobust),
            "ZBIRNA_REK_OD_NETA" => NapraviZbirnaOdNetaDef(),
            "SALDO_OBJEKTI" => NapraviGrupniDef("SALDO PO OBJEKTIMA", "DOK", s => PraznoSifra(s.Dok), s => s.Zaisplatu, s => s.Bruto),
            "PREGLED_EVID_1" => NapraviEvidencijskiDef("PREGLED PO EVID.BROJU", s => s.Neto),
            "PREGLED_EVID_2" => NapraviEvidencijskiDef("PREGLED PO EVID.BROJU 2", s => s.Bruto),
            "PREGLED_EVID_3" => NapraviEvidencijskiDef("PREGLED PO EVID.BROJU 3", s => s.Zaisplatu),
            "PREGLED_GRUPE_POTPIS" => NapraviGrupePotpisDef(),
            "PREGLED_GRUPA_GRUPA1" => NapraviGrupaGrupa1Def(),
            "SOLIDARNI_POREZ" => NapraviSolidarniPorezDef(),
            _ => null
        };

        if (def == null || def.Stavke.Count == 0)
        {
            Toast.Pokazi("Nema podataka za izabrani pregled.", ToastTip.Upozorenje);
            return;
        }

        var view = new Views.Zarade.FoxPregledTabelaView(def.Naslov, def.Podnaslov, def.Stavke, def.LabelIznos1, def.LabelIznos2);
        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null) view.Owner = aktivniProzor;
        view.ShowDialog();
    }

    private void OtvoriZbirnuRekapitulacijuFox()
    {
        if (Stavke.Count == 0)
        {
            Toast.Pokazi("Nema podataka za zbirnu rekapitulaciju.", ToastTip.Upozorenje);
            return;
        }

        var view = new Views.Zarade.ZbirnaRekapitulacijaReportView(
            Stavke.ToList(), _parametar, _appState.AktivnaFirma, DatumIsplate(_parametar));
        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null) view.Owner = aktivniProzor;
        view.ShowDialog();
    }

    // ══════════════════════════════════════════════════════════════════
    //  KARTICA RADNIKA
    // ══════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void OtvoriKarticu()
    {
        if (Selektovana == null)
        {
            Toast.Pokazi("Izaberite radnika.", ToastTip.Upozorenje);
            return;
        }

        var radnik = _sviRadnici.FirstOrDefault(r => r.Broj == Selektovana.Broj);
        var vm = new KarticaRadnikaViewModel(Selektovana, radnik, _parametar);
        var view = new Views.Zarade.KarticaRadnikaView { DataContext = vm };

        if (view.ShowDialog() == true)
        {
            // Izmene su potvrdjene u dijalogu, sada snimamo i osvezavamo pregled.
            if (!SacuvajIliVratiStanje())
                return;
            OsveziGrid();
            AzurirajSumarno();
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  BRISANJE RADNIKA
    // ══════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void ObrisiRadnika()
    {
        if (Selektovana == null) return;

        if (!ConfirmDialog.Pitaj(
            $"Obrisati radnika {Selektovana.ImePrez} (br. {Selektovana.Broj}) iz obračuna?",
            "Brisanje")) return;

        var brisaniMesec = Selektovana.Mesec;
        var brisanaIsplata = Selektovana.Isplata;
        Stavke.Remove(Selektovana);
        Selektovana = Stavke.FirstOrDefault();

        if (Stavke.Count == 0)
        {
            // Fox ldbrisi.prg: ako je poslednji radnik → briši iz srodnih tabela za taj mesec/isplata
            if (!SacuvajIliVratiStanje()) return;
            OcistiSrodneTabelePerioda(brisaniMesec, brisanaIsplata);
        }
        else
        {
            if (!SacuvajIliVratiStanje()) return;
        }

        AzurirajSumarno();
    }

    /// <summary>
    /// Fox ldbrisi.prg logika za poslednjeg radnika:
    /// briše sve zapise za dati mesec/isplata iz ldpod, lmes, ldprev, lddd.
    /// </summary>
    private void OcistiSrodneTabelePerioda(int mesec, int isplata)
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder)) return;

        foreach (var naziv in new[] { "ldpod.dbf", "lmes.dbf", "ldprev.dbf", "lddd.dbf" })
        {
            var putanja = LdObracunDbfReader.PronadjiDbf(folder, naziv);
            if (putanja == null) continue;

            try
            {
                var zapisi = DbfReader.CitajSveZapise(putanja);
                var ostali = zapisi.Where(z =>
                {
                    var m = Int(z, "MESEC");
                    var i = Int(z, "ISPLATA");
                    return !(m == mesec && i == isplata);
                }).ToList();

                if (ostali.Count == zapisi.Count) continue; // nista za brisanje

                var schema = DbfTableWriter.LoadSchema(putanja);
                DbfTableWriter.WriteTable(putanja, schema, ostali,
                    static (r, field) => r.TryGetValue(field, out var v) ? v : null);
            }
            catch { /* tiho — sporedne tabele ne smeju da blokiraju */ }
        }
    }

    [RelayCommand]
    private async Task Osvezi() => await UcitajPodatkeAsync();

    // ══════════════════════════════════════════════════════════════════
    //  ARHIVA / DEARHIVA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fox ldarhiv.prg: postavlja ARHIVA='*' svim stavkama tekuceg perioda.
    /// Zaključava obračun — nakon arhiviranja nije moguće ponavljati obračun.
    /// </summary>
    [RelayCommand]
    private void Arhiviraj()
    {
        if (!ProveriPodatkePreKoraka("arhiviranje obracuna", strogaProveraIsplate: true))
            return;

        if (_parametar == null || Stavke.Count == 0)
        {
            MessageBox.Show("Nema podataka za arhiviranje.", "Arhiva",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var vec = Stavke.Where(PripadaAktivnomObracunu).All(s => s.Arhiva == "*");
        if (vec)
        {
            MessageBox.Show("Tekući obračun je već arhiviran.", "Arhiva",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!ConfirmDialog.Pitaj(
            $"Arhivirati obračun za mesec {_parametar.Mesec}/{_parametar.Godina}, isplata {_parametar.Isplata}?\n\n" +
            "Nakon arhiviranja obračun se smatra zaključanim.",
            "Arhiviranje")) return;

        foreach (var s in Stavke.Where(PripadaAktivnomObracunu))
            s.Arhiva = "*";

        if (!SacuvajIliVratiStanje()) return;
        OsveziGrid();
        StatusInfo = $"Obračun arhiviran — {Stavke.Count(PripadaAktivnomObracunu)} radnika.";
    }

    /// <summary>
    /// Fox lddearhiv.prg: briše ARHIVA='*' sa stavki tekuceg perioda.
    /// Otvara zaključan obračun za izmene.
    /// </summary>
    [RelayCommand]
    private void Dearhiviraj()
    {
        if (_parametar == null || Stavke.Count == 0)
        {
            MessageBox.Show("Nema podataka za dearhiviranje.", "Dearhiva",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var imaArhiviranih = Stavke.Where(PripadaAktivnomObracunu).Any(s => s.Arhiva == "*");
        if (!imaArhiviranih)
        {
            MessageBox.Show("Tekući obračun nije arhiviran.", "Dearhiva",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!ConfirmDialog.Pitaj(
            $"Dearhivirati obračun za mesec {_parametar.Mesec}/{_parametar.Godina}, isplata {_parametar.Isplata}?\n\n" +
            "Obračun će biti ponovo otvoren za izmene.",
            "Dearhiviranje")) return;

        foreach (var s in Stavke.Where(PripadaAktivnomObracunu))
            s.Arhiva = string.Empty;

        if (!SacuvajIliVratiStanje()) return;
        OsveziGrid();
        StatusInfo = "Obračun dearhiviran — otvoren za izmene.";
    }

    // ══════════════════════════════════════════════════════════════════
    //  PREGLED HELPERS
    // ══════════════════════════════════════════════════════════════════

    private FoxPregledDefinicija NapraviSpisakDef(string naslov, string podnaslov, Func<LdObracunStavka, decimal> selektor)
    {
        var redovi = Stavke.OrderBy(s => s.Broj)
            .Select(s => new PregledTabelaStavka { Sifra = s.Broj.ToString(), Naziv = PraznoTekst(s.ImePrez), Iznos1 = selektor(s), Iznos2 = s.Zaisplatu })
            .Where(r => r.Iznos1 != 0m || r.Iznos2 != 0m).ToList();
        return new FoxPregledDefinicija(naslov, podnaslov, redovi, "IZNOS", "ZA ISPLATU");
    }

    private FoxPregledDefinicija NapraviZbirnuRekapitulacijuDef()
    {
        var redovi = new List<PregledTabelaStavka>
        {
            RekapRed("01", "Bruto zarada", Stavke.Sum(s => s.Bruto), 0m),
            RekapRed("02", "Neto zarada", Stavke.Sum(s => s.Neto), 0m),
            RekapRed("03", "Za isplatu", Stavke.Sum(s => s.Zaisplatu), 0m),
            RekapRed("04", "Porez", Stavke.Sum(s => s.Porez), 0m),
            RekapRed("05", "Doprinosi radnika", Stavke.Sum(s => s.Dopsocr), 0m),
            RekapRed("06", "Doprinosi firme", Stavke.Sum(s => s.Dopsocf), 0m),
            RekapRed("07", "Ukupne obustave", Stavke.Sum(s => s.Ukobust), 0m)
        };
        return new FoxPregledDefinicija("ZBIRNA REKAPITULACIJA", "Zbirni prikaz kljucnih iznosa obracuna", redovi, "IZNOS", "REZERVA");
    }

    private FoxPregledDefinicija NapraviMiniRekapDef()
    {
        var redovi = new List<PregledTabelaStavka>
        {
            RekapRed("A", "Broj radnika", Stavke.Count, 0m),
            RekapRed("B", "Bruto", Stavke.Sum(s => s.Bruto), 0m),
            RekapRed("C", "Neto", Stavke.Sum(s => s.Neto), 0m),
            RekapRed("D", "Za isplatu", Stavke.Sum(s => s.Zaisplatu), 0m)
        };
        return new FoxPregledDefinicija("MINI REKAPITULACIJA", "Kratak zbirni prikaz", redovi, "VREDNOST", "REZERVA");
    }

    private FoxPregledDefinicija NapraviZbirnaOdNetaDef()
    {
        var neto = Stavke.Sum(s => s.Neto);
        var bruto = Stavke.Sum(s => s.Bruto);
        var koef = bruto == 0m ? 0m : neto / bruto * 100m;
        var redovi = new List<PregledTabelaStavka>
        {
            RekapRed("01", "Neto", neto, 0m),
            RekapRed("02", "Bruto", bruto, 0m),
            RekapRed("03", "Neto/Bruto (%)", koef, 0m),
            RekapRed("04", "Porez + dop. radnika", Stavke.Sum(s => s.Porez + s.Dopsocr), 0m)
        };
        return new FoxPregledDefinicija("ZBIRNA REKAPITULACIJA OD NETA", "Kontrolni neto/bruto odnos", redovi, "VREDNOST", "REZERVA");
    }

    private FoxPregledDefinicija NapraviSamodoprinoseDef()
    {
        var redovi = Stavke.Where(s => s.Samodopr != 0m).OrderBy(s => s.Broj)
            .Select(s => new PregledTabelaStavka { Sifra = s.Broj.ToString(), Naziv = PraznoTekst(s.ImePrez), Iznos1 = s.Samodopr, Iznos2 = s.Zaisplatu })
            .ToList();
        return new FoxPregledDefinicija("SAMODOPRINOSI", "Radnici sa obracunatim samodoprinosom", redovi, "SAMODOPRINOS", "ZA ISPLATU");
    }

    private FoxPregledDefinicija NapraviEvidencijskiDef(string naslov, Func<LdObracunStavka, decimal> selektor)
    {
        var redovi = Stavke.OrderBy(s => s.Evidbroj).ThenBy(s => s.Broj)
            .Select(s => new PregledTabelaStavka { Sifra = PraznoSifra(s.Evidbroj), Naziv = $"{s.Broj} - {PraznoTekst(s.ImePrez)}", Iznos1 = selektor(s), Iznos2 = s.Zaisplatu })
            .Where(r => r.Iznos1 != 0m || r.Iznos2 != 0m).ToList();
        return new FoxPregledDefinicija(naslov, "Prikaz po evidencionom broju", redovi, "IZNOS", "ZA ISPLATU");
    }

    private FoxPregledDefinicija NapraviGrupePotpisDef()
    {
        var redovi = Stavke.GroupBy(s => s.Grupa).OrderBy(g => g.Key)
            .Select(g => new PregledTabelaStavka { Sifra = g.Key.ToString(), Naziv = $"GRUPA {g.Key} - POTPIS LISTA", Iznos1 = g.Count(), Iznos2 = g.Sum(x => x.Zaisplatu) })
            .ToList();
        return new FoxPregledDefinicija("PREGLED PO GRUPAMA POTPIS", "Broj radnika i zbir za isplatu po grupama", redovi, "BROJ RADNIKA", "ZA ISPLATU");
    }

    private FoxPregledDefinicija NapraviGrupaGrupa1Def()
    {
        var redovi = Stavke.GroupBy(s => new { s.Grupa, s.Grupa1 }).OrderBy(g => g.Key.Grupa).ThenBy(g => g.Key.Grupa1)
            .Select(g => new PregledTabelaStavka { Sifra = $"{g.Key.Grupa}/{g.Key.Grupa1}", Naziv = $"GRUPA {g.Key.Grupa} - GRUPA1 {g.Key.Grupa1}", Iznos1 = g.Sum(x => x.Bruto), Iznos2 = g.Sum(x => x.Zaisplatu) })
            .ToList();
        return new FoxPregledDefinicija("PREGLED PO GRUPI I GRUPI 1", "Zbirni prikaz po grupama", redovi, "BRUTO", "ZA ISPLATU");
    }

    private FoxPregledDefinicija NapraviSolidarniPorezDef()
    {
        var redovi = Stavke.Where(s => s.Solpor != 0m).OrderBy(s => s.Broj)
            .Select(s => new PregledTabelaStavka { Sifra = s.Broj.ToString(), Naziv = PraznoTekst(s.ImePrez), Iznos1 = s.Solpor, Iznos2 = s.Neto2 })
            .ToList();
        return new FoxPregledDefinicija("SOLIDARNI POREZ", "Prikaz obracunatog solidarnog poreza", redovi, "SOLIDARNI POREZ", "NETO 2");
    }

    private FoxPregledDefinicija NapraviGrupniDef(
        string naslov, string podnaslovPrefix,
        Func<LdObracunStavka, string> kljuc,
        Func<LdObracunStavka, decimal> iznos1,
        Func<LdObracunStavka, decimal> iznos2)
    {
        var redovi = Stavke.GroupBy(kljuc).OrderBy(g => g.Key)
            .Select(g => new PregledTabelaStavka { Sifra = PraznoSifra(g.Key), Naziv = $"{podnaslovPrefix} {PraznoSifra(g.Key)}", Iznos1 = g.Sum(iznos1), Iznos2 = g.Sum(iznos2) })
            .ToList();
        return new FoxPregledDefinicija(naslov, "Grupni zbirni prikaz", redovi, "IZNOS 1", "IZNOS 2");
    }

    private static PregledTabelaStavka RekapRed(string sifra, string naziv, decimal iznos1, decimal iznos2)
        => new() { Sifra = sifra, Naziv = naziv, Iznos1 = iznos1, Iznos2 = iznos2 };

    private static string PraznoTekst(string? tekst)
        => string.IsNullOrWhiteSpace(tekst) ? "-" : tekst.Trim();

    private static string PraznoSifra(string? sifra)
        => string.IsNullOrWhiteSpace(sifra) ? "-" : sifra.Trim();

    private sealed record FoxPregledDefinicija(
        string Naslov, string Podnaslov,
        List<PregledTabelaStavka> Stavke,
        string LabelIznos1, string LabelIznos2);

    // ══════════════════════════════════════════════════════════════════
    //  DBF MAPPING
    // ══════════════════════════════════════════════════════════════════

    private static LdObracunStavka MapirajZapis(Dictionary<string, object?> z)
    {
        return new LdObracunStavka
        {
            Broj = Int(z, "BROJ"), Sifraprih = Str(z, "SIFRAPRIH"), ImePrez = Str(z, "IME_PREZ"),
            Evidbroj = Str(z, "EVIDBROJ"), Maticnibr = Str(z, "MATICNIBR"), Idbroj = Str(z, "IDBROJ"),
            Dok = Str(z, "DOK"), Grupa = Int(z, "GRUPA"), Grupa1 = Int(z, "GRUPA1"), Mtr = Int(z, "MTR"),
            Mesec = Int(z, "MESEC"), Nazmes = Str(z, "NAZMES"), Godina = Str(z, "GODINA"),
            Casvr = Dec(z, "CASVR"), Casuc = Dec(z, "CASUC"), Casnoc = Dec(z, "CASNOC"),
            Casprod = Dec(z, "CASPROD"), Casradnap = Dec(z, "CASRADNAP"), Casned = Dec(z, "CASNED"),
            Casdor = Dec(z, "CASDOR"), Cslput = Dec(z, "CSLPUT"), Caspraz = Dec(z, "CASPRAZ"),
            Casbol = Dec(z, "CASBOL"), Casbol2 = Dec(z, "CASBOL2"), Casplac = Dec(z, "CASPLAC"),
            Casplac2 = Dec(z, "CASPLAC2"), Casgod = Dec(z, "CASGOD"), Casvv = Dec(z, "CASVV"),
            Cas1 = Dec(z, "CAS1"), Cas2 = Dec(z, "CAS2"), Cas3 = Dec(z, "CAS3"),
            Cassus = Dec(z, "CASSUS"), Casneplac = Dec(z, "CASNEPLAC"), Caspriprav = Dec(z, "CASPRIPRAV"),
            Casuk = Dec(z, "CASUK"), Dinvr = Dec(z, "DINVR"), Dinuc = Dec(z, "DINUC"),
            Dinnoc = Dec(z, "DINNOC"), Dinprod = Dec(z, "DINPROD"), Dinradnap = Dec(z, "DINRADNAP"),
            Dinned = Dec(z, "DINNED"), Dindor = Dec(z, "DINDOR"), Dinsl = Dec(z, "DINSL"),
            Dinpraz = Dec(z, "DINPRAZ"), Dinbol = Dec(z, "DINBOL"), Dinbol2 = Dec(z, "DINBOL2"),
            Dinplac = Dec(z, "DINPLAC"), Dinplac2 = Dec(z, "DINPLAC2"), Dingod = Dec(z, "DINGOD"),
            Dinvv = Dec(z, "DINVV"), Din1 = Dec(z, "DIN1"), Din2 = Dec(z, "DIN2"), Din3 = Dec(z, "DIN3"),
            Dinsus = Dec(z, "DINSUS"), Dinmin = Dec(z, "DINMIN"), Dinuk = Dec(z, "DINUK"),
            Dinpriprav = Dec(z, "DINPRIPRAV"), Stim1 = Dec(z, "STIM1"), Stim2 = Dec(z, "STIM2"),
            Stim3 = Dec(z, "STIM3"), Stim1proc = Dec(z, "STIM1PROC"), Stim2proc = Dec(z, "STIM2PROC"),
            Stim3proc = Dec(z, "STIM3PROC"), Topli = Dec(z, "TOPLI"), Regres = Dec(z, "REGRES"),
            Terenski = Dec(z, "TERENSKI"), Fiksna = Dec(z, "FIKSNA"), Dotacija = Dec(z, "DOTACIJA"),
            Ldodaci = Dec(z, "LDODACI"), Naknade = Dec(z, "NAKNADE"), Bruto = Dec(z, "BRUTO"),
            Neto = Dec(z, "NETO"), Neto2 = Dec(z, "NETO2"), Netosve = Dec(z, "NETOSVE"),
            Netoprev = Dec(z, "NETOPREV"), Netoost = Dec(z, "NETOOST"), Cenarada = Dec(z, "CENARADA"),
            Startbod = Dec(z, "STARTBOD"), Dopsocr = Dec(z, "DOPSOCR"), Dopsocf = Dec(z, "DOPSOCF"),
            Doppr = Dec(z, "DOPPR"), Dopzr = Dec(z, "DOPZR"), Dopnr = Dec(z, "DOPNR"),
            Doppf = Dec(z, "DOPPF"), Dopzf = Dec(z, "DOPZF"), Dopnf = Dec(z, "DOPNF"),
            Doppru = Dec(z, "DOPPRU"), Doppfu = Dec(z, "DOPPFU"), Dopzfu = Dec(z, "DOPZFU"),
            Dopnfu = Dec(z, "DOPNFU"), Doposlob = Dec(z, "DOPOSLOB"), Dopumanj = Dec(z, "DOPUMANJ"),
            Pioumanjr = Dec(z, "PIOUMANJR"), Pioumanjf = Dec(z, "PIOUMANJF"),
            Porez = Dec(z, "POREZ"), Porezs = Dec(z, "POREZS"), Porezu = Dec(z, "POREZU"),
            Poroslob = Dec(z, "POROSLOB"), Porumanj = Dec(z, "PORUMANJ"),
            Krediti = Dec(z, "KREDITI"), Kreditia = Dec(z, "KREDITIA"), Akontac = Dec(z, "AKONTAC"),
            Prevoz = Dec(z, "PREVOZ"), Kasa = Dec(z, "KASA"), Kasarata = Dec(z, "KASARATA"),
            Samodopr = Dec(z, "SAMODOPR"), Sindikat1 = Dec(z, "SINDIKAT1"), Sindikat2 = Dec(z, "SINDIKAT2"),
            Solidarn = Dec(z, "SOLIDARN"), Aliment = Dec(z, "ALIMENT"),
            Obust1 = Dec(z, "OBUST1"), Obust2 = Dec(z, "OBUST2"), Obust3 = Dec(z, "OBUST3"),
            Obust4 = Dec(z, "OBUST4"), Obust5 = Dec(z, "OBUST5"), Obust6 = Dec(z, "OBUST6"),
            Obustto = Dec(z, "OBUSTTO"), Solpor = Dec(z, "SOLPOR"), Ukobust = Dec(z, "UKOBUST"),
            Zaisplatu = Dec(z, "ZAISPLATU"), Benproc = Dec(z, "BENPROC"), Bendin = Dec(z, "BENDIN"),
            Komorajd = Dec(z, "KOMORAJD"), Komorasd = Dec(z, "KOMORASD"), Komorard = Dec(z, "KOMORARD"),
            Bkumanj = Dec(z, "BKUMANJ"), Arhiva = Str(z, "ARHIVA"), Arhiva2 = Str(z, "ARHIVA2"),
            Isplata = Int(z, "ISPLATA"), Vrsta = Str(z, "VRSTA"), Idbr = (long)Dec(z, "IDBR"),
            Poroslob1 = Dec(z, "POROSLOB1"), Poroslob2 = Dec(z, "POROSLOB2"),
            Poroslob3 = Dec(z, "POROSLOB3"), Poroslob4 = Dec(z, "POROSLOB4"),
            Prebruto1 = Dec(z, "PREBRUTO1"), Prebruto2 = Dec(z, "PREBRUTO2"), Prebruto3 = Dec(z, "PREBRUTO3"),
            Osnovp1 = Dec(z, "OSNOVP1"), Osnovp2 = Dec(z, "OSNOVP2"),
            Osnovp3 = Dec(z, "OSNOVP3"), Osnovp4 = Dec(z, "OSNOVP4"),
            Doppfs = Dec(z, "DOPPFS"), Dopzfs = Dec(z, "DOPZFS"), Dopnfs = Dec(z, "DOPNFS"),
        };
    }

    // ══════════════════════════════════════════════════════════════════
    //  PRENOSI — interno (pozvano iz LdPrenosiViewModel)
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Čita NETO iz LD fajla za isplatu=1 istog perioda i upisuje u AKONTAC tekućih stavki.
    /// Fox: ldprenbruto.scx — PRENOSI → PRENOS ZARADE II ISPLATA
    /// </summary>
    internal string IzvrsiPrenosZaradeIIIsplata()
    {
        if (_parametar == null)
            return "Parametri nisu podešeni.";

        var folder = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folder))
            return "Folder firme nije pronađen.";

        var param1 = new LdParametar
        {
            Mesec = _parametar.Mesec,
            Isplata = 1,
            Godina = _parametar.Godina
        };
        var ldPath1 = NadjiLdFajlZaPeriod(folder, param1);
        if (ldPath1 == null || !File.Exists(ldPath1))
            return "Nije pronađen LD fajl za isplatu 1 (LD.DBF).";

        if (string.Equals(ldPath1, _currentLdPath, StringComparison.OrdinalIgnoreCase))
            return "Izvor i cilj su isti fajl — tekući obračun je već isplata 1.";

        Dictionary<int, decimal> netoMapa;
        try
        {
            var zapisi = DbfReader.CitajSveZapise(ldPath1);
            netoMapa = zapisi
                .Where(z => Int(z, "MESEC") == _parametar.Mesec)
                .GroupBy(z => Int(z, "BROJ"))
                .ToDictionary(g => g.Key, g => Dec(g.First(), "NETO"));
        }
        catch (Exception ex)
        {
            return $"Greška pri čitanju LD fajla isplate 1: {ex.Message}";
        }

        var stavke = Stavke.Where(PripadaAktivnomObracunu).ToList();
        int azurirano = 0;
        foreach (var stavka in stavke)
        {
            if (netoMapa.TryGetValue(stavka.Broj, out var neto))
            {
                stavka.Akontac = neto;
                azurirano++;
            }
        }

        if (azurirano == 0)
            return "Nije pronađen nijedan radnik iz LD isplate 1.";

        if (!SacuvajIliVratiStanje())
            return _lastSaveError.Length > 0 ? _lastSaveError : "Snimanje nije uspelo.";
        OsveziGrid();
        AzurirajSumarno();
        return $"Preneto {azurirano} akontacija (NETO isplate 1 → AKONTAC).";
    }

    /// <summary>
    /// Za svakog radnika čiji NETO &lt; MINNAC (iz Parametri 1), upisuje razliku u DOTACIJA.
    /// Fox: PRENOSI → DOTACIJA DO MINIMALNE ZARADE
    /// </summary>
    internal string IzvrsiDotaciju()
    {
        if (_parametar == null)
            return "Parametri nisu podešeni.";

        var minNeto = (decimal)_parametar.Minnac;
        if (minNeto <= 0)
            return "Minimalna neto zarada (MINNAC) nije podešena u Parametri 1.";

        var stavke = Stavke.Where(PripadaAktivnomObracunu).ToList();
        if (stavke.Count == 0)
            return "Nema radnika u tekućem obračunu.";

        int azurirano = 0;
        foreach (var stavka in stavke)
        {
            var razlika = minNeto - stavka.Neto;
            if (razlika > 0m)
            {
                stavka.Dotacija = Math.Round(razlika, 2);
                azurirano++;
            }
        }

        if (azurirano == 0)
            return $"Svi radnici imaju neto ≥ minimum ({minNeto:N2}). Dotacija nije potrebna.";

        if (!SacuvajIliVratiStanje())
            return _lastSaveError.Length > 0 ? _lastSaveError : "Snimanje nije uspelo.";
        OsveziGrid();
        AzurirajSumarno();
        return $"Dotacija do minimuma {minNeto:N2} upisana za {azurirano} radnika.";
    }

    /// <summary>
    /// Upisuje isti iznos troškova restorana (TERENSKI) svim radnicima.
    /// Fox: PRENOSI → TROŠKOVI RESTORANA
    /// </summary>
    internal string IzvrsiTroskoviRestorana(decimal iznos)
    {
        var stavke = Stavke.Where(PripadaAktivnomObracunu).ToList();
        if (stavke.Count == 0)
            return "Nema radnika u tekućem obračunu.";

        int azurirano = 0;
        foreach (var stavka in stavke)
        {
            if (stavka.Terenski != iznos)
            {
                stavka.Terenski = iznos;
                azurirano++;
            }
        }

        if (azurirano == 0)
            return "Troškovi restorana su već postavljeni na tu vrednost.";

        if (!SacuvajIliVratiStanje())
            return _lastSaveError.Length > 0 ? _lastSaveError : "Snimanje nije uspelo.";
        OsveziGrid();
        AzurirajSumarno();
        return $"Troškovi restorana {iznos:N2} upisani za {azurirano} radnika (TERENSKI).";
    }

    /// <summary>
    /// Izvozi platni spisak u CSV fajl i otvara ga u Excel-u.
    /// Fox: PRENOSI → EXPORT U EXCEL
    /// </summary>
    internal string ExportUExcelInternal()
    {
        if (Stavke.Count == 0)
            return "Nema podataka za izvoz.";

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Sačuvaj platni spisak kao Excel",
            Filter = "Excel fajl (*.xlsx)|*.xlsx|Svi fajlovi (*.*)|*.*",
            DefaultExt = ".xlsx",
            FileName = $"PlatniSpisak_{_parametar?.Mesec}_{_parametar?.Godina}"
        };

        if (dlg.ShowDialog() != true)
            return "Izvoz otkazan.";

        try
        {
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.AddWorksheet("Platni Spisak");

            string[] zaglavlja =
            [
                "BR.", "IME I PREZIME", "ČAS.UC", "BRUTO",
                "POREZ", "DOP.RADNIK", "DOP.FIRMA",
                "NETO", "KREDITI", "AKONTAC", "UKOBUST", "ZA ISPLATU",
                "PREVOZ", "TOPLI", "REGRES", "DOTACIJA"
            ];

            for (int c = 0; c < zaglavlja.Length; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = zaglavlja[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1565C0");
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            }

            int row = 2;
            foreach (var s in Stavke)
            {
                ws.Cell(row, 1).Value  = s.Broj;
                ws.Cell(row, 2).Value  = s.ImePrez;
                ws.Cell(row, 3).Value  = (double)s.Casuc;
                ws.Cell(row, 4).Value  = (double)s.Bruto;
                ws.Cell(row, 5).Value  = (double)s.Porez;
                ws.Cell(row, 6).Value  = (double)s.Dopsocr;
                ws.Cell(row, 7).Value  = (double)s.Dopsocf;
                ws.Cell(row, 8).Value  = (double)s.Neto;
                ws.Cell(row, 9).Value  = (double)s.Krediti;
                ws.Cell(row, 10).Value = (double)s.Akontac;
                ws.Cell(row, 11).Value = (double)s.Ukobust;
                ws.Cell(row, 12).Value = (double)s.Zaisplatu;
                ws.Cell(row, 13).Value = (double)s.Prevoz;
                ws.Cell(row, 14).Value = (double)s.Topli;
                ws.Cell(row, 15).Value = (double)s.Regres;
                ws.Cell(row, 16).Value = (double)s.Dotacija;

                var numFmt = "#,##0.00";
                for (int c = 3; c <= 16; c++)
                    ws.Cell(row, c).Style.NumberFormat.Format = numFmt;

                if (row % 2 == 0)
                    ws.Range(row, 1, row, zaglavlja.Length).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E3F2FD");

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.Column(2).Width = Math.Min(ws.Column(2).Width, 35);

            wb.SaveAs(dlg.FileName);

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });

            return $"Izvezeno {Stavke.Count} radnika u {Path.GetFileName(dlg.FileName)}.";
        }
        catch (Exception ex)
        {
            return $"Greška pri izvozu: {ex.Message}";
        }
    }

    /// <summary>
    /// Kopira časove i dodatke iz izabranog LD fajla (npr. prethodnog perioda) u tekuće stavke.
    /// Fox: PRENOSI → KOPIRANJE ZARADE
    /// </summary>
    internal string IzvrsiKopiranjeZarade(string sourceLdPath)
    {
        if (string.IsNullOrWhiteSpace(sourceLdPath) || !File.Exists(sourceLdPath))
            return "Izvorni fajl nije pronađen.";

        if (string.Equals(sourceLdPath, _currentLdPath, StringComparison.OrdinalIgnoreCase))
            return "Izvor i cilj su isti fajl.";

        Dictionary<int, Dictionary<string, object?>> sourceMap;
        try
        {
            var zapisi = DbfReader.CitajSveZapise(sourceLdPath);
            sourceMap = zapisi
                .GroupBy(z => Int(z, "BROJ"))
                .ToDictionary(g => g.Key, g => g.First());
        }
        catch (Exception ex)
        {
            return $"Greška pri čitanju izvornog fajla: {ex.Message}";
        }

        var casPolja = new[]
        {
            ("Casuc","CASUC"), ("Casnoc","CASNOC"), ("Casprod","CASPROD"),
            ("Casradnap","CASRADNAP"), ("Casned","CASNED"), ("Casdor","CASDOR"),
            ("Cslput","CSLPUT"), ("Caspraz","CASPRAZ"), ("Casbol","CASBOL"),
            ("Casbol2","CASBOL2"), ("Casplac","CASPLAC"), ("Casplac2","CASPLAC2"),
            ("Casgod","CASGOD"), ("Casvv","CASVV"), ("Cas1","CAS1"),
            ("Cas2","CAS2"), ("Cas3","CAS3"), ("Cassus","CASSUS"),
            ("Casneplac","CASNEPLAC"), ("Caspriprav","CASPRIPRAV"),
            ("Topli","TOPLI"), ("Regres","REGRES"), ("Terenski","TERENSKI"),
            ("Fiksna","FIKSNA"), ("Stim1proc","STIM1PROC"),
            ("Stim2proc","STIM2PROC"), ("Stim3proc","STIM3PROC")
        };

        var tipStavke = typeof(LdObracunStavka);
        var stavke = Stavke.Where(PripadaAktivnomObracunu).ToList();
        int azurirano = 0;

        foreach (var stavka in stavke)
        {
            if (!sourceMap.TryGetValue(stavka.Broj, out var src)) continue;
            foreach (var (propName, dbfKey) in casPolja)
            {
                var prop = tipStavke.GetProperty(propName);
                if (prop?.CanWrite == true)
                    prop.SetValue(stavka, Dec(src, dbfKey));
            }
            azurirano++;
        }

        if (azurirano == 0)
            return "Nema podudarajućih radnika između fajlova.";

        if (!SacuvajIliVratiStanje())
            return _lastSaveError.Length > 0 ? _lastSaveError : "Snimanje nije uspelo.";
        OsveziGrid();
        AzurirajSumarno();
        return $"Skopiran unos (časovi + dodaci) za {azurirano} radnika iz {Path.GetFileName(sourceLdPath)}.";
    }

    /// <summary>
    /// Izvozi platni spisak u CSV za budžetski registar zaposlenih.
    /// Fox: PRENOSI → PRENOS ZA REGISTAR
    /// </summary>
    internal string IzvrsiPrenosZaRegistar()
    {
        if (Stavke.Count == 0)
            return "Nema podataka za prenos.";

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Sačuvaj za registar zaposlenih",
            Filter = "CSV fajl (*.csv)|*.csv|Tekstualni fajl (*.txt)|*.txt|Svi fajlovi (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = $"Registar_{_parametar?.Mesec}_{_parametar?.Godina}"
        };

        if (dlg.ShowDialog() != true)
            return "Prenos otkazan.";

        try
        {
            const string sep = ";";
            var zaglavlje = string.Join(sep,
                "R.BR.", "IME I PREZIME", "JMBG",
                "BRUTO", "POREZ", "DOP.RADNIK", "DOP.FIRMA", "DOP.UKUPNO",
                "NETO", "ZA ISPLATU");

            int rb = 1;
            var redovi = Stavke.Select(s => string.Join(sep,
                rb++, CsvEscape(s.ImePrez), s.Maticnibr,
                F(s.Bruto), F(s.Porez),
                F(s.Dopsocr), F(s.Dopsocf), F(s.Dopsocr + s.Dopsocf),
                F(s.Neto), F(s.Zaisplatu)));

            File.WriteAllLines(dlg.FileName,
                new[] { zaglavlje }.Concat(redovi),
                new System.Text.UTF8Encoding(true));

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });

            return $"Registar izvezen: {Stavke.Count} radnika → {Path.GetFileName(dlg.FileName)}.";
        }
        catch (Exception ex)
        {
            return $"Greška pri prenosu za registar: {ex.Message}";
        }
    }

    private static string F(decimal v) =>
        v.ToString("N2", CultureInfo.InvariantCulture);

    private static string CsvEscape(string? s) =>
        s?.Contains(';') == true ? $"\"{s?.Replace("\"", "\"\"")}\"" : (s ?? "");

    private static Dictionary<string, object?> MapirajStavkuURed(LdObracunStavka s)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["BROJ"] = (decimal)s.Broj, ["SIFRAPRIH"] = s.Sifraprih, ["IME_PREZ"] = s.ImePrez,
            ["EVIDBROJ"] = s.Evidbroj, ["MATICNIBR"] = s.Maticnibr, ["IDBROJ"] = s.Idbroj,
            ["DOK"] = s.Dok, ["GRUPA"] = (decimal)s.Grupa, ["GRUPA1"] = (decimal)s.Grupa1,
            ["MTR"] = (decimal)s.Mtr, ["MESEC"] = (decimal)s.Mesec, ["NAZMES"] = s.Nazmes,
            ["GODINA"] = s.Godina,
            ["CASVR"] = s.Casvr, ["CASUC"] = s.Casuc, ["CASNOC"] = s.Casnoc,
            ["CASPROD"] = s.Casprod, ["CASRADNAP"] = s.Casradnap, ["CASNED"] = s.Casned,
            ["CASDOR"] = s.Casdor, ["CSLPUT"] = s.Cslput, ["CASPRAZ"] = s.Caspraz,
            ["CASBOL"] = s.Casbol, ["CASBOL2"] = s.Casbol2, ["CASPLAC"] = s.Casplac,
            ["CASPLAC2"] = s.Casplac2, ["CASGOD"] = s.Casgod, ["CASVV"] = s.Casvv,
            ["CAS1"] = s.Cas1, ["CAS2"] = s.Cas2, ["CAS3"] = s.Cas3,
            ["CASSUS"] = s.Cassus, ["CASNEPLAC"] = s.Casneplac, ["CASPRIPRAV"] = s.Caspriprav,
            ["CASUK"] = s.Casuk, ["DINVR"] = s.Dinvr, ["DINUC"] = s.Dinuc,
            ["DINNOC"] = s.Dinnoc, ["DINPROD"] = s.Dinprod, ["DINRADNAP"] = s.Dinradnap,
            ["DINNED"] = s.Dinned, ["DINDOR"] = s.Dindor, ["DINSL"] = s.Dinsl,
            ["DINPRAZ"] = s.Dinpraz, ["DINBOL"] = s.Dinbol, ["DINBOL2"] = s.Dinbol2,
            ["DINPLAC"] = s.Dinplac, ["DINPLAC2"] = s.Dinplac2, ["DINGOD"] = s.Dingod,
            ["DINVV"] = s.Dinvv, ["DIN1"] = s.Din1, ["DIN2"] = s.Din2, ["DIN3"] = s.Din3,
            ["DINSUS"] = s.Dinsus, ["DINMIN"] = s.Dinmin, ["DINUK"] = s.Dinuk,
            ["DINPRIPRAV"] = s.Dinpriprav, ["STIM1"] = s.Stim1, ["STIM2"] = s.Stim2,
            ["STIM3"] = s.Stim3, ["STIM1PROC"] = s.Stim1proc, ["STIM2PROC"] = s.Stim2proc,
            ["STIM3PROC"] = s.Stim3proc, ["TOPLI"] = s.Topli, ["REGRES"] = s.Regres,
            ["TERENSKI"] = s.Terenski, ["FIKSNA"] = s.Fiksna, ["DOTACIJA"] = s.Dotacija,
            ["LDODACI"] = s.Ldodaci, ["NAKNADE"] = s.Naknade, ["BRUTO"] = s.Bruto,
            ["NETO"] = s.Neto, ["NETO2"] = s.Neto2, ["NETOSVE"] = s.Netosve,
            ["NETOPREV"] = s.Netoprev, ["NETOOST"] = s.Netoost, ["CENARADA"] = s.Cenarada,
            ["STARTBOD"] = s.Startbod, ["DOPSOCR"] = s.Dopsocr, ["DOPSOCF"] = s.Dopsocf,
            ["DOPPR"] = s.Doppr, ["DOPZR"] = s.Dopzr, ["DOPNR"] = s.Dopnr,
            ["DOPPF"] = s.Doppf, ["DOPZF"] = s.Dopzf, ["DOPNF"] = s.Dopnf,
            ["DOPPRU"] = s.Doppru, ["DOPPFU"] = s.Doppfu, ["DOPZFU"] = s.Dopzfu,
            ["DOPNFU"] = s.Dopnfu, ["DOPOSLOB"] = s.Doposlob, ["DOPUMANJ"] = s.Dopumanj,
            ["PIOUMANJR"] = s.Pioumanjr, ["PIOUMANJF"] = s.Pioumanjf,
            ["POREZ"] = s.Porez, ["POREZS"] = s.Porezs, ["POREZU"] = s.Porezu,
            ["POROSLOB"] = s.Poroslob, ["PORUMANJ"] = s.Porumanj,
            ["KREDITI"] = s.Krediti, ["KREDITIA"] = s.Kreditia, ["AKONTAC"] = s.Akontac,
            ["PREVOZ"] = s.Prevoz, ["KASA"] = s.Kasa, ["KASARATA"] = s.Kasarata,
            ["SAMODOPR"] = s.Samodopr, ["SINDIKAT1"] = s.Sindikat1, ["SINDIKAT2"] = s.Sindikat2,
            ["SOLIDARN"] = s.Solidarn, ["ALIMENT"] = s.Aliment,
            ["OBUST1"] = s.Obust1, ["OBUST2"] = s.Obust2, ["OBUST3"] = s.Obust3,
            ["OBUST4"] = s.Obust4, ["OBUST5"] = s.Obust5, ["OBUST6"] = s.Obust6,
            ["OBUSTTO"] = s.Obustto, ["SOLPOR"] = s.Solpor, ["UKOBUST"] = s.Ukobust,
            ["ZAISPLATU"] = s.Zaisplatu, ["BENPROC"] = s.Benproc, ["BENDIN"] = s.Bendin,
            ["KOMORAJD"] = s.Komorajd, ["KOMORASD"] = s.Komorasd, ["KOMORARD"] = s.Komorard,
            ["BKUMANJ"] = s.Bkumanj, ["ARHIVA"] = s.Arhiva, ["ARHIVA2"] = s.Arhiva2,
            ["ISPLATA"] = (decimal)s.Isplata, ["VRSTA"] = s.Vrsta, ["IDBR"] = (decimal)s.Idbr,
            ["POROSLOB1"] = s.Poroslob1, ["POROSLOB2"] = s.Poroslob2,
            ["POROSLOB3"] = s.Poroslob3, ["POROSLOB4"] = s.Poroslob4,
            ["PREBRUTO1"] = s.Prebruto1, ["PREBRUTO2"] = s.Prebruto2, ["PREBRUTO3"] = s.Prebruto3,
            ["OSNOVP1"] = s.Osnovp1, ["OSNOVP2"] = s.Osnovp2,
            ["OSNOVP3"] = s.Osnovp3, ["OSNOVP4"] = s.Osnovp4,
            ["DOPPFS"] = s.Doppfs, ["DOPZFS"] = s.Dopzfs, ["DOPNFS"] = s.Dopnfs,
        };
    }
}
