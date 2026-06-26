using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALPTK — izbor KEPU knjiga i formiranje stavki pazara.</summary>
public partial class NalptkViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly string _brnal;
    private readonly DateTime _datumDokumenta;

    public event Action? ZatvoriFormu;
    public event Action<IReadOnlyList<NalpRow>>? StavkeFormirane;

    public ObservableCollection<KepuKnjigaRow> Knjige { get; } = [];
    [ObservableProperty] private KepuKnjigaRow? _selectedRow;
    [ObservableProperty] private string _status = string.Empty;

    public string Naslov => $"IZBOR KEPU KNJIGE — NALOG {_brnal}";

    public NalptkViewModel(string firmPath, string brnal, DateTime datumDokumenta)
    {
        _firmPath = firmPath;
        _brnal = brnal.Trim();
        _datumDokumenta = datumDokumenta.Date;
        UcitajKnjige();
    }

    private void UcitajKnjige()
    {
        var path = Path.Combine(_firmPath, "aatm.dbf");
        if (!File.Exists(path))
        {
            Status = "aatm.dbf nije pronađen.";
            return;
        }

        try
        {
            foreach (var rec in new SimpleDbfReader(path).Zapisi())
            {
                Knjige.Add(new KepuKnjigaRow
                {
                    Izabrana = !rec.DajString("PRIKAZ").Trim()
                        .Equals("N", StringComparison.OrdinalIgnoreCase),
                    Sifprod = rec.DajString("SIFPROD").Trim(),
                    Naziv = rec.DajString("PNAZIV").Trim(),
                    Mesto = rec.DajString("PMESTO").Trim(),
                    KontoPazara = rec.DajString("KONTOPAZ").Trim(),
                    KontoUsluga = rec.DajString("KONTOU").Trim(),
                });
            }

            SelectedRow = Knjige.FirstOrDefault();
            Status = $"KEPU knjiga: {Knjige.Count}. Izaberite knjige za prenos.";
        }
        catch (Exception ex)
        {
            Status = $"Greška pri čitanju aatm.dbf: {ex.Message}";
        }
    }

    [RelayCommand]
    private void IzaberiSve()
    {
        foreach (var knjiga in Knjige)
            knjiga.Izabrana = true;
    }

    [RelayCommand]
    private void PonistiSve()
    {
        foreach (var knjiga in Knjige)
            knjiga.Izabrana = false;
    }

    [RelayCommand]
    private void DodajUNalog()
    {
        try
        {
            var izvori = new List<KepuPromet>();
            foreach (var knjiga in Knjige.Where(k => k.Izabrana))
            {
                var tmPath = Path.Combine(_firmPath, $"tm{knjiga.Sifprod}.dbf");
                if (!File.Exists(tmPath))
                    continue;

                foreach (var rec in new SimpleDbfReader(tmPath).Zapisi())
                {
                    izvori.Add(new KepuPromet(
                        knjiga.Sifprod,
                        knjiga.KontoPazara,
                        knjiga.KontoUsluga,
                        rec.DajString("BRNAL"),
                        rec.DajDecimal("UPLACENO"),
                        rec.DajDecimal("USLUGE")));
                }
            }

            var stavke = FormirajStavke(izvori, _brnal, _datumDokumenta);
            if (stavke.Count == 0)
            {
                MessageBox.Show(
                    $"Za nalog {_brnal} nema uplata ni usluga u izabranim KEPU knjigama.",
                    "IZBOR KEPU KNJIGE",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            StavkeFormirane?.Invoke(stavke);
            ZatvoriFormu?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Greška pri prenosu KEPU stavki:\n{ex.Message}",
                "IZBOR KEPU KNJIGE",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal static IReadOnlyList<NalpRow> FormirajStavke(
        IEnumerable<KepuPromet> prometi,
        string brnal,
        DateTime datumDokumenta)
    {
        var trazeniNalog = brnal.Trim();
        var rezultat = new List<NalpRow>();

        foreach (var grupa in prometi
                     .Where(p => p.Brnal.Trim().Equals(
                         trazeniNalog, StringComparison.OrdinalIgnoreCase))
                     .GroupBy(p => new
                     {
                         Sifprod = p.Sifprod.Trim(),
                         KontoPazara = p.KontoPazara.Trim(),
                         KontoUsluga = p.KontoUsluga.Trim(),
                     })
                     .OrderBy(g => g.Key.Sifprod, StringComparer.OrdinalIgnoreCase))
        {
            var uplaceno = grupa.Sum(p => p.Uplaceno);
            var usluge = grupa.Sum(p => p.Usluge);
            var dok = $"M{grupa.Key.Sifprod}";

            if (uplaceno != 0)
            {
                rezultat.Add(new NalpRow
                {
                    Konto = grupa.Key.KontoPazara,
                    Opis = $"PAZAR PRODAVNICE {grupa.Key.Sifprod}",
                    Brnal = trazeniNalog,
                    Datdok = datumDokumenta.Date,
                    Pot = uplaceno,
                    Dok = dok,
                });
            }

            if (usluge != 0)
            {
                rezultat.Add(new NalpRow
                {
                    Konto = grupa.Key.KontoUsluga,
                    Opis = $"PAZAR PROD.{grupa.Key.Sifprod} USLUGE",
                    Brnal = trazeniNalog,
                    Datdok = datumDokumenta.Date,
                    Pot = usluge,
                    Dok = dok,
                });
            }
        }

        return rezultat;
    }

    internal sealed record KepuPromet(
        string Sifprod,
        string KontoPazara,
        string KontoUsluga,
        string Brnal,
        decimal Uplaceno,
        decimal Usluge);
}
