using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NaldnevViewModel : ObservableObject
{
    private readonly List<NalpRow> _sviRedovi = [];

    public event Action? ZatvoriFormu;

    [ObservableProperty] private string _konto = string.Empty;
    [ObservableProperty] private DateTime? _datumOd;
    [ObservableProperty] private DateTime? _datumDo;
    [ObservableProperty] private string _dok = string.Empty;
    [ObservableProperty] private string _mp = string.Empty;
    [ObservableProperty] private string _mtr = "0";
    [ObservableProperty] private string _dugPotSve = "S";

    public NaldnevViewModel(string firmPath, int godina)
    {
        var aktivnaGodina = godina > 0 ? godina : DateTime.Today.Year;
        DatumOd = new DateTime(aktivnaGodina, 1, 1);
        DatumDo = new DateTime(aktivnaGodina, 12, 31);

        var path = Path.Combine(firmPath, "nal.dbf");
        if (!File.Exists(path))
            return;

        try
        {
            var reader = new SimpleDbfReader(path);
            _sviRedovi.AddRange(reader.Zapisi().Select(Nalp2ViewModel.NalpRowFromRecord));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Greška pri čitanju nal.dbf: {ex.Message}",
                "DNEVNIK GLAVNE KNJIGE",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Pregled()
    {
        var redovi = FormirajPregled(
            _sviRedovi, Konto, DatumOd, DatumDo, Dok, Mp, Mtr, DugPotSve);

        var vm = new NalogPregledViewModel(
            "DNEVNIK GLAVNE KNJIGE",
            redovi.Select(NalogPregledViewModel.IzNalp));
        new Views.NalogPregledWindow(vm).ShowDialog();
        ZatvoriFormu?.Invoke();
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal static IReadOnlyList<NalpRow> FormirajPregled(
        IEnumerable<NalpRow> redovi,
        string? konto,
        DateTime? datumOd,
        DateTime? datumDo,
        string? dok,
        string? mp,
        string? mtr,
        string? dugPotSve)
    {
        var kontoFilter = konto?.Trim() ?? string.Empty;
        var dokFilter = dok?.Trim() ?? string.Empty;
        var mpFilter = ParsirajOpcioniBroj(mp);
        var mtrFilter = ParsirajOpcioniBroj(mtr);
        var smer = dugPotSve?.Trim().ToUpperInvariant() switch
        {
            "D" => "D",
            "P" => "P",
            _ => "S",
        };

        var od = datumOd?.Date ?? DateTime.MinValue.Date;
        var doDatuma = datumDo?.Date ?? DateTime.MaxValue.Date;
        if (od > doDatuma)
            (od, doDatuma) = (doDatuma, od);

        return redovi
            .Where(r => string.IsNullOrEmpty(kontoFilter) ||
                        r.Konto.Trim().StartsWith(kontoFilter, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrEmpty(dokFilter) ||
                        r.Dok.Trim().Equals(dokFilter, StringComparison.OrdinalIgnoreCase))
            .Where(r => r.Datdok.HasValue &&
                        r.Datdok.Value.Date >= od &&
                        r.Datdok.Value.Date <= doDatuma)
            .Where(r => !mpFilter.HasValue || r.Mp == mpFilter.Value)
            .Where(r => !mtrFilter.HasValue || mtrFilter.Value == 0 || r.Mtr == mtrFilter.Value)
            .Where(r => smer == "S" ||
                        (smer == "D" && r.Dug != 0) ||
                        (smer == "P" && r.Pot != 0))
            .OrderBy(r => r.Datdok)
            .ThenBy(r => r.Brnal)
            .ThenBy(r => r.Konto)
            .ToList();
    }

    private static decimal? ParsirajOpcioniBroj(string? vrednost)
    {
        var tekst = vrednost?.Trim();
        if (string.IsNullOrEmpty(tekst))
            return null;

        return decimal.TryParse(tekst, out var broj) ? broj : null;
    }
}
