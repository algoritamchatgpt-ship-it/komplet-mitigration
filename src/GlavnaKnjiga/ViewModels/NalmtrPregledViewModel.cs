using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalmtrPregledViewModel : ObservableObject
{
    private static readonly PropertyInfo[] IznosProperties =
        Enumerable.Range(1, 30)
            .Select(i => typeof(NalmtrRow).GetProperty($"Iznos{i:D2}")!)
            .ToArray();

    private readonly string _firmPath;
    private List<NalmtrRow> _izvorniRedovi = [];

    public event Action? ZatvoriFormu;

    public IReadOnlyList<string> KoloneNazivi { get; }

    [ObservableProperty] private DateTime? _dat0;
    [ObservableProperty] private DateTime? _dat1;
    [ObservableProperty] private decimal _kurs = 80m;
    [ObservableProperty] private ObservableCollection<NalmtrRow> _redovi = [];
    [ObservableProperty] private NalmtrRow? _selectedRow;
    [ObservableProperty] private string _status = "Izaberite vrstu pregleda.";

    public NalmtrPregledViewModel(
        string firmPath, int godina, IReadOnlyList<string> koloneNazivi)
    {
        _firmPath = firmPath;
        KoloneNazivi = koloneNazivi;
        Dat0 = new DateTime(godina, 1, 1);
        Dat1 = new DateTime(godina, 12, 31);
        UcitajIzvor();
    }

    [RelayCommand]
    private void PregledKlasa5() => Prikazi("5", "Klasa 5");

    [RelayCommand]
    private void PregledKlasa6() => Prikazi("6", "Klasa 6");

    [RelayCommand]
    private void PregledSve() => Prikazi(null, "Sve klase");

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal static List<NalmtrRow> FormirajPregled(
        IEnumerable<NalmtrRow> rows, DateTime dat0, DateTime dat1, string? klasa)
    {
        return rows
            .Where(r => r.Datdok?.Date >= dat0.Date && r.Datdok?.Date <= dat1.Date)
            .Where(r => string.IsNullOrEmpty(klasa) ||
                        r.Konto.Trim().StartsWith(klasa, StringComparison.Ordinal))
            .GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g =>
            {
                var rezultat = new NalmtrRow
                {
                    Konto = g.Key,
                    Naziv = g.Select(r => r.Naziv).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
                            ?? string.Empty,
                    Dug = g.Sum(r => r.Dug),
                    Pot = g.Sum(r => r.Pot),
                    Saldo = g.Sum(r => r.Saldo),
                };

                for (var i = 0; i < IznosProperties.Length; i++)
                {
                    var suma = g.Sum(r => (decimal)(IznosProperties[i].GetValue(r) ?? 0m));
                    IznosProperties[i].SetValue(rezultat, suma);
                }

                rezultat.RecalcUkupno();
                return rezultat;
            })
            .OrderBy(r => r.Konto, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void Prikazi(string? klasa, string naziv)
    {
        if (Dat0 == null || Dat1 == null)
        {
            MessageBox.Show("Unesite početni i zadnji datum.", "PREGLED MESTA TROŠKOVA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Dat0 > Dat1)
        {
            MessageBox.Show("Početni datum ne može biti posle zadnjeg datuma.",
                "PREGLED MESTA TROŠKOVA", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var rezultat = FormirajPregled(
            _izvorniRedovi, Dat0.Value, Dat1.Value, klasa);
        Redovi = new ObservableCollection<NalmtrRow>(rezultat);
        SelectedRow = Redovi.FirstOrDefault();
        Status = $"{naziv}   |   Konta: {Redovi.Count}   |   Kurs: {Kurs:N4}   |   Ukupno: {Redovi.Sum(r => r.Ukupno):N2}";
    }

    private void UcitajIzvor()
    {
        var path = Path.Combine(_firmPath, "nalmtr.dbf");
        if (!File.Exists(path))
        {
            Status = "nalmtr.dbf nije pronađen.";
            return;
        }

        try
        {
            _izvorniRedovi = new SimpleDbfReader(path).Zapisi()
                .Select(NalmtrPrenosViewModel.CitajNalmtrRec)
                .ToList();
            Status = $"Učitano redova: {_izvorniRedovi.Count}";
        }
        catch (Exception ex)
        {
            Status = $"Greška: {ex.Message}";
        }
    }
}
