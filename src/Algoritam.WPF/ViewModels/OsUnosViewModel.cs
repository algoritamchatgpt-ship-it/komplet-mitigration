using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class OsPolje : ObservableObject
{
    public string Name { get; }
    public char FieldType { get; }
    public int Length { get; }
    public int Decimals { get; }

    [ObservableProperty] private string _vrednost = "";
    [ObservableProperty] private DateTime? _datumVrednost;
    [ObservableProperty] private bool _boolVrednost;

    public bool JeDatum           => FieldType == 'D';
    public bool JeBool            => FieldType == 'L';
    public bool JeTekstIliBrojcano => FieldType is 'C' or 'N' or 'F';

    public string Labela => MapLabelu(Name);

    public OsPolje(DbfTableWriter.DbfField field)
    {
        Name = field.Name; FieldType = field.Type; Length = field.Length; Decimals = field.Decimals;
    }

    private static string MapLabelu(string name) => name.ToUpperInvariant() switch
    {
        "SIFOS" or "SIFRA"            => "Šifra OS",
        "NAZIV"                        => "Naziv",
        "GRBROJ"                       => "Br. grupe",
        "DATUM"                        => "Datum nabavke",
        "INVBR"                        => "Inv. broj",
        "NABVRED"                      => "Nabavna vrednost",
        "OTPISANO" or "AMORT"          => "Otpisano",
        "SVRED" or "SADASNJA"          => "Sadašnja vrednost",
        "STOPA"                        => "Stopa amort. (%)",
        "VRSTA"                        => "Vrsta (P/N/S)",
        "LOKACIJA"                     => "Lokacija",
        "NAPOMENA" or "OPIS"           => "Napomena",
        "OTPIS" or "OTPISAN"           => "Otpisano",
        "DATOTPIS"                     => "Datum otpisa",
        "PRENOS"                       => "Prenet",
        "DATOPR"                       => "Datum prenosa",
        _ => name
    };
}

/// <summary>
/// ViewModel za unos novog osnovanog sredstva u os.dbf.
/// Dinamički čita shemu DBF-a i kreira editabilna polja.
/// </summary>
public partial class OsUnosViewModel : ObservableObject
{
    private readonly string? _dbfPath;
    private DbfTableWriter.DbfSchema? _schema;

    public ObservableCollection<OsPolje> Polja { get; } = [];
    public bool Uspesno { get; private set; }

    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _imaGresku;

    public OsUnosViewModel(string folderPath)
    {
        _dbfPath = NadjiDbf(folderPath, "os.dbf");
        UcitajShemu();
    }

    private void UcitajShemu()
    {
        if (_dbfPath is null || !File.Exists(_dbfPath))
        {
            StatusPoruka = "Tabela os.dbf nije pronađena u folderu firme.";
            ImaGresku = true;
            return;
        }
        try
        {
            _schema = DbfTableWriter.LoadSchema(_dbfPath);
            foreach (var field in _schema.Fields)
                Polja.Add(new OsPolje(field));
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greška pri čitanju sheme: {ex.Message}";
            ImaGresku = true;
        }
    }

    [RelayCommand(CanExecute = nameof(MozeSacuvati))]
    private void Sacuvaj(System.Windows.Window? window)
    {
        if (_schema is null || _dbfPath is null) return;

        try
        {
            var zapis = new Dictionary<string, object?>();
            foreach (var polje in Polja)
            {
                object? vrednost;
                if (polje.JeDatum)
                    vrednost = polje.DatumVrednost;
                else if (polje.JeBool)
                    vrednost = polje.BoolVrednost;
                else if (polje.FieldType is 'N' or 'F')
                {
                    var tekst = polje.Vrednost.Replace(',', '.');
                    vrednost = decimal.TryParse(tekst, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out var d) ? d : (decimal?)0m;
                }
                else
                    vrednost = polje.Vrednost;

                zapis[polje.Name] = vrednost;
            }

            DbfTableWriter.DodajRedove(_dbfPath, _schema, [zapis]);
            Uspesno = true;
            window?.Close();
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greška pri čuvanju: {ex.Message}";
        }
    }

    private bool MozeSacuvati() => !ImaGresku;

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
                    .FirstOrDefault(x => Path.GetFileName(x).Equals(fileName,
                        StringComparison.OrdinalIgnoreCase));
                if (ci is not null) return ci;
            }
            catch { }
        }
        return null;
    }
}
