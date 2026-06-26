using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly int _godina;
    private readonly List<NalpRow> _izvorniRedovi = [];
    private readonly Dictionary<string, string> _kontoNazivi =
        new(StringComparer.OrdinalIgnoreCase);
    private int _sortiranje;

    public event Action? ZatvoriFormu;
    public event Action<NalpRow>? PozicioniranjeTrazena;

    [ObservableProperty] private ObservableCollection<NalpRow> _redovi = [];
    [ObservableProperty] private NalpRow? _selectedRow;
    [ObservableProperty] private string _traziKonto = string.Empty;
    [ObservableProperty] private string _traziNalog = string.Empty;
    [ObservableProperty] private DateTime? _traziDatum;
    [ObservableProperty] private string _sortOpis = "SORT: DATUM / NALOG / KONTO";

    public string Brojac =>
        SelectedRow == null ? $"0 / {Redovi.Count}" :
        $"{Redovi.IndexOf(SelectedRow) + 1} / {Redovi.Count}";

    public string IzabraniNalog => SelectedRow?.Brnal.Trim() ?? string.Empty;
    public string IzabraniDatum => SelectedRow?.Datdok?.ToString("dd.MM.yyyy") ?? string.Empty;
    public string IzabraniKonto => SelectedRow?.Konto.Trim() ?? string.Empty;
    public string KontoNaziv =>
        SelectedRow != null &&
        _kontoNazivi.TryGetValue(SelectedRow.Konto.Trim(), out var naziv)
            ? naziv
            : string.Empty;

    public decimal NalogDuguje => SelectedRow == null
        ? 0
        : _izvorniRedovi
            .Where(r => Isto(r.Brnal, SelectedRow.Brnal))
            .Sum(r => r.Dug);

    public decimal NalogPotrazuje => SelectedRow == null
        ? 0
        : _izvorniRedovi
            .Where(r => Isto(r.Brnal, SelectedRow.Brnal))
            .Sum(r => r.Pot);

    public decimal NalogSaldo => NalogDuguje - NalogPotrazuje;

    public NalViewModel(string firmPath, int godina)
    {
        _firmPath = firmPath;
        _godina = godina;
        UcitajKonta();
        UcitajDnevnik();
    }

    private void UcitajKonta()
    {
        var path = Path.Combine(_firmPath, "konto.dbf");
        if (!File.Exists(path))
            return;

        try
        {
            var reader = new SimpleDbfReader(path);
            foreach (var rec in reader.Zapisi())
            {
                var konto = rec.DajString("KONTO").Trim();
                if (!string.IsNullOrEmpty(konto))
                    _kontoNazivi[konto] = rec.DajString("NAZIV").Trim();
            }
        }
        catch
        {
            // Dnevnik ostaje upotrebljiv i bez naziva konta.
        }
    }

    private void UcitajDnevnik()
    {
        var path = Path.Combine(_firmPath, "nal.dbf");
        if (!File.Exists(path))
            return;

        try
        {
            var reader = new SimpleDbfReader(path);
            _izvorniRedovi.AddRange(
                reader.Zapisi().Select(Nalp2ViewModel.NalpRowFromRecord));
            PrimeniSortiranje();
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

    partial void OnSelectedRowChanged(NalpRow? value)
    {
        OnPropertyChanged(nameof(Brojac));
        OnPropertyChanged(nameof(IzabraniNalog));
        OnPropertyChanged(nameof(IzabraniDatum));
        OnPropertyChanged(nameof(IzabraniKonto));
        OnPropertyChanged(nameof(KontoNaziv));
        OnPropertyChanged(nameof(NalogDuguje));
        OnPropertyChanged(nameof(NalogPotrazuje));
        OnPropertyChanged(nameof(NalogSaldo));
    }

    [RelayCommand]
    private void IdiNaVrh() => Pozicioniraj(Redovi.FirstOrDefault());

    [RelayCommand]
    private void IdiGore()
    {
        if (SelectedRow == null)
        {
            IdiNaVrh();
            return;
        }

        var index = Redovi.IndexOf(SelectedRow);
        if (index > 0)
            Pozicioniraj(Redovi[index - 1]);
    }

    [RelayCommand]
    private void IdiDole()
    {
        if (SelectedRow == null)
        {
            IdiNaVrh();
            return;
        }

        var index = Redovi.IndexOf(SelectedRow);
        if (index >= 0 && index < Redovi.Count - 1)
            Pozicioniraj(Redovi[index + 1]);
    }

    [RelayCommand]
    private void IdiNaDno() => Pozicioniraj(Redovi.LastOrDefault());

    [RelayCommand]
    private void NadjiKonto()
    {
        var tekst = TraziKonto.Trim();
        if (string.IsNullOrEmpty(tekst))
            return;

        PozicionirajIliObavesti(
            Redovi.FirstOrDefault(r =>
                r.Konto.Trim().StartsWith(tekst, StringComparison.OrdinalIgnoreCase)),
            $"Konto '{tekst}' nije pronađen.");
    }

    [RelayCommand]
    private void NadjiNalog()
    {
        var tekst = TraziNalog.Trim();
        if (string.IsNullOrEmpty(tekst))
            return;

        PozicionirajIliObavesti(
            Redovi.FirstOrDefault(r => Isto(r.Brnal, tekst)),
            $"Nalog '{tekst}' nije pronađen.");
    }

    [RelayCommand]
    private void NadjiDatum()
    {
        if (!TraziDatum.HasValue)
            return;

        PozicionirajIliObavesti(
            Redovi.FirstOrDefault(r => r.Datdok?.Date == TraziDatum.Value.Date),
            $"Za datum {TraziDatum:dd.MM.yyyy} nema stavki.");
    }

    [RelayCommand]
    private void PromeniSortiranje()
    {
        _sortiranje = (_sortiranje + 1) % 3;
        PrimeniSortiranje();
    }

    private void PrimeniSortiranje()
    {
        var izabrani = SelectedRow;
        var sortirani = SortirajRedove(_izvorniRedovi, _sortiranje);
        Redovi = new ObservableCollection<NalpRow>(sortirani);
        SortOpis = _sortiranje switch
        {
            1 => "SORT: KONTO / DATUM / NALOG",
            2 => "SORT: NALOG / KONTO / DATUM",
            _ => "SORT: DATUM / NALOG / KONTO",
        };

        Pozicioniraj(
            izabrani != null && Redovi.Contains(izabrani)
                ? izabrani
                : Redovi.FirstOrDefault());
    }

    internal static IReadOnlyList<NalpRow> SortirajRedove(
        IEnumerable<NalpRow> redovi,
        int sortiranje) =>
        sortiranje switch
        {
            1 => redovi
                .OrderBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Datdok)
                .ThenBy(r => r.Brnal.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList(),
            2 => redovi
                .OrderBy(r => r.Brnal.Trim(), StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Datdok)
                .ToList(),
            _ => redovi
                .OrderBy(r => r.Datdok)
                .ThenBy(r => r.Brnal.Trim(), StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList(),
        };

    [RelayCommand]
    private void PregledNaloga()
    {
        if (SelectedRow == null)
            return;

        var brnal = SelectedRow.Brnal.Trim();
        OtvoriPregled(
            $"PREGLED NALOGA {brnal}",
            _izvorniRedovi.Where(r => Isto(r.Brnal, brnal)));
    }

    [RelayCommand]
    private void PregledKonta()
    {
        if (SelectedRow == null)
            return;

        var konto = SelectedRow.Konto.Trim();
        var vm = new NalPregKontoViewModel(_firmPath, _godina, konto);
        new Views.NalPregKontoWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void DevizniNalog()
    {
        if (SelectedRow == null)
            return;

        var brnal = SelectedRow.Brnal.Trim();
        OtvoriPregled(
            $"DEVIZNI NALOG {brnal}",
            _izvorniRedovi.Where(r =>
                Isto(r.Brnal, brnal) &&
                (r.Devdug != 0 || r.Devpot != 0 || !string.IsNullOrWhiteSpace(r.Dev))));
    }

    [RelayCommand]
    private void DevizniKonto()
    {
        if (SelectedRow == null)
            return;

        var konto = SelectedRow.Konto.Trim();
        OtvoriPregled(
            $"DEVIZNI KONTO {konto}",
            _izvorniRedovi.Where(r =>
                Isto(r.Konto, konto) &&
                (r.Devdug != 0 || r.Devpot != 0 || !string.IsNullOrWhiteSpace(r.Dev))));
    }

    [RelayCommand]
    private void NalogSintetika()
    {
        if (SelectedRow == null)
            return;

        var brnal = SelectedRow.Brnal.Trim();
        var vm = new NalogPregledViewModel(
            $"SINTETIKA NALOGA {brnal}",
            FormirajSintetiku(_izvorniRedovi, brnal));
        new Views.NalogPregledWindow(vm).ShowDialog();
    }

    internal static IReadOnlyList<NalogPregledRow> FormirajSintetiku(
        IEnumerable<NalpRow> redovi,
        string brnal) =>
        redovi
            .Where(r => Isto(r.Brnal, brnal))
            .GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new NalogPregledRow
            {
                Konto = g.Key,
                Dug = g.Sum(r => r.Dug),
                Pot = g.Sum(r => r.Pot),
                Opis = g.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Opis))?.Opis ?? string.Empty,
                Brnal = brnal,
                Devdug = g.Sum(r => r.Devdug),
                Devpot = g.Sum(r => r.Devpot),
                Ulaz = g.Sum(r => r.Ulaz),
                Izlaz = g.Sum(r => r.Izlaz),
            })
            .OrderBy(r => r.Konto, StringComparer.OrdinalIgnoreCase)
            .ToList();

    [RelayCommand]
    private void KontniPlan()
    {
        var vm = new KontoPlanViewModel(_firmPath, "konto.dbf", "KONTNI PLAN");
        new Views.KontoPlanWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void DnevnikIzvestaj()
    {
        var vm = new NaldnevViewModel(_firmPath, _godina);
        new Views.NaldnevWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void Razno()
    {
        var vm = new NalraznoViewModel(_firmPath, _godina);
        new Views.NalraznoWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    private void OtvoriPregled(string naslov, IEnumerable<NalpRow> redovi)
    {
        var vm = new NalogPregledViewModel(
            naslov,
            redovi.Select(NalogPregledViewModel.IzNalp));
        new Views.NalogPregledWindow(vm).ShowDialog();
    }

    private void PozicionirajIliObavesti(NalpRow? red, string poruka)
    {
        if (red != null)
        {
            Pozicioniraj(red);
            return;
        }

        MessageBox.Show(poruka, "DNEVNIK GLAVNE KNJIGE",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Pozicioniraj(NalpRow? red)
    {
        if (red == null)
            return;

        SelectedRow = red;
        PozicioniranjeTrazena?.Invoke(red);
    }

    private static bool Isto(string? levo, string? desno) =>
        string.Equals(
            levo?.Trim(),
            desno?.Trim(),
            StringComparison.OrdinalIgnoreCase);
}
