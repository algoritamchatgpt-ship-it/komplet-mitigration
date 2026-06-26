using Algoritam.Application;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Fox forma LDSVEZARADE - tabela svih zarada. Čita direktno iz LD*.dbf fajlova.
/// </summary>
public partial class LdSveZaradeViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty]
    private ObservableCollection<LdObracunStavka> _stavke = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ObrisiStavkuCommand))]
    private LdObracunStavka? _selektovana;

    [ObservableProperty]
    private int _mesec;

    [ObservableProperty]
    private int _isplata;

    [ObservableProperty]
    private string _godina = string.Empty;

    [ObservableProperty]
    private string _vrsta = string.Empty;

    [ObservableProperty]
    private bool _sortPoMesecima = true;

    [ObservableProperty]
    private string _poruka = string.Empty;

    [ObservableProperty]
    private bool _ucitava;

    public LdSveZaradeViewModel(AppState appState)
    {
        _appState = appState;
        Mesec = 0;
        Isplata = 0;
        _ = UcitajAsync();
    }

    public string Naslov => "TABELA SVIH ZARADA (LDSVEZARADE)";

    [RelayCommand]
    private async Task UcitajAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Stavke = [];
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        try
        {
            Ucitava = true;
            var sve = await Task.Run(() => LdObracunDbfReader.CitajSve(folder));

            IEnumerable<LdObracunStavka> q = sve;
            if (Mesec > 0) q = q.Where(x => x.Mesec == Mesec);
            if (Isplata > 0) q = q.Where(x => x.Isplata == Isplata);
            if (!string.IsNullOrWhiteSpace(Godina)) q = q.Where(x => x.Godina == Godina.Trim());
            if (!string.IsNullOrWhiteSpace(Vrsta)) q = q.Where(x => x.Vrsta == Vrsta.Trim());

            q = SortPoMesecima
                ? q.OrderBy(x => x.Godina).ThenBy(x => x.Mesec).ThenBy(x => x.Isplata).ThenBy(x => x.Broj)
                : q.OrderBy(x => x.Broj).ThenBy(x => x.ImePrez);

            var lista = q.ToList();
            Stavke = new ObservableCollection<LdObracunStavka>(lista);
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {lista.Count} stavki. Ukupno NETO: {lista.Sum(x => x.Neto):N2}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri ucitavanju: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private bool MozeObrisiStavku() => Selektovana != null;

    [RelayCommand(CanExecute = nameof(MozeObrisiStavku))]
    private async Task ObrisiStavkuAsync()
    {
        if (Selektovana == null)
            return;

        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var potvrda = MessageBox.Show(
            $"Obrisati stavku za radnika '{Selektovana.ImePrez}' (broj {Selektovana.Broj})?",
            "Tabela svih zarada",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (potvrda != MessageBoxResult.Yes)
            return;

        try
        {
            Ucitava = true;
            var rezultat = await LdDbfMutator.ObrisiStavkeAsync(folder, [Selektovana]);
            if (rezultat.Izmenjeno == 0)
            {
                Poruka = "Nijedna stavka nije obrisana.";
                return;
            }

            await UcitajAsync();
            Poruka = $"Obrisano {rezultat.Izmenjeno} stavki (fajlova: {rezultat.Fajlovi}).";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri brisanju stavke: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private async Task ObrisiMesecAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var mesecZaBrisanje = Mesec > 0 ? Mesec : Selektovana?.Mesec ?? 0;
        if (mesecZaBrisanje <= 0)
        {
            Poruka = "Unesite mesec (ili izaberite stavku) pre brisanja meseca.";
            return;
        }

        var godinaFilter = (Godina ?? string.Empty).Trim();
        var vrstaFilter = (Vrsta ?? string.Empty).Trim();
        var isplataFilter = Isplata;

        var potvrdaTekst = $"Obrisati zarade za mesec {mesecZaBrisanje}"
            + (string.IsNullOrWhiteSpace(godinaFilter) ? string.Empty : $", godinu {godinaFilter}")
            + (isplataFilter > 0 ? $", isplatu {isplataFilter}" : string.Empty)
            + (string.IsNullOrWhiteSpace(vrstaFilter) ? string.Empty : $", vrstu {vrstaFilter}")
            + "?";

        var potvrda = MessageBox.Show(
            potvrdaTekst,
            "Tabela svih zarada",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (potvrda != MessageBoxResult.Yes)
            return;

        try
        {
            Ucitava = true;
            var rezultat = await LdDbfMutator.ObrisiPoUslovuAsync(folder, row =>
            {
                if (Int(row, "MESEC") != mesecZaBrisanje)
                    return false;

                if (isplataFilter > 0 && Int(row, "ISPLATA") != isplataFilter)
                    return false;

                if (!string.IsNullOrWhiteSpace(godinaFilter) &&
                    !string.Equals(Str(row, "GODINA"), godinaFilter, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrWhiteSpace(vrstaFilter) &&
                    !string.Equals(Str(row, "VRSTA"), vrstaFilter, StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            });

            if (rezultat.Izmenjeno == 0)
            {
                Poruka = "Nema stavki za zadati mesec/filter.";
                return;
            }

            await UcitajAsync();
            Poruka = $"Obrisano {rezultat.Izmenjeno} stavki za mesec {mesecZaBrisanje}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri brisanju meseca: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private void OcistiFilter()
    {
        Mesec = 0;
        Isplata = 0;
        Godina = string.Empty;
        Vrsta = string.Empty;
        Poruka = "Filter je resetovan.";
    }

    [RelayCommand]
    private Task OsveziAsync() => UcitajAsync();

    [RelayCommand]
    private void Prvi() => SelektujPoIndeks(0);

    [RelayCommand]
    private void Gore()
    {
        if (Stavke.Count == 0)
            return;

        if (Selektovana == null)
        {
            SelektujPoIndeks(0);
            return;
        }

        var index = Stavke.IndexOf(Selektovana);
        SelektujPoIndeks(index <= 0 ? 0 : index - 1);
    }

    [RelayCommand]
    private void Dole()
    {
        if (Stavke.Count == 0)
            return;

        if (Selektovana == null)
        {
            SelektujPoIndeks(0);
            return;
        }

        var index = Stavke.IndexOf(Selektovana);
        SelektujPoIndeks(index < 0 ? 0 : Math.Min(Stavke.Count - 1, index + 1));
    }

    [RelayCommand]
    private void Zadnji() => SelektujPoIndeks(Stavke.Count - 1);

    [RelayCommand]
    private async Task SortirajPoMesecimaAsync()
    {
        SortPoMesecima = !SortPoMesecima;
        await UcitajAsync();
        Poruka = SortPoMesecima
            ? "Sortiranje: po mesecima (Fox)."
            : "Sortiranje: po broju/imenu.";
    }

    [RelayCommand]
    private void FoxAkcija(string naziv)
    {
        var komanda = (naziv ?? string.Empty).Trim().ToUpperInvariant();
        switch (komanda)
        {
            case "JEDAN RADNIK SVE":
                PrikaziJedanRadnikSve();
                break;
            case "JEDAN RADNIK SKRACENI":
                PrikaziJedanRadnikSkraceni();
                break;
            case "PREGLED PODATAKA":
                PrikaziPregledPodataka();
                break;
            case "SALDO RADNIKA":
                PrikaziSaldoRadnika();
                break;
            case "SALDO SKRACENI":
                PrikaziSaldoSkraceni(false);
                break;
            case "SALDO SKRACENI ZA MESEC":
                PrikaziSaldoSkraceni(true);
                break;
            case "SALDO SVE":
                PrikaziSaldoSve(false);
                break;
            case "SALDO SVE ZA MESEC":
                PrikaziSaldoSve(true);
                break;
            case "SALDO PO MESECIMA":
                PrikaziSaldoPoMesecima();
                break;
            case "SALDO PO DATUMIMA":
                PrikaziSaldoPoDatumima();
                break;
            case "SALDO PO GRADOVIMA":
                PrikaziSaldoPoGradovima();
                break;
            default:
                var tekst = string.IsNullOrWhiteSpace(naziv) ? "Akcija" : naziv.Trim();
                Poruka = $"Nepoznata opcija '{tekst}'.";
                break;
        }
    }

    private void PrikaziJedanRadnikSve()
    {
        if (!TryGetIzabraniRadnik(out var broj, out var ime))
            return;

        var redovi = Stavke
            .Where(s => s.Broj == broj)
            .OrderBy(s => s.Godina)
            .ThenBy(s => s.Mesec)
            .ThenBy(s => s.Isplata)
            .Select(s => new JedanRadnikSveRed
            {
                Mesec = s.Mesec,
                Isplata = s.Isplata,
                Godina = s.Godina,
                Vrsta = s.Vrsta,
                Bruto = s.Bruto,
                Neto = s.Neto,
                Porez = s.Porez,
                DoprinosPioR = s.Doppr,
                DoprinosZdrR = s.Dopzr,
                DoprinosNezR = s.Dopnr,
                DoprinosPioF = s.Doppf,
                DoprinosZdrF = s.Dopzf,
                DoprinosNezF = s.Dopnf,
                ZaIsplatu = s.Zaisplatu,
                UkObust = s.Ukobust
            })
            .ToList();

        if (redovi.Count == 0)
        {
            Poruka = $"Nema stavki za radnika {broj}.";
            return;
        }

        OtvoriIzvestaj($"JEDAN RADNIK SVE - {broj} {ime}", redovi);
        Poruka = $"Prikazan izvestaj za radnika {broj}.";
    }

    private void PrikaziJedanRadnikSkraceni()
    {
        if (!TryGetIzabraniRadnik(out var broj, out var ime))
            return;

        var redovi = Stavke
            .Where(s => s.Broj == broj)
            .OrderBy(s => s.Godina)
            .ThenBy(s => s.Mesec)
            .ThenBy(s => s.Isplata)
            .Select(s => new JedanRadnikSkraceniRed
            {
                Mesec = s.Mesec,
                Isplata = s.Isplata,
                Godina = s.Godina,
                Vrsta = s.Vrsta,
                CasUkupno = s.Casuk,
                Bruto = s.Bruto,
                DoprinosPioR = s.Doppr,
                DoprinosPioF = s.Doppf,
                Neto = s.Neto,
                ZaIsplatu = s.Zaisplatu
            })
            .ToList();

        if (redovi.Count == 0)
        {
            Poruka = $"Nema stavki za radnika {broj}.";
            return;
        }

        OtvoriIzvestaj($"JEDAN RADNIK SKRACENI - {broj} {ime}", redovi);
        Poruka = $"Prikazan skraceni izvestaj za radnika {broj}.";
    }

    private void PrikaziPregledPodataka()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za pregled.";
            return;
        }

        var view = new Views.Zarade.PregledKoloneView(Stavke);
        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null)
            view.Owner = aktivniProzor;
        view.ShowDialog();
        Poruka = "Otvoren pregled podataka po kolonama.";
    }

    private void PrikaziSaldoRadnika()
    {
        var redovi = Stavke
            .OrderBy(s => s.Broj)
            .GroupBy(s => new { s.Broj, s.ImePrez })
            .Select(g => new SaldoRadnikaRed
            {
                Broj = g.Key.Broj,
                ImePrez = g.Key.ImePrez,
                CasBol = g.Sum(x => x.Casbol + x.Casbol2),
                CasRed = g.Sum(x => x.Casuk - x.Casbol - x.Casbol2),
                CasUk = g.Sum(x => x.Casuk),
                DinBol = g.Sum(x => x.Dinbol + x.Dinbol2),
                DinRed = g.Sum(x => x.Bruto - x.Dinbol - x.Dinbol2),
                Bruto = g.Sum(x => x.Bruto),
                DopSoc = g.Sum(x => x.Dopsocr),
                Benef = g.Sum(x => x.Bendin),
                NetoRed = g.Sum(x => x.Neto - ((x.Dinbol + x.Dinbol2) * 0.697m)),
                NetoBol = g.Sum(x => (x.Dinbol + x.Dinbol2) * 0.697m),
                NetoUk = g.Sum(x => x.Neto)
            })
            .ToList();

        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za saldo radnika.";
            return;
        }

        OtvoriIzvestaj("SALDO RADNIKA", redovi);
        Poruka = $"Prikazan saldo radnika ({redovi.Count} stavki).";
    }

    private void PrikaziSaldoSkraceni(bool samoMesec)
    {
        var izvor = FiltrirajPoMesecuAkoTreba(samoMesec, out var mesec);
        if (izvor == null)
            return;

        var redovi = izvor
            .OrderBy(s => s.Broj)
            .GroupBy(s => new { s.Broj, s.ImePrez })
            .Select(g => new SaldoSkraceniRed
            {
                Broj = g.Key.Broj,
                ImePrez = g.Key.ImePrez,
                CasBol = g.Sum(x => x.Casbol + x.Casbol2),
                CasRed = g.Sum(x => x.Casuk - x.Casbol - x.Casbol2),
                CasUk = g.Sum(x => x.Casuk),
                DinBol = g.Sum(x => x.Dinbol + x.Dinbol2),
                DinRed = g.Sum(x => x.Bruto - x.Dinbol - x.Dinbol2),
                Bruto = g.Sum(x => x.Bruto),
                DoppioR = g.Sum(x => x.Doppr),
                DoppioF = g.Sum(x => x.Doppf),
                DoppioSve = g.Sum(x => x.Doppr + x.Doppf)
            })
            .ToList();

        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za saldo skraceni.";
            return;
        }

        var naslov = samoMesec ? $"SALDO SKRACENI ZA MESEC {mesec}" : "SALDO SKRACENI";
        OtvoriIzvestaj(naslov, redovi);
        Poruka = $"Prikazan {naslov.ToLowerInvariant()}.";
    }

    private void PrikaziSaldoSve(bool samoMesec)
    {
        var izvor = FiltrirajPoMesecuAkoTreba(samoMesec, out var mesec);
        if (izvor == null)
            return;

        var redovi = izvor
            .OrderBy(s => s.Broj)
            .GroupBy(s => new { s.Broj, s.ImePrez })
            .Select(g => new SaldoSveRed
            {
                Broj = g.Key.Broj,
                ImePrez = g.Key.ImePrez,
                CasBol = g.Sum(x => x.Casbol + x.Casbol2),
                CasRed = g.Sum(x => x.Casuk - x.Casbol - x.Casbol2),
                CasUk = g.Sum(x => x.Casuk),
                DinBol = g.Sum(x => x.Dinbol + x.Dinbol2),
                DinRed = g.Sum(x => x.Bruto - x.Dinbol - x.Dinbol2),
                Bruto = g.Sum(x => x.Bruto),
                DopPioR = g.Sum(x => x.Doppr),
                DopZdrR = g.Sum(x => x.Dopzr),
                DopNezR = g.Sum(x => x.Dopnr),
                Porez = g.Sum(x => x.Porez),
                PorOslob = g.Sum(x => x.Poroslob1 + x.Poroslob2 + x.Poroslob3 + x.Poroslob4),
                OsnovPor = g.Sum(x => x.Bruto - x.Poroslob1 - x.Poroslob2 - x.Poroslob3 - x.Poroslob4),
                Neto = g.Sum(x => x.Neto),
                DopPioF = g.Sum(x => x.Doppf),
                DopZdrF = g.Sum(x => x.Dopzf),
                DopNezF = g.Sum(x => x.Dopnf),
                SvePorIDop = g.Sum(x => x.Doppr + x.Dopzr + x.Dopnr + x.Doppf + x.Dopzf + x.Dopnf + x.Porez),
                SvePio = g.Sum(x => x.Doppr + x.Doppf)
            })
            .ToList();

        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za saldo sve.";
            return;
        }

        var naslov = samoMesec ? $"SALDO SVE ZA MESEC {mesec}" : "SALDO SVE";
        OtvoriIzvestaj(naslov, redovi);
        Poruka = $"Prikazan {naslov.ToLowerInvariant()}.";
    }

    private void PrikaziSaldoPoMesecima()
    {
        var redovi = Stavke
            .OrderBy(s => s.Godina)
            .ThenBy(s => s.Mesec)
            .ThenBy(s => s.Vrsta)
            .GroupBy(s => new { s.Godina, s.Mesec, s.Nazmes, s.Vrsta })
            .Select(g => new SaldoPoMesecuRed
            {
                Godina = g.Key.Godina,
                Mesec = g.Key.Mesec,
                Nazmes = g.Key.Nazmes,
                Vrsta = g.Key.Vrsta,
                CasBol = g.Sum(x => x.Casbol + x.Casbol2),
                CasRed = g.Sum(x => x.Casuk - x.Casbol - x.Casbol2),
                CasUk = g.Sum(x => x.Casuk),
                DinBol = g.Sum(x => x.Dinbol + x.Dinbol2),
                DinRed = g.Sum(x => x.Bruto - x.Dinbol - x.Dinbol2),
                Bruto = g.Sum(x => x.Bruto),
                DopPioR = g.Sum(x => x.Doppr),
                DopZdrR = g.Sum(x => x.Dopzr),
                DopNezR = g.Sum(x => x.Dopnr),
                Porez = g.Sum(x => x.Porez),
                PorOslob = g.Sum(x => x.Poroslob1 + x.Poroslob2 + x.Poroslob3 + x.Poroslob4),
                OsnovPor = g.Sum(x => x.Bruto - x.Poroslob1 - x.Poroslob2 - x.Poroslob3 - x.Poroslob4),
                Neto = g.Sum(x => x.Neto)
            })
            .ToList();

        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za saldo po mesecima.";
            return;
        }

        OtvoriIzvestaj("SALDO PO MESECIMA", redovi);
        Poruka = $"Prikazan saldo po mesecima ({redovi.Count} stavki).";
    }

    private void PrikaziSaldoPoDatumima()
    {
        var redovi = Stavke
            .OrderBy(s => s.Datum ?? DateTime.MinValue)
            .ThenBy(s => s.Vrsta)
            .GroupBy(s => new { Datum = s.Datum?.Date, s.Vrsta })
            .Select(g => new SaldoPoDatumuRed
            {
                Datum = g.Key.Datum,
                Vrsta = g.Key.Vrsta,
                Bruto = g.Sum(x => x.Bruto),
                DopPioR = g.Sum(x => x.Doppr),
                DopZdrR = g.Sum(x => x.Dopzr),
                DopNezR = g.Sum(x => x.Dopnr),
                Porez = g.Sum(x => x.Porez),
                Neto = g.Sum(x => x.Neto)
            })
            .ToList();

        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za saldo po datumima.";
            return;
        }

        OtvoriIzvestaj("SALDO PO DATUMIMA", redovi);
        Poruka = $"Prikazan saldo po datumima ({redovi.Count} stavki).";
    }

    private void PrikaziSaldoPoGradovima()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var mapaGradova = UcitajMapuGradova(folder);
        var redovi = Stavke
            .GroupBy(s => new { Grad = GradZaBroj(mapaGradova, s.Broj), s.Godina, s.Mesec, s.Nazmes, s.Vrsta })
            .OrderBy(g => g.Key.Grad)
            .ThenBy(g => g.Key.Godina)
            .ThenBy(g => g.Key.Mesec)
            .ThenBy(g => g.Key.Vrsta)
            .Select(g => new SaldoPoGraduRed
            {
                Grad = g.Key.Grad,
                Godina = g.Key.Godina,
                Mesec = g.Key.Mesec,
                Nazmes = g.Key.Nazmes,
                Vrsta = g.Key.Vrsta,
                Bruto = g.Sum(x => x.Bruto),
                DopPioR = g.Sum(x => x.Doppr),
                DopZdrR = g.Sum(x => x.Dopzr),
                DopNezR = g.Sum(x => x.Dopnr),
                Porez = g.Sum(x => x.Porez),
                Neto = g.Sum(x => x.Neto),
                DopPioF = g.Sum(x => x.Doppf),
                DopZdrF = g.Sum(x => x.Dopzf),
                DopNezF = g.Sum(x => x.Dopnf),
                SvePorIDop = g.Sum(x => x.Doppr + x.Dopzr + x.Dopnr + x.Doppf + x.Dopzf + x.Dopnf + x.Porez)
            })
            .ToList();

        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za saldo po gradovima.";
            return;
        }

        OtvoriIzvestaj("SALDO PO GRADOVIMA", redovi);
        Poruka = $"Prikazan saldo po gradovima ({redovi.Count} stavki).";
    }

    private void OtvoriIzvestaj<T>(string naslov, IReadOnlyList<T> redovi)
    {
        var view = new Views.Zarade.LdBolGenericReportView(naslov, redovi, redovi.Count);
        var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (aktivniProzor != null)
            view.Owner = aktivniProzor;
        view.ShowDialog();
    }

    private IEnumerable<LdObracunStavka>? FiltrirajPoMesecuAkoTreba(bool samoMesec, out int mesec)
    {
        mesec = 0;
        if (!samoMesec)
            return Stavke;

        mesec = Mesec > 0 ? Mesec : Selektovana?.Mesec ?? 0;
        if (mesec <= 0)
        {
            Poruka = "Za ovu opciju unesite mesec ili selektujte stavku sa mesecom.";
            return null;
        }

        var ciljniMesec = mesec;
        return Stavke.Where(s => s.Mesec == ciljniMesec);
    }

    private bool TryGetIzabraniRadnik(out int broj, out string ime)
    {
        broj = 0;
        ime = string.Empty;
        if (Selektovana == null)
            Selektovana = Stavke.FirstOrDefault();

        if (Selektovana == null)
        {
            Poruka = "Izaberite radnika iz liste.";
            return false;
        }

        broj = Selektovana.Broj;
        ime = Selektovana.ImePrez;
        return true;
    }

    private static Dictionary<int, string> UcitajMapuGradova(string folderPath)
    {
        var ldradPath = Path.Combine(folderPath, "ldrad.dbf");
        if (!File.Exists(ldradPath))
            return new Dictionary<int, string>();

        try
        {
            return DbfReader.CitajSveZapise(ldradPath)
                .GroupBy(r => Int(r, "BROJ"))
                .Where(g => g.Key > 0)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var red = g.First();
                        var m4 = Str(red, "M4GRAD");
                        var mesto = Str(red, "MESTO");
                        return string.IsNullOrWhiteSpace(m4)
                            ? (string.IsNullOrWhiteSpace(mesto) ? "NEPOZNAT GRAD" : mesto)
                            : m4;
                    });
        }
        catch
        {
            return new Dictionary<int, string>();
        }
    }

    private static string GradZaBroj(IReadOnlyDictionary<int, string> mapa, int broj)
        => mapa.TryGetValue(broj, out var grad) && !string.IsNullOrWhiteSpace(grad)
            ? grad
            : "NEPOZNAT GRAD";

    private sealed class JedanRadnikSveRed
    {
        public int Mesec { get; init; }
        public int Isplata { get; init; }
        public string Godina { get; init; } = string.Empty;
        public string Vrsta { get; init; } = string.Empty;
        public decimal Bruto { get; init; }
        public decimal Neto { get; init; }
        public decimal Porez { get; init; }
        public decimal DoprinosPioR { get; init; }
        public decimal DoprinosZdrR { get; init; }
        public decimal DoprinosNezR { get; init; }
        public decimal DoprinosPioF { get; init; }
        public decimal DoprinosZdrF { get; init; }
        public decimal DoprinosNezF { get; init; }
        public decimal ZaIsplatu { get; init; }
        public decimal UkObust { get; init; }
    }

    private sealed class JedanRadnikSkraceniRed
    {
        public int Mesec { get; init; }
        public int Isplata { get; init; }
        public string Godina { get; init; } = string.Empty;
        public string Vrsta { get; init; } = string.Empty;
        public decimal CasUkupno { get; init; }
        public decimal Bruto { get; init; }
        public decimal DoprinosPioR { get; init; }
        public decimal DoprinosPioF { get; init; }
        public decimal Neto { get; init; }
        public decimal ZaIsplatu { get; init; }
    }

    private sealed class SaldoRadnikaRed
    {
        public int Broj { get; init; }
        public string ImePrez { get; init; } = string.Empty;
        public decimal CasBol { get; init; }
        public decimal CasRed { get; init; }
        public decimal CasUk { get; init; }
        public decimal DinBol { get; init; }
        public decimal DinRed { get; init; }
        public decimal Bruto { get; init; }
        public decimal DopSoc { get; init; }
        public decimal Benef { get; init; }
        public decimal NetoRed { get; init; }
        public decimal NetoBol { get; init; }
        public decimal NetoUk { get; init; }
    }

    private sealed class SaldoSkraceniRed
    {
        public int Broj { get; init; }
        public string ImePrez { get; init; } = string.Empty;
        public decimal CasBol { get; init; }
        public decimal CasRed { get; init; }
        public decimal CasUk { get; init; }
        public decimal DinBol { get; init; }
        public decimal DinRed { get; init; }
        public decimal Bruto { get; init; }
        public decimal DoppioR { get; init; }
        public decimal DoppioF { get; init; }
        public decimal DoppioSve { get; init; }
    }

    private sealed class SaldoSveRed
    {
        public int Broj { get; init; }
        public string ImePrez { get; init; } = string.Empty;
        public decimal CasBol { get; init; }
        public decimal CasRed { get; init; }
        public decimal CasUk { get; init; }
        public decimal DinBol { get; init; }
        public decimal DinRed { get; init; }
        public decimal Bruto { get; init; }
        public decimal DopPioR { get; init; }
        public decimal DopZdrR { get; init; }
        public decimal DopNezR { get; init; }
        public decimal Porez { get; init; }
        public decimal PorOslob { get; init; }
        public decimal OsnovPor { get; init; }
        public decimal Neto { get; init; }
        public decimal DopPioF { get; init; }
        public decimal DopZdrF { get; init; }
        public decimal DopNezF { get; init; }
        public decimal SvePorIDop { get; init; }
        public decimal SvePio { get; init; }
    }

    private sealed class SaldoPoMesecuRed
    {
        public string Godina { get; init; } = string.Empty;
        public int Mesec { get; init; }
        public string Nazmes { get; init; } = string.Empty;
        public string Vrsta { get; init; } = string.Empty;
        public decimal CasBol { get; init; }
        public decimal CasRed { get; init; }
        public decimal CasUk { get; init; }
        public decimal DinBol { get; init; }
        public decimal DinRed { get; init; }
        public decimal Bruto { get; init; }
        public decimal DopPioR { get; init; }
        public decimal DopZdrR { get; init; }
        public decimal DopNezR { get; init; }
        public decimal Porez { get; init; }
        public decimal PorOslob { get; init; }
        public decimal OsnovPor { get; init; }
        public decimal Neto { get; init; }
    }

    private sealed class SaldoPoDatumuRed
    {
        public DateTime? Datum { get; init; }
        public string Vrsta { get; init; } = string.Empty;
        public decimal Bruto { get; init; }
        public decimal DopPioR { get; init; }
        public decimal DopZdrR { get; init; }
        public decimal DopNezR { get; init; }
        public decimal Porez { get; init; }
        public decimal Neto { get; init; }
    }

    private sealed class SaldoPoGraduRed
    {
        public string Grad { get; init; } = string.Empty;
        public string Godina { get; init; } = string.Empty;
        public int Mesec { get; init; }
        public string Nazmes { get; init; } = string.Empty;
        public string Vrsta { get; init; } = string.Empty;
        public decimal Bruto { get; init; }
        public decimal DopPioR { get; init; }
        public decimal DopZdrR { get; init; }
        public decimal DopNezR { get; init; }
        public decimal Porez { get; init; }
        public decimal Neto { get; init; }
        public decimal DopPioF { get; init; }
        public decimal DopZdrF { get; init; }
        public decimal DopNezF { get; init; }
        public decimal SvePorIDop { get; init; }
    }

    private void SelektujPoIndeks(int indeks)
    {
        if (Stavke.Count == 0)
        {
            Selektovana = null;
            return;
        }

        indeks = Math.Clamp(indeks, 0, Stavke.Count - 1);
        Selektovana = Stavke[indeks];
    }

    private static string Str(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v != null ? (v.ToString() ?? string.Empty).Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0;
        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        if (int.TryParse(v.ToString(), out var parsed)) return parsed;
        return 0;
    }
}
