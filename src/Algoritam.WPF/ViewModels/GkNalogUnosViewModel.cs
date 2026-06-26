using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public class GkNalogStavka : ObservableObject
{
    private string _konto = "";
    private decimal _dug;
    private decimal _pot;
    private decimal _dodDug;
    private decimal _dodPot;
    private string _opis = "";
    private string _brDok = "";
    private DateTime _datDok = DateTime.Today;
    private string _dok = "";
    private string _mp = "";
    private string _dev = "";
    private decimal _devKurs;

    public string Konto    { get => _konto;   set => SetProperty(ref _konto,   value); }
    public decimal Dug     { get => _dug;     set => SetProperty(ref _dug,     value); }
    public decimal Pot     { get => _pot;     set => SetProperty(ref _pot,     value); }
    public decimal DodDug  { get => _dodDug;  set => SetProperty(ref _dodDug,  value); }
    public decimal DodPot  { get => _dodPot;  set => SetProperty(ref _dodPot,  value); }
    public string Opis     { get => _opis;    set => SetProperty(ref _opis,    value); }
    public string BrDok    { get => _brDok;   set => SetProperty(ref _brDok,   value); }
    public DateTime DatDok { get => _datDok;  set => SetProperty(ref _datDok,  value); }
    public string Dok      { get => _dok;     set => SetProperty(ref _dok,     value); }
    public string Mp       { get => _mp;      set => SetProperty(ref _mp,      value); }
    public string Dev      { get => _dev;     set => SetProperty(ref _dev,     value); }
    public decimal DevKurs { get => _devKurs; set => SetProperty(ref _devKurs, value); }
}

