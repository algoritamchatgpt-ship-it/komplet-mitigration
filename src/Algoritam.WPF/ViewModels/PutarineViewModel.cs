using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// PUTAR — Evidencija putarina (FoxPro: DO FORM PUTAR).
/// </summary>
public partial class PutarineViewModel : ObservableObject
{
    private readonly string _folderPath;
    private DbfOptimisticConcurrency.FileSnapshot? _snapshot;

    [ObservableProperty] private ObservableCollection<PutarinaStavka> _stavke = [];
    [ObservableProperty] private PutarinaStavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private string _naslov = "EVIDENCIJA PUTARINA";
    [ObservableProperty] private string _pretragaTekst = string.Empty;

    public PutarineViewModel(string folderPath)
    {
        _folderPath = folderPath;
        Ucitaj();
    }

    [RelayCommand] private void Prvi() { if (Stavke.Count > 0) Selektovana = Stavke[0]; }
    [RelayCommand] private void Prethodni()
    {
        if (Selektovana == null) return;
        var idx = Stavke.IndexOf(Selektovana);
        if (idx > 0) Selektovana = Stavke[idx - 1];
    }
    [RelayCommand] private void Sledeci()
    {
        if (Selektovana == null) return;
        var idx = Stavke.IndexOf(Selektovana);
        if (idx < Stavke.Count - 1) Selektovana = Stavke[idx + 1];
    }
    [RelayCommand] private void Poslednji() { if (Stavke.Count > 0) Selektovana = Stavke[^1]; }

    [RelayCommand]
    private void Dodaj()
    {
        var preth = Stavke.LastOrDefault();
        var maxStr = Stavke.Count == 0 ? "0" : (Stavke.Max(s => s.Redbr) ?? "0");
        var noviRedbr = int.TryParse(maxStr.Trim(), out var n) ? n + 1 : Stavke.Count + 1;
        var nova = new PutarinaStavka
        {
            Redbr  = noviRedbr.ToString("000000"),
            Datdok = preth?.Datdok ?? DateTime.Today,
            Grupa  = preth?.Grupa ?? string.Empty,
        };

        var vm = new PutarKarticaViewModel(_folderPath, nova);
        var dlg = new Algoritam.WPF.Views.Zarade.PutarKarticaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += () => dlg.Close();
        dlg.ShowDialog();
        if (!vm.Sačuvano) return;

        vm.PrenesiUStavku(nova);
        Stavke.Add(nova);
        Selektovana = nova;
        Poruka = $"Dodat zapis putarine: {nova.Relacija}.";
    }

    [RelayCommand]
    private void BrisanjeZadnjeg()
    {
        if (Stavke.Count == 0) return;
        var zadnji = Stavke[^1];
        Stavke.Remove(zadnji);
        Selektovana = Stavke.LastOrDefault();
        Poruka = "Obrisan zadnji zapis.";
    }

    [RelayCommand]
    private void KarticaF7()
    {
        if (Selektovana == null) { Poruka = "Selektujte zapis za uređivanje."; return; }

        var vm = new PutarKarticaViewModel(_folderPath, Selektovana);
        var dlg = new Algoritam.WPF.Views.Zarade.PutarKarticaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += () => dlg.Close();
        dlg.ShowDialog();
        if (!vm.Sačuvano) return;

        vm.PrenesiUStavku(Selektovana);
        Poruka = $"Zapis ažuriran: {Selektovana.Relacija}.";
        var idx = Stavke.IndexOf(Selektovana);
        if (idx >= 0)
        {
            Stavke[idx] = Selektovana;
            Selektovana = Stavke[idx];
        }
    }

    [RelayCommand]
    private void RelacijeF4()
    {
        var vm = new PutarRelacijeViewModel(_folderPath);
        var dlg = new Algoritam.WPF.Views.Zarade.PutarRelacijeView { DataContext = vm };
        vm.ZatvaranjeZahtevano += () => dlg.Close();
        dlg.Show();
    }

    [RelayCommand]
    private void PregledZaPeriod()
    {
        var vm = new PutarinePregledViewModel(Stavke.ToList());
        var dlg = new Algoritam.WPF.Views.Zarade.PutarinePregledView { DataContext = vm };
        vm.ZatvaranjeZahtevano += () => dlg.Close();
        dlg.Show();
    }

