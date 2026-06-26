using Algoritam.Infrastructure.Dbf;
using Algoritam.WPF.Controls;
using Algoritam.WPF.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LdParametar = Algoritam.Domain.Entities.LdParametar;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// FoxPro forma LDKRED - evidencija kredita.
/// Lokalno su prenete funkcije koje pripadaju samom modulu kredita:
/// navigacija, unos/izmena, pretrage, izbor radnika/partnera, otplate i jedan kredit.
/// </summary>
public partial class KreditiViewModel : ObservableObject
{
    private readonly string _folderPath;
    private IReadOnlyDictionary<int, KreditRadnikInfo> _radnici = new Dictionary<int, KreditRadnikInfo>();
    private Dictionary<string, KreditPartnerInfo> _partneri = new(StringComparer.OrdinalIgnoreCase);
    private readonly LdParametar? _parametar;

    private string? _ldkredDbfPath;
    private readonly List<RecordLockHandle> _aktivniLockovi = [];
    private int _sortMode;
    private bool _izvestajAkontacija;

    [ObservableProperty] private ObservableCollection<KreditStavka> _stavke = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ObrisiCommand))]
    private KreditStavka? _selektovana;
    [ObservableProperty] private string _naslov = "EVIDENCIJA KREDITA";
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private string _pretraga = string.Empty;
    [ObservableProperty] private string _radnikTekst = "Radnik: -";
    [ObservableProperty] private string _partnerTekst = "Partner: -";
    [ObservableProperty] private bool _ucitava;
    [ObservableProperty] private string _busyPoruka = "Učitavanje...";
    [ObservableProperty] private string _filterTekst = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListaPrazan))]
    private int _ukupnoStavki;

    public bool IsListaPrazan => UkupnoStavki == 0;
    public ICollectionView StavkeView { get; private set; }

    public KreditiViewModel(string folderPath, LdParametar? parametar = null)
    {
        _folderPath = folderPath;
        _parametar = parametar;
        StavkeView = CollectionViewSource.GetDefaultView(Stavke);

        if (_parametar != null)
        {
            _izvestajAkontacija = JeAkontacionaIsplata(_parametar.Isplata);
            Naslov = $"EVIDENCIJA KREDITA | Mesec {_parametar.Mesec}/{_parametar.Godina} | Isplata {_parametar.Isplata}";
        }
    }

    public async Task InitAsync()
    {
        Ucitava = true;
        BusyPoruka = "Učitavanje evidencije kredita...";
        try
        {
            var (krediti, radnici, partneri) = await Task.Run(() =>
            {
                var k = KreditiDbfSupport.UcitajKredite(_folderPath);
                var r = KreditiDbfSupport.UcitajRadnike(_folderPath);
                var p = new Dictionary<string, KreditPartnerInfo>(
                    KreditiDbfSupport.UcitajPartnere(_folderPath),
                    StringComparer.OrdinalIgnoreCase);
                return (k, r, p);
            });

            _radnici = radnici;
            _partneri = partneri;
            Stavke = new ObservableCollection<KreditStavka>(krediti);
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Učitano {Stavke.Count} kredita.";
        }
        catch (Exception ex)
        {
            Stavke = [];
            Poruka = $"Greška pri učitavanju: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    partial void OnStavkeChanged(ObservableCollection<KreditStavka> value)
    {
        StavkeView = CollectionViewSource.GetDefaultView(value);
        StavkeView.Filter = null;
        UkupnoStavki = value.Count;
        OnPropertyChanged(nameof(StavkeView));
    }

    partial void OnFilterTekstChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            StavkeView.Filter = null;
        }
        else
        {
            var upit = value.Trim().ToLowerInvariant();
            StavkeView.Filter = obj => obj is KreditStavka s &&
                (s.Broj.ToString().Contains(upit) ||
                 (s.Partija ?? "").ToLowerInvariant().Contains(upit) ||
                 (s.EvidBroj ?? "").ToLowerInvariant().Contains(upit) ||
                 (s.Sifra ?? "").ToLowerInvariant().Contains(upit));
        }
        StavkeView.Refresh();
    }

    [RelayCommand]
    private void OcistiFilter() => FilterTekst = "";

    partial void OnSelektovanaChanged(KreditStavka? value)
    {
        if (value == null)
        {
            RadnikTekst = "Radnik: -";
            PartnerTekst = "Partner: -";
            return;
        }

        if (_radnici.TryGetValue(value.Broj, out var radnik))
        {
            var maticni = string.IsNullOrWhiteSpace(radnik.MaticniBr) ? string.Empty : $"  |  JMBG: {radnik.MaticniBr}";
            RadnikTekst = $"Radnik: {radnik.ImePrez}  |  evid. {radnik.EvidBroj}{maticni}";
        }
        else
        {
            RadnikTekst = value.Broj > 0 ? $"Radnik broj: {value.Broj}" : "Radnik: -";
        }

        if (!string.IsNullOrWhiteSpace(value.Sifra) && _partneri.TryGetValue(value.Sifra, out var partner))
        {
            var racun = string.IsNullOrWhiteSpace(partner.ZiroRac) ? partner.Mesto : partner.ZiroRac;
            PartnerTekst = $"Partner: {partner.Sifra}  {partner.Naziv}  |  {racun}";
        }
        else
        {
            PartnerTekst = string.IsNullOrWhiteSpace(value.Sifra)
                ? "Partner: -"
                : $"Partner sifra: {value.Sifra}";
        }
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count == 0)
            return;

        Selektovana = Stavke[0];
    }

    [RelayCommand]
    private void Prethodni()
    {
        if (Selektovana == null || Stavke.Count == 0)
            return;

        var index = Stavke.IndexOf(Selektovana);
        if (index > 0)
            Selektovana = Stavke[index - 1];
    }

    [RelayCommand]
    private void Sledeci()
    {
        if (Selektovana == null || Stavke.Count == 0)
            return;

        var index = Stavke.IndexOf(Selektovana);
        if (index >= 0 && index < Stavke.Count - 1)
            Selektovana = Stavke[index + 1];
    }

    [RelayCommand]
    private void Poslednji()
    {
        if (Stavke.Count == 0)
            return;

        Selektovana = Stavke[^1];
    }

    [RelayCommand]
    private void Dodaj()
    {
        _ldkredDbfPath ??= KreditiDbfSupport.PronadjiDbf(_folderPath, "ldkred.dbf");

        var noviKredit = SledeciKredit();
        if (!string.IsNullOrWhiteSpace(_ldkredDbfPath))
        {
            const int maxPokusaja = 100;
            for (var pokusaj = 0; pokusaj < maxPokusaja; pokusaj++)
            {
                if (DbfOptimisticConcurrency.TryAcquireRecordLock(
                        _ldkredDbfPath,
                        $"KREDIT:{noviKredit}",
                        Environment.UserName,
                        out var lockHandle,
                        out _))
                {
                    if (lockHandle != null)
                        _aktivniLockovi.Add(lockHandle);
                    break;
                }

                noviKredit++;
                if (pokusaj == maxPokusaja - 1)
                {
                    Poruka = "Nije moguće dodati kredit — previše istovremenih korisnika.";
                    return;
                }
            }
        }

        var nova = new KreditStavka
        {
            Kredit = noviKredit,
            DatDok = DateTime.Today,
            ZaObaviti = "*",
            Arhiva = " ",
            Arhiva2 = " ",
            Preneto = " ",
            Numred = Stavke.Count + 1
        };

        Stavke.Add(nova);
        Selektovana = nova;
        Poruka = $"Dodat je kredit {nova.Kredit}.";
    }

    private bool MozeObrisi() => Selektovana != null;

    [RelayCommand(CanExecute = nameof(MozeObrisi))]
    private void Obrisi()
    {
        if (Selektovana == null)
            return;

        var zaBrisanje = Selektovana;
        Stavke.Remove(zaBrisanje);
        Selektovana = Stavke.FirstOrDefault();
        Poruka = $"Obrisan je kredit {zaBrisanje.Kredit}. Kliknite SAČUVAJ.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        try
        {
            Renumerisi();

            // Merge: zadrži zapise sa diska koje ovaj korisnik nema (drugi računar ih dodao)
            var mojiBrojevi = Stavke.Select(x => x.Kredit).ToHashSet();
            var diskZapisi = KreditiDbfSupport.UcitajKredite(_folderPath);
            var tudiZapisi = diskZapisi.Where(x => !mojiBrojevi.Contains(x.Kredit)).ToList();

            var spojeno = Stavke.Concat(tudiZapisi)
                .OrderBy(x => x.Kredit)
                .ThenBy(x => x.Numred)
                .ToList();

            KreditiDbfSupport.SacuvajKredite(_folderPath, spojeno);
            OslobodiLokove();
            UcitajPodatke();
            Poruka = $"Sačuvano. Ukupno {Stavke.Count} kredita u ldkred.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri cuvanju kredita: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SrediKredite()
    {
        try
        {
            var otplate = KreditiDbfSupport.UcitajSveOtplate(_folderPath);
            KreditiDbfSupport.SrediKredite(Stavke, otplate, _radnici);
            KreditiDbfSupport.SacuvajOtplate(_folderPath, otplate);
            Renumerisi();
            KreditiDbfSupport.SacuvajKredite(_folderPath, Stavke);
            UcitajPodatke();
            Poruka = "Stanja kredita su uskladjena sa otplatama.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri sredjivanju kredita: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Sortiraj()
    {
        _sortMode = (_sortMode + 1) % 4;

        var sortirano = _sortMode switch
        {
            1 => Stavke.OrderBy(x => x.Kredit).ThenBy(x => x.Broj).ToList(),
            2 => Stavke.OrderBy(x => x.Broj).ThenBy(x => x.Kredit).ToList(),
            3 => Stavke.OrderBy(x => x.Sifra).ThenBy(x => x.Kredit).ToList(),
            _ => Stavke.OrderBy(x => x.Numred).ToList()
        };

        Rebind(sortirano);
        Poruka = _sortMode switch
        {
            1 => "Sortiranje: KREDIT",
            2 => "Sortiranje: BROJ",
            3 => "Sortiranje: SIFRA",
            _ => "Sortiranje: izvorni redosled"
        };
    }

    [RelayCommand]
    private void TraziKredit()
    {
        if (string.IsNullOrWhiteSpace(Pretraga))
        {
            Poruka = "Unesite broj kredita za pretragu.";
            return;
        }

        var trazeni = ParseInt(Pretraga);
        var nadjen = Stavke.FirstOrDefault(x => x.Kredit == trazeni);
        PostaviNadjenuStavku(nadjen, $"Pronađen kredit {trazeni}.", "Kredit nije pronađen.");
    }

    [RelayCommand]
    private void TraziSifru()
    {
        if (string.IsNullOrWhiteSpace(Pretraga))
        {
            Poruka = "Unesite sifru radnika (BROJ) za pretragu.";
            return;
        }

        var broj = ParseInt(Pretraga);
        var nadjen = Stavke.FirstOrDefault(x => x.Broj == broj);
        PostaviNadjenuStavku(nadjen, $"Pronađena sifra radnika {broj}.", "Sifra radnika nije pronađena.");
    }

    [RelayCommand]
    private void TraziEvidBroj()
    {
        if (string.IsNullOrWhiteSpace(Pretraga))
        {
            Poruka = "Unesite evidencioni broj za pretragu.";
            return;
        }

        var upit = Pretraga.Trim();
        var nadjen = Stavke.FirstOrDefault(x =>
            x.EvidBroj.Equals(upit, StringComparison.OrdinalIgnoreCase) ||
            (_radnici.TryGetValue(x.Broj, out var radnik) &&
             radnik.EvidBroj.Equals(upit, StringComparison.OrdinalIgnoreCase)));

        PostaviNadjenuStavku(nadjen, $"Pronađen evid. broj {upit}.", "Evidencioni broj nije pronađen.");
    }

    [RelayCommand]
    private void TraziPartnera()
    {
        if (string.IsNullOrWhiteSpace(Pretraga))
        {
            Poruka = "Unesite partnera ili sifru partnera za pretragu.";
            return;
        }

        var upit = Pretraga.Trim();
        var nadjen = Stavke.FirstOrDefault(x =>
            x.Sifra.Equals(upit, StringComparison.OrdinalIgnoreCase) ||
            (_partneri.TryGetValue(x.Sifra, out var partner) &&
             partner.Naziv.Contains(upit, StringComparison.OrdinalIgnoreCase)));

        PostaviNadjenuStavku(nadjen, $"Pronađen partner {upit}.", "Partner nije pronađen.");
    }

    [RelayCommand]
    private void IzaberiRadnika()
    {
        if (Selektovana == null)
        {
            Poruka = "Prvo izaberite kredit.";
            return;
        }

        var vm = new KreditLookupViewModel(
            "Izbor radnika",
            _radnici.Values
                .OrderBy(x => x.Broj)
                .Select(x => new KreditLookupItem(x.Broj.ToString(CultureInfo.InvariantCulture), x.ImePrez, x.EvidBroj))
                .ToList());

        var view = new Views.Zarade.KreditLookupView { DataContext = vm };
        if (view.ShowDialog() != true || vm.Izabrana == null)
            return;

        Selektovana.Broj = ParseInt(vm.Izabrana.Sifra);
        if (_radnici.TryGetValue(Selektovana.Broj, out var radnik))
        {
            Selektovana.EvidBroj = radnik.EvidBroj;
            Selektovana.Grupa = radnik.Grupa;
        }

        OnSelektovanaChanged(Selektovana);
        Poruka = $"Dodeljen radnik {Selektovana.Broj}.";
    }

    [RelayCommand]
    private void IzaberiPartnera()
    {
        if (Selektovana == null)
        {
            Poruka = "Prvo izaberite kredit.";
            return;
        }

        var vm = new KreditLookupViewModel(
            "Izbor partnera",
            _partneri.Values
                .OrderBy(x => x.Sifra)
                .Select(x => new KreditLookupItem(
                    x.Sifra,
                    x.Naziv,
                    string.IsNullOrWhiteSpace(x.ZiroRac) ? x.Mesto : x.ZiroRac))
                .ToList(),
            DodajPartnerIzLookupa);

        var view = new Views.Zarade.KreditLookupView { DataContext = vm };
        if (view.ShowDialog() != true || vm.Izabrana == null)
            return;

        Selektovana.Sifra = vm.Izabrana.Sifra;
        OnSelektovanaChanged(Selektovana);
        Poruka = $"Dodeljen partner {Selektovana.Sifra}.";
    }

    [RelayCommand]
    private void JedanKredit()
    {
        if (Selektovana == null)
        {
            Poruka = "Prvo izaberite kredit.";
            return;
        }

        var vm = new KreditiDetaljViewModel(Selektovana.Clone(), RadnikTekst, PartnerTekst);
        var view = new Views.Zarade.KreditiDetaljView { DataContext = vm };
        if (view.ShowDialog() != true)
            return;

        Selektovana.CopyFrom(vm.Stavka);
        OnSelektovanaChanged(Selektovana);
        Poruka = $"Azuriran je kredit {Selektovana.Kredit}.";
    }

    [RelayCommand]
    private void PregledOtplata()
    {
        if (Selektovana == null)
        {
            Poruka = "Prvo izaberite kredit.";
            return;
        }

        var vm = new KreditiOtplataViewModel(_folderPath, Selektovana.Clone());
        var view = new Views.Zarade.KreditiOtplataView { DataContext = vm };
        if (view.ShowDialog() != true || !vm.Sačuvano)
            return;

        SrediKredite();
    }

    [RelayCommand]
    private void AktivniKrediti()
    {
        var aktivni = Stavke
            .Where(x => JeAktivanKredit(x))
            .OrderBy(x => x.Kredit)
            .ThenBy(x => x.Broj)
            .Select(x => new PregledTabelaStavka
            {
                Sifra = x.Kredit.ToString(CultureInfo.InvariantCulture),
                Naziv = $"{NazivRadnika(x.Broj)} | {NazivPartnera(x.Sifra)}",
                Iznos1 = x.AktivnaRata,
                Iznos2 = x.Ostatak
            })
            .ToList();

        if (aktivni.Count == 0)
        {
            Poruka = "Nema aktivnih kredita.";
            return;
        }

        OtvoriPregled("AKTIVNI KREDITI", "Pregled aktivnih kredita", aktivni, "AKTIVNA RATA", "OSTATAK");
    }

    [RelayCommand]
    private async Task Osvezi()
    {
        OslobodiLokove();
        await InitAsync();
    }

    public void OslobodiLokove()
    {
        foreach (var lok in _aktivniLockovi)
            try { lok.Dispose(); } catch { }
        _aktivniLockovi.Clear();
    }

    [RelayCommand]
    private void PostaviKonacno()
    {
        _izvestajAkontacija = false;
        Poruka = "Izvestaji su prebaceni na KONACNO (AKTIVRATA).";
    }

    [RelayCommand]
    private void PostaviAkontaciju()
    {
        _izvestajAkontacija = true;
        Poruka = "Izvestaji su prebaceni na AKONTACIJU (AKONTRATA).";
    }

    [RelayCommand]
    private void RasknjizavanjePlate()
    {
        IzvrsiRasknjizavanje(zaAkontaciju: false);
    }

    [RelayCommand]
    private void RasknjizavanjeAkontacije()
    {
        IzvrsiRasknjizavanje(zaAkontaciju: true);
    }

    [RelayCommand]
    private void SpisakJedneFirme()
    {
        if (Selektovana == null || string.IsNullOrWhiteSpace(Selektovana.Sifra))
        {
            Poruka = "Izaberite kredit sa sifrom partnera.";
            return;
        }

        var sifra = Selektovana.Sifra.Trim();
        var stavke = AktivneStavkeZaIzvestaj()
            .Where(x => x.Sifra.Equals(sifra, StringComparison.OrdinalIgnoreCase))
            .Select(x => new PregledTabelaStavka
            {
                Sifra = x.Broj.ToString(CultureInfo.InvariantCulture),
                Naziv = $"{NazivRadnika(x.Broj)} | kredit {x.Kredit}",
                Iznos1 = TrenutnaRata(x),
                Iznos2 = x.Ostatak
            })
            .OrderBy(x => x.Sifra)
            .ToList();

        if (stavke.Count == 0)
        {
            Poruka = "Nema stavki za izabranu firmu.";
            return;
        }

        OtvoriPregled("SPISAK JEDNE FIRME", $"Partner {sifra} | {NazivPartnera(sifra)} | {ModIzvestajaTekst()}", stavke, "RATA", "OSTATAK");
    }

    [RelayCommand]
    private void SaldoFirmi()
    {
        var stavke = AktivneStavkeZaIzvestaj()
            .GroupBy(x => x.Sifra, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = g.Key,
                Naziv = NazivPartnera(g.Key),
                Iznos1 = g.Sum(x => TrenutnaRata(x)),
                Iznos2 = g.Sum(x => x.Ostatak)
            })
            .OrderBy(x => x.Sifra)
            .ToList();

        if (stavke.Count == 0)
        {
            Poruka = "Nema podataka za SALDO FIRMI.";
            return;
        }

        OtvoriPregled("SALDO FIRMI", ModIzvestajaTekst(), stavke, "AKTIVNA RATA", "SALDO");
    }

    [RelayCommand]
    private void SaldoRadnika()
    {
        var stavke = AktivneStavkeZaIzvestaj()
            .GroupBy(x => x.Broj)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = g.Key.ToString(CultureInfo.InvariantCulture),
                Naziv = NazivRadnika(g.Key),
                Iznos1 = g.Sum(x => TrenutnaRata(x)),
                Iznos2 = g.Sum(x => x.Ostatak)
            })
            .OrderBy(x => x.Sifra)
            .ToList();

        if (stavke.Count == 0)
        {
            Poruka = "Nema podataka za SALDO RADNIKA.";
            return;
        }

        OtvoriPregled("SALDO RADNIKA", ModIzvestajaTekst(), stavke, "AKTIVNA RATA", "SALDO");
    }

    [RelayCommand]
    private void SveFirme()
    {
        var stavke = AktivneStavkeZaIzvestaj()
            .OrderBy(x => x.Sifra)
            .ThenBy(x => x.Broj)
            .ThenBy(x => x.Kredit)
            .Select(x => new PregledTabelaStavka
            {
                Sifra = x.Sifra,
                Naziv = $"{NazivPartnera(x.Sifra)} | {NazivRadnika(x.Broj)} | kredit {x.Kredit}",
                Iznos1 = TrenutnaRata(x),
                Iznos2 = x.Ostatak
            })
            .ToList();

        if (stavke.Count == 0)
        {
            Poruka = "Nema podataka za SVE FIRME.";
            return;
        }

        OtvoriPregled("SVE FIRME", ModIzvestajaTekst(), stavke, "AKTIVNA RATA", "SALDO");
    }

    [RelayCommand]
    private void ListiciRadnici()
    {
        var aktivne = AktivneStavkeZaIzvestaj();

        var mojaGrupa = Selektovana != null ? GetGrupaStavke(Selektovana) : 0;
        if (mojaGrupa > 0)
            aktivne = aktivne.Where(x => GetGrupaStavke(x) == mojaGrupa).ToList();

        var stavke = aktivne
            .GroupBy(x => x.Broj)
            .Select(g => new PregledTabelaStavka
            {
                Sifra = g.Key.ToString(CultureInfo.InvariantCulture),
                Naziv = NazivRadnika(g.Key),
                Iznos1 = g.Sum(x => x.Ostatak),
                Iznos2 = g.Sum(x => Math.Max(0m, x.Ostatak - TrenutnaRata(x)))
            })
            .OrderBy(x => x.Sifra)
            .ToList();

        if (stavke.Count == 0)
        {
            Poruka = mojaGrupa > 0
                ? $"Nema aktivnih kredita za grupu {mojaGrupa}."
                : "Nema podataka za LISTICE RADNICI.";
            return;
        }

        var podnaslov = mojaGrupa > 0
            ? $"{ModIzvestajaTekst()} | Grupa {mojaGrupa} (pre obracuna)"
            : $"{ModIzvestajaTekst()} (pre obracuna)";

        OtvoriPregled("LISTICI RADNICI", podnaslov, stavke, "DUG", "DUG POSLE RATE");
    }

    private int GetGrupaStavke(KreditStavka stavka)
    {
        if (stavka.Grupa > 0) return stavka.Grupa;
        if (stavka.Broj > 0 && _radnici.TryGetValue(stavka.Broj, out var r) && r.Grupa > 0)
            return r.Grupa;
        return 0;
    }

    [RelayCommand]
    private void ListicRadnik()
    {
        if (Selektovana == null)
        {
            Poruka = "Izaberite kredit/radnika.";
            return;
        }

        var broj = Selektovana.Broj;
        var stavke = AktivneStavkeZaIzvestaj()
            .Where(x => x.Broj == broj)
            .OrderBy(x => x.Kredit)
            .Select(x => new PregledTabelaStavka
            {
                Sifra = x.Kredit.ToString(CultureInfo.InvariantCulture),
                Naziv = $"{NazivPartnera(x.Sifra)} | partija {x.Partija}",
                Iznos1 = x.Ostatak,
                Iznos2 = Math.Max(0m, x.Ostatak - TrenutnaRata(x))
            })
            .ToList();

        if (stavke.Count == 0)
        {
            Poruka = "Nema aktivnih kredita za izabranog radnika.";
            return;
        }

        OtvoriPregled("LISTIC RADNIK", $"{NazivRadnika(broj)} | {ModIzvestajaTekst()} (pre obracuna)", stavke, "DUG", "DUG POSLE RATE");
    }

    private void UcitajPodatke()
    {
        Ucitava = true;
        try
        {
            var lista = KreditiDbfSupport.UcitajKredite(_folderPath);
            Stavke = new ObservableCollection<KreditStavka>(lista);
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Učitano {Stavke.Count} kredita.";
        }
        catch (Exception ex)
        {
            Stavke = [];
            Poruka = $"Greška pri učitavanju kredita: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private void IzvrsiRasknjizavanje(bool zaAkontaciju)
    {
        var datum = PitajDatumRasknjizavanja(zaAkontaciju ? "RASKNJIZAVANJE AKONTACIJE" : "RASKNJIZAVANJE PLATE");
        if (datum == null)
            return;

        try
        {
            var rezultat = KreditiDbfSupport.IzvrsiRasknjizavanje(_folderPath, zaAkontaciju, datum.Value, _radnici);
            UcitajPodatke();
            Poruka = rezultat.Poruka;
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri rasknjizavanju: {ex.Message}";
        }
    }

    private DateTime? PitajDatumRasknjizavanja(string naslov)
    {
        var predlog = KreditiDbfSupport.UcitajPredlozeniDatumRasknjizavanja(_folderPath);
        var periodTekst = _parametar == null
            ? "tekuceg perioda"
            : $"mesec {_parametar.Mesec}/{_parametar.Godina}, isplata {_parametar.Isplata}";

        var info = new TextBlock
        {
            Text = $"Unesite datum placanja za {periodTekst} (mora biti isti kao u Parametri 2).",
            Margin = new Thickness(0, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new System.Windows.Media.FontFamily("Tahoma"),
            FontSize = 12
        };

        var datumPicker = new DatePicker
        {
            SelectedDate = predlog,
            DisplayDate = predlog,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var ok = new Button
        {
            Content = "U REDU",
            Width = 90,
            Height = 30,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };

        var odustani = new Button
        {
            Content = "ODUSTANI",
            Width = 90,
            Height = 30,
            IsCancel = true
        };

        var dugmad = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        dugmad.Children.Add(ok);
        dugmad.Children.Add(odustani);

        var panel = new StackPanel
        {
            Margin = new Thickness(14),
            MinWidth = 360
        };
        panel.Children.Add(info);
        panel.Children.Add(datumPicker);
        panel.Children.Add(dugmad);

        var dialog = new Window
        {
            Title = naslov,
            Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight
        };

        var owner = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
        if (owner != null)
            dialog.Owner = owner;

        ok.Click += (_, _) =>
        {
            if (!datumPicker.SelectedDate.HasValue)
            {
                Toast.Pokazi("Morate uneti datum.", ToastTip.Upozorenje);
                return;
            }

            dialog.DialogResult = true;
        };

        if (dialog.ShowDialog() != true || !datumPicker.SelectedDate.HasValue)
            return null;

        var izabraniDatum = datumPicker.SelectedDate.Value.Date;
        if (_parametar != null)
        {
            var datumIzParametara = _parametar.Redispl switch
            {
                1 => _parametar.Dat1,
                2 => _parametar.Dat2,
                3 => _parametar.Dat3,
                _ => _parametar.Dat4
            };

            if (datumIzParametara.HasValue && datumIzParametara.Value.Date != izabraniDatum)
            {
                if (!ConfirmDialog.Pitaj(
                    $"Unet je datum {izabraniDatum:dd.MM.yyyy}, a u Parametri 2 je {datumIzParametara:dd.MM.yyyy}.\n\nNastaviti sa rasknjižavanjem?",
                    "Krediti"))
                    return null;
            }
        }

        return izabraniDatum;
    }

    private void PostaviNadjenuStavku(KreditStavka? stavka, string successMessage, string failMessage)
    {
        if (stavka == null)
        {
            Poruka = failMessage;
            return;
        }

        Selektovana = stavka;
        Poruka = successMessage;
    }

    private void Rebind(IReadOnlyCollection<KreditStavka> lista)
    {
        Stavke = new ObservableCollection<KreditStavka>(lista);
        Selektovana = Stavke.FirstOrDefault();
    }

    private void Renumerisi()
    {
        for (var i = 0; i < Stavke.Count; i++)
            Stavke[i].Numred = i + 1;
    }

    private int SledeciKredit()
    {
        var memMax = Stavke.Count == 0 ? 0 : Stavke.Max(x => x.Kredit);
        if (string.IsNullOrWhiteSpace(_ldkredDbfPath) || !File.Exists(_ldkredDbfPath))
            return memMax + 1;
        try
        {
            var diskMax = KreditiDbfSupport.UcitajKredite(_folderPath)
                .Select(x => x.Kredit).DefaultIfEmpty(0).Max();
            return Math.Max(memMax, diskMax) + 1;
        }
        catch { return memMax + 1; }
    }

    private bool JeAktivanKredit(KreditStavka stavka)
    {
        if (!string.IsNullOrWhiteSpace(stavka.Arhiva) && stavka.Arhiva.Trim() == "*")
            return false;

        if (string.IsNullOrWhiteSpace(stavka.ZaObaviti) || stavka.ZaObaviti.Trim() == " ")
            return false;

        return stavka.AktivnaRata != 0m || stavka.AkontRata != 0m || stavka.Ostatak != 0m;
    }

    private decimal TrenutnaRata(KreditStavka stavka)
        => _izvestajAkontacija ? stavka.AkontRata : stavka.AktivnaRata;

    private string ModIzvestajaTekst()
        => _izvestajAkontacija ? "AKONTACIJA" : "KONACNO";

    private List<KreditStavka> AktivneStavkeZaIzvestaj()
        => Stavke.Where(JeAktivanKredit).Where(x => TrenutnaRata(x) != 0m).ToList();

    private string NazivPartnera(string sifra)
    {
        if (string.IsNullOrWhiteSpace(sifra))
            return "-";

        if (!_partneri.TryGetValue(sifra.Trim(), out var partner))
            return $"Partner {sifra}";

        var racun = string.IsNullOrWhiteSpace(partner.ZiroRac) ? partner.Mesto : partner.ZiroRac;
        return string.IsNullOrWhiteSpace(racun) ? partner.Naziv : $"{partner.Naziv}  |  {racun}";
    }

    private string NazivRadnika(int broj)
    {
        if (broj <= 0)
            return "-";

        return _radnici.TryGetValue(broj, out var radnik) ? radnik.ImePrez : $"Radnik {broj}";
    }

    private void OtvoriPregled(string naslov, string podnaslov, List<PregledTabelaStavka> stavke, string label1, string label2)
    {
        var view = new Views.Zarade.FoxPregledTabelaView(naslov, podnaslov, stavke, label1, label2);
        var owner = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
        if (owner != null)
            view.Owner = owner;

        view.ShowDialog();
    }

    private KreditLookupItem? DodajPartnerIzLookupa()
    {
        try
        {
            var sviPartneri = PartneriDbfSupport.UcitajPartnere(_folderPath).ToList();
            var novaSifra = PredloziNovuSifruPartnera(sviPartneri);
            var novi = new PartnerStavka
            {
                Sifra = novaSifra,
                Pib2 = novaSifra,
                Drzava = "SRBIJA",
                Preneto = " "
            };

            var vm = new PartnerKarticaViewModel(novi, noviUnos: true);
            var view = new Views.Zarade.PartnerKarticaView { DataContext = vm };
            var owner = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            if (owner != null)
                view.Owner = owner;

            if (view.ShowDialog() != true)
                return null;

            if (sviPartneri.Any(x => x.Sifra.Equals(vm.Stavka.Sifra, StringComparison.OrdinalIgnoreCase)))
            {
                Toast.Pokazi($"Partner sa šifrom {vm.Stavka.Sifra} već postoji.", ToastTip.Upozorenje);
                return null;
            }

            sviPartneri.Add(vm.Stavka);
            PartneriDbfSupport.SacuvajPartnere(_folderPath, sviPartneri);

            _partneri[vm.Stavka.Sifra] = new KreditPartnerInfo
            {
                Sifra = vm.Stavka.Sifra,
                Naziv = vm.Stavka.Naziv,
                Mesto = vm.Stavka.Mesto
            };

            Poruka = $"Dodat je partner {vm.Stavka.Sifra}.";
            return new KreditLookupItem(vm.Stavka.Sifra, vm.Stavka.Naziv, vm.Stavka.Mesto);
        }
        catch (Exception ex)
        {
            Toast.Pokazi($"Greška pri dodavanju partnera: {ex.Message}", ToastTip.Greska);
            return null;
        }
    }

    private static string PredloziNovuSifruPartnera(IEnumerable<PartnerStavka> partneri)
    {
        var max = partneri
            .Select(x => ParseInt(x.Sifra))
            .DefaultIfEmpty(0)
            .Max();

        return (max + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static int ParseInt(string text)
        => int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var broj) ? broj : 0;

    private static bool JeAkontacionaIsplata(int isplata)
        => isplata == 2;
}

public partial class KreditStavka : ObservableObject
{
    [ObservableProperty] private int _kredit;
    [ObservableProperty] private int _broj;
    [ObservableProperty] private string _sifra = string.Empty;
    [ObservableProperty] private string _partija = string.Empty;
    [ObservableProperty] private decimal _iznos;
    [ObservableProperty] private int _koliko;
    [ObservableProperty] private decimal _prvaRata;
    [ObservableProperty] private decimal _ostaleRate;
    [ObservableProperty] private string _zaObaviti = "*";
    [ObservableProperty] private decimal _aktivnaRata;
    [ObservableProperty] private decimal _akontRata;
    [ObservableProperty] private decimal _ostatak;
    [ObservableProperty] private decimal _odbijeno;
    [ObservableProperty] private string _evidBroj = string.Empty;
    [ObservableProperty] private string _modelo = string.Empty;
    [ObservableProperty] private DateTime _datDok = DateTime.Today;
    [ObservableProperty] private int _grupa;
    [ObservableProperty] private string _arhiva = " ";
    [ObservableProperty] private string _arhiva2 = " ";
    [ObservableProperty] private string _preneto = " ";
    [ObservableProperty] private long _idBr;
    [ObservableProperty] private int _numred;

    public KreditStavka Clone() => new()
    {
        Kredit = Kredit,
        Broj = Broj,
        Sifra = Sifra,
        Partija = Partija,
        Iznos = Iznos,
        Koliko = Koliko,
        PrvaRata = PrvaRata,
        OstaleRate = OstaleRate,
        ZaObaviti = ZaObaviti,
        AktivnaRata = AktivnaRata,
        AkontRata = AkontRata,
        Ostatak = Ostatak,
        Odbijeno = Odbijeno,
        EvidBroj = EvidBroj,
        Modelo = Modelo,
        DatDok = DatDok,
        Grupa = Grupa,
        Arhiva = Arhiva,
        Arhiva2 = Arhiva2,
        Preneto = Preneto,
        IdBr = IdBr,
        Numred = Numred
    };

    public void CopyFrom(KreditStavka other)
    {
        Kredit = other.Kredit;
        Broj = other.Broj;
        Sifra = other.Sifra;
        Partija = other.Partija;
        Iznos = other.Iznos;
        Koliko = other.Koliko;
        PrvaRata = other.PrvaRata;
        OstaleRate = other.OstaleRate;
        ZaObaviti = other.ZaObaviti;
        AktivnaRata = other.AktivnaRata;
        AkontRata = other.AkontRata;
        Ostatak = other.Ostatak;
        Odbijeno = other.Odbijeno;
        EvidBroj = other.EvidBroj;
        Modelo = other.Modelo;
        DatDok = other.DatDok;
        Grupa = other.Grupa;
        Arhiva = other.Arhiva;
        Arhiva2 = other.Arhiva2;
        Preneto = other.Preneto;
        IdBr = other.IdBr;
        Numred = other.Numred;
    }
}
