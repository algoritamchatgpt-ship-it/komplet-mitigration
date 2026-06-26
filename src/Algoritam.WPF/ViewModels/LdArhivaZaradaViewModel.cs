using Algoritam.Application;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Dbf;
using Algoritam.WPF.Models;
using Algoritam.WPF.Views.Zarade;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Fox forma LDARHIVA - pregled i operacije nad ldarhiva.dbf.
/// </summary>
public partial class LdArhivaZaradaViewModel : ObservableObject
{
    private readonly AppState _appState;
    private List<LdObracunStavka> _sveStavke = [];
    private string? _ldArhivaPath;

    [ObservableProperty]
    private ObservableCollection<LdObracunStavka> _stavke = [];

    [ObservableProperty]
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
    private string _putanjaArhive = string.Empty;

    [ObservableProperty]
    private bool _samoArhivirane;

    [ObservableProperty]
    private string _poruka = string.Empty;

    [ObservableProperty]
    private bool _ucitava;

    public LdArhivaZaradaViewModel(AppState appState)
    {
        _appState = appState;
        PostaviPodrazumevaniPeriod();
        PutanjaArhive = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        _ = UcitajAsync();
    }

    public string Naslov => "ARHIVIRANE ZARADE (LDARHIVA)";

    [RelayCommand]
    private async Task UcitajAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            _sveStavke = [];
            Stavke = [];
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        try
        {
            Ucitava = true;
            PutanjaArhive = string.IsNullOrWhiteSpace(PutanjaArhive) ? folder : PutanjaArhive;
            _ldArhivaPath = LdBolovanjeDbfSupport.PronadjiDbf(folder, "ldarhiva.dbf");

            List<LdObracunStavka> sve;
            if (!string.IsNullOrWhiteSpace(_ldArhivaPath))
            {
                var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(_ldArhivaPath));
                sve = zapisi.Select(MapirajArhivaZapis).ToList();
            }
            else
            {
                sve = await Task.Run(() => LdObracunDbfReader.CitajSve(folder));
            }

            _sveStavke = sve;
            PrimeniFilter();

            var ukupno = Stavke.Sum(x => x.Zaisplatu);
            var izvor = string.IsNullOrWhiteSpace(_ldArhivaPath) ? "LD*.dbf" : "ldarhiva.dbf";
            Poruka = $"Ucitano {Stavke.Count} stavki iz {izvor}. Ukupno za isplatu: {ukupno:N2}.";
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

    [RelayCommand]
    private Task OsveziAsync() => UcitajAsync();

    [RelayCommand]
    private void Prvi() => SelektujPoIndeks(0);

    [RelayCommand]
    private void Zadnji() => SelektujPoIndeks(Stavke.Count - 1);

    [RelayCommand]
    private void Gore()
    {
        if (Stavke.Count == 0)
            return;

        var index = Selektovana is null ? 0 : Stavke.IndexOf(Selektovana);
        if (index <= 0)
            index = 0;
        else
            index--;

        SelektujPoIndeks(index);
    }

    [RelayCommand]
    private void Dole()
    {
        if (Stavke.Count == 0)
            return;

        var index = Selektovana is null ? 0 : Stavke.IndexOf(Selektovana);
        if (index < 0)
            index = 0;
        else if (index < Stavke.Count - 1)
            index++;

        SelektujPoIndeks(index);
    }

    [RelayCommand]
    private async Task OznaciArhivuAsync() => await PostaviArhivuAsync("*", "Oznacena");

    [RelayCommand]
    private async Task SkiniArhivuAsync() => await PostaviArhivuAsync(string.Empty, "Skinuta");

    [RelayCommand]
    private async Task DodavanjeMesecaAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        if (Mesec <= 0)
        {
            Poruka = "Unesite mesec za dodavanje.";
            return;
        }

        var vrsta = NormalizujVrstu(Vrsta, Selektovana?.Vrsta);
        if (string.IsNullOrWhiteSpace(vrsta))
        {
            Poruka = "Unesite vrstu isplate (A/B/P/I/R/U).";
            return;
        }

