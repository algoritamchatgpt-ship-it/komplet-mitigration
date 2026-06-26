using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public record GkNalogZaglavlje(
    string BrNal, DateTime Datum, string VrNal, string Opis,
    decimal Dug, decimal Pot, DateTime? DatKnji, string Preneto);

public record GkNalogStavkaPregled(
    string Konto, decimal Dug, decimal Pot, decimal DodDug, decimal DodPot,
    string Opis, DateTime DatDok, string Dok, string BrDok, string Mp, decimal Mtr,
    string Dev, decimal DevKurs, string Dp);

public partial class GkNaloziViewModel : ObservableObject
{
    private readonly AppState _appState;
    private List<GkNalogZaglavlje> _sviNalozi = [];

    public string Naslov => "PREGLED NALOGA";
    public string FolderPath => _appState.AktivnaFirma?.FolderPath ?? string.Empty;

    [ObservableProperty] private ObservableCollection<GkNalogZaglavlje> _nalozi = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(IzmeniCommand))]
    private GkNalogZaglavlje? _izabraniNalog;
    [ObservableProperty] private ObservableCollection<GkNalogStavkaPregled> _stavke = [];
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private string _filterTekst = "";

    public decimal UkupnoDug => Stavke.Sum(s => s.Dug);
    public decimal UkupnoPot => Stavke.Sum(s => s.Pot);

    public event Action? ZatvaranjeZahtevano;

    public GkNaloziViewModel(AppState appState)
    {
        _appState = appState;
        _ = UcitajNalozeAsync();
    }

    private async Task UcitajNalozeAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje naloga...";
        try
        {
            var zaglavijaPath = NadjiDbf(FolderPath, "nalbroj.dbf");
            if (zaglavijaPath is null)
            {
                StatusPoruka = "Tabela nalbroj.dbf nije pronađena.";
                return;
            }

            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(zaglavijaPath));
            _sviNalozi = zapisi.Select(MapirajZaglavlje).ToList();
            PrimenjiFilter();
            StatusPoruka = $"Nalozi: {_sviNalozi.Count}";
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greška: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private static GkNalogZaglavlje MapirajZaglavlje(Dictionary<string, object?> r)
    {
        static string G(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) ? v?.ToString()?.Trim() ?? "" : "";
        static decimal D(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) && v is not null && decimal.TryParse(v.ToString(), out var n) ? n : 0;
        static DateTime Dt(Dictionary<string, object?> d, string k, DateTime def = default) =>
            d.TryGetValue(k, out var v) && v is DateTime dt ? dt :
            d.TryGetValue(k, out var v2) && DateTime.TryParse(v2?.ToString(), out var dt2) ? dt2 : def;

        var datKnji = Dt(r, "DATKNJI");
        return new GkNalogZaglavlje(
            G(r, "BRNAL"), Dt(r, "DATUM"), G(r, "VRNAL"), G(r, "OPIS"),
            D(r, "DUG"), D(r, "POT"),
            datKnji == default ? null : datKnji,
            G(r, "PRENETO"));
    }

    partial void OnFilterTekstChanged(string value) => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterTekst))
        {
            Nalozi = new ObservableCollection<GkNalogZaglavlje>(_sviNalozi);
            return;
        }
        var q = FilterTekst.ToLowerInvariant();
        Nalozi = new ObservableCollection<GkNalogZaglavlje>(
            _sviNalozi.Where(n =>
                n.BrNal.ToLowerInvariant().Contains(q) ||
                n.Opis.ToLowerInvariant().Contains(q) ||
                n.VrNal.ToLowerInvariant().Contains(q)));
    }

    partial void OnIzabraniNalogChanged(GkNalogZaglavlje? value)
    {
        if (value is null) { Stavke.Clear(); return; }
        _ = UcitajStavkeAsync(value.BrNal);
    }

    private async Task UcitajStavkeAsync(string brNal)
    {
        try
        {
            var stavkePath = NadjiDbf(FolderPath, "nalp.dbf")
                          ?? NadjiDbf(FolderPath, "nal.dbf");
            if (stavkePath is null) return;

            var sveStavke = await Task.Run(() => DbfReader.CitajSveZapise(stavkePath));
            var filtrirane = sveStavke
                .Where(s => s.TryGetValue("BRNAL", out var bn) &&
                            bn?.ToString()?.Trim() == brNal)
                .Select(MapirajStavkuPregled)
                .ToList();

            Stavke = new ObservableCollection<GkNalogStavkaPregled>(filtrirane);
            OnPropertyChanged(nameof(UkupnoDug));
            OnPropertyChanged(nameof(UkupnoPot));
        }
        catch { }
    }

    private static GkNalogStavkaPregled MapirajStavkuPregled(Dictionary<string, object?> r)
    {
        static string G(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) ? v?.ToString()?.Trim() ?? "" : "";
        static decimal D(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) && v is not null && decimal.TryParse(v.ToString(), out var n) ? n : 0;
        static DateTime Dt(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) && v is DateTime dt ? dt :
            d.TryGetValue(k, out var v2) && DateTime.TryParse(v2?.ToString(), out var dt2) ? dt2 : DateTime.MinValue;

        return new GkNalogStavkaPregled(
            G(r, "KONTO"), D(r, "DUG"), D(r, "POT"), D(r, "DODDUG"), D(r, "DODPOT"),
            G(r, "OPIS"), Dt(r, "DATDOK"), G(r, "DOK"), G(r, "BRDOK"), G(r, "MP"), D(r, "MTR"),
            G(r, "DEV"), D(r, "DEVKURS"), G(r, "DP"));
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajNalozeAsync();

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    [RelayCommand(CanExecute = nameof(MozeIzmeniti))]
    private void Izmeni()
    {
        if (IzabraniNalog is null) return;

        var vm = new GkNalogUnosViewModel(FolderPath, IzabraniNalog.BrNal);
        var view = new Views.GlavnaKnjiga.GkNalogUnosView { DataContext = vm };
        view.ShowDialog();

        if (vm.Uspesno)
            _ = UcitajNalozeAsync();
    }

    private bool MozeIzmeniti() => IzabraniNalog is not null && IzabraniNalog.DatKnji is null;

    private static string? NadjiDbf(string folderPath, string fileName)
    {
        foreach (var dir in new[] { folderPath,
            Path.Combine(folderPath, "data00"),
            Path.Combine(folderPath, "01") })
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
