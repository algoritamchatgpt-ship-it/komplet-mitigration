using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// LDPUTNAL - Putni nalozi (FoxPro: DO FORM LDPUTNAL).
/// </summary>
public partial class LdPutniNaloziViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly Dictionary<int, string> _radniciByBroj = [];
    private DbfOptimisticConcurrency.FileSnapshot? _snapshot;

    [ObservableProperty] private ObservableCollection<LdPutniNalogStavka> _stavke = [];
    [ObservableProperty] private LdPutniNalogStavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private string _imeRadnika = string.Empty;
    [ObservableProperty] private string _naslov = "PUTNI NALOZI";

    public LdPutniNaloziViewModel(string folderPath)
    {
        _folderPath = folderPath;
        Ucitaj();
    }

    partial void OnSelektovanaChanged(LdPutniNalogStavka? value)
    {
        if (value == null) { ImeRadnika = string.Empty; return; }
        ImeRadnika = _radniciByBroj.TryGetValue(value.Broj, out var ime) ? ime : string.Empty;
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count == 0) return;
        Selektovana = Stavke[0];
    }

    [RelayCommand]
    private void Prethodni()
    {
        if (Selektovana == null || Stavke.Count == 0) return;
        var idx = Stavke.IndexOf(Selektovana);
        if (idx > 0) Selektovana = Stavke[idx - 1];
    }

    [RelayCommand]
    private void Sledeci()
    {
        if (Selektovana == null || Stavke.Count == 0) return;
        var idx = Stavke.IndexOf(Selektovana);
        if (idx < Stavke.Count - 1) Selektovana = Stavke[idx + 1];
    }

    [RelayCommand]
    private void Poslednji()
    {
        if (Stavke.Count == 0) return;
        Selektovana = Stavke[^1];
    }

    [RelayCommand]
    private void Dodaj()
    {
        var noviPutnal = Stavke.Count == 0 ? 1 : Stavke.Max(x => x.Putnal) + 1;
        var nova = new LdPutniNalogStavka
        {
            Putnal = noviPutnal,
            Datdok = DateTime.Today,
            Broj   = Selektovana?.Broj ?? 0,
        };
        Stavke.Add(nova);
        Selektovana = nova;
        Poruka = $"Dodat putni nalog br. {noviPutnal}. Kliknite JEDAN NALOG da popunite.";
    }

    [RelayCommand]
    private void BrisanjeJednog()
    {
        if (Selektovana == null) return;
        var br = Selektovana.Putnal;
        Stavke.Remove(Selektovana);
        Selektovana = Stavke.FirstOrDefault();
        Poruka = $"Obrisan putni nalog br. {br}.";
    }

    [RelayCommand]
    private void JedanNalog()
    {
        if (Selektovana == null)
        {
            Poruka = "Selektujte putni nalog u tabeli, ili kliknite DODAJ za novi.";
            return;
        }

        var vm = new LdPutnal1ViewModel(Selektovana, _radniciByBroj);
        var dlg = new Algoritam.WPF.Views.Zarade.LdPutnal1View { DataContext = vm };
        vm.ZatvaranjeZahtevano += () => dlg.Close();
        dlg.ShowDialog();

        if (!vm.Sačuvano) return;

        vm.PrenesiUStavku(Selektovana);
        ImeRadnika = _radniciByBroj.TryGetValue(Selektovana.Broj, out var ime) ? ime : string.Empty;
        Poruka = $"Putni nalog br. {Selektovana.Putnal} ažuriran.";
    }

    [RelayCommand]
    private void PregledZaPeriod()
    {
        var vm = new LdPutnal2ViewModel(Stavke.ToList(), _radniciByBroj);
        var dlg = new Algoritam.WPF.Views.Zarade.LdPutnal2View { DataContext = vm };
        vm.ZatvaranjeZahtevano += () => dlg.Close();
        dlg.Show();
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        if (Stavke.Count == 0) { Poruka = "Nema podataka za čuvanje."; return; }
        if (string.IsNullOrWhiteSpace(_folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var putnalPath = Path.Combine(_folderPath, "putnal.dbf");
        if (!File.Exists(putnalPath)) { Poruka = "putnal.dbf nije pronađen — ne mogu da sačuvam."; return; }

        if (_snapshot != null && DbfOptimisticConcurrency.HasFileChanged(putnalPath, _snapshot))
        {
            var r = System.Windows.MessageBox.Show(
                "Fajl putnal.dbf je izmenjen od strane drugog korisnika.\nNastaviti sa čuvanjem (prepisati)?",
                "Upozorenje — dual korisnici",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (r != System.Windows.MessageBoxResult.Yes) return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(putnalPath);
            var rows = Stavke.Select(s => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["PUTNAL"]   = (decimal)s.Putnal,
                ["DATDOK"]   = s.Datdok,
                ["DATVRAC"]  = s.Datvrac,
                ["CILJ"]     = s.Cilj,
                ["SVRHA1"]   = s.Svrha1,
                ["SVRHA2"]   = s.Svrha2,
                ["IZVEST1"]  = s.Izvest1,
                ["IZVEST2"]  = s.Izvest2,
                ["IZVEST3"]  = s.Izvest3,
                ["AKONTAC"]  = s.Akontac,
                ["VOZILO"]   = s.Vozilo,
                ["RACUN1"]   = s.Racun1,  ["BRRAC1"] = s.Brrac1,
                ["RACUN2"]   = s.Racun2,  ["BRRAC2"] = s.Brrac2,
                ["RACUN3"]   = s.Racun3,  ["BRRAC3"] = s.Brrac3,
                ["RACUN4"]   = s.Racun4,  ["BRRAC4"] = s.Brrac4,
                ["RACUN5"]   = s.Racun5,  ["BRRAC5"] = s.Brrac5,
                ["SVEGA"]    = s.Svega,
                ["ZAISPLAT"] = s.Zaisplat,
                ["NAPOMENA"] = s.Napomena,
                ["BROJ"]     = (decimal)s.Broj,
                ["CASPOL"]   = (decimal)s.Caspol,
                ["CASDOL"]   = (decimal)s.Casdol,
                ["BRDNEV"]   = s.Brdnev,
                ["DNEVNICA"] = s.Dnevnica,
                ["IZNOSDN"]  = s.Iznosdn,
                ["TROSAK"]   = s.Trosak,
                ["PRENETO"]  = s.Preneto,
                ["IDBR"]     = (decimal)s.Idbr,
            }).ToList<Dictionary<string, object?>>();

            DbfTableWriter.WriteTable(
                putnalPath,
                schema,
                rows,
                static (row, field) => row.TryGetValue(field, out var v) ? v : null);

            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(putnalPath);
            Poruka = $"Sačuvano {Stavke.Count} putnih naloga u putnal.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čuvanju: {ex.Message}";
        }
    }

    private void Ucitaj()
    {
        Stavke.Clear();
        _radniciByBroj.Clear();

        if (string.IsNullOrWhiteSpace(_folderPath)) { Poruka = "Nije izabrana firma."; return; }

        UcitajRadnike();

        var putnalPath = Path.Combine(_folderPath, "putnal.dbf");
        if (!File.Exists(putnalPath)) { Poruka = "putnal.dbf nije pronađen."; return; }

        try
        {
            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(putnalPath);
            var zapisi = DbfReader.CitajSveZapise(putnalPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new LdPutniNalogStavka
                {
                    Putnal   = Int(z, "PUTNAL"),
                    Datdok   = Dat(z, "DATDOK"),
                    Datvrac  = Dat(z, "DATVRAC"),
                    Cilj     = Str(z, "CILJ"),
                    Svrha1   = Str(z, "SVRHA1"),
                    Svrha2   = Str(z, "SVRHA2"),
                    Izvest1  = Str(z, "IZVEST1"),
                    Izvest2  = Str(z, "IZVEST2"),
                    Izvest3  = Str(z, "IZVEST3"),
                    Akontac  = Dec(z, "AKONTAC"),
                    Vozilo   = Str(z, "VOZILO"),
                    Racun1   = Dec(z, "RACUN1"),  Brrac1 = Str(z, "BRRAC1"),
                    Racun2   = Dec(z, "RACUN2"),  Brrac2 = Str(z, "BRRAC2"),
                    Racun3   = Dec(z, "RACUN3"),  Brrac3 = Str(z, "BRRAC3"),
                    Racun4   = Dec(z, "RACUN4"),  Brrac4 = Str(z, "BRRAC4"),
                    Racun5   = Dec(z, "RACUN5"),  Brrac5 = Str(z, "BRRAC5"),
                    Svega    = Dec(z, "SVEGA"),
                    Zaisplat = Dec(z, "ZAISPLAT"),
                    Napomena = Str(z, "NAPOMENA"),
                    Broj     = Int(z, "BROJ"),
                    Caspol   = Int(z, "CASPOL"),
                    Casdol   = Int(z, "CASDOL"),
                    Brdnev   = Dec(z, "BRDNEV"),
                    Dnevnica = Dec(z, "DNEVNICA"),
                    Iznosdn  = Dec(z, "IZNOSDN"),
                    Trosak   = Dec(z, "TROSAK"),
                    Preneto  = Str(z, "PRENETO"),
                    Idbr     = Long(z, "IDBR"),
                });
            }
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Učitano {Stavke.Count} putnih naloga.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri učitavanju: {ex.Message}";
        }
    }

    private void UcitajRadnike()
    {
        var ldradPath = Path.Combine(_folderPath, "ldrad.dbf");
        if (!File.Exists(ldradPath)) return;
        foreach (var z in DbfReader.CitajSveZapise(ldradPath))
        {
            var br = Int(z, "BROJ");
            if (br != 0 && !_radniciByBroj.ContainsKey(br))
                _radniciByBroj[br] = Str(z, "IME_PREZ");
        }
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;
    private static int Int(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? (int)d : 0;
    private static long Long(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? (long)d : 0L;
    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;
    private static DateTime Dat(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is DateTime dt ? dt : DateTime.Today;
}

public class LdPutniNalogStavka
{
    public int Putnal { get; set; }
    public DateTime Datdok { get; set; }
    public DateTime Datvrac { get; set; }
    public string Cilj { get; set; } = string.Empty;
    public string Svrha1 { get; set; } = string.Empty;
    public string Svrha2 { get; set; } = string.Empty;
    public string Izvest1 { get; set; } = string.Empty;
    public string Izvest2 { get; set; } = string.Empty;
    public string Izvest3 { get; set; } = string.Empty;
    public decimal Akontac { get; set; }
    public string Vozilo { get; set; } = string.Empty;
    public decimal Racun1 { get; set; }
    public string Brrac1 { get; set; } = string.Empty;
    public decimal Racun2 { get; set; }
    public string Brrac2 { get; set; } = string.Empty;
    public decimal Racun3 { get; set; }
    public string Brrac3 { get; set; } = string.Empty;
    public decimal Racun4 { get; set; }
    public string Brrac4 { get; set; } = string.Empty;
    public decimal Racun5 { get; set; }
    public string Brrac5 { get; set; } = string.Empty;
    public decimal Svega { get; set; }
    public decimal Zaisplat { get; set; }
    public string Napomena { get; set; } = string.Empty;
    public int Broj { get; set; }
    public int Caspol { get; set; }
    public int Casdol { get; set; }
    public decimal Brdnev { get; set; }
    public decimal Dnevnica { get; set; }
    public decimal Iznosdn { get; set; }
    public decimal Trosak { get; set; }
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}
