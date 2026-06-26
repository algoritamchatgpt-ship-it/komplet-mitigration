using Algoritam.Application.Services;
using Algoritam.Infrastructure.Dbf;
using Algoritam.Infrastructure.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class FormiranjeLozinkiViewModel : ObservableObject
{
    private readonly IPutanjaService _putanjaService;

    [ObservableProperty]
    private ObservableCollection<LozinkaStavka> _stavke = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ObrisiCommand))]
    private LozinkaStavka? _selektovana;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SacuvajCommand))]
    private string _id = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SacuvajCommand))]
    private string _korisnik = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SacuvajCommand))]
    private string _lozinka = string.Empty;

    [ObservableProperty]
    private string _poruka = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SacuvajCommand))]
    [NotifyCanExecuteChangedFor(nameof(ObrisiCommand))]
    private bool _ucitava;

    public FormiranjeLozinkiViewModel(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
        Osvezi();
    }

    partial void OnSelektovanaChanged(LozinkaStavka? value)
    {
        if (value is null)
            return;

        Id = value.Id;
        Korisnik = value.Korisnik;
        Lozinka = value.Lozinka;
    }

    private bool MozeSacuvaj() =>
        !Ucitava &&
        !string.IsNullOrWhiteSpace(Id) &&
        !string.IsNullOrWhiteSpace(Korisnik) &&
        !string.IsNullOrWhiteSpace(Lozinka);

    private bool MozeObrisi() => !Ucitava && Selektovana is not null;

    [RelayCommand]
    private void Osvezi()
    {
        Ucitava = true;
        try
        {
            if (!TryResolvePaths(out _, out var lozinkePath, out _, out var message))
            {
                Stavke = [];
                Poruka = message;
                return;
            }

            var rows = DbfReader.CitajSveZapise(lozinkePath);
            var lista = rows
                .Select(row => new LozinkaStavka
                {
                    Id = TwoDigit(row.Str("PAS")),
                    Korisnik = row.Str("KORISNIK"),
                    Lozinka = row.Str("LOZINKA"),
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Korisnik))
                .OrderBy(item => ParseId(item.Id))
                .ThenBy(item => item.Korisnik, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Stavke = new ObservableCollection<LozinkaStavka>(lista);
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano korisnika: {Stavke.Count}.";
        }
        catch (Exception ex)
        {
            Stavke = [];
            Poruka = $"Greska pri ucitavanju lozinki: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private void Novi()
    {
        Selektovana = null;
        Id = PredloziNaredniId();
        Korisnik = string.Empty;
        Lozinka = string.Empty;
        Poruka = "Unesite ID, korisničko ime i lozinku pa kliknite Sačuvaj.";
    }

    [RelayCommand(CanExecute = nameof(MozeSacuvaj))]
    private void Sacuvaj()
    {
        Ucitava = true;
        try
        {
            if (!TryParseOperatorId(Id, out var operatorId))
            {
                Poruka = "ID mora biti broj od 1 do 99.";
                return;
            }

            if (!TryResolvePaths(out var rootPath, out var lozinkePath, out var lozinkeaPath, out var message))
            {
                Poruka = message;
                return;
            }

            var korisnikIme = Korisnik.Trim();
            var lozinkaTekst = Lozinka.Trim();
            var pas = operatorId.ToString("00", CultureInfo.InvariantCulture);

            var schema = DbfTableWriter.LoadSchema(lozinkePath);
            var rows = DbfReader.CitajSveZapise(lozinkePath)
                .Select(CloneRow)
                .ToList();

            var duplicateByUsername = rows.FirstOrDefault(row =>
                string.Equals(row.Str("KORISNIK"), korisnikIme, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(TwoDigit(row.Str("PAS")), pas, StringComparison.OrdinalIgnoreCase));

            if (duplicateByUsername is not null)
            {
                Poruka = $"Korisnik '{korisnikIme}' vec postoji pod ID {TwoDigit(duplicateByUsername.Str("PAS"))}.";
                return;
            }

            var target = rows.FirstOrDefault(row =>
                string.Equals(TwoDigit(row.Str("PAS")), pas, StringComparison.OrdinalIgnoreCase));

            if (target is null)
            {
                target = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                rows.Add(target);
            }

            target["PAS"] = pas;
            target["KORISNIK"] = korisnikIme;
            target["LOZINKA"] = lozinkaTekst;
            target["KORIME"] = korisnikIme;
            target["AKTIVAN"] = "D";
            target["OPERATER"] = string.Equals(pas, "01", StringComparison.OrdinalIgnoreCase) ? "1" : target.Str("OPERATER");
            target["PASSNIVO"] = target.Dec("PASSNIVO") ?? 1m;
            target["DATUM"] = DateTime.Today;
            target["PRENETO"] = " ";
            target["IDBR"] = target.Dec("IDBR") ?? NextIdbr(rows);

            EnsureModuleRights(target);

            DbfTableWriter.WriteTable(
                lozinkePath,
                schema,
                rows,
                static (row, fieldName) => row.TryGetValue(fieldName, out var value) ? value : null);

            UpsertLozinkea(lozinkeaPath, pas, korisnikIme, target.Dec("IDBR") ?? 0m);
            FinWorkspaceResolver.EnsureDataFoldersForOperator(rootPath, operatorId);

            Poruka = $"Sačuvan korisnik '{korisnikIme}' (ID {pas}). Kreirani su data folderi za operatera.";
            Osvezi();
            Selektovana = Stavke.FirstOrDefault(item => item.Id == pas);
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri cuvanju: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand(CanExecute = nameof(MozeObrisi))]
    private void Obrisi()
    {
        if (Selektovana is null)
            return;

        Ucitava = true;
        try
        {
            if (!TryResolvePaths(out _, out var lozinkePath, out var lozinkeaPath, out var message))
            {
                Poruka = message;
                return;
            }

            var pas = TwoDigit(Selektovana.Id);
            var schema = DbfTableWriter.LoadSchema(lozinkePath);
            var rows = DbfReader.CitajSveZapise(lozinkePath)
                .Select(CloneRow)
                .Where(row => !string.Equals(TwoDigit(row.Str("PAS")), pas, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (rows.Count == 0)
            {
                Poruka = "Mora ostati bar jedan korisnik u tabeli LOZINKE.";
                return;
            }

            DbfTableWriter.WriteTable(
                lozinkePath,
                schema,
                rows,
                static (row, fieldName) => row.TryGetValue(fieldName, out var value) ? value : null);

            RemoveLozinkea(lozinkeaPath, pas);

            Poruka = $"Obrisan je korisnik ID {pas}.";
            Osvezi();
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri brisanju: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private bool TryResolvePaths(
        out string rootPath,
        out string lozinkePath,
        out string lozinkeaPath,
        out string message)
    {
        rootPath = string.Empty;
        lozinkePath = string.Empty;
        lozinkeaPath = string.Empty;
        message = string.Empty;

        var finPutanja = _putanjaService.DajFinPutanju();
        if (string.IsNullOrWhiteSpace(finPutanja))
        {
            message = "Putanja nije postavljena. Vratite se na pocetni ekran i izaberite putanju.";
            return false;
        }

        rootPath = FinWorkspaceResolver.NormalizeRootPath(finPutanja);
        if (!Directory.Exists(rootPath))
        {
            message = $"Putanja ne postoji: {rootPath}";
            return false;
        }

        if (!FinWorkspaceResolver.EnsureLozinkeTables(rootPath, out lozinkePath, out lozinkeaPath, out message))
            return false;

        return true;
    }

    private static void EnsureModuleRights(Dictionary<string, object?> row)
    {
        SetIfMissing(row, "PASSGK", true);
        SetIfMissing(row, "PASSAN", true);
        SetIfMissing(row, "PASSBL", true);
        SetIfMissing(row, "PASSTV", true);
        SetIfMissing(row, "PASSTM", true);
        SetIfMissing(row, "PASSUS", true);
        SetIfMissing(row, "PASSLD", true);
        SetIfMissing(row, "PASSOST", true);
        SetIfMissing(row, "PASSPRN", true);
        SetIfMissing(row, "PASSPRO", true);
        SetIfMissing(row, "PASSOS", true);
        SetIfMissing(row, "PASSPROF", true);
        SetIfMissing(row, "PASSDEL", true);
        SetIfMissing(row, "PASSTVRA", true);
        SetIfMissing(row, "PASSTVKAL", true);
        SetIfMissing(row, "PASSTVRAC", true);
        SetIfMissing(row, "PASSTVNIV", true);
        SetIfMissing(row, "PASSTMRA", true);
        SetIfMissing(row, "PASSTMKAL", true);
        SetIfMissing(row, "PASSTMRAC", true);
        SetIfMissing(row, "PASSTMNIV", true);
    }

    private static void UpsertLozinkea(string lozinkeaPath, string pas, string korisnik, decimal idbr)
    {
        var schema = DbfTableWriter.LoadSchema(lozinkeaPath);
        var rows = DbfReader.CitajSveZapise(lozinkeaPath)
            .Select(CloneRow)
            .ToList();

        var target = rows.FirstOrDefault(row =>
            string.Equals(TwoDigit(row.Str("PAS")), pas, StringComparison.OrdinalIgnoreCase));

        if (target is null)
        {
            target = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            rows.Add(target);
        }

        target["PAS"] = pas;
        target["KORISNIK"] = korisnik;
        target["AKTIVAN"] = "D";
        target["DATUM"] = DateTime.Today;
        target["VREME0"] = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        target["VREME1"] = string.Empty;
        target["PRENETO"] = " ";
        target["IDBR"] = idbr > 0m ? idbr : NextIdbr(rows);

        DbfTableWriter.WriteTable(
            lozinkeaPath,
            schema,
            rows,
            static (row, fieldName) => row.TryGetValue(fieldName, out var value) ? value : null);
    }

    private static void RemoveLozinkea(string lozinkeaPath, string pas)
    {
        var schema = DbfTableWriter.LoadSchema(lozinkeaPath);
        var rows = DbfReader.CitajSveZapise(lozinkeaPath)
            .Select(CloneRow)
            .Where(row => !string.Equals(TwoDigit(row.Str("PAS")), pas, StringComparison.OrdinalIgnoreCase))
            .ToList();

        DbfTableWriter.WriteTable(
            lozinkeaPath,
            schema,
            rows,
            static (row, fieldName) => row.TryGetValue(fieldName, out var value) ? value : null);
    }

    private static Dictionary<string, object?> CloneRow(Dictionary<string, object?> row)
        => new(row, StringComparer.OrdinalIgnoreCase);

    private string PredloziNaredniId()
    {
        var max = Stavke
            .Select(item => ParseId(item.Id))
            .DefaultIfEmpty(0)
            .Max();

        max = Math.Clamp(max + 1, 1, 99);
        return max.ToString("00", CultureInfo.InvariantCulture);
    }

    private static bool TryParseOperatorId(string value, out int operatorId)
    {
        operatorId = 0;
        if (!int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return false;

        if (parsed is < 1 or > 99)
            return false;

        operatorId = parsed;
        return true;
    }

    private static int ParseId(string value)
        => int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0;

    private static string TwoDigit(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id.ToString("00", CultureInfo.InvariantCulture)
            : trimmed;
    }

    private static decimal NextIdbr(IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var max = rows
            .Select(row => row.Dec("IDBR"))
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .DefaultIfEmpty(0m)
            .Max();

        return max + 1m;
    }

    private static void SetIfMissing(Dictionary<string, object?> row, string key, object value)
    {
        if (!row.ContainsKey(key))
            row[key] = value;
    }
}

public partial class LozinkaStavka : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _korisnik = string.Empty;
    [ObservableProperty] private string _lozinka = string.Empty;
}

internal static class FormiranjeLozinkiRecordExtensions
{
    public static string Str(this IReadOnlyDictionary<string, object?> row, string fieldName)
        => row.TryGetValue(fieldName, out var value)
            ? Convert.ToString(value)?.Trim() ?? string.Empty
            : string.Empty;

    public static decimal? Dec(this IReadOnlyDictionary<string, object?> row, string fieldName)
    {
        if (!row.TryGetValue(fieldName, out var value) || value is null)
            return null;

        return value switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            _ when decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }
}
