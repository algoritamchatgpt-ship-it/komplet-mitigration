using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALDOPUNIKONTO — DOPUNA KONTA SA 00: appends '00' to KONTO in nal.dbf and konto.dbf</summary>
public partial class NalDopuniKontoViewModel : ObservableObject
{
    private readonly string _firmPath;
    public event Action? ZatvoriFormu;

    [ObservableProperty] private string _dopuni = "N";

    public NalDopuniKontoViewModel(string firmPath)
    {
        _firmPath = firmPath;
    }

    [RelayCommand]
    private void Izlaz()
    {
        if (Dopuni.Trim().ToUpper() == "D")
            DopuniKonta();
        ZatvoriFormu?.Invoke();
    }

    private void DopuniKonta()
    {
        try
        {
            var nal = DopuniTabelu(Path.Combine(_firmPath, "nal.dbf"));
            var konto = DopuniTabelu(Path.Combine(_firmPath, "konto.dbf"));

            MessageBox.Show(
                $"Dopuna je završena.\n\nnal.dbf: {nal} redova\nkonto.dbf: {konto} redova",
                "DOPUNA KONTA SA 00",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška: {ex.Message}", "DOPUNA KONTA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    internal static int DopuniTabelu(string dbfPath)
    {
        if (!File.Exists(dbfPath)) return 0;

        var reader = new SimpleDbfReader(dbfPath);
        var schema = DbfTableWriter.LoadSchema(dbfPath);
        var kontoPolje = schema.Fields.FirstOrDefault(f =>
            f.Name.Equals("KONTO", StringComparison.OrdinalIgnoreCase));
        if (kontoPolje == null) return 0;

        var rows = new List<Dictionary<string, object?>>();
        var promenjeno = 0;

        foreach (var rec in reader.Zapisi())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in schema.Fields)
            {
                row[field.Name] = field.Type switch
                {
                    'D' => rec.DajDate(field.Name),
                    'N' or 'F' => rec.DajDecimal(field.Name),
                    'L' => rec.DajBool(field.Name),
                    _ => rec.DajString(field.Name),
                };
            }

            var konto = rec.DajString("KONTO");
            if (!string.IsNullOrWhiteSpace(konto))
            {
                row["KONTO"] = DopunjenoKonto(konto, kontoPolje.Length);
                promenjeno++;
            }

            rows.Add(row);
        }

        DbfTableWriter.WriteTable(
            dbfPath,
            schema,
            rows,
            (row, field) => row.TryGetValue(field, out var value) ? value : null);

        return promenjeno;
    }

    internal static string DopunjenoKonto(string konto, int sirinaPolja)
    {
        var rezultat = konto.Trim() + "00";
        return rezultat.Length <= sirinaPolja ? rezultat : rezultat[..sirinaPolja];
    }
}
