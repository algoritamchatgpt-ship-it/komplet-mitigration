using Algoritam.WPF.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Algoritam.WPF.ViewModels;

public partial class KreditiOtplataViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly KreditStavka _kredit;
    private int _sortMode;

    [ObservableProperty] private ObservableCollection<KreditOtplataStavka> _stavke = [];
    [ObservableProperty] private KreditOtplataStavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private string _naslov;

    public bool Sačuvano { get; private set; }

    public KreditiOtplataViewModel(string folderPath, KreditStavka kredit)
    {
        _folderPath = folderPath;
        _kredit = kredit;
        _naslov = $"EVIDENCIJA OTPLATA KREDITA {kredit.Kredit}";
        Ucitaj();
    }

    [RelayCommand]
    private void Dodaj()
    {
        var nova = new KreditOtplataStavka
        {
            Kredit = _kredit.Kredit,
            Broj = _kredit.Broj,
            Sifra = _kredit.Sifra,
            DatDok = DateTime.Today,
            Mesec = DateTime.Today.Month,
            Arhiva = " ",
            Arhiva2 = " ",
            Preneto = " ",
            Numred = Stavke.Count + 1
        };

        Stavke.Add(nova);
        Selektovana = nova;
        Poruka = "Dodata je nova otplata.";
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count > 0)
            Selektovana = Stavke[0];
    }

    [RelayCommand]
    private void Prethodni()
    {
        if (Selektovana == null)
            return;

        var index = Stavke.IndexOf(Selektovana);
        if (index > 0)
            Selektovana = Stavke[index - 1];
    }

    [RelayCommand]
    private void Sledeci()
    {
        if (Selektovana == null)
            return;

        var index = Stavke.IndexOf(Selektovana);
        if (index >= 0 && index < Stavke.Count - 1)
            Selektovana = Stavke[index + 1];
    }

    [RelayCommand]
    private void Poslednji()
    {
        if (Stavke.Count > 0)
            Selektovana = Stavke[^1];
    }

    [RelayCommand]
    private void Brisanje()
    {
        var uklonjeno = Stavke.RemoveAll(x => x.Dug == 0m && x.Iznos == 0m);
        Renumerisi();
        Selektovana = Stavke.FirstOrDefault();
        Poruka = uklonjeno == 0
            ? "Nema praznih stavki za brisanje."
            : $"Uklonjeno praznih otplata: {uklonjeno}.";
    }

    [RelayCommand]
    private void Sortiraj()
    {
        _sortMode = (_sortMode + 1) % 4;

        var sortirano = _sortMode switch
        {
            1 => Stavke.OrderBy(x => x.Broj).ThenBy(x => x.DatDok).ToList(),
            2 => Stavke.OrderBy(x => x.Sifra).ThenBy(x => x.DatDok).ToList(),
            3 => Stavke.OrderBy(x => x.DatDok).ThenBy(x => x.Numred).ToList(),
            _ => Stavke.OrderBy(x => x.Numred).ToList()
        };

        Stavke = new ObservableCollection<KreditOtplataStavka>(sortirano);
        Selektovana = Stavke.FirstOrDefault();
        Poruka = _sortMode switch
        {
            1 => "Sortiranje: BROJ",
            2 => "Sortiranje: SIFRA",
            3 => "Sortiranje: DATDOK",
            _ => "Sortiranje: izvorni redosled"
        };
    }

    [RelayCommand]
    private void JedanKredit()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema otplata za pregled.";
            return;
        }

        KreditiDbfSupport.ReizracunajSaldo(Stavke);

        var redovi = Stavke
            .OrderBy(x => x.DatDok)
            .ThenBy(x => x.Numred)
            .Select(x => new PregledTabelaStavka
            {
                Sifra = x.DatDok.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                Naziv = $"Saldo {x.Saldo:N2} / nalog {x.BrNal}".Trim(),
                Iznos1 = x.Dug,
                Iznos2 = x.Iznos
            })
            .ToList();

        var view = new Views.Zarade.FoxPregledTabelaView(
            $"JEDAN KREDIT {_kredit.Kredit}",
            $"{_kredit.Partija}  |  {_kredit.Sifra}",
            redovi,
            "DUG",
            "UPLATA");

        view.ShowDialog();
    }

    [RelayCommand]
    private void Sacuvaj(System.Windows.Window? window)
    {
        try
        {
            var sveOtplate = KreditiDbfSupport.UcitajSveOtplate(_folderPath)
                .Where(x => x.Kredit != _kredit.Kredit)
                .ToList();

            Renumerisi();
            foreach (var stavka in Stavke)
                sveOtplate.Add(stavka.Clone());

            KreditiDbfSupport.SacuvajOtplate(_folderPath, sveOtplate);
            Sačuvano = true;
            Poruka = "Otplate su sačuvane.";

            if (window != null)
                window.DialogResult = true;
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri cuvanju otplata: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Otkazi(System.Windows.Window? window)
    {
        if (window != null)
            window.DialogResult = false;
    }

    private void Ucitaj()
    {
        try
        {
            var lista = KreditiDbfSupport.UcitajSveOtplate(_folderPath)
                .Where(x => x.Kredit == _kredit.Kredit)
                .OrderBy(x => x.DatDok)
                .ThenBy(x => x.Numred)
                .ToList();

            Renumerisi(lista);
            Stavke = new ObservableCollection<KreditOtplataStavka>(lista);
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} otplata za kredit {_kredit.Kredit}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri ucitavanju otplata: {ex.Message}";
        }
    }

    private void Renumerisi()
        => Renumerisi(Stavke);

    private static void Renumerisi(IEnumerable<KreditOtplataStavka> stavke)
    {
        var index = 1;
        foreach (var stavka in stavke)
            stavka.Numred = index++;
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();
}

public partial class KreditOtplataStavka : ObservableObject
{
    [ObservableProperty] private int _kredit;
    [ObservableProperty] private int _broj;
    [ObservableProperty] private string _sifra = string.Empty;
    [ObservableProperty] private DateTime _datDok = DateTime.Today;
    [ObservableProperty] private decimal _dug;
    [ObservableProperty] private decimal _iznos;
    [ObservableProperty] private decimal _saldo;
    [ObservableProperty] private string _brNal = string.Empty;
    [ObservableProperty] private string _dev = string.Empty;
    [ObservableProperty] private decimal _devKurs;
    [ObservableProperty] private decimal _devDug;
    [ObservableProperty] private decimal _devPot;
    [ObservableProperty] private decimal _devSaldo;
    [ObservableProperty] private int _mesec;
    [ObservableProperty] private string _arhiva = " ";
    [ObservableProperty] private string _arhiva2 = " ";
    [ObservableProperty] private string _preneto = " ";
    [ObservableProperty] private long _idBr;
    [ObservableProperty] private int _numred;

    public KreditOtplataStavka Clone() => new()
    {
        Kredit = Kredit,
        Broj = Broj,
        Sifra = Sifra,
        DatDok = DatDok,
        Dug = Dug,
        Iznos = Iznos,
        Saldo = Saldo,
        BrNal = BrNal,
        Dev = Dev,
        DevKurs = DevKurs,
        DevDug = DevDug,
        DevPot = DevPot,
        DevSaldo = DevSaldo,
        Mesec = Mesec,
        Arhiva = Arhiva,
        Arhiva2 = Arhiva2,
        Preneto = Preneto,
        IdBr = IdBr,
        Numred = Numred
    };
}

internal static class ObservableCollectionExtensions
{
    public static int RemoveAll<T>(this ObservableCollection<T> source, Func<T, bool> predicate)
    {
        var uklonjeno = 0;
        for (var i = source.Count - 1; i >= 0; i--)
        {
            if (!predicate(source[i]))
                continue;

            source.RemoveAt(i);
            uklonjeno++;
        }

        return uklonjeno;
    }
}