    [RelayCommand]
    private void Trazi()
    {
        if (string.IsNullOrWhiteSpace(PretragaTekst)) return;
        var upit = PretragaTekst.Trim();
        var found = Stavke.FirstOrDefault(s =>
            s.Relacija.Contains(upit, StringComparison.OrdinalIgnoreCase) ||
            s.Putnal.Contains(upit, StringComparison.OrdinalIgnoreCase) ||
            s.Sifrel.Contains(upit, StringComparison.OrdinalIgnoreCase));
        if (found != null) { Selektovana = found; Poruka = $"Pronađeno: {found.Relacija}"; }
        else Poruka = "Nije pronađen zapis.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        if (Stavke.Count == 0) { Poruka = "Nema podataka za čuvanje."; return; }
        if (string.IsNullOrWhiteSpace(_folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var path = Path.Combine(_folderPath, "putar.dbf");
        if (!File.Exists(path)) { Poruka = "putar.dbf nije pronađen."; return; }

        if (_snapshot != null && DbfOptimisticConcurrency.HasFileChanged(path, _snapshot))
        {
            var r = System.Windows.MessageBox.Show(
                "Fajl putar.dbf je izmenjen od strane drugog korisnika.\nNastaviti sa čuvanjem (prepisati)?",
                "Upozorenje — dual korisnici",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (r != System.Windows.MessageBoxResult.Yes) return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            var rows = Stavke.Select(s => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["REDBR"]    = s.Redbr,
                ["PUTNAL"]   = s.Putnal,
                ["DATDOK"]   = s.Datdok,
                ["VREME"]    = s.Vreme,
                ["SIFREL"]   = s.Sifrel,
                ["RELACIJA"] = s.Relacija,
                ["IZNOS"]    = s.Iznos,
                ["GRUPA"]    = s.Grupa,
                ["KOL"]      = s.Kol,
                ["PRENETO"]  = s.Preneto,
                ["IDBR"]     = s.Idbr,
            }).ToList<Dictionary<string, object?>>();

            DbfTableWriter.WriteTable(path, schema, rows,
                static (row, field) => row.TryGetValue(field, out var v) ? v : null);
            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(path);
            Poruka = $"Sačuvano {Stavke.Count} zapisa u putar.dbf.";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    private void Ucitaj()
    {
        Stavke.Clear();
        if (string.IsNullOrWhiteSpace(_folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var path = Path.Combine(_folderPath, "putar.dbf");
        if (!File.Exists(path)) { Poruka = "putar.dbf nije pronađen."; return; }

        try
        {
            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(path);
            foreach (var z in DbfReader.CitajSveZapise(path))
            {
                Stavke.Add(new PutarinaStavka
                {
                    Redbr    = Str(z, "REDBR"),
                    Putnal   = Str(z, "PUTNAL"),
                    Datdok   = Dat(z, "DATDOK"),
                    Vreme    = Str(z, "VREME"),
                    Sifrel   = Str(z, "SIFREL"),
                    Relacija = Str(z, "RELACIJA"),
                    Iznos    = Dec(z, "IZNOS"),
                    Grupa    = Str(z, "GRUPA"),
                    Kol      = Dec(z, "KOL"),
                    Preneto  = Str(z, "PRENETO"),
                    Idbr     = Str(z, "IDBR"),
                });
            }
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Učitano {Stavke.Count} zapisa putarina.";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    [RelayCommand] private void Osvezi() => Ucitaj();

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;
    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;
    private static DateTime Dat(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is DateTime dt ? dt : DateTime.Today;
}

public partial class PutarinaStavka : ObservableObject
{
    [ObservableProperty] private string _redbr = string.Empty;
    [ObservableProperty] private string _putnal = string.Empty;
    [ObservableProperty] private DateTime _datdok = DateTime.Today;
    [ObservableProperty] private string _vreme = string.Empty;
    [ObservableProperty] private string _sifrel = string.Empty;
    [ObservableProperty] private string _relacija = string.Empty;
    [ObservableProperty] private decimal _iznos;
    [ObservableProperty] private string _grupa = string.Empty;
    [ObservableProperty] private decimal _kol = 1m;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private string _idbr = string.Empty;
}
