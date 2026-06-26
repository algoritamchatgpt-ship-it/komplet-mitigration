using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalkarticeViewModel : ObservableObject
{
    private readonly List<NalpRow> _sviRedovi = [];

    public event Action? ZatvoriFormu;

    [ObservableProperty] private string _konto = string.Empty;
    [ObservableProperty] private DateTime? _datumOd;
    [ObservableProperty] private DateTime? _datumDo;
    [ObservableProperty] private string _dok = string.Empty;
    [ObservableProperty] private string _mp = string.Empty;
    [ObservableProperty] private string _mtr = "0";

    public NalkarticeViewModel(string firmPath, int godina)
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
                "KARTICE GLAVNE KNJIGE",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Pregled()
    {
        var redovi = FormirajKarticu(
            _sviRedovi, Konto, DatumOd, DatumDo, Dok, Mp, Mtr);

        var vm = new NalogPregledViewModel(
            "KARTICE GLAVNE KNJIGE",
            redovi.Select(NalogPregledViewModel.IzNalp));
        new Views.NalogPregledWindow(vm).ShowDialog();
        ZatvoriFormu?.Invoke();
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal static IReadOnlyList<NalpRow> FormirajKarticu(
        IEnumerable<NalpRow> redovi,
        string? konto,
        DateTime? datumOd,
        DateTime? datumDo,
        string? dok,
        string? mp,
        string? mtr)
    {
        var saSaldom = new List<NalpRow>();
        foreach (var grupa in redovi
                     .OrderBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
                     .ThenBy(r => r.Datdok)
                     .ThenBy(r => r.Brnal.Trim(), StringComparer.OrdinalIgnoreCase)
                     .GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            decimal saldo = 0;
            foreach (var red in grupa)
            {
                saldo += red.Dug - red.Pot;
                var kopija = red.Clone();
                kopija.Dpsaldo = saldo;
                saSaldom.Add(kopija);
            }
        }

        return NaldnevViewModel.FormirajPregled(
            saSaldom, konto, datumOd, datumDo, dok, mp, mtr, "S");
    }
}
