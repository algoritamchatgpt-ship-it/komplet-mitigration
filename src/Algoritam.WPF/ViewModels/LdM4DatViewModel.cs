using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class LdM4DatStavka : ObservableObject
{
    [ObservableProperty] private int      _mes;
    [ObservableProperty] private DateTime _dat = DateTime.Today;
    [ObservableProperty] private int      _cas;
    [ObservableProperty] private bool     _preneto;
}

public partial class LdM4DatViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private ObservableCollection<LdM4DatStavka> _stavke = [];
    [ObservableProperty] private LdM4DatStavka? _selektovana;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private bool _imaNeacuvana;

    public event Action? ZatvaranjeZahtevano;

    public LdM4DatViewModel(string folderPath)
    {
        _folderPath = folderPath;
        Ucitaj();
    }

    private void Ucitaj()
    {
        Stavke.Clear();
        var path = PronadjiDbf(_folderPath, "ldm4dat.dbf");
        if (path is null) { Poruka = "ldm4dat.dbf nije pronađen — datumi se ne mogu urediti."; return; }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(path);
            foreach (var z in zapisi)
            {
                Stavke.Add(new LdM4DatStavka
                {
                    Mes     = DecToInt(z, "MES"),
                    Dat     = DateVal(z, "DAT"),
                    Cas     = DecToInt(z, "CAS"),
                    Preneto = StrVal(z, "PRENETO") == "*",
                });
            }
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Učitano {Stavke.Count} stavki.";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    [RelayCommand]
    private void Dodaj()
    {
        var s = new LdM4DatStavka { Mes = Stavke.Count + 1, Dat = DateTime.Today };
        Stavke.Add(s);
        Selektovana = s;
        ImaNeacuvana = true;
    }

    [RelayCommand]
    private void BrisiRed()
    {
        if (Selektovana is null) return;
        Stavke.Remove(Selektovana);
        Selektovana = Stavke.LastOrDefault();
        ImaNeacuvana = true;
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = PronadjiDbf(_folderPath, "ldm4dat.dbf");
        if (path is null) { Poruka = "ldm4dat.dbf nije pronađen — nema gde da se sačuva."; return; }

        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            var lista = Stavke.ToList();

            DbfTableWriter.WriteTable(path, schema, lista, (s, fieldName) => fieldName switch
            {
                "MES"     => (decimal)s.Mes,
                "DAT"     => (object?)s.Dat,
                "CAS"     => (decimal)s.Cas,
                "PRENETO" => s.Preneto ? "*" : " ",
                "IDBR"    => 0m,
                _         => null
            });

            ImaNeacuvana = false;
            Poruka = $"Sačuvano {Stavke.Count} stavki u ldm4dat.dbf.";
        }
        catch (Exception ex) { Poruka = $"Greška pri čuvanju: {ex.Message}"; }
    }

    [RelayCommand]
    private void Zatvori()
    {
        if (ImaNeacuvana)
        {
            var r = System.Windows.MessageBox.Show(
                "Postoje nesačuvane izmene. Sačuvati pre zatvaranja?",
                "Datumi isplate M4",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);
            if (r == System.Windows.MessageBoxResult.Cancel) return;
            if (r == System.Windows.MessageBoxResult.Yes) Sacuvaj();
        }
        ZatvaranjeZahtevano?.Invoke();
    }

    private static string? PronadjiDbf(string folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath)) return null;
        var direct = Path.Combine(folderPath, fileName);
        if (File.Exists(direct)) return direct;
        return Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    private static int DecToInt(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v is null) return 0;
        if (v is decimal d) return (int)d;
        return 0;
    }

    private static DateTime DateVal(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v is null) return DateTime.Today;
        if (v is DateTime dt) return dt;
        return DateTime.Today;
    }

    private static string StrVal(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v is string s ? s.Trim() : "";
}
