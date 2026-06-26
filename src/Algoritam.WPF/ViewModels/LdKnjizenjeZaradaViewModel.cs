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
/// Fox forma LDKON - knjizenje zarada. Čita/piše direktno iz ldkon.dbf/ldkon00.dbf/ldpod.dbf.
/// </summary>
public partial class LdKnjizenjeZaradaViewModel : ObservableObject
{
    private readonly AppState _appState;
    private DbfOptimisticConcurrency.FileSnapshot? _ldkonSnapshot;

    [ObservableProperty]
    private ObservableCollection<LdKnjizenjeStavka> _stavke = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ObrisiCommand))]
    private LdKnjizenjeStavka? _selektovana;

    [ObservableProperty]
    private int _mesec;

    [ObservableProperty]
    private int _isplata;

    [ObservableProperty]
    private string _godina = string.Empty;

    [ObservableProperty]
    private string _vrsta = "U";

    [ObservableProperty]
    private DateTime? _datumDok = DateTime.Today;

    [ObservableProperty]
    private string _brojNaloga = string.Empty;

    [ObservableProperty]
    private string _mp = string.Empty;

    [ObservableProperty]
    private int _mtr;

    [ObservableProperty]
    private string _poruka = string.Empty;

    [ObservableProperty]
    private bool _ucitava;

    public LdKnjizenjeZaradaViewModel(AppState appState)
    {
        _appState = appState;
        PostaviPodrazumevaneVrednosti();
        _ = UcitajAsync();
    }

    public string Naslov => "KNJIZENJE ZARADA (LDKON)";

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
            var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldkon.dbf");
            if (putanja == null)
            {
                Stavke = [];
                Poruka = "ldkon.dbf nije pronađen u folderu firme.";
                return;
            }

            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(putanja));
            _ldkonSnapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(putanja);
            var lista = zapisi.Select(MapirajKnjizenje).OrderBy(x => x.Kod).ThenBy(x => x.Opis).ToList();

            Stavke = new ObservableCollection<LdKnjizenjeStavka>(lista);
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {lista.Count} stavki. Ukupno: {lista.Sum(x => x.Iznos):N2}.";
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
    private void Dodaj()
    {
        var stavka = new LdKnjizenjeStavka
        {
            Vrsta = SkiniVrstu(Vrsta),
            Datdok = DatumDok,
            Brnal = (BrojNaloga ?? string.Empty).Trim(),
            Mp = (Mp ?? string.Empty).Trim(),
            Mtr = Mtr
        };

        Stavke.Add(stavka);
        Selektovana = stavka;
        Poruka = "Dodata je nova stavka. Kliknite Sačuvaj.";
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
        Poruka = $"Uklonjena je stavka '{zaBrisanje.Kod}'. Kliknite Sačuvaj.";
    }

    [RelayCommand]
    private async Task BrisiSveAsync()
    {
        Stavke.Clear();
        Selektovana = null;
        await SacuvajAsync();
        Poruka = "Sve stavke su uklonjene iz prikaza.";
    }

    [RelayCommand]
    private async Task SacuvajAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldkon.dbf");
        if (putanja == null)
        {
            Poruka = "ldkon.dbf nije pronađen — nije moguće sačuvati.";
            return;
        }

        if (_ldkonSnapshot != null && DbfOptimisticConcurrency.HasFileChanged(putanja, _ldkonSnapshot))
        {
            var r = MessageBox.Show(
                "Fajl ldkon.dbf je izmenjen od strane drugog korisnika.\nNastaviti sa čuvanjem (prepisati)?",
                "Upozorenje — dual korisnici", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(putanja);

            var redovi = Stavke.OrderBy(x => x.Kod).ThenBy(x => x.Opis)
                .Select(s => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["VRSTA"]  = SkiniVrstu(s.Vrsta),
                    ["KOD"]    = (s.Kod ?? string.Empty).Trim(),
                    ["OPIS"]   = (s.Opis ?? string.Empty).Trim(),
                    ["KONTO"]  = (s.Konto ?? string.Empty).Trim(),
                    ["KONTOP"] = (s.Kontop ?? string.Empty).Trim(),
                    ["IZNOS"]  = s.Iznos,
                    ["DATDOK"] = s.Datdok,
                    ["BRNAL"]  = (s.Brnal ?? string.Empty).Trim(),
                    ["MP"]     = (s.Mp ?? string.Empty).Trim(),
                    ["MTR"]    = (decimal)s.Mtr,
                    ["PRENETO"] = (s.Preneto ?? string.Empty).Trim(),
                    ["IDBR"]   = (decimal)s.Idbr
                })
                .ToList();

            await Task.Run(() => DbfTableWriter.WriteTable(
                putanja,
                schema,
                redovi,
                static (r, fieldName) => r.TryGetValue(fieldName, out var v) ? v : null));

            _ldkonSnapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(putanja);
            Poruka = $"Knjiženje sačuvano u ldkon.dbf ({redovi.Count} stavki).";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri cuvanju: {ex.Message}";
        }
    }

    public async Task SacuvajBezPorukeAsync()
    {
        var prethodnaPoruka = Poruka;
        await SacuvajAsync();

        if (Poruka.StartsWith("Knjiženje sačuvano", StringComparison.OrdinalIgnoreCase))
            Poruka = prethodnaPoruka;
    }

    [RelayCommand]
    private async Task PrenosKontaAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldkon00.dbf");
        if (putanja == null)
        {
            Poruka = "Tabela konta (ldkon00.dbf) nije pronađena.";
            return;
        }

        try
        {
            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(putanja));
            var sablon = zapisi.Select(z => new LdKontoSablonStavka
            {
                Vrsta = Str(z, "VRSTA"),
                Kod = Str(z, "KOD"),
                Opis = Str(z, "OPIS"),
                Konto = Str(z, "KONTO"),
                Kontop = Str(z, "KONTOP"),
                Preneto = Str(z, "PRENETO"),
                Idbr = Long(z, "IDBR")
            }).OrderBy(x => x.Kod).ThenBy(x => x.Opis).ToList();

            if (sablon.Count == 0)
            {
                Poruka = "Tabela konta (ldkon00.dbf) je prazna.";
                return;
            }

            var poKodu = Stavke
                .GroupBy(x => NormalizujKod(x.Kod))
                .ToDictionary(g => g.Key, g => g.First());

            var datum = DatumDok;
            var nalog = (BrojNaloga ?? string.Empty).Trim();
            var mesto = (Mp ?? string.Empty).Trim();

            var nove = sablon.Select(s =>
            {
                var kljuc = NormalizujKod(s.Kod);
                poKodu.TryGetValue(kljuc, out var postojeca);

                return new LdKnjizenjeStavka
                {
                    Vrsta = SkiniVrstu(s.Vrsta),
                    Kod = (s.Kod ?? string.Empty).Trim(),
                    Opis = (s.Opis ?? string.Empty).Trim(),
                    Konto = (s.Konto ?? string.Empty).Trim(),
                    Kontop = (s.Kontop ?? string.Empty).Trim(),
                    Iznos = postojeca?.Iznos ?? 0m,
                    Datdok = postojeca?.Datdok ?? datum,
                    Brnal = (postojeca?.Brnal ?? nalog).Trim(),
                    Mp = (postojeca?.Mp ?? mesto).Trim(),
                    Mtr = postojeca?.Mtr ?? Mtr,
                    Preneto = (s.Preneto ?? string.Empty).Trim(),
                    Idbr = s.Idbr
                };
            }).ToList();

            Stavke = new ObservableCollection<LdKnjizenjeStavka>(nove);
            Selektovana = Stavke.FirstOrDefault();
            await SacuvajAsync();
            Poruka = $"Preneto {nove.Count} konta iz ldkon00.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri prenosu konta: {ex.Message}";
        }
    }

    [RelayCommand]
    private Task PrenosPodatakaAkontacijaAsync()
        => PrenosPodatakaIzLdpod00Async(akontacija: true);

    [RelayCommand]
    private Task PrenosPodatakaKonacnaIsplataAsync()
        => PrenosPodatakaIzLdpod00Async(akontacija: false);

    private async Task PrenosPodatakaIzLdpod00Async(bool akontacija)
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        if (Stavke.Count == 0)
        {
            Poruka = "Nema stavki za prenos podataka. Prvo uradite Prenos konta.";
            return;
        }

        var putanjaLdpod00 = LdObracunDbfReader.PronadjiDbf(folder, "ldpod00.dbf");
        if (putanjaLdpod00 == null)
        {
            Poruka = "ldpod00.dbf nije pronađen.";
            return;
        }

        try
        {
            Ucitava = true;
            var ldpodZapisi = await Task.Run(() => DbfReader.CitajSveZapise(putanjaLdpod00));
            if (ldpodZapisi.Count == 0)
            {
                Poruka = "ldpod00.dbf je prazan.";
                return;
            }

            Dictionary<string, object?>? ldparam = null;
            var putanjaLdpParam = LdObracunDbfReader.PronadjiDbf(folder, "ldparam.dbf");
            if (putanjaLdpParam != null)
            {
                ldparam = await Task.Run(() => DbfReader.CitajSveZapise(putanjaLdpParam).FirstOrDefault());
            }

            var prazno = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var param = ldparam ?? prazno;

            var mesecFilter = Mesec > 0 ? Mesec : Int(param, "MESEC");
            var isplataFilter = Isplata > 0 ? Isplata : Int(param, "ISPLATA");
            var redIsplate = Int(param, "REDISPL");
            if (redIsplate <= 0) redIsplate = 1;
            if (redIsplate > 4) redIsplate = 4;

            var datumDok = IzracunajDatumDok(ldparam, redIsplate) ?? DatumDok ?? DateTime.Today;
            var brojNaloga = string.IsNullOrWhiteSpace(BrojNaloga)
                ? IzracunajBrojNaloga(isplataFilter, redIsplate, mesecFilter)
                : (BrojNaloga ?? string.Empty).Trim();

            var podaciPoKodu = ldpodZapisi
                .Where(x => !string.IsNullOrWhiteSpace(Str(x, "KOD")))
                .GroupBy(x => NormalizujKod(Str(x, "KOD")))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            decimal pioPodela = 0m;
            decimal iznos27 = 0m;
            decimal iznos31 = 0m;
            var azurirano = 0;

            foreach (var stavka in Stavke)
            {
                var kod = (stavka.Kod ?? string.Empty).Trim();
                if (JeKod(kod, "27.") || JeKod(kod, "31."))
                    continue;

                if (!TryPronadjiLdpod00Zapis(podaciPoKodu, kod, mesecFilter, isplataFilter, out var pod))
                    continue;

                var iznos = IzracunajIznosPoRedosledu(
                    pod,
                    redIsplate,
                    akontacija,
                    konacniNacinZaKod51: JeKod(kod, "51."));

                if (JeKod(kod, "152."))
                    pioPodela = Math.Round(iznos / 2m, 2, MidpointRounding.AwayFromZero);

                if (stavka.Iznos != iznos)
                {
                    stavka.Iznos = iznos;
                    azurirano++;
                }
            }

            if (TryPronadjiLdpod00Zapis(podaciPoKodu, "27.", mesecFilter, isplataFilter, out var pod27))
                iznos27 = IzracunajIznosPoRedosledu(pod27, redIsplate, akontacija, konacniNacinZaKod51: false) - pioPodela;

            if (TryPronadjiLdpod00Zapis(podaciPoKodu, "31.", mesecFilter, isplataFilter, out var pod31))
                iznos31 = IzracunajIznosPoRedosledu(pod31, redIsplate, akontacija, konacniNacinZaKod51: false) - pioPodela;

            foreach (var stavka in Stavke)
            {
                var kod = (stavka.Kod ?? string.Empty).Trim();
                if (JeKod(kod, "27.") && stavka.Iznos != iznos27)
                {
                    stavka.Iznos = iznos27;
                    azurirano++;
                }
                else if (JeKod(kod, "31.") && stavka.Iznos != iznos31)
                {
                    stavka.Iznos = iznos31;
                    azurirano++;
                }

                stavka.Datdok = datumDok;
                stavka.Brnal = brojNaloga;
            }

            Stavke = new ObservableCollection<LdKnjizenjeStavka>(Stavke.OrderBy(x => x.Kod).ThenBy(x => x.Opis).ToList());
            Selektovana = Stavke.FirstOrDefault();

            var tip = akontacija ? "akontacije" : "konacne isplate";
            await SacuvajAsync();
            Poruka = $"Preneti podaci {tip} iz ldpod00. Azurirano stavki: {azurirano}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri prenosu podataka: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    private static decimal IzracunajIznosPoRedosledu(
        Dictionary<string, object?> ldpod00Red,
        int redIsplate,
        bool akontacija,
        bool konacniNacinZaKod51)
    {
        var s1 = Dec(ldpod00Red, "S1U");
        var s2 = Dec(ldpod00Red, "S2U");
        var s3 = Dec(ldpod00Red, "S3U");
        var s4 = Dec(ldpod00Red, "S4U");

        if (!akontacija || konacniNacinZaKod51)
        {
            return redIsplate switch
            {
                1 => s1,
                2 => s2,
                3 => s3,
                4 => s4,
                _ => 0m
            };
        }

        return redIsplate switch
        {
            1 => s1,
            2 => s2 - s1,
            3 => s3 - s2 - s1,
            4 => s4 - s3 - s2 - s1,
            _ => 0m
        };
    }

    private static bool TryPronadjiLdpod00Zapis(
        Dictionary<string, List<Dictionary<string, object?>>> podaciPoKodu,
        string kod,
        int mesec,
        int isplata,
        out Dictionary<string, object?> zapis)
    {
        zapis = null!;
        if (!podaciPoKodu.TryGetValue(NormalizujKod(kod), out var kandidati) || kandidati.Count == 0)
            return false;

        zapis = kandidati.FirstOrDefault(x =>
                    (mesec <= 0 || Int(x, "MESEC") == mesec) &&
                    (isplata <= 0 || Int(x, "ISPLATA") == isplata))
                ?? kandidati.First();

        return zapis != null;
    }

    private static DateTime? IzracunajDatumDok(Dictionary<string, object?>? ldparam, int redIsplate)
    {
        if (ldparam == null)
            return null;

        return redIsplate switch
        {
            1 => Dat(ldparam, "DAT1"),
            2 => Dat(ldparam, "DAT2"),
            3 => Dat(ldparam, "DAT3"),
            4 => Dat(ldparam, "DAT4"),
            _ => null
        };
    }

    private static string IzracunajBrojNaloga(int isplata, int redIsplate, int mesec)
    {
        var prefiks = isplata switch
        {
            1 => "N",
            2 => "P",
            3 => "B",
            4 => "I",
            5 => "R",
            _ => "N"
        };

        if (mesec < 0) mesec = 0;
        if (mesec > 99) mesec %= 100;
        if (redIsplate < 0) redIsplate = 0;

        return $"LD{prefiks}{redIsplate}{mesec:00}";
    }

    private static bool JeKod(string? kod, string trazeniKod)
        => string.Equals(NormalizujKod(kod), NormalizujKod(trazeniKod), StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private void PrimeniZaglavlje()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema stavki za izmenu.";
            return;
        }

        var datum = DatumDok;
        var nalog = (BrojNaloga ?? string.Empty).Trim();
        var mesto = (Mp ?? string.Empty).Trim();

        foreach (var s in Stavke)
        {
            s.Datdok = datum;
            s.Brnal = nalog;
            s.Mp = mesto;
            s.Mtr = Mtr;
        }

        Poruka = "Zaglavlje je primenjeno na sve stavke. Kliknite Sačuvaj.";
    }

    [RelayCommand]
    private async Task KnjizenjeIsplateAsync()
    {
        await PopuniIznoseIzLdpodAsync(koristiSvu: true, "Izvršeno knjiženje isplate (SVU).");
    }

    [RelayCommand]
    private async Task KnjizenjeUkalkulisaneAsync()
    {
        await PopuniIznoseIzLdpodAsync(koristiSvu: false, "Izvršeno knjiženje ukalkulisane zarade (SU).");
    }

    private async Task PopuniIznoseIzLdpodAsync(bool koristiSvu, string porukaUspeha)
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldpod.dbf");
        if (putanja == null)
        {
            Poruka = "ldpod.dbf nije pronađen.";
            return;
        }

        try
        {
            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(putanja));
            var vrstaFilter = SkiniVrstu(Vrsta);

            var podaci = zapisi.Select(z => new
            {
                Kod = Str(z, "KOD"),
                Opis = Str(z, "OPIS"),
                Vrsta = Str(z, "VRSTA"),
                Mesec = Int(z, "MESEC"),
                Isplata = Int(z, "ISPLATA"),
                Su = Dec(z, "SU"),
                Svu = Dec(z, "SVU")
            }).AsEnumerable();

            // ISPLATA=0 u ldpod znaci "svi" — tretiramo isto kao poklapanje
            if (Mesec > 0) podaci = podaci.Where(x => x.Mesec == 0 || x.Mesec == Mesec);
            if (Isplata > 0) podaci = podaci.Where(x => x.Isplata == 0 || x.Isplata == Isplata);
            if (!string.IsNullOrWhiteSpace(vrstaFilter)) podaci = podaci.Where(x => string.IsNullOrWhiteSpace(x.Vrsta) || x.Vrsta == vrstaFilter);

            var iznosi = podaci
                .Select(x => new
                {
                    x.Kod,
                    x.Opis,
                    x.Vrsta,
                    Iznos = koristiSvu ? x.Svu : x.Su
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Kod) && x.Iznos != 0m)
                .GroupBy(x => NormalizujKod(x.Kod))
                .Select(g =>
                {
                    var prvi = g.First();
                    return new { prvi.Kod, prvi.Opis, prvi.Vrsta, Iznos = g.Sum(x => x.Iznos) };
                })
                .ToList();

            if (iznosi.Count == 0)
            {
                Poruka = "Nema podataka u ldpod.dbf. Kliknite 'Generiši zbirne podatke' pa pokušajte ponovo.";
                return;
            }

            var poKodu = Stavke
                .GroupBy(x => NormalizujKod(x.Kod))
                .ToDictionary(g => g.Key, g => g.First());

            var datum = DatumDok;
            var nalog = (BrojNaloga ?? string.Empty).Trim();
            var mesto = (Mp ?? string.Empty).Trim();

            foreach (var i in iznosi)
            {
                if (poKodu.TryGetValue(NormalizujKod(i.Kod), out var stavka))
                {
                    stavka.Iznos = i.Iznos;
                    if (string.IsNullOrWhiteSpace(stavka.Opis))
                        stavka.Opis = i.Opis;
                    if (string.IsNullOrWhiteSpace(stavka.Vrsta))
                        stavka.Vrsta = SkiniVrstu(i.Vrsta);
                    continue;
                }

                Stavke.Add(new LdKnjizenjeStavka
                {
                    Vrsta = SkiniVrstu(i.Vrsta),
                    Kod = i.Kod,
                    Opis = i.Opis,
                    Iznos = i.Iznos,
                    Datdok = datum,
                    Brnal = nalog,
                    Mp = mesto,
                    Mtr = Mtr
                });
            }

            Selektovana = Stavke.FirstOrDefault();
            await SacuvajAsync();
            Poruka = $"{porukaUspeha} Stavki azurirano: {iznosi.Count}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri knjiženju: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GenerisiLdpodAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            MessageBox.Show("Folder aktivne firme nije pronađen.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Traži ldpod.dbf u firma folderu; ako ne postoji nudi korisniku da ga locira
        var putanjaLdpod = LdObracunDbfReader.PronadjiDbf(folder, "ldpod.dbf");
        if (putanjaLdpod == null)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "ldpod.dbf nije nadjen automatski — izaberite ga ručno",
                Filter = "ldpod.dbf|ldpod.dbf|DBF fajlovi|*.dbf",
                FileName = "ldpod.dbf"
            };
            if (dlg.ShowDialog() != true)
                return;
            putanjaLdpod = dlg.FileName;
        }

        try
        {
            Ucitava = true;
            var radnici = await Task.Run(() => LdObracunDbfReader.CitajSve(folder));

            if (radnici.Count == 0)
            {
                MessageBox.Show("Platni spisak je prazan — nema podataka za generisanje.\nPrvo uradite Obračun BRUTO u Platnom Spisu.", "Nema podataka", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Agregira po svim radnicima — identično Fox ldpodaci.prg / PUNIPOD01
            decimal bruto = 0, neto = 0, porez = 0, dopSocR = 0, dopSocF = 0;
            decimal doppr = 0, dopzr = 0, dopnr = 0, doppf = 0, dopzf = 0, dopnf = 0;
            decimal bendin = 0, komorajd = 0, komorasd = 0, komorard = 0;
            decimal netoprev = 0, krediti = 0, akontac = 0, solidarn = 0, samodopr = 0;
            decimal sindikat1 = 0, sindikat2 = 0, aliment = 0, kasa = 0, kasarata = 0;
            decimal prevoz = 0, obust1 = 0, obust2 = 0, obust3 = 0, ukobust = 0, zaisplatu = 0;

            foreach (var r in radnici)
            {
                bruto     += r.Bruto;
                neto      += r.Neto;
                porez     += r.Porez + r.Porezu;
                dopSocR   += r.Dopsocr;
                dopSocF   += r.Dopsocf;
                doppr     += r.Doppr;
                dopzr     += r.Dopzr;
                dopnr     += r.Dopnr;
                doppf     += r.Doppf;
                dopzf     += r.Dopzf;
                dopnf     += r.Dopnf;
                bendin    += r.Bendin;
                komorajd  += r.Komorajd;
                komorasd  += r.Komorasd;
                komorard  += r.Komorard;
                netoprev  += r.Netoprev;
                krediti   += r.Krediti + r.Kreditia;
                akontac   += r.Akontac;
                solidarn  += r.Solidarn;
                samodopr  += r.Samodopr;
                sindikat1 += r.Sindikat1;
                sindikat2 += r.Sindikat2;
                aliment   += r.Aliment;
                kasa      += r.Kasa;
                kasarata  += r.Kasarata;
                prevoz    += r.Prevoz;
                obust1    += r.Obust1;
                obust2    += r.Obust2;
                obust3    += r.Obust3;
                ukobust   += r.Ukobust;
                zaisplatu += r.Zaisplatu;
            }

            // Redosled i KOD identični Rekapitulaciji / ldrekap2.prg
            var stavke = new List<(string Kod, string Opis, decimal Su, decimal Svu)>
            {
                ("24.",  "BRUTO ZARADA",             bruto,     bruto),
                ("22.",  "POREZ NA ZARADE",           porez,     porez),
                ("26.",  "DOPRINOSI RADNIKA",         dopSocR,   dopSocR),
                ("21.",  "NETO ZARADA",               neto,      neto),
                ("27.",  "PIO RADNIKA",               doppr,     doppr),
                ("28.",  "ZDRAVSTVENO RADNIKA",       dopzr,     dopzr),
                ("29.",  "ZAPOSLJAVANJE RADNIKA",     dopnr,     dopnr),
                ("30.",  "DOPRINOSI POSLODAVCA",      dopSocF,   dopSocF),
                ("31.",  "PIO POSLODAVAC",            doppf,     doppf),
                ("32.",  "ZDRAVSTVENO POSLODAVAC",    dopzf,     dopzf),
                ("33.",  "ZAPOSLJAVANJE POSLODAVAC",  dopnf,     dopnf),
                ("52.",  "DOP ZA BENEFICIRANI STAZ",  bendin,    bendin),
                ("34.",  "CLANARINA KOMORI",          komorajd,  komorajd),
                ("35.",  "CLANARINA KOMORI SRBIJE",   komorasd,  komorasd),
                ("36.",  "CLANARINA KOMORI REGIONA",  komorard,  komorard),
                ("153.", "NAKNADA PREVOZA",           netoprev,  netoprev),
                ("38.",  "KREDITI",                  krediti,   krediti),
                ("39.",  "AKONTACIJA",               akontac,   akontac),
                ("40.",  "SOLIDARNOST",              solidarn,  solidarn),
                ("41.",  "SAMODOPRINOS",             samodopr,  samodopr),
                ("42.",  "SINDIKAT 1",               sindikat1, sindikat1),
                ("43.",  "SINDIKAT 2",               sindikat2, sindikat2),
                ("44.",  "ALIMENTACIJA",             aliment,   aliment),
                ("45.",  "KASA",                     kasa,      kasa),
                ("46.",  "KASA RATA",                kasarata,  kasarata),
                ("154.", "OBUSTAVA PREVOZA",         prevoz,    prevoz),
                ("47.",  "OSTALE OBUSTAVE 1",        obust1,    obust1),
                ("48.",  "OSTALE OBUSTAVE 2",        obust2,    obust2),
                ("49.",  "OSTALE OBUSTAVE 3",        obust3,    obust3),
                ("50.",  "UKUPNE OBUSTAVE",          ukobust,   ukobust),
                ("51.",  "ZA ISPLATU",               0m,        zaisplatu),
            };

            var schema = DbfTableWriter.LoadSchema(putanjaLdpod);
            var mesecZa = Mesec > 0 ? Mesec : DateTime.Now.Month;
            var isplataZa = Isplata > 0 ? Isplata : 1;

            var redovi = stavke.Select(s => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["KOD"]     = s.Kod,
                ["OPIS"]    = s.Opis,
                ["SU"]      = s.Su,
                ["SVU"]     = s.Svu,
                ["S1U"]     = s.Su,
                ["SV1U"]    = s.Svu,
                ["MESEC"]   = (decimal)mesecZa,
                ["ISPLATA"] = (decimal)isplataZa,
                ["VRSTA"]   = SkiniVrstu(Vrsta),
                ["PRENETO"] = string.Empty,
                ["IDBR"]    = 0m
            }).ToList();

            await Task.Run(() => DbfTableWriter.WriteTable(
                putanjaLdpod, schema, redovi,
                static (r, f) => r.TryGetValue(f, out var v) ? v : null));

            var msg = $"Generisano {redovi.Count} zbirnih stavki u ldpod.dbf\n(mesec {mesecZa}, isplata {isplataZa}).\n\nSada možete koristiti:\n• Knjizenje isplate\n• Knjizenje ukalkulisane";
            Poruka = msg.Replace("\n", " ");
            MessageBox.Show(msg, "Uspešno generisano", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri generisanju: {ex.Message}";
            MessageBox.Show($"Greška pri generisanju:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Ucitava = false;
        }
    }

    private void PostaviPodrazumevaneVrednosti()
    {
        Mesec = DateTime.Now.Month;
        Isplata = 1;
        Godina = DateTime.Now.Year.ToString();
        Vrsta = "U";
        DatumDok = DateTime.Today;
        Mtr = Mesec;

        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder)) return;

        var dbfPath = LdObracunDbfReader.PronadjiDbf(folder, "ldparam.dbf");
        if (dbfPath == null) return;

        try
        {
            var red = DbfReader.CitajSveZapise(dbfPath).FirstOrDefault();
            if (red == null) return;

            var m = Int(red, "MESEC");
            var i = Int(red, "ISPLATA");
            var g = Str(red, "GODINA");
            if (m > 0) { Mesec = m; Mtr = m; }
            if (i > 0) Isplata = i;
            if (!string.IsNullOrWhiteSpace(g)) Godina = g;
        }
        catch { }
    }

    private static LdKnjizenjeStavka MapirajKnjizenje(Dictionary<string, object?> z)
    {
        return new LdKnjizenjeStavka
        {
            Vrsta = Str(z, "VRSTA"),
            Kod = Str(z, "KOD"),
            Opis = Str(z, "OPIS"),
            Konto = Str(z, "KONTO"),
            Kontop = Str(z, "KONTOP"),
            Iznos = Dec(z, "IZNOS"),
            Datdok = Dat(z, "DATDOK"),
            Brnal = Str(z, "BRNAL"),
            Mp = Str(z, "MP"),
            Mtr = Int(z, "MTR"),
            Preneto = Str(z, "PRENETO"),
            Idbr = Long(z, "IDBR")
        };
    }

    private static string Str(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0;
        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        if (int.TryParse(v.ToString(), out var parsed)) return parsed;
        return 0;
    }

    private static long Long(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0L;
        if (v is decimal d) return (long)d;
        if (v is long l) return l;
        if (long.TryParse(v.ToString(), out var parsed)) return parsed;
        return 0L;
    }

    private static decimal Dec(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0m;
        if (v is decimal d) return d;
        if (decimal.TryParse(v.ToString(), out var parsed)) return parsed;
        return 0m;
    }

    private static DateTime? Dat(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return null;
        if (v is DateTime dt) return dt;
        if (DateTime.TryParse(v.ToString(), out var parsed)) return parsed;
        return null;
    }

    private static string NormalizujKod(string? kod) => (kod ?? string.Empty).Trim().ToUpperInvariant();

    private static string SkiniVrstu(string? vrsta)
    {
        var v = (vrsta ?? string.Empty).Trim();
        return v.Length == 0 ? string.Empty : v.Substring(0, 1);
    }

    [RelayCommand]
    private Task OsveziAsync() => UcitajAsync();
}