public partial class GkNalogUnosViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string? _brNalZaIzmenu;

    public bool JeIzmena => _brNalZaIzmenu is not null;
    public string Naslov => JeIzmena ? "IZMENA NALOGA ZA KNJIŽENJE" : "IZRADA NALOGA ZA KNJIŽENJE";

    [ObservableProperty] private string _brNal = "";
    [ObservableProperty] private DateTime _datum = DateTime.Today;
    [ObservableProperty] private string _vrNal = "";
    [ObservableProperty] private string _opis = "";
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _imaGresku;
    [ObservableProperty] private bool _ucitava;

    public ObservableCollection<GkNalogStavka> Stavke { get; } = [];
    public List<string> VrsteNaloga { get; private set; } = [];

    public decimal UkupnoDug => Stavke.Sum(s => s.Dug);
    public decimal UkupnoPot => Stavke.Sum(s => s.Pot);
    public decimal Razlika => UkupnoDug - UkupnoPot;
    public bool JeBalansiran => Math.Abs(Razlika) < 0.005m && Stavke.Count > 0;

    public bool Uspesno { get; private set; }

    public GkNalogUnosViewModel(string folderPath)
    {
        _folderPath = folderPath;
        Stavke.CollectionChanged += OnStavkeChanged;
        _ = InitAsync();
    }

    public GkNalogUnosViewModel(string folderPath, string brNalZaIzmenu)
    {
        _folderPath = folderPath;
        _brNalZaIzmenu = brNalZaIzmenu;
        Stavke.CollectionChanged += OnStavkeChanged;
        _ = InitZaIzmenuAsync(brNalZaIzmenu);
    }

    private void OnStavkeChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (GkNalogStavka st in e.NewItems)
                st.PropertyChanged += OnStavkaPropertyChanged;
        if (e.OldItems != null)
            foreach (GkNalogStavka st in e.OldItems)
                st.PropertyChanged -= OnStavkaPropertyChanged;
        OsveziTotale();
    }

    private void OnStavkaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GkNalogStavka.Dug) or nameof(GkNalogStavka.Pot))
            OsveziTotale();
    }

    private void OsveziTotale()
    {
        OnPropertyChanged(nameof(UkupnoDug));
        OnPropertyChanged(nameof(UkupnoPot));
        OnPropertyChanged(nameof(Razlika));
        OnPropertyChanged(nameof(JeBalansiran));
        SacuvajCommand.NotifyCanExecuteChanged();
        ProknjiziCommand.NotifyCanExecuteChanged();
    }

    private async Task InitAsync()
    {
        Ucitava = true;
        try
        {
            var vrsteTask = Task.Run(() => UcitajVrste());
            var brNalTask = Task.Run(() => SlededeciBroj());
            await Task.WhenAll(vrsteTask, brNalTask);
            VrsteNaloga = vrsteTask.Result;
            BrNal = brNalTask.Result;
            OnPropertyChanged(nameof(VrsteNaloga));
        }
        catch { }
        finally
        {
            Ucitava = false;
        }
    }

    private async Task InitZaIzmenuAsync(string brNal)
    {
        Ucitava = true;
        try
        {
            VrsteNaloga = await Task.Run(() => UcitajVrste());
            OnPropertyChanged(nameof(VrsteNaloga));

            var headerPath = NadjiDbf(_folderPath, "nalbroj.dbf");
            if (headerPath is not null)
            {
                var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(headerPath));
                var header = zapisi.LastOrDefault(r =>
                    r.TryGetValue("BRNAL", out var bn) && bn?.ToString()?.Trim() == brNal);
                if (header is not null)
                {
                    BrNal = brNal;
                    Datum = header.TryGetValue("DATUM", out var d) && d is DateTime dt ? dt : DateTime.Today;
                    VrNal = header.GetValueOrDefault("VRNAL")?.ToString()?.Trim() ?? "";
                    Opis = header.GetValueOrDefault("OPIS")?.ToString()?.Trim() ?? "";
                }
                else
                {
                    BrNal = brNal;
                    StatusPoruka = "Header naloga nije pronađen u nalbroj.dbf.";
                    ImaGresku = true;
                }
            }

            var stavkePath = NadjiDbf(_folderPath, "nalp.dbf");
            if (stavkePath is not null)
            {
                var sveStavke = await Task.Run(() => DbfReader.CitajSveZapise(stavkePath));
                var stavkeZaNalog = sveStavke
                    .Where(r => r.TryGetValue("BRNAL", out var bn) && bn?.ToString()?.Trim() == brNal)
                    .Select(MapirajZapisUStavku)
                    .ToList();
                foreach (var st in stavkeZaNalog) Stavke.Add(st);
            }
        }
        catch (Exception ex)
        {
            ImaGresku = true;
            StatusPoruka = $"Greška pri učitavanju naloga: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private static GkNalogStavka MapirajZapisUStavku(Dictionary<string, object?> r)
    {
        static string G(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) ? v?.ToString()?.Trim() ?? "" : "";
        static decimal D(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) && v is not null && decimal.TryParse(v.ToString(), out var n) ? n : 0;
        static DateTime Dt(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) && v is DateTime dt ? dt : DateTime.Today;

        return new GkNalogStavka
        {
            Konto = G(r, "KONTO"),
            Dug = D(r, "DUG"),
            Pot = D(r, "POT"),
            DodDug = D(r, "DODDUG"),
            DodPot = D(r, "DODPOT"),
            Opis = G(r, "OPIS"),
            DatDok = Dt(r, "DATDOK"),
            Dok = G(r, "DOK"),
            BrDok = G(r, "BRDOK"),
            Mp = G(r, "MP"),
            Dev = G(r, "DEV"),
            DevKurs = D(r, "DEVKURS"),
        };
    }

    private List<string> UcitajVrste()
    {
        var path = NadjiDbf(_folderPath, "nalvrsta.dbf");
        if (path is null) return [];
        try
        {
            return DbfReader.CitajSveZapise(path)
                .Where(r => r.TryGetValue("VRNAL", out var v) && !string.IsNullOrWhiteSpace(v?.ToString()))
                .Select(r => r["VRNAL"]!.ToString()!.Trim())
                .Distinct()
                .ToList();
        }
        catch { return []; }
    }

    private string SlededeciBroj()
    {
        var path = NadjiDbf(_folderPath, "nalbroj.dbf");
        if (path is null) return "000001";
        try
        {
            var zapisi = DbfReader.CitajSveZapise(path);
            var maxBroj = zapisi
                .Select(r => r.TryGetValue("BRNAL", out var v) ? v?.ToString()?.Trim() ?? "" : "")
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => long.TryParse(s, out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();
            return (maxBroj + 1).ToString("D6");
        }
        catch { return "000001"; }
    }

    [RelayCommand]
    private void DodajStavku() => Stavke.Add(new GkNalogStavka { DatDok = Datum });

    [RelayCommand]
    private void ObrisiStavku(GkNalogStavka? st)
    {
        if (st is not null) Stavke.Remove(st);
    }

    // Save draft to nalp.dbf (work-in-progress) OR update existing draft nalog
    [RelayCommand(CanExecute = nameof(MozeSacuvati))]
    private void Sacuvaj(System.Windows.Window? window)
    {
        ImaGresku = false;
        StatusPoruka = "Čuvanje...";
        try
        {
            if (JeIzmena)
            {
                AzurirajNalbroj();
                ZameniStavkeUNalp();
                Uspesno = true;
                StatusPoruka = "Izmene naloga sačuvane u radnoj tabeli (nalp.dbf).";
            }
            else
            {
                SacuvajNalbroj(proknjizeno: false);
                SacuvajStavkeUNalp();
                Uspesno = true;
                StatusPoruka = "Nalog sačuvan u radnoj tabeli (nalp.dbf).";
            }
            window?.Close();
        }
        catch (Exception ex)
        {
            ImaGresku = true;
            StatusPoruka = $"Greška pri čuvanju: {ex.Message}";
        }
    }

    private bool MozeSacuvati() => JeBalansiran && !string.IsNullOrWhiteSpace(BrNal);

    // KNJIZI F5 — post from nalp.dbf draft to nal.dbf permanent ledger
    [RelayCommand(CanExecute = nameof(MozeProknjiziti))]
    private void Proknjizi(System.Windows.Window? window)
    {
        var potvrda = System.Windows.MessageBox.Show(
            $"Proknjižiti nalog {BrNal}?\nDuguje: {UkupnoDug:N2}  Potražuje: {UkupnoPot:N2}",
            "Potvrda knjiženja",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (potvrda != System.Windows.MessageBoxResult.Yes) return;

        ImaGresku = false;
        StatusPoruka = "Knjiženje...";
        try
        {
            SacuvajNalbroj(proknjizeno: true);
            SacuvajStavkeUNalp();
            PrebaciNalpUNal();
            Uspesno = true;
            StatusPoruka = "Nalog proknjižen.";
            window?.Close();
        }
        catch (Exception ex)
        {
            ImaGresku = true;
            StatusPoruka = $"Greška pri knjiženju: {ex.Message}";
        }
    }

    private bool MozeProknjiziti() => !JeIzmena && JeBalansiran && !string.IsNullOrWhiteSpace(BrNal);

    private List<Dictionary<string, object?>> IzgradiZapisteStavki()
    {
        return Stavke.Select((st, i) => new Dictionary<string, object?>
        {
            ["KONTO"]   = st.Konto,
            ["DUG"]     = st.Dug,
            ["POT"]     = st.Pot,
            ["DODDUG"]  = st.DodDug,
            ["DODPOT"]  = st.DodPot,
            ["OPIS"]    = st.Opis,
            ["DATDOK"]  = st.DatDok,
            ["BRNAL"]   = BrNal,
            ["DOK"]     = st.Dok,
            ["BRDOK"]   = st.BrDok,
            ["MP"]      = st.Mp,
            ["DEV"]     = st.Dev,
            ["DEVKURS"] = st.DevKurs,
            ["DATUM"]   = Datum,
            ["PRENETO"] = "N",
            ["IDBR"]    = (long)(i + 1),
        }).ToList();
    }

    private void SacuvajNalbroj(bool proknjizeno)
    {
        var path = NadjiDbf(_folderPath, "nalbroj.dbf") ??
                   Path.Combine(_folderPath, "nalbroj.dbf");

        var schema = DbfTableWriter.LoadSchema(path);
        var sviZapisi = DbfReader.CitajSveZapise(path);
        var maxIdbr = sviZapisi
            .Select(r => r.TryGetValue("IDBR", out var v) ? Convert.ToInt64(v ?? 0) : 0L)
            .DefaultIfEmpty(0L).Max();

        var zapis = new Dictionary<string, object?>
        {
            ["BRNAL"]   = BrNal,
            ["DATUM"]   = Datum,
            ["VRNAL"]   = VrNal,
            ["OPIS"]    = Opis,
            ["DATOD"]   = Datum,
            ["DATDO"]   = Datum,
            ["DUG"]     = UkupnoDug,
            ["POT"]     = UkupnoPot,
            ["DATKNJI"] = proknjizeno ? (object?)DateTime.Today : null,
            ["PRENETO"] = "N",
            ["IDBR"]    = maxIdbr + 1,
        };

        DbfTableWriter.DodajRedove(path, schema, [zapis]);
    }

    private void SacuvajStavkeUNalp()
    {
        var path = NadjiDbf(_folderPath, "nalp.dbf");
        string schemaPath;

        if (path is not null)
        {
            schemaPath = path;
        }
        else
        {
            schemaPath = NadjiDbf(_folderPath, "nal.dbf") ??
                         throw new FileNotFoundException("Tabela nalp.dbf / nal.dbf nije pronađena.");
            path = Path.Combine(Path.GetDirectoryName(schemaPath)!, "nalp.dbf");
        }

        var noviZapisi = IzgradiZapisteStavki();

        if (path == schemaPath || File.Exists(path))
        {
            var targetSchema = DbfTableWriter.LoadSchema(path == schemaPath ? schemaPath : path);
            DbfTableWriter.DodajRedove(path == schemaPath ? schemaPath : path, targetSchema, noviZapisi);
        }
        else
        {
            var schema = DbfTableWriter.LoadSchema(schemaPath);
            DbfTableWriter.WriteTable(path, schema, noviZapisi,
                (row, field) => row.TryGetValue(field, out var v) ? v : null);
        }
    }

    // Update existing nalbroj.dbf header row in place (edit mode — nalog still unposted)
    private void AzurirajNalbroj()
    {
        var path = NadjiDbf(_folderPath, "nalbroj.dbf") ??
                   throw new FileNotFoundException("Tabela nalbroj.dbf nije pronađena.");

        var schema = DbfTableWriter.LoadSchema(path);
        var sviZapisi = DbfReader.CitajSveZapise(path);

        var idx = sviZapisi.FindLastIndex(r =>
            r.TryGetValue("BRNAL", out var bn) && bn?.ToString()?.Trim() == BrNal);
        if (idx < 0)
            throw new InvalidOperationException($"Nalog {BrNal} nije pronađen u nalbroj.dbf.");

        var zapis = sviZapisi[idx];
        zapis["DATUM"] = Datum;
        zapis["VRNAL"] = VrNal;
        zapis["OPIS"]  = Opis;
        zapis["DATOD"] = Datum;
        zapis["DATDO"] = Datum;
        zapis["DUG"]   = UkupnoDug;
        zapis["POT"]   = UkupnoPot;

        DbfTableWriter.WriteTable(path, schema, sviZapisi,
            (row, field) => row.TryGetValue(field, out var v) ? v : null);
    }

    // Replace nalp.dbf stavke for this BRNAL with the edited set (edit mode)
    private void ZameniStavkeUNalp()
    {
        var path = NadjiDbf(_folderPath, "nalp.dbf") ??
                   throw new FileNotFoundException("Tabela nalp.dbf nije pronađena.");

        var schema = DbfTableWriter.LoadSchema(path);
        var sviZapisi = DbfReader.CitajSveZapise(path);

        var preostali = sviZapisi
            .Where(r => !(r.TryGetValue("BRNAL", out var bn) && bn?.ToString()?.Trim() == BrNal))
            .ToList();
        preostali.AddRange(IzgradiZapisteStavki());

        DbfTableWriter.WriteTable(path, schema, preostali,
            (row, field) => row.TryGetValue(field, out var v) ? v : null);
    }

    // Copies nalp.dbf rows for this BRNAL into nal.dbf (permanent ledger)
    private void PrebaciNalpUNal()
    {
        var nalpPath = NadjiDbf(_folderPath, "nalp.dbf");
        if (nalpPath is null) return;

        var nalPath = NadjiDbf(_folderPath, "nal.dbf");
        if (nalPath is null) return;

        var stavke = DbfReader.CitajSveZapise(nalpPath)
            .Where(r => r.TryGetValue("BRNAL", out var bn) && bn?.ToString()?.Trim() == BrNal)
            .ToList();

        if (stavke.Count == 0) return;

        var nalSchema = DbfTableWriter.LoadSchema(nalPath);
        DbfTableWriter.DodajRedove(nalPath, nalSchema, stavke);
    }

    [RelayCommand]
    private void Odustani(System.Windows.Window? window) => window?.Close();

    private static string? NadjiDbf(string folderPath, string fileName)
    {
        foreach (var dir in new[] { folderPath,
            Path.Combine(folderPath, "data00"),
            Path.Combine(folderPath, "01"),
            Path.Combine(folderPath, "..") })
        {
            if (!Directory.Exists(dir)) continue;
            var f = Path.Combine(dir, fileName);
            if (File.Exists(f)) return f;
            try
            {
                var ci = Directory.GetFiles(dir, "*.dbf", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(x => Path.GetFileName(x).Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (ci is not null) return ci;
            }
            catch { }
        }
        return null;
    }
}
