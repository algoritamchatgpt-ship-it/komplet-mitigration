using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class KreditLookupViewModel : ObservableObject
{
    private readonly List<KreditLookupItem> _sveStavke;
    private readonly Func<KreditLookupItem?>? _dodajHandler;

    [ObservableProperty] private string _naslov;
    [ObservableProperty] private string _filter = string.Empty;
    [ObservableProperty] private string _filterSifra = string.Empty;
    [ObservableProperty] private string _filterNaziv = string.Empty;
    [ObservableProperty] private string _filterDodatno = string.Empty;
    [ObservableProperty] private ObservableCollection<KreditLookupItem> _stavke;
    [ObservableProperty] private KreditLookupItem? _izabrana;

    public KreditLookupViewModel(string naslov, IReadOnlyCollection<KreditLookupItem> stavke, Func<KreditLookupItem?>? dodajHandler = null)
    {
        _naslov = naslov;
        _sveStavke = stavke.ToList();
        _stavke = new ObservableCollection<KreditLookupItem>(_sveStavke);
        _izabrana = _stavke.FirstOrDefault();
        _dodajHandler = dodajHandler;
    }

    public string DodatnoLabel =>
        Naslov.Contains("partner", StringComparison.OrdinalIgnoreCase) ? "Mesto" : "Dodatno";
    public bool OmoguciDodavanje => _dodajHandler != null;

    partial void OnFilterChanged(string value) => PrimeniFilter();
    partial void OnFilterSifraChanged(string value) => PrimeniFilter();
    partial void OnFilterNazivChanged(string value) => PrimeniFilter();
    partial void OnFilterDodatnoChanged(string value) => PrimeniFilter();

    [RelayCommand]
    private void TraziSifru()
    {
        var upit = (FilterSifra ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(upit))
            return;

        var nadjena = Stavke.FirstOrDefault(x => x.Sifra.Equals(upit, StringComparison.OrdinalIgnoreCase))
            ?? Stavke.FirstOrDefault(x => x.Sifra.StartsWith(upit, StringComparison.OrdinalIgnoreCase));

        if (nadjena != null)
            Izabrana = nadjena;
    }

    [RelayCommand]
    private void TraziNaziv()
    {
        var upit = (FilterNaziv ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(upit))
            return;

        var nadjena = Stavke.FirstOrDefault(x => x.Naziv.Contains(upit, StringComparison.OrdinalIgnoreCase));
        if (nadjena != null)
            Izabrana = nadjena;
    }

    [RelayCommand]
    private void OcistiFiltere()
    {
        Filter = string.Empty;
        FilterSifra = string.Empty;
        FilterNaziv = string.Empty;
        FilterDodatno = string.Empty;
        PrimeniFilter();
    }

    [RelayCommand]
    private void Dodaj()
    {
        if (_dodajHandler == null)
            return;

        var nova = _dodajHandler();
        if (nova == null)
            return;

        _sveStavke.Add(nova);
        PrimeniFilter();
        Izabrana = Stavke.FirstOrDefault(x => x.Sifra.Equals(nova.Sifra, StringComparison.OrdinalIgnoreCase)) ?? Stavke.FirstOrDefault();
    }

    private void PrimeniFilter()
    {
        var global = (Filter ?? string.Empty).Trim();
        var sifra = (FilterSifra ?? string.Empty).Trim();
        var naziv = (FilterNaziv ?? string.Empty).Trim();
        var dodatno = (FilterDodatno ?? string.Empty).Trim();

        IEnumerable<KreditLookupItem> filtrirano = _sveStavke;

        if (!string.IsNullOrWhiteSpace(global))
        {
            filtrirano = filtrirano.Where(x =>
                x.Sifra.Contains(global, StringComparison.OrdinalIgnoreCase) ||
                x.Naziv.Contains(global, StringComparison.OrdinalIgnoreCase) ||
                x.Dodatno.Contains(global, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(sifra))
        {
            filtrirano = filtrirano.Where(x =>
                x.Sifra.StartsWith(sifra, StringComparison.OrdinalIgnoreCase) ||
                x.Sifra.Contains(sifra, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(naziv))
        {
            filtrirano = filtrirano.Where(x =>
                x.Naziv.Contains(naziv, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(dodatno))
        {
            filtrirano = filtrirano.Where(x =>
                x.Dodatno.Contains(dodatno, StringComparison.OrdinalIgnoreCase));
        }

        Stavke = new ObservableCollection<KreditLookupItem>(filtrirano.ToList());
        Izabrana = Stavke.FirstOrDefault();
    }

    [RelayCommand]
    private void Potvrdi(System.Windows.Window? window)
    {
        if (Izabrana == null)
            return;

        if (window != null)
            window.DialogResult = true;
    }

    [RelayCommand]
    private void Otkazi(System.Windows.Window? window)
    {
        if (window != null)
            window.DialogResult = false;
    }
}

public sealed class KreditLookupItem
{
    public KreditLookupItem(string sifra, string naziv, string dodatno)
    {
        Sifra = sifra ?? string.Empty;
        Naziv = naziv ?? string.Empty;
        Dodatno = dodatno ?? string.Empty;
    }

    public string Sifra { get; }
    public string Naziv { get; }
    public string Dodatno { get; }
}
