using Algoritam.Application;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Data;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

public partial class LdParametriViewModel : ObservableObject
{
    private sealed record SnimanjeRezultat(bool Uspesno, string Poruka, bool Konflikt = false);
    public sealed record MesecOpcija(int Vrednost, string Prikaz);

    private readonly AppState _appState;
    private LdParametar _original = new();
    private static readonly Dictionary<string, PropertyInfo> _parametarPropMap = BuildPropMap();
    private string? _ldParamDbfPath;
    private DbfOptimisticConcurrency.FileSnapshot? _snapshotNaPocetkuIzmene;
    private string? _potpisNaPocetkuIzmene;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Naslov))]
    [NotifyCanExecuteChangedFor(nameof(SacuvajCommand))]
    [NotifyCanExecuteChangedFor(nameof(OtkaziCommand))]
    private bool _jeIzmena;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private LdParametar _parametar = new();

    [ObservableProperty]
    private string _poruka = "";

    public IReadOnlyList<MesecOpcija> Meseci { get; } = BuildMeseci();
    public IReadOnlyList<string> Godine { get; } = Enumerable.Range(1900, 301).Select(g => g.ToString()).ToArray();

    public LdParametriViewModel(AppState appState, int selectedTabIndex = 0)
    {
        _appState = appState;
        SelectedTabIndex = selectedTabIndex;
        Ucitaj();
        PripremiSnapshotZaIzmenu();
        _original = Clone(Parametar);
        JeIzmena = false;
    }

    public string Naslov => SelectedTabIndex == 0
        ? "LDPAR - Parametri 1"
        : "LDPAR2 - Parametri 2";

    [RelayCommand]
    private void Izmeni()
    {
        PripremiSnapshotZaIzmenu();
        _original = Clone(Parametar);
        JeIzmena = true;
        Poruka = "Rezim izmene je aktivan.";
    }

    [RelayCommand(CanExecute = nameof(MozeSacuvaj))]
    private Task Sacuvaj()
    {
        try
        {
            var rezultat = SacuvajUFoxDbf(Parametar);
            Poruka = rezultat.Poruka;

            if (!rezultat.Uspesno)
            {
                if (rezultat.Konflikt)
                {
                    MessageBox.Show(
                        rezultat.Poruka,
                        "Konflikt pri snimanju",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Ucitaj();
                }

                return Task.CompletedTask;
            }

            SacuvajNajOsnUBazu(Parametar.Najosn);
            _original = Clone(Parametar);
            JeIzmena = false;
            Poruka = string.IsNullOrWhiteSpace(rezultat.Poruka)
                ? "Parametri su sačuvani u ldparam.dbf."
                : rezultat.Poruka;
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri cuvanju: {ex.Message}";
        }
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(MozeSacuvaj))]
    private void Otkazi()
    {
        Parametar = Clone(_original);
        JeIzmena = false;
        Poruka = "Izmene su otkazane.";
    }

    [RelayCommand]
    private void ObnoviPodrazumevano()
    {
        Parametar = LdParametarDefaults.Kreiraj();
        OsveziMesec();
        JeIzmena = true;
        Poruka = "Ucitan je podrazumevani skup vrednosti za ldparam.";
    }

    [RelayCommand]
    public void OsveziMesec()
    {
        // Mesec može biti 1-48: +12 za drugu grupu, +24 za treću, +36 za četvrtu grupu istog meseca
        Parametar.Mesec = Math.Clamp(Parametar.Mesec, 1, 48);
        Parametar.Godina = ParseGodina(Parametar.Godina).ToString();
        var info = IzracunajMesecInfoFox(Parametar.Mesec, Parametar.Godina);

        Parametar.Nazmes = info.Nazmes;
        Parametar.Dana = info.Dana;
        Parametar.Cmes = info.RadniSati;
        Parametar.Czakon = info.RadniSati;

        OnPropertyChanged(nameof(Parametar));
    }

    private bool MozeSacuvaj() => JeIzmena;

    private void Ucitaj()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Parametar = LdParametarDefaults.Kreiraj();
            UcitajNajOsnIzBaze(Parametar);
            _original = Clone(Parametar);
            Poruka = "Aktivna firma nije izabrana. Ucitane su podrazumevane vrednosti.";
            return;
        }

        var dbfPath = PronadjiDbf(folderPath, "ldparam.dbf");
        _ldParamDbfPath = dbfPath;
        if (dbfPath is null)
        {
            Parametar = LdParametarDefaults.Kreiraj();
            UcitajNajOsnIzBaze(Parametar);
            _original = Clone(Parametar);
            _snapshotNaPocetkuIzmene = null;
            _potpisNaPocetkuIzmene = null;
            Poruka = "ldparam.dbf nije pronađen. Ucitane su podrazumevane vrednosti.";
            return;
        }

        try
        {
            var red = TryUcitajPostojeciRed(dbfPath);
            if (red is null)
            {
                Parametar = LdParametarDefaults.Kreiraj();
                UcitajNajOsnIzBaze(Parametar);
                _original = Clone(Parametar);
                _snapshotNaPocetkuIzmene = DbfOptimisticConcurrency.CaptureFileSnapshot(dbfPath);
                _potpisNaPocetkuIzmene = null;
                Poruka = "ldparam.dbf je prazan. Ucitane su podrazumevane vrednosti.";
                return;
            }

            var param = new LdParametar();
            foreach (var kvp in red)
            {
                var key = Normalize(kvp.Key);
                if (!_parametarPropMap.TryGetValue(key, out var prop) || kvp.Value is null)
                    continue;

                try
                {
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    prop.SetValue(param, Convert.ChangeType(kvp.Value, targetType));
                }
                catch { }
            }

            UcitajNajOsnIzBaze(param);
            Parametar = param;
            _original = Clone(param);
            _snapshotNaPocetkuIzmene = DbfOptimisticConcurrency.CaptureFileSnapshot(dbfPath);
            _potpisNaPocetkuIzmene = DbfOptimisticConcurrency.ComputeRecordSignature(red);
            Poruka = "Parametri ucitani iz ldparam.dbf.";
        }
        catch (Exception ex)
        {
            Parametar = LdParametarDefaults.Kreiraj();
            UcitajNajOsnIzBaze(Parametar);
            _original = Clone(Parametar);
            _snapshotNaPocetkuIzmene = null;
            _potpisNaPocetkuIzmene = null;
            Poruka = $"Greska pri ucitavanju ldparam.dbf: {ex.Message}";
        }
    }

    private static LdParametar Clone(LdParametar source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<LdParametar>(json) ?? new LdParametar();
    }

    private SnimanjeRezultat SacuvajUFoxDbf(LdParametar parametar)
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return new SnimanjeRezultat(false, "Folder firme nije pronađen.");

        try
        {
            var ciljnaPutanja = PronadjiDbf(folderPath, "ldparam.dbf") ?? Path.Combine(folderPath, "ldparam.dbf");
            _ldParamDbfPath = ciljnaPutanja;
            var schema = UcitajSemuLdParam(ciljnaPutanja);

            if (!DbfOptimisticConcurrency.TryAcquireRecordLock(
                    ciljnaPutanja,
                    "__FILE__",
                    Environment.UserName,
                    out var lockHandle,
                    out var lockPoruka))
            {
                return new SnimanjeRezultat(false, lockPoruka, Konflikt: true);
            }

            using (lockHandle)
            {
                if (_snapshotNaPocetkuIzmene is not null &&
                    File.Exists(ciljnaPutanja) &&
                    DbfOptimisticConcurrency.HasFileChanged(ciljnaPutanja, _snapshotNaPocetkuIzmene))
                {
                    var trenutniRed = TryUcitajPostojeciRed(ciljnaPutanja);
                    var trenutniPotpis = trenutniRed is null
                        ? null
                        : DbfOptimisticConcurrency.ComputeRecordSignature(trenutniRed);

                    if (!string.Equals(trenutniPotpis, _potpisNaPocetkuIzmene, StringComparison.Ordinal))
                    {
                        return new SnimanjeRezultat(
                            false,
                            "Parametre je u medjuvremenu promenio drugi korisnik. Osvezite ekran i pokusajte ponovo.",
                            Konflikt: true);
                    }
                }

                var postojeciRed = TryUcitajPostojeciRed(ciljnaPutanja);
                var red = BuildRow(parametar, schema, postojeciRed);

                DbfTableWriter.WriteTable(
                    ciljnaPutanja,
                    schema,
                    [red],
                    static (r, fieldName) => r.TryGetValue(fieldName, out var value) ? value : null);
            }

            if (File.Exists(ciljnaPutanja))
            {
                _snapshotNaPocetkuIzmene = DbfOptimisticConcurrency.CaptureFileSnapshot(ciljnaPutanja);
                var noviRed = TryUcitajPostojeciRed(ciljnaPutanja);
                _potpisNaPocetkuIzmene = noviRed is null
                    ? null
                    : DbfOptimisticConcurrency.ComputeRecordSignature(noviRed);
            }
            else
            {
                _snapshotNaPocetkuIzmene = null;
                _potpisNaPocetkuIzmene = null;
            }

            return new SnimanjeRezultat(true, "FOX ldparam.dbf je azuriran.");
        }
        catch (Exception ex)
        {
            return new SnimanjeRezultat(false, $"Greska upisa u FOX ldparam.dbf: {ex.Message}");
        }
    }

    private static Dictionary<string, object?> BuildRow(
        LdParametar parametar,
        DbfTableWriter.DbfSchema schema,
        Dictionary<string, object?>? seed)
    {
        var row = seed is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(seed, StringComparer.OrdinalIgnoreCase);

        foreach (var field in schema.Fields)
        {
            if (_parametarPropMap.TryGetValue(Normalize(field.Name), out var prop))
                row[field.Name] = prop.GetValue(parametar);
        }

        return row;
    }

    private static Dictionary<string, PropertyInfo> BuildPropMap()
        => typeof(LdParametar)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => Normalize(p.Name), p => p, StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string value)
        => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string? PronadjiDbf(string folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return null;

        var kandidati = new List<string>
        {
            folderPath,
            Path.Combine(folderPath, "data00"),
            Path.Combine(folderPath, "01"),
            Path.Combine(folderPath, "data01")
        };

        var parent = Directory.GetParent(folderPath)?.FullName;
        if (!string.IsNullOrWhiteSpace(parent))
        {
            kandidati.Add(parent);
            kandidati.Add(Path.Combine(parent, "data00"));
            kandidati.Add(Path.Combine(parent, "01"));
            kandidati.Add(Path.Combine(parent, "data01"));
        }

        foreach (var kandidat in kandidati.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var found = PronadjiDbfCaseInsensitive(kandidat, fileName);
            if (!string.IsNullOrWhiteSpace(found))
                return found;
        }

        return null;
    }

    private static string? PronadjiDbfCaseInsensitive(string? folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return null;

        var exact = Path.Combine(folderPath, fileName);
        if (File.Exists(exact))
            return exact;

        return Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    private void PripremiSnapshotZaIzmenu()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            _ldParamDbfPath = null;
            _snapshotNaPocetkuIzmene = null;
            _potpisNaPocetkuIzmene = null;
            return;
        }

        _ldParamDbfPath = PronadjiDbf(folderPath, "ldparam.dbf");
        if (string.IsNullOrWhiteSpace(_ldParamDbfPath) || !File.Exists(_ldParamDbfPath))
        {
            _snapshotNaPocetkuIzmene = null;
            _potpisNaPocetkuIzmene = null;
            return;
        }

        _snapshotNaPocetkuIzmene = DbfOptimisticConcurrency.CaptureFileSnapshot(_ldParamDbfPath);
        var postojeciRed = TryUcitajPostojeciRed(_ldParamDbfPath);
        _potpisNaPocetkuIzmene = postojeciRed is null
            ? null
            : DbfOptimisticConcurrency.ComputeRecordSignature(postojeciRed);
    }

    private static DbfTableWriter.DbfSchema UcitajSemuLdParam(string ciljnaPutanja)
    {
        if (File.Exists(ciljnaPutanja))
            return DbfTableWriter.LoadSchema(ciljnaPutanja);

        foreach (var root in KandidatiZaRoot())
        {
            var convertTemplate = Path.Combine(root, "newproject", "src", "Algoritam.WPF", "convert to sql", "ldparam.dbf");
            if (File.Exists(convertTemplate))
                return DbfTableWriter.LoadSchema(convertTemplate);

            var oldTemplate = Path.Combine(root, "old-project", "F1", "ldparam.dbf");
            if (File.Exists(oldTemplate))
                return DbfTableWriter.LoadSchema(oldTemplate);
        }

        throw new FileNotFoundException("Sema za ldparam.dbf nije pronađena.");
    }

    private static IEnumerable<string> KandidatiZaRoot()
    {
        var kandidati = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static void AddParents(HashSet<string> target, string? startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
                return;

            var dir = new DirectoryInfo(startPath);
            while (dir != null)
            {
                target.Add(dir.FullName);
                dir = dir.Parent;
            }
        }

        AddParents(kandidati, AppContext.BaseDirectory);
        AddParents(kandidati, Environment.CurrentDirectory);

        return kandidati;
    }

    private static Dictionary<string, object?>? TryUcitajPostojeciRed(string putanja)
    {
        if (!File.Exists(putanja))
            return null;

        var first = DbfReader.CitajSveZapise(putanja).FirstOrDefault();
        if (first is null)
            return null;

        return new Dictionary<string, object?>(first, StringComparer.OrdinalIgnoreCase);
    }

    private static (string Nazmes, int Dana, int RadniSati) IzracunajMesecInfoFox(int mesec, string? godinaText)
    {
        // mesec 13-24 = druga grupa, 25-36 = treća, 37-48 = četvrta (isti kalendarski mesec)
        var kalMesec = mesec > 12 ? ((mesec - 1) % 12) + 1 : Math.Clamp(mesec, 1, 12);
        var godina = ParseGodina(godinaText);
        var februarDana = DateTime.IsLeapYear(godina) ? 29 : 28;

        var (nazmes, dana) = kalMesec switch
        {
            1 => ("JANUAR", 31),
            2 => ("FEBRUAR", februarDana),
            3 => ("MART", 31),
            4 => ("APRIL", 30),
            5 => ("MAJ", 31),
            6 => ("JUN", 30),
            7 => ("JUL", 31),
            8 => ("AVGUST", 31),
            9 => ("SEPTEMBAR", 30),
            10 => ("OKTOBAR", 31),
            11 => ("NOVEMBAR", 30),
            12 => ("DECEMBAR", 31),
            _ => ("JANUAR", 31)
        };

        var datOd = new DateTime(godina, kalMesec, 1);
        var datDo = new DateTime(godina, kalMesec, dana);
        var sati = 0;

        for (var datum = datOd; datum <= datDo; datum = datum.AddDays(1))
        {
            if (datum.DayOfWeek != DayOfWeek.Saturday && datum.DayOfWeek != DayOfWeek.Sunday)
                sati += 8;
        }

        return (nazmes, dana, sati);
    }

    private static int ParseGodina(string? godinaText)
        => int.TryParse((godinaText ?? "").Trim(), out var godina) && godina >= 1900 && godina <= 2200
            ? godina
            : DateTime.Today.Year;

    private static IReadOnlyList<MesecOpcija> BuildMeseci()
    {
        var meseci = new[] { "JANUAR","FEBRUAR","MART","APRIL","MAJ","JUN",
                             "JUL","AVGUST","SEPTEMBAR","OKTOBAR","NOVEMBAR","DECEMBAR" };
        var grupe = new[] { "", " (II GR.)", " (III GR.)", " (IV GR.)" };
        var lista = new List<MesecOpcija>(48);
        for (int g = 0; g < 4; g++)
            for (int m = 0; m < 12; m++)
            {
                int vrednost = g * 12 + m + 1;
                lista.Add(new(vrednost, $"{vrednost:00} - {meseci[m]}{grupe[g]}"));
            }
        return lista;
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    private void UcitajNajOsnIzBaze(LdParametar param)
    {
        var dbPath = _appState.DbPath;
        if (string.IsNullOrWhiteSpace(dbPath)) return;
        try
        {
            using var db = new FirmaDbContext(dbPath);
            LdParametriSchemaBootstrapper.Ensure(db);
            var rec = db.LdParametri.AsNoTracking().FirstOrDefault();
            if (rec != null && rec.Najosn != 0)
                param.Najosn = rec.Najosn;
        }
        catch { }
    }

    private void SacuvajNajOsnUBazu(decimal najosn)
    {
        var dbPath = _appState.DbPath;
        if (string.IsNullOrWhiteSpace(dbPath)) return;
        try
        {
            using var db = new FirmaDbContext(dbPath);
            LdParametriSchemaBootstrapper.Ensure(db);
            var rec = db.LdParametri.FirstOrDefault();
            if (rec != null)
            {
                rec.Najosn = najosn;
                db.SaveChanges();
            }
        }
        catch { }
    }
}
