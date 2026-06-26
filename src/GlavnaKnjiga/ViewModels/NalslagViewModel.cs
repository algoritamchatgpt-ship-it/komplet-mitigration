using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalslagViewModel : ObservableObject
{
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;
    public event Action<IReadOnlyList<NalslagRow>>? OtvoriNalSlag2;

    [ObservableProperty] private DateTime? _pocetniDatum;
    [ObservableProperty] private DateTime? _zadnjiDatum;
    [ObservableProperty] private bool _prikaziSve;
    [ObservableProperty] private ObservableCollection<NalslagRow> _redovi = [];
    [ObservableProperty] private NalslagRow? _izabraniRed;
    [ObservableProperty] private string _status = "Izaberite period i pokrenite pregled.";

    public NalslagViewModel(string firmPath, int godina)
    {
        _firmPath = firmPath;
        PocetniDatum = new DateTime(godina, 1, 1);
        ZadnjiDatum = new DateTime(godina, 12, 31);
    }

    [RelayCommand]
    private void Pregled()
    {
        if (PocetniDatum == null || ZadnjiDatum == null)
        {
            MessageBox.Show("Unesite početni i zadnji datum.", "PROVERA SLAGANJA NALOGA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (PocetniDatum > ZadnjiDatum)
        {
            MessageBox.Show("Početni datum ne može biti posle zadnjeg datuma.",
                "PROVERA SLAGANJA NALOGA", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var path = Path.Combine(_firmPath, "nal.dbf");
        if (!File.Exists(path))
        {
            MessageBox.Show("nal.dbf ne postoji.", "PROVERA SLAGANJA NALOGA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var izvor = new SimpleDbfReader(path).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord);
            var rezultat = FormirajPregled(izvor);
            Redovi = new ObservableCollection<NalslagRow>(rezultat);
            IzabraniRed = Redovi.FirstOrDefault();

            var neslozeni = rezultat.Count(r => !r.JeSlozen);
            Status = $"Naloga: {rezultat.Count}   |   Nesloženih: {neslozeni}";
            OtvoriNalSlag2?.Invoke(rezultat);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri proveri slaganja:\n{ex.Message}",
                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal List<NalslagRow> FormirajPregled(IEnumerable<NalpRow> izvor)
    {
        if (PocetniDatum == null || ZadnjiDatum == null) return [];

        var rezultat = izvor
            .Where(r => r.Datdok >= PocetniDatum && r.Datdok <= ZadnjiDatum)
            .GroupBy(r => r.Brnal.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new NalslagRow
            {
                Brnal = g.Key,
                Dug = g.Sum(r => r.Dug),
                Pot = g.Sum(r => r.Pot),
            })
            .Where(r => PrikaziSve || !r.JeSlozen)
            .OrderBy(r => r.Brnal, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return rezultat;
    }
}