        var godina = NormalizujGodinu(Godina, Selektovana?.Godina);
        if (string.IsNullOrWhiteSpace(godina))
        {
            Poruka = "Unesite godinu.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_ldArhivaPath))
        {
            Poruka = "Nije pronađen ldarhiva.dbf.";
            return;
        }

        var izvorniFolder = string.IsNullOrWhiteSpace(PutanjaArhive) ? folder : PutanjaArhive.Trim();
        if (!Directory.Exists(izvorniFolder))
        {
            Poruka = "Putanja arhive nije validna.";
            return;
        }

        var izvorPath = LdBolovanjeDbfSupport.PronadjiDbf(izvorniFolder, "ldarhiva.dbf");
        if (string.IsNullOrWhiteSpace(izvorPath))
        {
            Poruka = $"U putanji '{izvorniFolder}' nije pronađen ldarhiva.dbf.";
            return;
        }

        try
        {
            Ucitava = true;
            var cilj = await Task.Run(() => DbfReader.CitajSveZapise(_ldArhivaPath));
            if (cilj.Any(r => Int(r, "MESEC") == Mesec
                              && StringEquals(Str(r, "GODINA"), godina)
                              && StringEquals(Str(r, "VRSTA"), vrsta)))
            {
                Poruka = "Za zadati mesec/godinu/vrstu vec postoje podaci u arhivi.";
                return;
            }

            var isplata = MapVrstaUIsplatu(vrsta);
            var izvor = await Task.Run(() => DbfReader.CitajSveZapise(izvorPath));
            var zaAppend = izvor
                .Where(r => Int(r, "MESEC") == Mesec)
                .Where(r => isplata <= 0 || Int(r, "ISPLATA") == isplata)
                .Where(r => string.IsNullOrWhiteSpace(godina) || StringEquals(Str(r, "GODINA"), godina))
                .ToList();

            if (zaAppend.Count == 0)
            {
                Poruka = "Nema stavki za dodavanje u izabranoj arhivi.";
                return;
            }

            foreach (var row in zaAppend)
            {
                var nov = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase)
                {
                    ["MESEC"] = (decimal)Mesec,
                    ["VRSTA"] = vrsta,
                    ["ISPLATA"] = (decimal)isplata,
                    ["GODINA"] = godina
                };
                cilj.Add(nov);
            }

            UpisiTabelu(_ldArhivaPath, cilj);
            await UcitajAsync();
            Poruka = $"Dodato {zaAppend.Count} stavki za {Mesec:00}/{godina} ({vrsta}).";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri dodavanju meseca: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private async Task BrisanjeMesecaAsync()
    {
        var mesecZaBrisanje = Mesec > 0 ? Mesec : Selektovana?.Mesec ?? 0;
        if (mesecZaBrisanje <= 0)
        {
            Poruka = "Unesite mesec (ili izaberite stavku) za brisanje.";
            return;
        }

        var vrsta = NormalizujVrstu(Vrsta, Selektovana?.Vrsta);
        var godina = NormalizujGodinu(Godina, Selektovana?.Godina);
        var isplata = Isplata > 0 ? Isplata : (Selektovana?.Isplata ?? 0);

        var potvrdaTekst = $"Obrisati podatke za mesec {mesecZaBrisanje}"
            + (string.IsNullOrWhiteSpace(godina) ? string.Empty : $", godinu {godina}")
            + (string.IsNullOrWhiteSpace(vrsta) ? string.Empty : $", vrstu {vrsta}")
            + (isplata > 0 ? $", isplatu {isplata}" : string.Empty)
            + "?";

        if (MessageBox.Show(potvrdaTekst, "LDARHIVA", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            Ucitava = true;

            if (!string.IsNullOrWhiteSpace(_ldArhivaPath))
            {
                var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(_ldArhivaPath));
                var ostali = zapisi.Where(r =>
                {
                    if (Int(r, "MESEC") != mesecZaBrisanje)
                        return true;
                    if (!string.IsNullOrWhiteSpace(godina) && !StringEquals(Str(r, "GODINA"), godina))
                        return true;
                    if (!string.IsNullOrWhiteSpace(vrsta) && !StringEquals(Str(r, "VRSTA"), vrsta))
                        return true;
                    if (isplata > 0 && Int(r, "ISPLATA") != isplata)
                        return true;
                    return false;
                }).ToList();

                var obrisano = zapisi.Count - ostali.Count;
                if (obrisano <= 0)
                {
                    Poruka = "Nema stavki za zadati uslov.";
                    return;
                }

                UpisiTabelu(_ldArhivaPath, ostali);
                await UcitajAsync();
                Poruka = $"Obrisano {obrisano} stavki.";
                return;
            }

            var folder = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
            var rezultat = await LdDbfMutator.ObrisiPoUslovuAsync(folder, row =>
            {
                if (Int(row, "MESEC") != mesecZaBrisanje) return false;
                if (!string.IsNullOrWhiteSpace(godina) && !StringEquals(Str(row, "GODINA"), godina)) return false;
                if (!string.IsNullOrWhiteSpace(vrsta) && !StringEquals(Str(row, "VRSTA"), vrsta)) return false;
                if (isplata > 0 && Int(row, "ISPLATA") != isplata) return false;
                return true;
            });

            if (rezultat.Izmenjeno == 0)
            {
                Poruka = "Nema stavki za zadati uslov.";
                return;
            }

            await UcitajAsync();
            Poruka = $"Obrisano {rezultat.Izmenjeno} stavki (fajlova: {rezultat.Fajlovi}).";
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
    private async Task BrisanjePrazninaAsync()
    {
        if (MessageBox.Show("Obrisati stavke bez imena/prezimena?", "LDARHIVA", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            Ucitava = true;

            if (!string.IsNullOrWhiteSpace(_ldArhivaPath))
            {
                var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(_ldArhivaPath));
                var ostali = zapisi.Where(r => !string.IsNullOrWhiteSpace(Str(r, "IME_PREZ"))).ToList();
                var obrisano = zapisi.Count - ostali.Count;
                if (obrisano <= 0)
                {
                    Poruka = "Nema praznih stavki za brisanje.";
                    return;
                }

                UpisiTabelu(_ldArhivaPath, ostali);
                await UcitajAsync();
                Poruka = $"Obrisano {obrisano} praznih stavki.";
                return;
            }

            var folder = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
            var rezultat = await LdDbfMutator.ObrisiPoUslovuAsync(
                folder,
                row => string.IsNullOrWhiteSpace(Str(row, "IME_PREZ")));

            if (rezultat.Izmenjeno == 0)
            {
                Poruka = "Nema praznih stavki za brisanje.";
                return;
            }

            await UcitajAsync();
            Poruka = $"Obrisano {rezultat.Izmenjeno} praznih stavki (fajlova: {rezultat.Fajlovi}).";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri brisanju praznina: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private async Task UnesiRegBrojPioAsync()
    {
        if (string.IsNullOrWhiteSpace(_ldArhivaPath))
        {
            Poruka = "Nije pronađen ldarhiva.dbf.";
            return;
        }

        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var ldradPath = LdBolovanjeDbfSupport.PronadjiDbf(folder, "ldrad.dbf");
        if (string.IsNullOrWhiteSpace(ldradPath))
        {
            Poruka = "Nije pronađen ldrad.dbf.";
            return;
        }

        try
        {
            Ucitava = true;
            var arhiva = await Task.Run(() => DbfReader.CitajSveZapise(_ldArhivaPath));
            var radnici = await Task.Run(() => DbfReader.CitajSveZapise(ldradPath));
            var mapa = radnici
                .GroupBy(r => Int(r, "BROJ"))
                .Where(g => g.Key > 0)
                .ToDictionary(g => g.Key, g => g.First());

            var izmenjeno = 0;
            foreach (var row in arhiva)
            {
                var broj = Int(row, "BROJ");
                if (!mapa.TryGetValue(broj, out var radnik))
                    continue;

                izmenjeno += PostaviAkoDrugacije(row, "REGSOC", Str(radnik, "REGSOC"));
                izmenjeno += PostaviAkoDrugacije(row, "M4GRAD", Str(radnik, "M4GRAD"));
                izmenjeno += PostaviAkoDrugacije(row, "MATICNIBR", Str(radnik, "MATICNIBR"));
            }

            if (izmenjeno == 0)
            {
                Poruka = "Nema promena za REGSOC/M4GRAD/MATICNIBR.";
                return;
            }

            UpisiTabelu(_ldArhivaPath, arhiva);
            await UcitajAsync();
            Poruka = $"Azurirano {izmenjeno} polja (REGSOC/M4GRAD/MATICNIBR).";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri unosu reg.broj.pio: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private async Task PrenesiPpodpAsync()
    {
        if (string.IsNullOrWhiteSpace(_ldArhivaPath))
        {
            Poruka = "Nije pronađen ldarhiva.dbf.";
            return;
        }

        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var ldppodpPath = LdBolovanjeDbfSupport.PronadjiDbf(folder, "ldppodp.dbf");
        var ldppodoPath = LdBolovanjeDbfSupport.PronadjiDbf(folder, "ldppodo.dbf");
        if (string.IsNullOrWhiteSpace(ldppodpPath) && string.IsNullOrWhiteSpace(ldppodoPath))
        {
            Poruka = "Nedostaju ldppodp.dbf i ldppodo.dbf.";
            return;
        }

        try
        {
            Ucitava = true;
            var cilj = await Task.Run(() => DbfReader.CitajSveZapise(_ldArhivaPath));
            var ukupno = 0;

            if (!string.IsNullOrWhiteSpace(ldppodpPath))
            {
                var ppodp = await Task.Run(() => DbfReader.CitajSveZapise(ldppodpPath));
                ukupno += DodajIzPpodp(cilj, ppodp, "S", ubaciPorez: true);
            }

            if (!string.IsNullOrWhiteSpace(ldppodoPath))
            {
                var ppodo = await Task.Run(() => DbfReader.CitajSveZapise(ldppodoPath));
                ukupno += DodajIzPpodp(cilj, ppodo, "V", ubaciPorez: false);
            }

            if (ukupno == 0)
            {
                Poruka = "Nema novih stavki za prenos iz PPODP tabela.";
                return;
            }

            UpisiTabelu(_ldArhivaPath, cilj);
            await UcitajAsync();
            Poruka = $"Preneto {ukupno} stavki iz PPODP tabela.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri prenosu PPODP: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private void PregledPodataka()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za pregled.";
            return;
        }

        var view = new PregledKoloneView(Stavke);
        PostaviOwnerIPrikazi(view);
    }

    [RelayCommand]
    private void JedanRadnikSve()
    {
        if (Selektovana == null)
        {
            Poruka = "Izaberite radnika.";
            return;
        }

        var s = Selektovana;
        var redovi = new List<PregledTabelaStavka>
        {
            Red("01", "Bruto", s.Bruto, 0m),
            Red("02", "Neto", s.Neto, 0m),
            Red("03", "Za isplatu", s.Zaisplatu, 0m),
            Red("04", "Porez", s.Porez, 0m),
            Red("05", "Doprinosi radnik", s.Dopsocr, 0m),
            Red("06", "Doprinosi firma", s.Dopsocf, 0m),
            Red("07", "Naknade", s.Naknade, 0m),
            Red("08", "Ukupne obustave", s.Ukobust, 0m),
            Red("09", "Casovi ukupno", s.Casuk, 0m),
            Red("10", "Dinarski ukupno", s.Dinuk, 0m)
        };

        OtvoriFoxTabelu(
            "JEDAN RADNIK SVE",
            $"{s.Broj} - {PraznoTekst(s.ImePrez)}",
            redovi,
            "IZNOS",
            "REZERVA");
    }

    [RelayCommand]
    private void JedanRadnikSkraceni()
    {
        if (Selektovana == null)
        {
            Poruka = "Izaberite radnika.";
            return;
        }

        var s = Selektovana;
        var redovi = new List<PregledTabelaStavka>
        {
            Red("01", "Bruto", s.Bruto, s.Neto),
            Red("02", "Porez", s.Porez, s.Dopsocr),
            Red("03", "Doprinosi firma", s.Dopsocf, s.Ukobust),
            Red("04", "Za isplatu", s.Zaisplatu, s.Naknade)
        };

        OtvoriFoxTabelu(
            "JEDAN RADNIK SKRACENI",
            $"{s.Broj} - {PraznoTekst(s.ImePrez)}",
            redovi,
            "IZNOS 1",
            "IZNOS 2");
    }

    [RelayCommand]
    private void SaldoRadnika()
        => OtvoriFoxTabelu(
            "SALDO RADNIKA",
            "Zbir po radniku",
            Stavke.GroupBy(s => new { s.Broj, s.ImePrez })
                .OrderBy(g => g.Key.Broj)
                .Select(g => new PregledTabelaStavka
                {
                    Sifra = g.Key.Broj.ToString(CultureInfo.InvariantCulture),
                    Naziv = PraznoTekst(g.Key.ImePrez),
                    Iznos1 = g.Sum(x => x.Bruto),
                    Iznos2 = g.Sum(x => x.Zaisplatu)
                })
                .ToList(),
            "BRUTO",
            "ZA ISPLATU");

    [RelayCommand]
    private void SaldoSkraceni()
        => OtvoriFoxTabelu(
            "SALDO SKRACENI",
            "Neto i obustave po radniku",
            Stavke.GroupBy(s => new { s.Broj, s.ImePrez })
                .OrderBy(g => g.Key.Broj)
                .Select(g => new PregledTabelaStavka
                {
                    Sifra = g.Key.Broj.ToString(CultureInfo.InvariantCulture),
                    Naziv = PraznoTekst(g.Key.ImePrez),
                    Iznos1 = g.Sum(x => x.Neto),
                    Iznos2 = g.Sum(x => x.Ukobust)
                })
                .ToList(),
            "NETO",
            "OBUSTAVE");

    [RelayCommand]
    private void SaldoSkraceniZaMesec()
    {
        var mesec = Mesec > 0 ? Mesec : Selektovana?.Mesec ?? 0;
        if (mesec <= 0)
        {
            Poruka = "Unesite mesec za skraceni saldo.";
            return;
        }

        var redovi = Stavke
            .Where(s => s.Mesec == mesec)
            .GroupBy(s => new { s.Broj, s.ImePrez })
            .OrderBy(g => g.Key.Broj)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = g.Key.Broj.ToString(CultureInfo.InvariantCulture),
                Naziv = PraznoTekst(g.Key.ImePrez),
                Iznos1 = g.Sum(x => x.Neto),
                Iznos2 = g.Sum(x => x.Ukobust)
            })
            .ToList();

        OtvoriFoxTabelu("SALDO SKRACENI ZA MESEC", $"Mesec {mesec:00}", redovi, "NETO", "OBUSTAVE");
    }

    [RelayCommand]
    private void SaldoSve()
    {
        var redovi = new List<PregledTabelaStavka>
        {
            Red("01", "Bruto", Stavke.Sum(x => x.Bruto), 0m),
            Red("02", "Neto", Stavke.Sum(x => x.Neto), 0m),
            Red("03", "Za isplatu", Stavke.Sum(x => x.Zaisplatu), 0m),
            Red("04", "Porez", Stavke.Sum(x => x.Porez), 0m),
            Red("05", "Doprinosi radnik", Stavke.Sum(x => x.Dopsocr), 0m),
            Red("06", "Doprinosi firma", Stavke.Sum(x => x.Dopsocf), 0m),
            Red("07", "Naknade", Stavke.Sum(x => x.Naknade), 0m),
            Red("08", "Obustave", Stavke.Sum(x => x.Ukobust), 0m)
        };

        OtvoriFoxTabelu("SALDO SVE", "Zbirni saldo", redovi, "IZNOS", "REZERVA");
    }

    [RelayCommand]
    private void SaldoSveZaMesec()
    {
        var mesec = Mesec > 0 ? Mesec : Selektovana?.Mesec ?? 0;
        if (mesec <= 0)
        {
            Poruka = "Unesite mesec za saldo.";
            return;
        }

        var filtrirano = Stavke.Where(s => s.Mesec == mesec).ToList();
        if (filtrirano.Count == 0)
        {
            Poruka = "Nema podataka za zadati mesec.";
            return;
        }

        var redovi = new List<PregledTabelaStavka>
        {
            Red("01", "Bruto", filtrirano.Sum(x => x.Bruto), 0m),
            Red("02", "Neto", filtrirano.Sum(x => x.Neto), 0m),
            Red("03", "Za isplatu", filtrirano.Sum(x => x.Zaisplatu), 0m),
            Red("04", "Porez", filtrirano.Sum(x => x.Porez), 0m),
            Red("05", "Doprinosi radnik", filtrirano.Sum(x => x.Dopsocr), 0m),
            Red("06", "Doprinosi firma", filtrirano.Sum(x => x.Dopsocf), 0m)
        };

        OtvoriFoxTabelu("SALDO SVE ZA MESEC", $"Mesec {mesec:00}", redovi, "IZNOS", "REZERVA");
    }

    [RelayCommand]
    private void SaldoPoMesecima()
    {
        var redovi = Stavke
            .GroupBy(s => new { Godina = PraznoTekst(s.Godina), s.Mesec })
            .OrderBy(g => g.Key.Godina)
            .ThenBy(g => g.Key.Mesec)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = $"{g.Key.Godina}/{g.Key.Mesec:00}",
                Naziv = $"Mesec {g.Key.Mesec:00}",
                Iznos1 = g.Sum(x => x.Bruto),
                Iznos2 = g.Sum(x => x.Zaisplatu)
            })
            .ToList();

        OtvoriFoxTabelu("SALDO PO MESECIMA", "Grupni prikaz po mesecima", redovi, "BRUTO", "ZA ISPLATU");
    }

    [RelayCommand]
    private void SaldoPoDatumima()
    {
        var redovi = Stavke
            .Where(s => s.Datum != null)
            .GroupBy(s => s.Datum!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = g.Key.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                Naziv = "Datum isplate",
                Iznos1 = g.Sum(x => x.Bruto),
                Iznos2 = g.Sum(x => x.Zaisplatu)
            })
            .ToList();

        OtvoriFoxTabelu("SALDO PO DATUMIMA", "Grupni prikaz po datumima", redovi, "BRUTO", "ZA ISPLATU");
    }

    [RelayCommand]
    private void SaldoDoprinosa()
    {
        var redovi = Stavke
            .GroupBy(s => new { s.Broj, s.ImePrez })
            .OrderBy(g => g.Key.Broj)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = g.Key.Broj.ToString(CultureInfo.InvariantCulture),
                Naziv = PraznoTekst(g.Key.ImePrez),
                Iznos1 = g.Sum(x => x.Dopsocr),
                Iznos2 = g.Sum(x => x.Dopsocf)
            })
            .ToList();

        OtvoriFoxTabelu("SALDO DOPRINOSA", "Doprinosi radnik/firma", redovi, "RADNIK", "FIRMA");
    }

