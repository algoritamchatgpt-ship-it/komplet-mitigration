using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class OsSifarnikViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<OsSifarnikViewModel>();
    private readonly AppState _appState;
    private readonly IPutanjaService? _putanjaService;

    [ObservableProperty] private int    _aktivniTab = 0;
    [ObservableProperty] private string _poruka     = "";

    private bool _izmijenjeno;
    public bool ImaNeSnimljenih => _izmijenjeno;

    [ObservableProperty] private ObservableCollection<OsVrstaStavka>  _vrsteOs           = [];
    [ObservableProperty] private ObservableCollection<OsAgStavka>     _amortGrupe        = [];
    [ObservableProperty] private ObservableCollection<OsAgPodStavka>  _amortPodgrupe     = [];
    [ObservableProperty] private ObservableCollection<OsIzvorStavka>  _izvoriFinansiranja = [];
    [ObservableProperty] private ObservableCollection<OsOsnKStavka>   _osnKoriscenja     = [];

    [ObservableProperty] private ObservableCollection<OsKontoStavka>  _kontaOs          = [];

    [ObservableProperty] private OsVrstaStavka?  _izabranaVrsta;
    [ObservableProperty] private OsAgStavka?     _izabranaAmortGrupa;
    [ObservableProperty] private OsAgPodStavka?  _izabranaAmortPodgrupa;
    [ObservableProperty] private OsIzvorStavka?  _izabraniIzvor;
    [ObservableProperty] private OsOsnKStavka?   _izabraniOsnov;
    [ObservableProperty] private OsKontoStavka?  _izabraniKonto;

    public OsSifarnikViewModel(AppState appState, IPutanjaService? putanjaService = null)
    {
        _appState       = appState;
        _putanjaService = putanjaService;
        UcitajSve();
    }

    private void UcitajSve()
    {
        UcitajVrsteOs();
        UcitajAmortGrupe();
        UcitajAmortPodgrupe();
        UcitajIzvorFinansiranja();
        UcitajOsnKoriscenja();
        UcitajKontoOs();
        Poruka = $"Učitano: Vrste OS ({VrsteOs.Count}), " +
                 $"Amort. grupe ({AmortGrupe.Count}), " +
                 $"Podgrupe ({AmortPodgrupe.Count}), " +
                 $"Izvor fin. ({IzvoriFinansiranja.Count}), " +
                 $"Osnov korišćenja ({OsnKoriscenja.Count}), " +
                 $"Konto ({KontaOs.Count})";
        _izmijenjeno = false;
    }

    // ═══ UČITAVANJE ═══

    private void UcitajVrsteOs()
    {
        var path = DbfPutanja("osvrsta.dbf");
        if (path == null) { VrsteOs = []; return; }
        try
        {
            VrsteOs = new ObservableCollection<OsVrstaStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsVrstaStavka
                {
                    Vrsta   = DbfReader.Str(r, "VRSTA"),
                    Naziv   = DbfReader.Str(r, "NAZIV"),
                    Preneto = DbfReader.Str(r, "PRENETO"),
                    IDBr    = (int)DbfReader.Dec(r, "IDBR"),
                }));
        }
        catch (Exception ex) { VrsteOs = []; Poruka = $"osvrsta.dbf: {ex.Message}"; }
    }

    private void UcitajAmortGrupe()
    {
        var path = DbfPutanja("osag.dbf");
        if (path == null) { AmortGrupe = []; return; }
        try
        {
            AmortGrupe = new ObservableCollection<OsAgStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsAgStavka
                {
                    Ag      = DbfReader.Str(r, "AG"),
                    AgStopa = DbfReader.Dec(r, "AGSTOPA"),
                    Opis    = DbfReader.Str(r, "OPIS"),
                    Vrsta   = DbfReader.Str(r, "VRSTA"),
                    Preneto = DbfReader.Str(r, "PRENETO"),
                    IDBr    = (int)DbfReader.Dec(r, "IDBR"),
                }));
        }
        catch (Exception ex) { AmortGrupe = []; Poruka = $"osag.dbf: {ex.Message}"; }
    }

    private void UcitajAmortPodgrupe()
    {
        var path = DbfPutanja("osagpod.dbf");
        if (path == null) { AmortPodgrupe = []; return; }
        try
        {
            AmortPodgrupe = new ObservableCollection<OsAgPodStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsAgPodStavka
                {
                    AgPod   = DbfReader.Str(r, "AGPOD"),
                    Ag      = DbfReader.Str(r, "AG"),
                    Opis    = DbfReader.Str(r, "OPIS"),
                    Preneto = DbfReader.Str(r, "PRENETO"),
                    IDBr    = (int)DbfReader.Dec(r, "IDBR"),
                }));
        }
        catch (Exception ex) { AmortPodgrupe = []; Poruka = $"osagpod.dbf: {ex.Message}"; }
    }

    private void UcitajIzvorFinansiranja()
    {
        var path = DbfPutanja("osizvorf.dbf");
        if (path == null) { IzvoriFinansiranja = []; return; }
        try
        {
            IzvoriFinansiranja = new ObservableCollection<OsIzvorStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsIzvorStavka
                {
                    Izvor   = DbfReader.Str(r, "IZVOR"),
                    Naziv   = DbfReader.Str(r, "NAZIV"),
                    Preneto = DbfReader.Str(r, "PRENETO"),
                    IDBr    = (int)DbfReader.Dec(r, "IDBR"),
                }));
        }
        catch (Exception ex) { IzvoriFinansiranja = []; Poruka = $"osizvorf.dbf: {ex.Message}"; }
    }

    private void UcitajOsnKoriscenja()
    {
        var path = DbfPutanja("ososnk.dbf");
        if (path == null) { OsnKoriscenja = []; return; }
        try
        {
            OsnKoriscenja = new ObservableCollection<OsOsnKStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsOsnKStavka
                {
                    OsnovKor = DbfReader.Str(r, "OSNOVKOR"),
                    Naziv    = DbfReader.Str(r, "NAZIV"),
                    Preneto  = DbfReader.Str(r, "PRENETO"),
                    IDBr     = (int)DbfReader.Dec(r, "IDBR"),
                }));
        }
        catch (Exception ex) { OsnKoriscenja = []; Poruka = $"ososnk.dbf: {ex.Message}"; }
    }

    private void UcitajKontoOs()
    {
        var path = DbfPutanja("konto.dbf");
        if (path == null) { KontaOs = []; return; }
        try
        {
            KontaOs = new ObservableCollection<OsKontoStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsKontoStavka
                {
                    Konto   = DbfReader.Str(r, "KONTO"),
                    Opis    = DbfReader.Str(r, "OPIS"),
                    Preneto = DbfReader.Str(r, "PRENETO"),
                    IDBr    = (int)DbfReader.Dec(r, "IDBR"),
                }));
        }
        catch (Exception ex) { KontaOs = []; Poruka = $"konto.dbf: {ex.Message}"; }
    }

    [RelayCommand]
    private void PopuniKontoIzFin()
    {
        var targetPath = DbfPutanja("konto.dbf");
        if (targetPath == null) { Poruka = "konto.dbf nije pronađen u folderu firme."; return; }

        if (KontaOs.Count > 0)
        {
            if (MessageBox.Show($"Konto tabela već ima {KontaOs.Count} zapisa. Prebrisati?",
                    "Uvoz konta iz FIN", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    != MessageBoxResult.Yes)
                return;
        }

        var finRoot = _putanjaService?.DajFinPutanju() ?? string.Empty;
        var sourcePath = NadjiKontoDbfUFin(finRoot);
        if (sourcePath == null) { Poruka = "konto.dbf nije pronađen u FIN folderu."; return; }

        try
        {
            File.Copy(sourcePath, targetPath, overwrite: true);
            UcitajKontoOs();
            Poruka = $"Uvezeno {KontaOs.Count} konta iz: {Path.GetDirectoryName(sourcePath)}";
            _log.Information("konto.dbf: uveženo {Count} konta iz {Source}", KontaOs.Count, sourcePath);
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri uvozu konta: {ex.Message}";
            _log.Error(ex, "Greška pri uvozu konto.dbf");
        }
    }

    private static string? NadjiKontoDbfUFin(string finRoot)
    {
        if (string.IsNullOrWhiteSpace(finRoot)) return null;
        var candidates = new[]
        {
            Path.Combine(finRoot, "konto.dbf"),
            Path.Combine(finRoot, "KONTO.DBF"),
            Path.Combine(finRoot, "data00", "konto.dbf"),
            Path.Combine(finRoot, "DATA00", "KONTO.DBF"),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    // ═══ DODAJ ═══

    // Legacy: osvrsta.scx/osizvorf.scx/ososnk.scx (tabovi 0,3,4) na DODAJ auto-numerišu
    // sam ŠIFRA kod (REPLACE VRSTA/IZVOR/OSNOVKOR WITH STR(RECNO(),4,0)). osag.scx/
    // osagpod.scx (tabovi 1,2) NE auto-numerišu kod — ostaje prazan za ručni unos
    // (alfanumerički AG/AGPOD kodovi poput "1", "1.01").
    [RelayCommand]
    private void Dodaj()
    {
        switch (AktivniTab)
        {
            case 0:
                var v = new OsVrstaStavka { IDBr = SledeciIdbr(VrsteOs.Select(x => x.IDBr)), Preneto = "N",
                    Vrsta = SledecaSifra4(VrsteOs.Select(x => x.Vrsta)) };
                VrsteOs.Add(v); IzabranaVrsta = v; break;
            case 1:
                var g = new OsAgStavka { IDBr = SledeciIdbr(AmortGrupe.Select(x => x.IDBr)), Preneto = "N" };
                AmortGrupe.Add(g); IzabranaAmortGrupa = g; break;
            case 2:
                var p = new OsAgPodStavka { IDBr = SledeciIdbr(AmortPodgrupe.Select(x => x.IDBr)), Preneto = "N" };
                AmortPodgrupe.Add(p); IzabranaAmortPodgrupa = p; break;
            case 3:
                var i = new OsIzvorStavka { IDBr = SledeciIdbr(IzvoriFinansiranja.Select(x => x.IDBr)), Preneto = "N",
                    Izvor = SledecaSifra4(IzvoriFinansiranja.Select(x => x.Izvor)) };
                IzvoriFinansiranja.Add(i); IzabraniIzvor = i; break;
            case 4:
                var o = new OsOsnKStavka { IDBr = SledeciIdbr(OsnKoriscenja.Select(x => x.IDBr)), Preneto = "N",
                    OsnovKor = SledecaSifra4(OsnKoriscenja.Select(x => x.OsnovKor)) };
                OsnKoriscenja.Add(o); IzabraniOsnov = o; break;
            case 5:
                Poruka = "Kontni plan se uvozi iz FIN sistema — koristite UVEZI IZ FIN."; return;
        }
        Poruka = "Novi red dodan. Unesite podatke i kliknite Sačuvaj.";
        _izmijenjeno = true;
    }

    private static int SledeciIdbr(IEnumerable<int> existingIds)
    {
        var ids = existingIds.ToList();
        return ids.Count == 0 ? 1 : ids.Max() + 1;
    }

    private static string SledecaSifra4(IEnumerable<string?> existingCodes)
    {
        var max = existingCodes
            .Select(c => int.TryParse((c ?? "").Trim(), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return (max + 1).ToString("0000");
    }

    private void OtvoriKarticuZaTrenutni()
    {
        OsSifarnikKarticaViewModel? vm = AktivniTab switch
        {
            0 when IzabranaVrsta         != null => OsSifarnikKarticaViewModel.ZaVrstu(IzabranaVrsta),
            1 when IzabranaAmortGrupa    != null => OsSifarnikKarticaViewModel.ZaAg(IzabranaAmortGrupa),
            2 when IzabranaAmortPodgrupa != null => OsSifarnikKarticaViewModel.ZaAgPod(IzabranaAmortPodgrupa),
            3 when IzabraniIzvor         != null => OsSifarnikKarticaViewModel.ZaIzvor(IzabraniIzvor),
            4 when IzabraniOsnov         != null => OsSifarnikKarticaViewModel.ZaOsnov(IzabraniOsnov),
            _ => null
        };
        if (vm == null) return;

        var win = new OsSifarnikKarticaWindow(vm);
        if (win.ShowDialog() == true)
            Poruka = $"Kartica sačuvana. Kliknite SAČUVAJ da zapišete u DBF.";
        else
        {
            // Korisnik otkazio — ukloni samo-dodati prazni red
            switch (AktivniTab)
            {
                case 0: if (IzabranaVrsta         != null) VrsteOs.Remove(IzabranaVrsta);                 break;
                case 1: if (IzabranaAmortGrupa    != null) AmortGrupe.Remove(IzabranaAmortGrupa);         break;
                case 2: if (IzabranaAmortPodgrupa != null) AmortPodgrupe.Remove(IzabranaAmortPodgrupa);   break;
                case 3: if (IzabraniIzvor         != null) IzvoriFinansiranja.Remove(IzabraniIzvor);      break;
                case 4: if (IzabraniOsnov         != null) OsnKoriscenja.Remove(IzabraniOsnov);           break;
            }
            Poruka = "Dodavanje otkazano.";
        }
    }

    // ═══ OBRIŠI ═══

    [RelayCommand]
    private void Obrisi()
    {
        var opis = AktivniTab switch
        {
            0 when IzabranaVrsta         != null => $"vrstu '{IzabranaVrsta.Vrsta}'",
            1 when IzabranaAmortGrupa    != null => $"grupu '{IzabranaAmortGrupa.Ag}'",
            2 when IzabranaAmortPodgrupa != null => $"podgrupu '{IzabranaAmortPodgrupa.AgPod}'",
            3 when IzabraniIzvor         != null => $"izvor '{IzabraniIzvor.Izvor}'",
            4 when IzabraniOsnov         != null => $"osnov '{IzabraniOsnov.OsnovKor}'",
            _ => null
        };

        if (opis == null) { Poruka = "Nije izabran red za brisanje."; return; }

        if (MessageBox.Show($"Brisanje: {opis}\n\nDa li ste sigurni?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        switch (AktivniTab)
        {
            case 0: VrsteOs.Remove(IzabranaVrsta!);                 break;
            case 1: AmortGrupe.Remove(IzabranaAmortGrupa!);         break;
            case 2: AmortPodgrupe.Remove(IzabranaAmortPodgrupa!);   break;
            case 3: IzvoriFinansiranja.Remove(IzabraniIzvor!);      break;
            case 4: OsnKoriscenja.Remove(IzabraniOsnov!);           break;
        }
        Poruka = $"Obrisan {opis}. Kliknite SAČUVAJ da sačuvate promenu.";
        _izmijenjeno = true;
    }

    // ═══ SAČUVAJ ═══

    [RelayCommand]
    private void Sacuvaj()
    {
        switch (AktivniTab)
        {
            case 0: SacuvajVrsteOs();           break;
            case 1: SacuvajAmortGrupe();        break;
            case 2: SacuvajAmortPodgrupe();     break;
            case 3: SacuvajIzvorFinansiranja(); break;
            case 4: SacuvajOsnKoriscenja();     break;
        }
        _izmijenjeno = false;
    }

    private void SacuvajVrsteOs()
    {
        var path = DbfPutanja("osvrsta.dbf");
        if (path == null) { Poruka = "osvrsta.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, VrsteOs.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "VRSTA"   => (object?)s.Vrsta,
                    "NAZIV"   => s.Naziv,
                    "PRENETO" => s.Preneto,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Vrste OS sačuvane ({VrsteOs.Count} zapisa).";
            _log.Information("osvrsta.dbf: sačuvano {Count} zapisa", VrsteOs.Count);
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; _log.Error(ex, "Greška pri snimanju osvrsta.dbf"); }
    }

    private void SacuvajAmortGrupe()
    {
        var path = DbfPutanja("osag.dbf");
        if (path == null) { Poruka = "osag.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, AmortGrupe.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "AG"      => (object?)s.Ag,
                    "AGSTOPA" => s.AgStopa,
                    "OPIS"    => s.Opis,
                    "VRSTA"   => s.Vrsta,
                    "PRENETO" => s.Preneto,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Amortizacione grupe sačuvane ({AmortGrupe.Count} zapisa).";
            _log.Information("osag.dbf: sačuvano {Count} zapisa", AmortGrupe.Count);
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; _log.Error(ex, "Greška pri snimanju osag.dbf"); }
    }

    private void SacuvajAmortPodgrupe()
    {
        var path = DbfPutanja("osagpod.dbf");
        if (path == null) { Poruka = "osagpod.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, AmortPodgrupe.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "AGPOD"   => (object?)s.AgPod,
                    "AG"      => s.Ag,
                    "OPIS"    => s.Opis,
                    "PRENETO" => s.Preneto,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Podgrupe amortizacije sačuvane ({AmortPodgrupe.Count} zapisa).";
            _log.Information("osagpod.dbf: sačuvano {Count} zapisa", AmortPodgrupe.Count);
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; _log.Error(ex, "Greška pri snimanju osagpod.dbf"); }
    }

    private void SacuvajIzvorFinansiranja()
    {
        var path = DbfPutanja("osizvorf.dbf");
        if (path == null) { Poruka = "osizvorf.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, IzvoriFinansiranja.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "IZVOR"   => (object?)s.Izvor,
                    "NAZIV"   => s.Naziv,
                    "PRENETO" => s.Preneto,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Izvor finansiranja sačuvan ({IzvoriFinansiranja.Count} zapisa).";
            _log.Information("osizvorf.dbf: sačuvano {Count} zapisa", IzvoriFinansiranja.Count);
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; _log.Error(ex, "Greška pri snimanju osizvorf.dbf"); }
    }

    private void SacuvajOsnKoriscenja()
    {
        var path = DbfPutanja("ososnk.dbf");
        if (path == null) { Poruka = "ososnk.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, OsnKoriscenja.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "OSNOVKOR" => (object?)s.OsnovKor,
                    "NAZIV"    => s.Naziv,
                    "PRENETO"  => s.Preneto,
                    "IDBR"     => (object?)s.IDBr,
                    _          => null
                });
            Poruka = $"Osnov korišćenja sačuvan ({OsnKoriscenja.Count} zapisa).";
            _log.Information("ososnk.dbf: sačuvano {Count} zapisa", OsnKoriscenja.Count);
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; _log.Error(ex, "Greška pri snimanju ososnk.dbf"); }
    }

    // ═══ KARTICA — otvara dijalog za uređivanje izabranog reda ═══

    [RelayCommand]
    private void Kartica()
    {
        var imaIzbor = AktivniTab switch
        {
            0 => IzabranaVrsta         != null,
            1 => IzabranaAmortGrupa    != null,
            2 => IzabranaAmortPodgrupa != null,
            3 => IzabraniIzvor         != null,
            4 => IzabraniOsnov         != null,
            _ => false
        };

        if (!imaIzbor)
        {
            Poruka = "Nije izabran red. Kliknite na red u tabeli pa zatim KARTICA.";
            return;
        }

        OtvoriKarticuZaTrenutni();
    }

    // ═══ POPISNA LISTA ═══

    [RelayCommand]
    private void PopisnaLista()
    {
        if (AktivniTab == 5) { Poruka = "Popisna lista nije dostupna za kontni plan."; return; }

        var tip = AktivniTab switch
        {
            0 => OsSifarnikPopisnaListaViewModel.Tip.VrsteOs,
            1 => OsSifarnikPopisnaListaViewModel.Tip.AmortGrupe,
            2 => OsSifarnikPopisnaListaViewModel.Tip.AmortPodgrupe,
            3 => OsSifarnikPopisnaListaViewModel.Tip.IzvoriFinansiranja,
            4 => OsSifarnikPopisnaListaViewModel.Tip.OsnKoriscenja,
            _ => OsSifarnikPopisnaListaViewModel.Tip.VrsteOs
        };

        var vm = new OsSifarnikPopisnaListaViewModel(
            tip,
            VrsteOs, AmortGrupe, AmortPodgrupe, IzvoriFinansiranja, OsnKoriscenja);

        var win = new OsSifarnikPopisnaListaWindow(vm);
        win.ShowDialog();
    }

    // ═══ NAVIGACIJA ═══

    [RelayCommand]
    private void Osvezi() => UcitajSve();

    [RelayCommand]
    private void Prvi() => PostaviTekuciRed(0);

    [RelayCommand]
    private void Zadnji()
    {
        var poslednji = TrenutniBrojRedova() - 1;
        if (poslednji >= 0) PostaviTekuciRed(poslednji);
    }

    [RelayCommand]
    private void Dole() => PostaviTekuciRed(TrenutniIndexReda() + 1);

    [RelayCommand]
    private void Gore() => PostaviTekuciRed(TrenutniIndexReda() - 1);

    private int TrenutniBrojRedova() =>
        AktivniTab switch
        {
            0 => VrsteOs.Count,
            1 => AmortGrupe.Count,
            2 => AmortPodgrupe.Count,
            3 => IzvoriFinansiranja.Count,
            4 => OsnKoriscenja.Count,
            5 => KontaOs.Count,
            _ => 0
        };

    private int TrenutniIndexReda() =>
        AktivniTab switch
        {
            0 => IzabranaVrsta         is null ? -1 : VrsteOs.IndexOf(IzabranaVrsta),
            1 => IzabranaAmortGrupa    is null ? -1 : AmortGrupe.IndexOf(IzabranaAmortGrupa),
            2 => IzabranaAmortPodgrupa is null ? -1 : AmortPodgrupe.IndexOf(IzabranaAmortPodgrupa),
            3 => IzabraniIzvor         is null ? -1 : IzvoriFinansiranja.IndexOf(IzabraniIzvor),
            4 => IzabraniOsnov         is null ? -1 : OsnKoriscenja.IndexOf(IzabraniOsnov),
            5 => IzabraniKonto         is null ? -1 : KontaOs.IndexOf(IzabraniKonto),
            _ => -1
        };

    private void PostaviTekuciRed(int index)
    {
        var ukupno = TrenutniBrojRedova();
        if (ukupno == 0) { Poruka = "Nema redova za navigaciju."; return; }

        index = Math.Clamp(index, 0, ukupno - 1);

        switch (AktivniTab)
        {
            case 0: IzabranaVrsta         = VrsteOs[index];           break;
            case 1: IzabranaAmortGrupa    = AmortGrupe[index];        break;
            case 2: IzabranaAmortPodgrupa = AmortPodgrupe[index];     break;
            case 3: IzabraniIzvor         = IzvoriFinansiranja[index]; break;
            case 4: IzabraniOsnov  = OsnKoriscenja[index]; break;
            case 5: IzabraniKonto  = KontaOs[index];       break;
        }

        Poruka = $"Pozicija: {index + 1}/{ukupno}.";
    }

    // ═══ UVOZ IZ PRAVILNIKA ═══

    [RelayCommand]
    private void UvozIzPravilnika()
    {
        var poruka = AmortGrupe.Count > 0 || AmortPodgrupe.Count > 0
            ? $"Tabele već imaju {AmortGrupe.Count} grupa i {AmortPodgrupe.Count} podgrupa.\n\nUvoz će ZAMENITI sve postojeće podatke.\n\nDa li ste sigurni?"
            : "Uvesti grupe I–V i sve podgrupe prema Pravilniku o amortizaciji za poreske svrhe?";

        if (MessageBox.Show(poruka, "Uvoz iz Pravilnika",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        AmortGrupe = new ObservableCollection<OsAgStavka>
        {
            new() { IDBr=1, Ag="1", AgStopa=2.5m,  Opis="Nekretnine i infrastrukturni objekti", Vrsta="", Preneto="N" },
            new() { IDBr=2, Ag="2", AgStopa=10m,   Opis="Transportna sredstva, mašine i oprema", Vrsta="", Preneto="N" },
            new() { IDBr=3, Ag="3", AgStopa=15m,   Opis="Alati, nameštaj i ostala osnovna sredstva", Vrsta="", Preneto="N" },
            new() { IDBr=4, Ag="4", AgStopa=20m,   Opis="Specijalizovana oprema i uređaji", Vrsta="", Preneto="N" },
            new() { IDBr=5, Ag="5", AgStopa=30m,   Opis="Kompjuteri, IT oprema i prateća sredstva", Vrsta="", Preneto="N" },
        };

        var podgrupe = new List<OsAgPodStavka>();
        int id = 1;

        void Dodaj(string ag, int br, string opis) =>
            podgrupe.Add(new OsAgPodStavka
            {
                IDBr    = id++,
                Ag      = ag,
                AgPod   = $"{ag}.{br:D2}",
                Opis    = opis,
                Preneto = "N"
            });

        // Grupa I — 2,5%
        Dodaj("1",  1, "Asfaltne površine");
        Dodaj("1",  2, "Piste aerodroma");
        Dodaj("1",  3, "Brane za akumulaciju vode");
        Dodaj("1",  4, "Cevovodi za gas");
        Dodaj("1",  5, "Pristanišne građevine");
        Dodaj("1",  6, "Elektrane");
        Dodaj("1",  7, "Elektroenergetski vodovi");
        Dodaj("1",  8, "Pokretne stepenice (eskalatori)");
        Dodaj("1",  9, "Hangari");
        Dodaj("1", 10, "Lukobrani");
        Dodaj("1", 11, "Marine");
        Dodaj("1", 12, "Mostovi");
        Dodaj("1", 13, "Nadvožnjaci i vijadukti");
        Dodaj("1", 14, "Naftovodi");
        Dodaj("1", 15, "Odvodne i napojne kanale");
        Dodaj("1", 16, "Parking površine");
        Dodaj("1", 17, "Putevi i autoputevi");
        Dodaj("1", 18, "Ribnjaci");
        Dodaj("1", 19, "Skladišta i rezervoari");
        Dodaj("1", 20, "Sportski objekti (stadioni, bazeni, sportske dvorane)");
        Dodaj("1", 21, "Silose za žito");
        Dodaj("1", 22, "Tuneli");
        Dodaj("1", 23, "Vodovodne i cevovodne mreže");
        Dodaj("1", 24, "Železnička infrastruktura");
        Dodaj("1", 25, "Zgrade");
        Dodaj("1", 26, "Sva ostala nepokretnost koja nije navedena");

        // Grupa II — 10%
        Dodaj("2",  1, "Vazduhoplovi");
        Dodaj("2",  2, "Automobili");
        Dodaj("2",  3, "Brodovi i druga plovila");
        Dodaj("2",  4, "Klima uređaji");
        Dodaj("2",  5, "Liftovi");
        Dodaj("2",  6, "Kotlovi");
        Dodaj("2",  7, "Brodski nameštaj");
        Dodaj("2",  8, "Medicinska oprema");
        Dodaj("2",  9, "Ograde");
        Dodaj("2", 10, "Kancelarijska oprema");
        Dodaj("2", 11, "Oprema za solarnu energiju");
        Dodaj("2", 12, "Električna, gasna, grejna i vodna infrastruktura");
        Dodaj("2", 13, "Železnička kola");
        Dodaj("2", 14, "Vinogradi");
        Dodaj("2", 15, "Voćnjaci");
        Dodaj("2", 16, "Nematerijalna ulaganja (koncesije, licence, patenti, žigovi, dizajni, autorska prava, franšize)");

        // Grupa III — 15%
        Dodaj("3",  1, "Alati i inventar");
        Dodaj("3",  2, "Autobusi");
        Dodaj("3",  3, "Oprema termoelektrana");
        Dodaj("3",  4, "Oprema za mlekare");
        Dodaj("3",  5, "Kase");
        Dodaj("3",  6, "Zabavni aparati");
        Dodaj("3",  7, "Rashladni uređaji za povrće");
        Dodaj("3",  8, "Kalkulatori");
        Dodaj("3",  9, "Kamioni i prikolice");
        Dodaj("3", 10, "Laboratorijska oprema");
        Dodaj("3", 11, "Mašine za čišćenje žita");
        Dodaj("3", 12, "Fotokopir aparati");
        Dodaj("3", 13, "Nameštaj");
        Dodaj("3", 14, "Istraživačka oprema");
        Dodaj("3", 15, "Betonare");
        Dodaj("3", 16, "Mobilni električni agregati (generatori)");
        Dodaj("3", 17, "Radarski sistemi");
        Dodaj("3", 18, "TV antene");
        Dodaj("3", 19, "Ostala osnovna sredstva koja nisu navedena u grupama II–V");

        // Grupa IV — 20%
        Dodaj("4",  1, "Nameštaj za vazduhoplove");
        Dodaj("4",  2, "Oprema za zaštitu od zagađenja vazduha i vode (nelicencirana)");
        Dodaj("4",  3, "Radio i TV emitujuća oprema");
        Dodaj("4",  4, "Oprema za bušenje nafte");
        Dodaj("4",  5, "Oprema za preradu ruda");
        Dodaj("4",  6, "Rezervni delovi za vazduhoplove");
        Dodaj("4",  7, "Telegrafska i telefonska oprema (žice i kablovi)");

        // Grupa V — 30%
        Dodaj("5",  1, "Vozila za iznajmljivanje; taxi vozila");
        Dodaj("5",  2, "Bilbordi");
        Dodaj("5",  3, "Putne i železničke barijere");
        Dodaj("5",  4, "Svetleće reklame");
        Dodaj("5",  5, "Elektronska oprema za obradu podataka (kompjuteri) i softver");
        Dodaj("5",  6, "IT infrastrukturna oprema (mreže, internet)");
        Dodaj("5",  7, "Filmovi");
        Dodaj("5",  8, "TV reklame i spotovi");
        Dodaj("5",  9, "Mobilna građevinska oprema");
        Dodaj("5", 10, "Kalupi za livenje");
        Dodaj("5", 11, "Bibliotečke knjige za iznajmljivanje");
        Dodaj("5", 12, "Industrijski noževi");
        Dodaj("5", 13, "Oprema za seču drveta");
        Dodaj("5", 14, "Tkanine (tepisi, zavese, draperije, tapiserije)");
        Dodaj("5", 15, "Mobilna električna oprema (bušilice, brusilice)");
        Dodaj("5", 16, "Mobilni kampovi");
        Dodaj("5", 17, "Čitači bar-kodova");
        Dodaj("5", 18, "Traktori");
        Dodaj("5", 19, "Uniforme");
        Dodaj("5", 20, "Novčićni video automati");
        Dodaj("5", 21, "Video kasete, CD, DVD i sl.");
        Dodaj("5", 22, "Priplodna stoka");

        AmortPodgrupe = new ObservableCollection<OsAgPodStavka>(podgrupe);
        _izmijenjeno  = true;
        Poruka = $"Uvezeno {AmortGrupe.Count} grupa i {AmortPodgrupe.Count} podgrupa iz Pravilnika. " +
                 "Kliknite SAČUVAJ da sačuvate u bazu (posebno za tab Grupe, posebno za tab Podgrupe).";
        _log.Information("Uvoz iz Pravilnika: {Grupe} grupa, {Podgrupe} podgrupa", AmortGrupe.Count, AmortPodgrupe.Count);
    }

    // ═══ HELPER ═══

    private string? DbfPutanja(string ime) => DbfHelper.NadjiDbf(_appState, ime);
}