    [RelayCommand]
    private void SaldoZaNaknade()
    {
        var redovi = Stavke
            .GroupBy(s => new { s.Broj, s.ImePrez })
            .OrderBy(g => g.Key.Broj)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = g.Key.Broj.ToString(CultureInfo.InvariantCulture),
                Naziv = PraznoTekst(g.Key.ImePrez),
                Iznos1 = g.Sum(x => x.Naknade),
                Iznos2 = g.Sum(x => x.Zaisplatu)
            })
            .ToList();

        OtvoriFoxTabelu("SALDO ZA NAKNADE", "Naknade i isplata po radniku", redovi, "NAKNADE", "ZA ISPLATU");
    }

    [RelayCommand]
    private void Mesto()
    {
        var redovi = Stavke
            .GroupBy(s => s.Mtr)
            .OrderBy(g => g.Key)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = g.Key.ToString(CultureInfo.InvariantCulture),
                Naziv = $"MESTO {g.Key}",
                Iznos1 = g.Sum(x => x.Casuk),
                Iznos2 = g.Sum(x => x.Dinuk)
            })
            .ToList();

        OtvoriFoxTabelu("MESTO", "Pregled po mestu troska", redovi, "CASOVI", "DINARI");
    }

    [RelayCommand]
    private void M4kStari()
    {
        var redovi = Stavke
            .GroupBy(s => new { s.Broj, s.ImePrez })
            .OrderBy(g => g.Key.Broj)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = g.Key.Broj.ToString(CultureInfo.InvariantCulture),
                Naziv = PraznoTekst(g.Key.ImePrez),
                Iznos1 = g.Sum(x => x.Neto),
                Iznos2 = g.Sum(x => x.Casuk)
            })
            .ToList();

        OtvoriFoxTabelu("M-4K STARI", "Stari M4 zbir", redovi, "NETO", "CASOVI");
    }

    [RelayCommand]
    private void M4kNovi()
    {
        var redovi = Stavke
            .GroupBy(s => PraznoTekst(s.Maticnibr))
            .OrderBy(g => g.Key)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = g.Key,
                Naziv = "Maticni broj",
                Iznos1 = g.Sum(x => x.Bruto),
                Iznos2 = g.Sum(x => x.Doppr + x.Doppf)
            })
            .ToList();

        OtvoriFoxTabelu("M-4K NOVI", "Novi M4 zbir", redovi, "BRUTO", "PIO");
    }

    [RelayCommand]
    private void M4NoviPojedinacni()
    {
        if (Selektovana == null)
        {
            Poruka = "Izaberite radnika.";
            return;
        }

        var s = Selektovana;
        var redovi = new List<PregledTabelaStavka>
        {
            Red("01", "Bruto", s.Bruto, 0m),
            Red("02", "Din bolovanja", s.Dinbol + s.Dinbol2, 0m),
            Red("03", "PIO radnik", s.Doppr, 0m),
            Red("04", "PIO firma", s.Doppf, 0m),
            Red("05", "Neto", s.Neto, s.Zaisplatu)
        };

        OtvoriFoxTabelu("M-4 NOVI POJEDINACNI", $"{s.Broj} - {PraznoTekst(s.ImePrez)}", redovi, "IZNOS", "REZERVA");
    }

    [RelayCommand]
    private void M4SpVlasnik() => M4NoviPojedinacni();

    [RelayCommand]
    private void M4C3Vlasnik() => M4NoviPojedinacni();

    [RelayCommand]
    private void OcistiFilter()
    {
        Vrsta = string.Empty;
        Godina = string.Empty;
        Mesec = 0;
        Isplata = 0;
        SamoArhivirane = false;
        PrimeniFilter();
        Poruka = "Filter je resetovan.";
    }

    private async Task PostaviArhivuAsync(string arhivaVrednost, string opisAkcije)
    {
        var stavkeZaIzmenu = Stavke.ToList();
        if (stavkeZaIzmenu.Count == 0)
        {
            Poruka = "Nema stavki za izmenu arhive.";
            return;
        }

        try
        {
            Ucitava = true;

            if (!string.IsNullOrWhiteSpace(_ldArhivaPath))
            {
                var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(_ldArhivaPath));
                var cilj = NapraviMultiskup(stavkeZaIzmenu.Select(NapraviKljuc));
                var izmenjeno = 0;
                var vrednost = (arhivaVrednost ?? string.Empty).Trim();

                foreach (var row in zapisi)
                {
                    if (!PotrosiKljuc(cilj, NapraviKljuc(row)))
                        continue;

                    var postojece = Str(row, "ARHIVA");
                    if (string.Equals(postojece, vrednost, StringComparison.Ordinal))
                        continue;

                    row["ARHIVA"] = vrednost;
                    izmenjeno++;
                }

                if (izmenjeno == 0)
                {
                    Poruka = "Nema promena za izabrane stavke.";
                    return;
                }

                UpisiTabelu(_ldArhivaPath, zapisi);
                await UcitajAsync();
                Poruka = $"{opisAkcije} arhiva za {izmenjeno} stavki.";
                return;
            }

            var folder = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
            var rezultat = await LdDbfMutator.PostaviArhivuPoStavkamaAsync(folder, stavkeZaIzmenu, arhivaVrednost);
            if (rezultat.Izmenjeno == 0)
            {
                Poruka = "Nema promena za izabrane stavke.";
                return;
            }

            await UcitajAsync();
            Poruka = $"{opisAkcije} arhiva za {rezultat.Izmenjeno} stavki (fajlova: {rezultat.Fajlovi}).";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri izmeni arhive: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private void PrimeniFilter()
    {
        IEnumerable<LdObracunStavka> q = _sveStavke;
        if (Mesec > 0) q = q.Where(x => x.Mesec == Mesec);
        if (Isplata > 0) q = q.Where(x => x.Isplata == Isplata);
        if (!string.IsNullOrWhiteSpace(Godina)) q = q.Where(x => StringEquals(x.Godina, Godina));
        if (!string.IsNullOrWhiteSpace(Vrsta)) q = q.Where(x => StringEquals(x.Vrsta, Vrsta));
        if (SamoArhivirane) q = q.Where(x => StringEquals(x.Arhiva, "*"));

        var lista = q.OrderBy(x => x.Broj).ThenBy(x => x.ImePrez).ThenBy(x => x.Mesec).ToList();
        Stavke = new ObservableCollection<LdObracunStavka>(lista);
        Selektovana = Stavke.FirstOrDefault();
    }

    private static int DodajIzPpodp(List<Dictionary<string, object?>> cilj, IEnumerable<Dictionary<string, object?>> izvor, string vrsta, bool ubaciPorez)
    {
        var dodato = 0;
        foreach (var s in izvor)
        {
            var nova = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["BROJ"] = BrojDec(s, "BROJ"),
                ["BRUTO"] = Dec(s, "OSNOVICA"),
                ["DOPPR"] = Dec(s, "DOPPIO"),
                ["DOPZR"] = Dec(s, "DOPZDR"),
                ["DOPNR"] = Dec(s, "DOPNEZ"),
                ["MESEC"] = BrojDec(s, "MESEC"),
                ["VRSTA"] = vrsta,
                ["NAZMES"] = Str(s, "NAZMES"),
                ["GODINA"] = (Dat(s, "TDAT1")?.Year ?? Int(s, "GODINA")).ToString(CultureInfo.InvariantCulture),
                ["ARHIVA"] = " "
            };

            if (ubaciPorez)
                nova["POREZ"] = Dec(s, "PORDOH");

            cilj.Add(nova);
            dodato++;
        }

        return dodato;
    }

    private static int PostaviAkoDrugacije(Dictionary<string, object?> row, string field, string novaVrednost)
    {
        if (string.IsNullOrWhiteSpace(novaVrednost))
            return 0;

        var postojece = Str(row, field);
        if (StringEquals(postojece, novaVrednost))
            return 0;

        row[field] = novaVrednost;
        return 1;
    }

    [RelayCommand]
    private async Task VratiIzArhiveAsync()
    {
        if (string.IsNullOrWhiteSpace(_ldArhivaPath))
        {
            Poruka = "Nije pronađen ldarhiva.dbf.";
            return;
        }

        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var mesecZaVracanje = Mesec > 0 ? Mesec : Selektovana?.Mesec ?? 0;
        if (mesecZaVracanje <= 0)
        {
            Poruka = "Unesite mesec (ili izaberite stavku) za vraćanje.";
            return;
        }

        var vrsta = NormalizujVrstu(Vrsta, Selektovana?.Vrsta);
        var godina = NormalizujGodinu(Godina, Selektovana?.Godina);
        var isplata = Isplata > 0 ? Isplata : (Selektovana?.Isplata ?? 0);
        if (isplata <= 0 && !string.IsNullOrWhiteSpace(vrsta))
            isplata = MapVrstaUIsplatu(vrsta);

        var targetFileName = isplata switch
        {
            2 => "ldp.dbf",
            3 => "ldb.dbf",
            4 => "ldi.dbf",
            5 => "ldr.dbf",
            _ => "ld.dbf"
        };

        var targetPath = LdBolovanjeDbfSupport.PronadjiDbf(folder, targetFileName)
                      ?? LdBolovanjeDbfSupport.PronadjiDbf(folder, "ld0.dbf")
                      ?? LdBolovanjeDbfSupport.PronadjiDbf(folder, "ld.dbf");

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            Poruka = $"Nije pronađen ciljni fajl '{targetFileName}' (ni ld0.dbf ni ld.dbf) u folderu firme.";
            return;
        }

        var arhivaZapisi = await Task.Run(() => DbfReader.CitajSveZapise(_ldArhivaPath));
        var zaVracanje = arhivaZapisi.Where(r =>
        {
            if (Int(r, "MESEC") != mesecZaVracanje) return false;
            if (!string.IsNullOrWhiteSpace(godina) && !StringEquals(Str(r, "GODINA"), godina)) return false;
            if (!string.IsNullOrWhiteSpace(vrsta) && !StringEquals(Str(r, "VRSTA"), vrsta)) return false;
            if (isplata > 0 && Int(r, "ISPLATA") != isplata) return false;
            return true;
        }).ToList();

        if (zaVracanje.Count == 0)
        {
            Poruka = "Nema stavki u arhivi za zadati uslov.";
            return;
        }

        var opis = $"mesec {mesecZaVracanje}"
            + (string.IsNullOrWhiteSpace(godina) ? string.Empty : $", god. {godina}")
            + (string.IsNullOrWhiteSpace(vrsta) ? string.Empty : $", vrsta {vrsta}")
            + (isplata > 0 ? $", isplata {isplata}" : string.Empty);

        var upozorenje = $"Vraćanje {zaVracanje.Count} zapisa ({opis}) iz arhive u '{Path.GetFileName(targetPath)}'.\n\n"
                       + "PAŽNJA: Postojeći zapisi za isti period u ciljnom fajlu biće zamenjeni arhiviranim podacima.\n\n"
                       + "Da li želite da nastavite?";

        if (MessageBox.Show(upozorenje, "VRATI IZ ARHIVE", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            Ucitava = true;

            var targetZapisi = await Task.Run(() => DbfReader.CitajSveZapise(targetPath));

            var preostali = targetZapisi.Where(r =>
            {
                if (Int(r, "MESEC") != mesecZaVracanje) return true;
                if (!string.IsNullOrWhiteSpace(godina) && !StringEquals(Str(r, "GODINA"), godina)) return true;
                if (isplata > 0 && Int(r, "ISPLATA") != isplata) return true;
                return false;
            }).ToList();

            var vratiZapise = zaVracanje.Select(r =>
            {
                var novi = new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase);
                novi["ARHIVA"] = " ";
                if (novi.ContainsKey("ARHIVA2")) novi["ARHIVA2"] = " ";
                return novi;
            }).ToList();

            var merged = preostali.Concat(vratiZapise).ToList();
            UpisiTabelu(targetPath, merged);

            await UcitajAsync();
            Poruka = $"Vraćeno {zaVracanje.Count} zapisa ({opis}) u '{Path.GetFileName(targetPath)}'.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri vraćanju iz arhive: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private static void UpisiTabelu(string putanja, IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var schema = DbfTableWriter.LoadSchema(putanja);
        DbfTableWriter.WriteTable(
            putanja,
            schema,
            rows,
            static (r, fieldName) => r.TryGetValue(fieldName, out var v) ? v : null);
    }

    private static LdObracunStavka MapirajArhivaZapis(Dictionary<string, object?> z)
    {
        var vrsta = Str(z, "VRSTA");
        return new LdObracunStavka
        {
            Broj = Int(z, "BROJ"),
            Sifraprih = Str(z, "SIFRAPRIH"),
            ImePrez = Str(z, "IME_PREZ"),
            Evidbroj = Str(z, "EVIDBROJ"),
            Maticnibr = Str(z, "MATICNIBR"),
            Idbroj = Str(z, "IDBROJ"),
            Dok = Str(z, "DOK"),
            Grupa = Int(z, "GRUPA"),
            Grupa1 = Int(z, "GRUPA1"),
            Mtr = Int(z, "MTR"),
            Mesec = Int(z, "MESEC"),
            Isplata = Int(z, "ISPLATA") > 0 ? Int(z, "ISPLATA") : MapVrstaUIsplatu(vrsta),
            Nazmes = Str(z, "NAZMES"),
            Godina = Str(z, "GODINA"),
            Datum = Dat(z, "DATUM") ?? Dat(z, "DAT1") ?? Dat(z, "DAT2") ?? Dat(z, "DAT3") ?? Dat(z, "DAT4"),
            Vrsta = vrsta,
            Casuk = Dec(z, "CASUK"),
            Din1 = Dec(z, "DIN1"),
            Din2 = Dec(z, "DIN2"),
            Din3 = Dec(z, "DIN3"),
            Dinuk = Dec(z, "DINUK"),
            Dinuc = Dec(z, "DINUC"),
            Dinnoc = Dec(z, "DINNOC"),
            Dinprod = Dec(z, "DINPROD"),
            Dinned = Dec(z, "DINNED"),
            Dinradnap = Dec(z, "DINRADNAP"),
            Dinbol = Dec(z, "DINBOL"),
            Dinbol2 = Dec(z, "DINBOL2"),
            Bruto = Dec(z, "BRUTO"),
            Porez = Dec(z, "POREZ"),
            Neto = Dec(z, "NETO"),
            Neto2 = Dec(z, "NETO2"),
            Prevoz = Dec(z, "PREVOZ"),
            Akontac = Dec(z, "AKONTAC"),
            Dopsocr = Dec(z, "DOPSOCR"),
            Dopsocf = Dec(z, "DOPSOCF"),
            Doppr = Dec(z, "DOPPR"),
            Dopzr = Dec(z, "DOPZR"),
            Dopnr = Dec(z, "DOPNR"),
            Doppf = Dec(z, "DOPPF"),
            Dopzf = Dec(z, "DOPZF"),
            Dopnf = Dec(z, "DOPNF"),
            Naknade = Dec(z, "NAKNADE"),
            Ukobust = Dec(z, "UKOBUST"),
            Zaisplatu = Dec(z, "ZAISPLATU"),
            Solpor = Dec(z, "SOLPOR"),
            Arhiva = Str(z, "ARHIVA"),
            Arhiva2 = Str(z, "ARHIVA2"),
            Idbr = Long(z, "IDBR")
        };
    }

    private static string NormalizujVrstu(string? primarna, string? fallback)
    {
        var vrsta = (primarna ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(vrsta))
            vrsta = (fallback ?? string.Empty).Trim().ToUpperInvariant();
        return vrsta;
    }

    private static string NormalizujGodinu(string? primarna, string? fallback)
    {
        var godina = (primarna ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(godina))
            godina = (fallback ?? string.Empty).Trim();
        return godina;
    }

    private void OtvoriFoxTabelu(
        string naslov,
        string podnaslov,
        IReadOnlyCollection<PregledTabelaStavka> redovi,
        string label1,
        string label2)
    {
        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za trazeni pregled.";
            return;
        }

        var view = new FoxPregledTabelaView(naslov, podnaslov, redovi, label1, label2);
        PostaviOwnerIPrikazi(view);
    }

    private static void PostaviOwnerIPrikazi(Window view)
    {
        var owner = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (owner != null && owner != view)
            view.Owner = owner;
        view.ShowDialog();
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

    private void PostaviPodrazumevaniPeriod()
    {
        Mesec = DateTime.Now.Month;
        Isplata = 0;
        Godina = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
        Vrsta = string.Empty;

        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder))
            return;

        var dbfPath = LdObracunDbfReader.PronadjiDbf(folder, "ldparam.dbf");
        if (dbfPath == null)
            return;

        try
        {
            var red = DbfReader.CitajSveZapise(dbfPath).FirstOrDefault();
            if (red == null)
                return;

            var m = Int(red, "MESEC");
            var i = Int(red, "ISPLATA");
            var g = Str(red, "GODINA");
            if (m > 0) Mesec = m;
            if (i > 0) Isplata = i;
            if (!string.IsNullOrWhiteSpace(g)) Godina = g;
        }
        catch
        {
            // Nije kriticno; ostaje DateTime podrazumevana vrednost.
        }
    }

    private static int MapVrstaUIsplatu(string? vrstaRaw)
    {
        var vrsta = (vrstaRaw ?? string.Empty).Trim().ToUpperInvariant();
        return vrsta switch
        {
            "A" => 1,
            "P" => 2,
            "B" => 3,
            "I" => 4,
            "R" => 5,
            "U" => 6,
            _ => 0
        };
    }

    private static string NapraviKljuc(LdObracunStavka s)
    {
        if (s.Idbr != 0)
            return $"IDBR:{s.Idbr}";

        return string.Join("|",
            s.Broj,
            s.Mesec,
            PraznoTekst(s.Godina).ToUpperInvariant(),
            s.Isplata,
            PraznoTekst(s.Vrsta).ToUpperInvariant(),
            PraznoTekst(s.Maticnibr).ToUpperInvariant(),
            PraznoTekst(s.Idbroj).ToUpperInvariant(),
            PraznoTekst(s.ImePrez).ToUpperInvariant());
    }

    private static string NapraviKljuc(Dictionary<string, object?> z)
    {
        var idbr = Long(z, "IDBR");
        if (idbr != 0)
            return $"IDBR:{idbr}";

        return string.Join("|",
            Int(z, "BROJ"),
            Int(z, "MESEC"),
            PraznoTekst(Str(z, "GODINA")).ToUpperInvariant(),
            Int(z, "ISPLATA"),
            PraznoTekst(Str(z, "VRSTA")).ToUpperInvariant(),
            PraznoTekst(Str(z, "MATICNIBR")).ToUpperInvariant(),
            PraznoTekst(Str(z, "IDBROJ")).ToUpperInvariant(),
            PraznoTekst(Str(z, "IME_PREZ")).ToUpperInvariant());
    }

    private static Dictionary<string, int> NapraviMultiskup(IEnumerable<string> kljucevi)
    {
        var mapa = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kljuc in kljucevi.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            if (mapa.TryGetValue(kljuc, out var broj))
                mapa[kljuc] = broj + 1;
            else
                mapa[kljuc] = 1;
        }

        return mapa;
    }

    private static bool PotrosiKljuc(Dictionary<string, int> multiskup, string kljuc)
    {
        if (string.IsNullOrWhiteSpace(kljuc))
            return false;

        if (!multiskup.TryGetValue(kljuc, out var broj) || broj <= 0)
            return false;

        if (broj == 1)
            multiskup.Remove(kljuc);
        else
            multiskup[kljuc] = broj - 1;

        return true;
    }

    private static PregledTabelaStavka Red(string sifra, string naziv, decimal iznos1, decimal iznos2)
        => new() { Sifra = sifra, Naziv = naziv, Iznos1 = iznos1, Iznos2 = iznos2 };

    private static string PraznoTekst(string? tekst)
        => string.IsNullOrWhiteSpace(tekst) ? "-" : tekst.Trim();

    private static bool StringEquals(string? a, string? b)
        => string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);

    private static string Str(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v != null ? (v.ToString() ?? string.Empty).Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0;
        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        if (v is long l) return (int)l;
        return int.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var p)
            ? p
            : int.TryParse(v.ToString(), out p) ? p : 0;
    }

    private static long Long(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0L;
        if (v is decimal d) return (long)d;
        if (v is long l) return l;
        return long.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var p)
            ? p
            : long.TryParse(v.ToString(), out p) ? p : 0L;
    }

    private static decimal Dec(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0m;
        if (v is decimal d) return d;
        if (v is int i) return i;
        if (v is long l) return l;
        return decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var p)
            ? p
            : decimal.TryParse(v.ToString(), out p) ? p : 0m;
    }

    private static decimal BrojDec(Dictionary<string, object?> z, string k)
    {
        var v = Dec(z, k);
        return v == 0m ? 0m : v;
    }

    private static DateTime? Dat(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null)
            return null;

        return v switch
        {
            DateTime dt => dt,
            string s when DateTime.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
