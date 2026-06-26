using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using Algoritam.WPF.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Text;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za šifarnik radnika — čita ldrad.dbf iz foldera izabrane firme.
/// Originalni FoxPro poziv: DO FORM LDRAD
/// </summary>
public partial class RadniciViewModel : ObservableObject
{
    private List<Radnik> _sviRadnici = [];
    private List<Dictionary<string, object?>> _rawDbfRecords = [];

    private string _folderPath = "";
    private string? _ldradDbfPath;
    private Radnik? _originalnaKopija;
    private string? _pocetniKljucZapisa;
    private string? _pocetniPotpisZapisa;
    private RecordLockHandle? _aktivniLock;

    [ObservableProperty] private ObservableCollection<Radnik> _filtovaniRadnici = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NijeURezimuIzmene))]
    [NotifyPropertyChangedFor(nameof(NaslovProzora))]
    [NotifyCanExecuteChangedFor(nameof(IzmeniCommand))]
    [NotifyCanExecuteChangedFor(nameof(SacuvajCommand))]
    [NotifyCanExecuteChangedFor(nameof(OtkaziIzmenuCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrviCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrethodniCommand))]
    [NotifyCanExecuteChangedFor(nameof(SledeciCommand))]
    [NotifyCanExecuteChangedFor(nameof(PoslednjiCommand))]
    [NotifyCanExecuteChangedFor(nameof(DodajCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrisiCommand))]
    [NotifyCanExecuteChangedFor(nameof(OsveziCommand))]
    [NotifyCanExecuteChangedFor(nameof(SrediRadniStazCommand))]
    [NotifyCanExecuteChangedFor(nameof(KopirajRedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportUExcelCommand))]
    private bool _jeURezimuIzmene;

    public bool NijeURezimuIzmene => !JeURezimuIzmene;
    public string NaslovProzora => JeURezimuIzmene ? "Šifarnik radnika *" : "Šifarnik radnika";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BrisiCommand))]
    [NotifyCanExecuteChangedFor(nameof(SrediRadniStazCommand))]
    [NotifyCanExecuteChangedFor(nameof(KopirajRedCommand))]
    private Radnik? _selektovani;
    [ObservableProperty] private int _trenutnaIndeks;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListaPrazan))]
    private int _ukupnoRadnika;
    [ObservableProperty] private string _statusInfo = "";

    public bool IsListaPrazan => UkupnoRadnika == 0;
    [ObservableProperty] private string _poruka = "";

    [ObservableProperty] private string _tekstIme = "";
    [ObservableProperty] private string _tekstEvidbroj = "";

    partial void OnTekstImeChanged(string value) => TraziIme();
    partial void OnTekstEvidbrojChanged(string value) => TraziEvidbroj();

    public RadniciViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(folderPath);
    }

    partial void OnSelektovaniChanged(Radnik? value)
    {
        if (value != null && FiltovaniRadnici.Count > 0)
        {
            TrenutnaIndeks = FiltovaniRadnici.IndexOf(value) + 1;
        }
        else
        {
            TrenutnaIndeks = 0;
        }
        AzurirajStatus();
    }

    private void AzurirajStatus()
    {
        UkupnoRadnika = FiltovaniRadnici.Count;
        StatusInfo = UkupnoRadnika > 0
            ? $"Radnik {TrenutnaIndeks} od {UkupnoRadnika}"
            : "Nema radnika";
    }

    private void UcitajPodatke(string folderPath)
    {
        OtpustiZakljucavanje();
        _sviRadnici.Clear();
        _rawDbfRecords.Clear();
        FiltovaniRadnici.Clear();

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Poruka = "Nije izabrana firma.";
            AzurirajStatus();
            return;
        }

        var dbfPath = PronadjiLdradDbfPath(folderPath);
        _ldradDbfPath = dbfPath;

        try
        {
            if (dbfPath == null)
            {
                Poruka = $"ldrad.dbf nije pronađen u: {folderPath}";
                AzurirajStatus();
                return;
            }

            _rawDbfRecords = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in _rawDbfRecords)
            {
                var brisanje = Str(z, "BRISANJE");
                if (brisanje.Equals("D", StringComparison.OrdinalIgnoreCase)) continue;
                var broj = Int(z, "BROJ");
                var (prezime, ime) = RazdvojiImePrezime(
                    Str(z, "PREZIME"),
                    Str(z, "IME"),
                    Str(z, "IME_PREZ"));
                var brojDana = PrviNenultiInt(Int(z, "BROJDANA"), Int(z, "DAN"));
                var brojMeseci = PrviNenultiInt(Int(z, "BROJMES"), Int(z, "MESEC"));
                var brojGodina = PrviNenultiInt(Int(z, "BROJGOD"), Int(z, "GODINA"));

                _sviRadnici.Add(new Radnik
                {
                    Broj            = broj,
                    Prezime         = prezime,
                    Ime             = ime,
                    EvidBroj        = PrviNePrazan(Str(z, "EVIDBROJ"), broj > 0 ? broj.ToString() : ""),
                    Sifra           = Str(z, "SIFRA"),
                    MaticniBr       = Str(z, "MATICNIBR"),
                    IdBroj          = Str(z, "IDBROJ"),
                    Pol             = Str(z, "POL"),
                    Adresa          = Str(z, "ADRESA"),
                    Posta           = Str(z, "POSTA"),
                    Mesto           = Str(z, "MESTO"),
                    Telefon         = Str(z, "TELEFON"),
                    RadnoMesto      = Str(z, "RADNOMES"),
                    Stepen          = Str(z, "STEPEN"),
                    SkolSprema      = Str(z, "SKOSPREMA"),
                    VrstaZap        = Str(z, "VRSTAZAP"),
                    DatumPrijema    = Dat(z, "DATPRI"),
                    DatumUgovora    = Dat(z, "DATUGOVOR"),
                    DatumZasnivanja = Dat(z, "DATZASNIV"),
                    DatumZaposlenja = Dat(z, "DATZAPOS"),
                    DatumOtkaza     = Dat(z, "DATOTKAZ"),
                    Koef            = Dec(z, "KOEF"),
                    KoefDod         = Dec(z, "KOEFDOD"),
                    KoefUkup        = Dec(z, "KOEFUKUP"),
                    StartBod        = Dec(z, "STARTBOD"),
                    Osnovica        = Dec(z, "OSNOVICA"),
                    OsnovBruto      = Dec(z, "OSNOVBRUTO"),
                    Staz            = Int(z, "STAZ"),
                    BenProc         = Dec(z, "BENPROC"),
                    ProcUvec        = Dec(z, "PROCUVEC"),
                    MinProc         = Dec(z, "MINPROC"),
                    Napomena        = Str(z, "NAPOMENA"),
                    Napomena2       = Str(z, "NAPOMENA2"),
                    Partija         = Str(z, "PARTIJA"),
                    Brisanje        = brisanje,
                    // Organizacija
                    MestoPoreza     = Str(z, "MESTOPL"),
                    PoslJedinica    = Str(z, "PJ"),
                    VrstaPrimanja   = Str(z, "VRSTAPRIM"),
                    Grupa           = Int(z, "GRUPA"),
                    Grupa1          = Int(z, "GRUPA1"),
                    MestoTroskova   = Str(z, "MESTOTRO"),
                    SifraBanke      = Str(z, "SIFRABAN"),
                    ZiroRacun       = Str(z, "ZIRORAC"),
                    // Obustave
                    SolidProc       = Dec(z, "SOLPROC"),
                    AlimentProc     = Dec(z, "ALIMPROC"),
                    SindProc1       = Dec(z, "SIND1PROC"),
                    SindProc2       = Dec(z, "SIND2PROC"),
                    Kasa            = Dec(z, "KASA"),
                    KasaRata        = Dec(z, "KASARATA"),
                    // Samodoprinos
                    SamodoprSifra   = Int(z, "SAMSIF"),
                    SamodoprProc    = Dec(z, "SAMOPROC"),
                    // Razno
                    Prevoz          = Str(z, "PREVOZ"),
                    DatMinulogRada  = Dat(z, "DATMIN"),
                    BrojDana        = brojDana,
                    BrojMeseci      = brojMeseci,
                    BrojGodina      = brojGodina,
                    ProcUmanjenja   = Dec(z, "PROCUMANJ"),
                    PorezFondZar    = Dec(z, "POREZFOND"),
                    Neaktivan       = Str(z, "NEAKTIVAN"),
                    NepunoRadno     = Str(z, "MFP8NEPUN"),
                    DatNezap        = Dat(z, "DATNEZAP"),
                    Porolaks        = Str(z, "POROLAKS"),
                    Email           = Str(z, "EMAIL"),
                        Roditelj        = Str(z, "RODITELJ"),
                    // Osiguranje
                    LboBroj          = Str(z, "LBOBROJ"),
                    ZkBroj           = Str(z, "ZKBROJ"),
                    DatumOsigOd      = Dat(z, "DATOSIG0"),
                    DatumOsigDo      = Dat(z, "DATOSIG1"),
                    OsnovOsiguranja  = Str(z, "OSNOVOSIG"),
                    RegBrojSocijalno = Str(z, "REGSOC"),
                    });
                }
                Poruka = $"Ucitano {_sviRadnici.Count} radnika iz DBF.";

            foreach (var r in _sviRadnici)
                FiltovaniRadnici.Add(r);

            if (FiltovaniRadnici.Count > 0)
                Selektovani = FiltovaniRadnici[0];
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri citanju: {ex.Message}";
        }

        AzurirajStatus();
    }

    // ── Edit mode commands ───────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(MozeIzmeni))]
    private void Izmeni()
    {
        if (Selektovani == null) return;

        var kljuc = NapraviKljucZapisa(Selektovani.Broj, Selektovani.EvidBroj);
        if (!PokusajZakljucavanje(kljuc, out var greska))
        {
            Poruka = greska;
            PrikaziGreskuZakljucavanja(greska);
            return;
        }

        _pocetniKljucZapisa = kljuc;
        var raw = PronadjiRawZapisPoKljucu(_rawDbfRecords, kljuc);
        _pocetniPotpisZapisa = raw != null
            ? DbfOptimisticConcurrency.ComputeRecordSignature(raw)
            : null;

        // Sacuvaj kopiju za otkazivanje
        _originalnaKopija = Selektovani.Kopiraj();
        JeURezimuIzmene = true;
        Poruka = "Rezim izmene - izmenite podatke i kliknite Sačuvaj.";
    }
    private bool MozeIzmeni() => Selektovani != null && !JeURezimuIzmene;

    [RelayCommand(CanExecute = nameof(MozeSacuvati))]
    private Task Sacuvaj()
    {
        if (Selektovani == null) return Task.CompletedTask;

        _ldradDbfPath ??= PronadjiLdradDbfPath(_folderPath);
        if (string.IsNullOrWhiteSpace(_ldradDbfPath) || !File.Exists(_ldradDbfPath))
        {
            Poruka = "ldrad.dbf nije pronađen. Nije moguće sačuvati.";
            return Task.CompletedTask;
        }

        try
        {
            if (Selektovani.Broj <= 0)
                Selektovani.Broj = PredloziNoviBroj();
            if (string.IsNullOrWhiteSpace(Selektovani.EvidBroj))
                Selektovani.EvidBroj = Selektovani.Broj.ToString(CultureInfo.InvariantCulture);
            if (Selektovani.StartBod == 0m)
                Selektovani.StartBod = Selektovani.KoefUkup != 0m ? Selektovani.KoefUkup : Selektovani.Koef;

            var diskZapisi = DbfReader.CitajSveZapise(_ldradDbfPath);
            var pocetniSaDiska = PronadjiRawZapisPoKljucu(diskZapisi, _pocetniKljucZapisa);

            if (!string.IsNullOrWhiteSpace(_pocetniPotpisZapisa))
            {
                if (pocetniSaDiska == null)
                {
                    PrijaviKonflikt("Zapis je u medjuvremenu obrisan ili vise ne postoji.");
                    return Task.CompletedTask;
                }

                var trenutniPotpis = DbfOptimisticConcurrency.ComputeRecordSignature(pocetniSaDiska);
                if (!string.Equals(trenutniPotpis, _pocetniPotpisZapisa, StringComparison.Ordinal))
                {
                    PrijaviKonflikt("Zapis je promenio drugi korisnik. Osveži podatke i ponovi izmenu.");
                    return Task.CompletedTask;
                }
            }

            if (PostojiSukobKljuca(diskZapisi, Selektovani, pocetniSaDiska))
            {
                // Novi zapis (nema pocetniKljuc) — automatski dodeli sledeci slobodan broj
                if (string.IsNullOrWhiteSpace(_pocetniKljucZapisa))
                {
                    var noviAuto = diskZapisi
                        .Select(z => Int(z, "BROJ"))
                        .DefaultIfEmpty(0)
                        .Max() + 1;
                    Selektovani.Broj = noviAuto;
                    Selektovani.EvidBroj = noviAuto.ToString(CultureInfo.InvariantCulture);
                    // Ponovi proveru sa novim brojem
                    if (PostojiSukobKljuca(diskZapisi, Selektovani, pocetniSaDiska))
                    {
                        PrijaviKonflikt("Record vec postoji (isti broj ili evidencioni broj). Osveži podatke i pokusaj ponovo.");
                        return Task.CompletedTask;
                    }
                }
                else
                {
                    PrijaviKonflikt("Record vec postoji (isti broj ili evidencioni broj). Osveži podatke i pokusaj ponovo.");
                    return Task.CompletedTask;
                }
            }

            // Pronadji ili kreiraj record koji treba azurirati
            var rawRec = pocetniSaDiska ?? PronadjiRawZapis(diskZapisi, Selektovani);
            if (rawRec == null)
            {
                rawRec = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                diskZapisi.Add(rawRec);
            }

            AzurirajRawZapis(rawRec, Selektovani);

            // Pisi sve zapise nazad u ldrad.dbf.
            // Ako EMAIL ne postoji, dodaj ga; ako postoji ali je prekratak, prosiri ga.
            var schema = OsigurajEmailKolonu(_ldradDbfPath, 60);
            DbfTableWriter.WriteTable(
                _ldradDbfPath,
                schema,
                diskZapisi,
                static (r, name) => r.TryGetValue(name, out var v) ? v : null);

            var broj = Selektovani.Broj;
            var evidBroj = Selektovani.EvidBroj;
            _originalnaKopija = null;
            _pocetniKljucZapisa = null;
            _pocetniPotpisZapisa = null;
            JeURezimuIzmene = false;
            OtpustiZakljucavanje();
            UcitajPodatke(_folderPath);
            Selektovani = FiltovaniRadnici.FirstOrDefault(r =>
                r.Broj == broj ||
                (!string.IsNullOrWhiteSpace(evidBroj) && string.Equals(r.EvidBroj, evidBroj, StringComparison.OrdinalIgnoreCase)));
            Poruka = Selektovani != null
                ? $"Sačuvano u ldrad.dbf: {Selektovani.Prezime} {Selektovani.Ime}"
                : $"Sačuvano u ldrad.dbf (broj {broj}).";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri cuvanju: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    private static Dictionary<string, object?>? PronadjiRawZapis(
        IReadOnlyList<Dictionary<string, object?>> zapisi,
        Radnik r)
    {
        if (r.Broj > 0)
        {
            var pobroju = zapisi.FirstOrDefault(z => Int(z, "BROJ") == r.Broj);
            if (pobroju != null) return pobroju;
        }
        if (!string.IsNullOrWhiteSpace(r.EvidBroj))
        {
            return zapisi.FirstOrDefault(z =>
                string.Equals(Str(z, "EVIDBROJ"), r.EvidBroj, StringComparison.OrdinalIgnoreCase));
        }
        return null;
    }

    private static void AzurirajRawZapis(Dictionary<string, object?> z, Radnik r)
    {
        z["BROJ"]       = (decimal)r.Broj;
        z["PREZIME"]    = r.Prezime;
        z["IME"]        = r.Ime;
        z["IME_PREZ"]   = $"{r.Prezime} {r.Ime}".Trim();
        z["EVIDBROJ"]   = r.EvidBroj;
        z["SIFRA"]      = r.Sifra;
        z["MATICNIBR"]  = r.MaticniBr;
        z["IDBROJ"]     = r.IdBroj;
        z["POL"]        = r.Pol;
        z["ADRESA"]     = r.Adresa;
        z["POSTA"]      = r.Posta;
        z["MESTO"]      = r.Mesto;
        z["TELEFON"]    = r.Telefon;
        z["RADNOMES"]   = r.RadnoMesto;
        z["STEPEN"]     = r.Stepen;
        z["SKOSPREMA"]  = r.SkolSprema;
        z["VRSTAZAP"]   = r.VrstaZap;
        z["DATPRI"]     = r.DatumPrijema;
        z["DATUGOVOR"]  = r.DatumUgovora;
        z["DATZASNIV"]  = r.DatumZasnivanja;
        z["DATZAPOS"]   = r.DatumZaposlenja;
        z["DATOTKAZ"]   = r.DatumOtkaza;
        z["KOEF"]       = r.Koef;
        z["KOEFDOD"]    = r.KoefDod;
        z["KOEFUKUP"]   = r.KoefUkup;
        z["STARTBOD"]   = r.StartBod;
        z["OSNOVICA"]   = r.Osnovica;
        z["OSNOVBRUTO"] = r.OsnovBruto;
        z["STAZ"]       = (decimal)r.Staz;
        z["BENPROC"]    = r.BenProc;
        z["PROCUVEC"]   = r.ProcUvec;
        z["MINPROC"]    = r.MinProc;
        z["NAPOMENA"]   = r.Napomena;
        z["NAPOMENA2"]  = r.Napomena2;
        z["PARTIJA"]    = r.Partija;
        z["BRISANJE"]   = r.Brisanje;
        z["MESTOPL"]    = r.MestoPoreza;
        z["PJ"]         = r.PoslJedinica;
        z["VRSTAPRIM"]  = r.VrstaPrimanja;
        z["GRUPA"]      = (decimal)r.Grupa;
        z["GRUPA1"]     = (decimal)r.Grupa1;
        z["MESTOTRO"]   = r.MestoTroskova;
        z["SIFRABAN"]   = r.SifraBanke;
        z["ZIRORAC"]    = r.ZiroRacun;
        z["SOLPROC"]    = r.SolidProc;
        z["ALIMPROC"]   = r.AlimentProc;
        z["SIND1PROC"]  = r.SindProc1;
        z["SIND2PROC"]  = r.SindProc2;
        z["KASA"]       = r.Kasa;
        z["KASARATA"]   = r.KasaRata;
        z["SAMSIF"]     = (decimal)r.SamodoprSifra;
        z["SAMOPROC"]   = r.SamodoprProc;
        z["PREVOZ"]     = r.Prevoz;
        z["DATMIN"]     = r.DatMinulogRada;
        z["BROJDANA"]   = (decimal)r.BrojDana;
        z["BROJMES"]    = (decimal)r.BrojMeseci;
        z["BROJGOD"]    = (decimal)r.BrojGodina;
        z["DAN"]        = r.BrojDana > 0 ? r.BrojDana.ToString(CultureInfo.InvariantCulture) : string.Empty;
        z["MESEC"]      = r.BrojMeseci > 0 ? r.BrojMeseci.ToString(CultureInfo.InvariantCulture) : string.Empty;
        z["GODINA"]     = r.BrojGodina > 0 ? r.BrojGodina.ToString(CultureInfo.InvariantCulture) : string.Empty;
        z["PROCUMANJ"]  = r.ProcUmanjenja;
        z["POREZFOND"]  = r.PorezFondZar;
        z["NEAKTIVAN"]  = r.Neaktivan;
        z["MFP8NEPUN"]  = r.NepunoRadno;
        z["DATNEZAP"]   = r.DatNezap;
        z["POROLAKS"]   = r.Porolaks;
        z["EMAIL"]      = r.Email;
        z["RODITELJ"]   = r.Roditelj;
        z["LBOBROJ"]    = r.LboBroj;
        z["ZKBROJ"]     = r.ZkBroj;
        z["DATOSIG0"]   = r.DatumOsigOd;
        z["DATOSIG1"]   = r.DatumOsigDo;
        z["OSNOVOSIG"]  = r.OsnovOsiguranja;
        z["REGSOC"]     = r.RegBrojSocijalno;
    }
    private bool MozeSacuvati() => JeURezimuIzmene;

    [RelayCommand(CanExecute = nameof(MozeOtkazati))]
    private void OtkaziIzmenu()
    {
        if (_originalnaKopija != null && Selektovani != null)
        {
            // Vrati originalne vrednosti
            Selektovani.VratiIz(_originalnaKopija);
            _originalnaKopija = null;
        }

        _pocetniKljucZapisa = null;
        _pocetniPotpisZapisa = null;
        OtpustiZakljucavanje();
        JeURezimuIzmene = false;
        Poruka = "Izmena otkazana.";
    }
    private bool MozeOtkazati() => JeURezimuIzmene;

    // ── Navigation commands ──────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(NijeURezimuIzmene))]
    private void Prvi()
    {
        if (FiltovaniRadnici.Count > 0)
            Selektovani = FiltovaniRadnici[0];
    }

    [RelayCommand(CanExecute = nameof(NijeURezimuIzmene))]
    private void Prethodni()
    {
        if (FiltovaniRadnici.Count == 0) return;
        var idx = Selektovani != null ? FiltovaniRadnici.IndexOf(Selektovani) : -1;
        if (idx > 0)
            Selektovani = FiltovaniRadnici[idx - 1];
    }

    [RelayCommand(CanExecute = nameof(NijeURezimuIzmene))]
    private void Sledeci()
    {
        if (FiltovaniRadnici.Count == 0) return;
        var idx = Selektovani != null ? FiltovaniRadnici.IndexOf(Selektovani) : -1;
        if (idx < FiltovaniRadnici.Count - 1)
            Selektovani = FiltovaniRadnici[idx + 1];
    }

    [RelayCommand(CanExecute = nameof(NijeURezimuIzmene))]
    private void Poslednji()
    {
        if (FiltovaniRadnici.Count > 0)
            Selektovani = FiltovaniRadnici[^1];
    }

    // ── Add command ──────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(NijeURezimuIzmene))]
    private void Dodaj()
    {
        var noviBroj = PredloziNoviBroj();
        string kljuc;
        const int maxPokusaja = 100;
        int pokusaj = 0;
        while (true)
        {
            kljuc = NapraviKljucZapisa(noviBroj, noviBroj.ToString(CultureInfo.InvariantCulture));
            if (PokusajZakljucavanje(kljuc, out _))
                break;
            if (++pokusaj >= maxPokusaja)
            {
                Poruka = "Nije moguće dodati novog radnika — previše istovremenih korisnika.";
                return;
            }
            noviBroj++;
        }

        var novi = new Radnik
        {
            Broj = noviBroj,
            EvidBroj = noviBroj.ToString(CultureInfo.InvariantCulture)
        };
        _sviRadnici.Add(novi);
        FiltovaniRadnici.Add(novi);
        Selektovani = novi;
        _originalnaKopija = novi.Kopiraj();
        _pocetniKljucZapisa = null;
        _pocetniPotpisZapisa = null;
        JeURezimuIzmene = true;
        Poruka = "Novi radnik - unesite podatke i kliknite Sačuvaj.";
        AzurirajStatus();
    }

    // ── Delete command ───────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(MozeBrisati))]
    private Task Brisi()
    {
        if (Selektovani == null) return Task.CompletedTask;
        var broj = Selektovani.Broj;
        var evidBroj = Selektovani.EvidBroj;
        var kljuc = NapraviKljucZapisa(broj, evidBroj);

        var potvrda = MessageBox.Show(
            $"Da li ste sigurni da želite da obrišete radnika:\n\n{Selektovani.Prezime} {Selektovani.Ime} (evid.br. {Selektovani.EvidBroj})?",
            "Brisanje radnika",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (potvrda != MessageBoxResult.Yes) return Task.CompletedTask;

        _ldradDbfPath ??= PronadjiLdradDbfPath(_folderPath);
        if (string.IsNullOrWhiteSpace(_ldradDbfPath) || !File.Exists(_ldradDbfPath))
        {
            Poruka = "ldrad.dbf nije pronađen. Nije moguće obrisati.";
            return Task.CompletedTask;
        }

        RecordLockHandle? lockHandle = null;

        try
        {
            if (!DbfOptimisticConcurrency.TryAcquireRecordLock(
                    _ldradDbfPath,
                    kljuc,
                    Environment.UserName,
                    out lockHandle,
                    out var lockGreska))
            {
                Poruka = lockGreska;
                MessageBox.Show(lockGreska, "Zapis je zakljucan", MessageBoxButton.OK, MessageBoxImage.Information);
                return Task.CompletedTask;
            }

            var diskZapisi = DbfReader.CitajSveZapise(_ldradDbfPath);
            var rawRec = PronadjiRawZapisPoKljucu(diskZapisi, kljuc);
            if (rawRec == null)
            {
                Poruka = "Record je vec obrisan ili promenjen od strane drugog korisnika.";
                UcitajPodatke(_folderPath);
                return Task.CompletedTask;
            }

            // Oznaci kao logicki obrisan (FoxPro BRISANJE = "D")
            rawRec["BRISANJE"] = "D";

            // Pisi sve zapise nazad
            var schema = DbfTableWriter.LoadSchema(_ldradDbfPath);
            DbfTableWriter.WriteTable(
                _ldradDbfPath,
                schema,
                diskZapisi,
                static (r, name) => r.TryGetValue(name, out var v) ? v : null);

            UcitajPodatke(_folderPath);
            Selektovani = FiltovaniRadnici.FirstOrDefault(r =>
                r.Broj == broj ||
                (!string.IsNullOrWhiteSpace(evidBroj) && string.Equals(r.EvidBroj, evidBroj, StringComparison.OrdinalIgnoreCase)));
            Poruka = $"Obrisan radnik: {broj}";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri brisanju: {ex.Message}";
        }
        finally
        {
            lockHandle?.Dispose();
        }

        return Task.CompletedTask;
    }
    private bool MozeBrisati() => NijeURezimuIzmene && Selektovani != null;

    // ── Search commands ──────────────────────────────────────────────────────

    [RelayCommand]
    private void TraziIme()
    {
        if (string.IsNullOrWhiteSpace(TekstIme))
        {
            OcistiPretragu();
            return;
        }

        var upit = TekstIme.Trim().ToLowerInvariant();
        var rezultati = _sviRadnici
            .Where(r => r.Prezime.ToLowerInvariant().Contains(upit)
                     || r.Ime.ToLowerInvariant().Contains(upit))
            .ToList();

        PrikaziFiltrirane(rezultati);
    }

    [RelayCommand]
    private void TraziEvidbroj()
    {
        if (string.IsNullOrWhiteSpace(TekstEvidbroj))
        {
            OcistiPretragu();
            return;
        }

        var upit = TekstEvidbroj.Trim();
        var rezultati = _sviRadnici
            .Where(r => r.EvidBroj.Equals(upit, StringComparison.OrdinalIgnoreCase))
            .ToList();

        PrikaziFiltrirane(rezultati);
    }

    [RelayCommand]
    private void OcistiPretragu()
    {
        TekstIme = "";
        TekstEvidbroj = "";
        PrikaziFiltrirane(_sviRadnici);
    }

    [RelayCommand]
    private void PronadjiPutanju()
    {
        var dbfPath = _ldradDbfPath ?? PronadjiLdradDbfPath(_folderPath);
        var poruka = string.IsNullOrWhiteSpace(dbfPath)
            ? $"LDRAD.DBF nije pronađen u folderu:\n{_folderPath}"
            : $"Radnici se čitaju iz DBF:\n{dbfPath}";

        MessageBox.Show(poruka, "Putanja tabele Radnici", MessageBoxButton.OK, MessageBoxImage.Information);
        Poruka = poruka.Replace('\n', ' ');

        if (!string.IsNullOrWhiteSpace(dbfPath))
            OtvoriFolderUExploreru(dbfPath!);
    }

    private void PrikaziFiltrirane(List<Radnik> lista)
    {
        // If filter yields 0 results, show all
        var prikazLista = lista.Count > 0 ? lista : _sviRadnici;

        FiltovaniRadnici.Clear();
        foreach (var r in prikazLista)
            FiltovaniRadnici.Add(r);

        Selektovani = FiltovaniRadnici.Count > 0 ? FiltovaniRadnici[0] : null;
        AzurirajStatus();
    }

    // ── Helper methods ───────────────────────────────────────────────────────

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s : string.Empty;

    private static decimal Dec(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0m;
        return v switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double d => (decimal)d,
            float f => (decimal)f,
            string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
            string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out var d) => d,
            _ => 0m
        };
    }

    private static int Int(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0;
        return v switch
        {
            int i => i,
            long l => (int)l,
            decimal d => (int)d,
            double d => (int)d,
            float f => (int)f,
            string s when int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) => i,
            string s when int.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out var i) => i,
            _ => 0
        };
    }

    private static DateTime? Dat(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is DateTime dt ? dt : (DateTime?)null;

    private static int ParseInt(string? value)
        => int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

    private static int PrviNenultiInt(params int[] kandidati)
    {
        foreach (var kandidat in kandidati)
        {
            if (kandidat != 0)
                return kandidat;
        }

        return 0;
    }

    private static string PrviNePrazan(params string?[] kandidati)
    {
        foreach (var kandidat in kandidati)
        {
            if (!string.IsNullOrWhiteSpace(kandidat))
                return kandidat.Trim();
        }

        return string.Empty;
    }

    private static (string Prezime, string Ime) RazdvojiImePrezime(
        string? prezime,
        string? ime,
        string? imePrezime)
    {
        var p = prezime?.Trim() ?? string.Empty;
        var i = ime?.Trim() ?? string.Empty;
        var ip = imePrezime?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(p) || !string.IsNullOrWhiteSpace(i))
            return (p, i);

        if (string.IsNullOrWhiteSpace(ip))
            return (string.Empty, string.Empty);

        if (ip.Contains(','))
        {
            var delovi = ip.Split(',', 2, StringSplitOptions.TrimEntries);
            var prez = delovi.Length > 0 ? delovi[0] : string.Empty;
            var im = delovi.Length > 1 ? delovi[1] : string.Empty;
            return (prez, im);
        }

        var reci = ip.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (reci.Length == 1)
            return (reci[0], string.Empty);

        var imeKandidata = reci[^1];
        var prezimeKandidata = string.Join(' ', reci.Take(reci.Length - 1));
        return (prezimeKandidata, imeKandidata);
    }

    private bool PokusajZakljucavanje(string kljuc, out string poruka)
    {
        _ldradDbfPath ??= PronadjiLdradDbfPath(_folderPath);
        if (string.IsNullOrWhiteSpace(_ldradDbfPath))
        {
            poruka = $"ldrad.dbf nije pronađen (pretrazeni folder: {_folderPath}).";
            return false;
        }

        if (DbfOptimisticConcurrency.TryAcquireRecordLock(
                _ldradDbfPath,
                kljuc,
                Environment.UserName,
                out var lockHandle,
                out poruka))
        {
            _aktivniLock = lockHandle;
            return true;
        }

        return false;
    }

    private static void PrikaziGreskuZakljucavanja(string poruka)
    {
        var naslov = poruka.Contains("nije pronađen", StringComparison.OrdinalIgnoreCase)
            ? "Nedostaje tabela Radnici"
            : "Zapis je zakljucan";

        var ikonica = naslov == "Zapis je zakljucan"
            ? MessageBoxImage.Information
            : MessageBoxImage.Warning;

        MessageBox.Show(poruka, naslov, MessageBoxButton.OK, ikonica);
    }

    private void OtpustiZakljucavanje()
    {
        _aktivniLock?.Dispose();
        _aktivniLock = null;
    }

    private void PrijaviKonflikt(string razlog)
    {
        OtpustiZakljucavanje();
        _pocetniKljucZapisa = null;
        _pocetniPotpisZapisa = null;
        _originalnaKopija = null;
        JeURezimuIzmene = false;
        Poruka = razlog;
        MessageBox.Show(razlog, "Konflikt pri snimanju", MessageBoxButton.OK, MessageBoxImage.Warning);
        UcitajPodatke(_folderPath);
    }

    private static string NapraviKljucZapisa(int broj, string? evidBroj)
    {
        if (broj > 0)
            return $"BROJ:{broj}";

        var evid = (evidBroj ?? string.Empty).Trim();
        return $"EVID:{evid.ToUpperInvariant()}";
    }

    private static Dictionary<string, object?>? PronadjiRawZapisPoKljucu(
        IReadOnlyList<Dictionary<string, object?>> zapisi,
        string? kljuc)
    {
        if (string.IsNullOrWhiteSpace(kljuc))
            return null;

        if (kljuc.StartsWith("BROJ:", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(kljuc[5..], NumberStyles.Any, CultureInfo.InvariantCulture, out var broj))
        {
            return zapisi.FirstOrDefault(z => Int(z, "BROJ") == broj);
        }

        if (kljuc.StartsWith("EVID:", StringComparison.OrdinalIgnoreCase))
        {
            var evid = kljuc[5..].Trim();
            return zapisi.FirstOrDefault(z => string.Equals(
                Str(z, "EVIDBROJ").Trim(),
                evid,
                StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private static bool PostojiSukobKljuca(
        IReadOnlyList<Dictionary<string, object?>> zapisi,
        Radnik kandidat,
        Dictionary<string, object?>? izuzetiZapis)
    {
        return zapisi.Any(z =>
        {
            if (ReferenceEquals(z, izuzetiZapis))
                return false;

            var istiBroj = kandidat.Broj > 0 && Int(z, "BROJ") == kandidat.Broj;
            var istiEvid = !string.IsNullOrWhiteSpace(kandidat.EvidBroj) &&
                           string.Equals(
                               Str(z, "EVIDBROJ").Trim(),
                               kandidat.EvidBroj.Trim(),
                               StringComparison.OrdinalIgnoreCase);
            return istiBroj || istiEvid;
        });
    }

    private int PredloziNoviBroj()
    {
        // Čitaj sa diska da dobijemo stvarni max u trenutku dodavanja
        // (sprečava konflikt kada oba računara istovremeno dodaju novog radnika)
        _ldradDbfPath ??= PronadjiLdradDbfPath(_folderPath);
        if (!string.IsNullOrWhiteSpace(_ldradDbfPath) && File.Exists(_ldradDbfPath))
        {
            try
            {
                var diskZapisi = DbfReader.CitajSveZapise(_ldradDbfPath);
                var maxNaDisku = diskZapisi
                    .Select(z => Int(z, "BROJ"))
                    .DefaultIfEmpty(0)
                    .Max();
                return maxNaDisku + 1;
            }
            catch { /* fallback na in-memory */ }
        }

        var max = _sviRadnici
            .Where(r => r.Broj > 0)
            .Select(r => r.Broj)
            .DefaultIfEmpty(0)
            .Max();

        return max + 1;
    }

    private static string? PronadjiLdradDbfPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return null;

        var kandidati = new List<string>
        {
            folderPath,
            Path.Combine(folderPath, "data00"),
            Path.Combine(folderPath, "01"),
            Path.Combine(folderPath, "data01")
        };

        var parent = Directory.GetParent(folderPath)?.FullName;
        if (!string.IsNullOrWhiteSpace(parent))
        {
            kandidati.Add(parent);
            kandidati.Add(Path.Combine(parent, "data00"));
            kandidati.Add(Path.Combine(parent, "01"));
            kandidati.Add(Path.Combine(parent, "data01"));
        }

        foreach (var kandidat in kandidati.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var found = PronadjiDbfCaseInsensitive(kandidat, "ldrad.dbf");
            if (!string.IsNullOrWhiteSpace(found))
                return found;
        }

        return null;
    }

    private static string? PronadjiDbfCaseInsensitive(string? folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return null;

        var exact = Path.Combine(folderPath, fileName);
        if (File.Exists(exact))
            return exact;

        return Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));
    }

    private static void OtvoriFolderUExploreru(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")
                {
                    UseShellExecute = true
                });
                return;
            }

            var folder = Directory.Exists(path)
                ? path
                : Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"")
                {
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignorisemo fallback greske otvaranja explorera.
        }
    }

    [RelayCommand(CanExecute = nameof(NijeURezimuIzmene))]
    private void Osvezi() => UcitajPodatke(_folderPath);

    private static DbfTableWriter.DbfSchema OsigurajEmailKolonu(string dbfPath, int minimalnaDuzina)
    {
        var schema = DbfTableWriter.LoadSchema(dbfPath);
        var emailPolje = schema.Fields.FirstOrDefault(f =>
            string.Equals(f.Name, "EMAIL", StringComparison.OrdinalIgnoreCase));

        if (emailPolje == null)
            return DbfTableWriter.EnsureFieldExists(dbfPath, "EMAIL", 'C', minimalnaDuzina);

        if (char.ToUpperInvariant(emailPolje.Type) == 'C' && emailPolje.Length >= minimalnaDuzina)
            return schema;

        var novaPolja = new List<DbfTableWriter.DbfField>(schema.Fields.Count);
        var offset = 1;

        foreach (var f in schema.Fields)
        {
            if (string.Equals(f.Name, "EMAIL", StringComparison.OrdinalIgnoreCase))
            {
                var novaDuzina = Math.Max(f.Length, minimalnaDuzina);
                novaPolja.Add(new DbfTableWriter.DbfField("EMAIL", 'C', novaDuzina, 0, offset));
                offset += novaDuzina;
                continue;
            }

            novaPolja.Add(new DbfTableWriter.DbfField(f.Name, f.Type, f.Length, f.Decimals, offset));
            offset += f.Length;
        }

        var novaSema = new DbfTableWriter.DbfSchema
        {
            TemplatePath = dbfPath,
            Version = schema.Version,
            CodePageMark = schema.CodePageMark,
            HeaderLength = (ushort)(32 + (novaPolja.Count * 32) + 1),
            RecordLength = (ushort)offset,
            HeaderBytes = NapraviHeader(schema, novaPolja, (ushort)(32 + (novaPolja.Count * 32) + 1), (ushort)offset),
            Fields = novaPolja,
            Encoding = schema.Encoding
        };

        var sviZapisi = DbfReader.CitajSveZapise(dbfPath);
        var tempPath = dbfPath + ".emailtmp";

        try
        {
            DbfTableWriter.WriteTable(
                tempPath,
                novaSema,
                sviZapisi,
                static (r, name) => r.TryGetValue(name, out var v) ? v : null);

            File.Move(tempPath, dbfPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }

            throw;
        }

        return DbfTableWriter.LoadSchema(dbfPath);
    }

    private static byte[] NapraviHeader(
        DbfTableWriter.DbfSchema originalSchema,
        IReadOnlyList<DbfTableWriter.DbfField> fields,
        ushort headerLength,
        ushort recordLength)
    {
        var header = new byte[headerLength];
        Array.Copy(originalSchema.HeaderBytes, 0, header, 0, Math.Min(32, originalSchema.HeaderBytes.Length));

        for (int i = 0; i < fields.Count; i++)
        {
            var f = fields[i];
            var basePos = 32 + (i * 32);
            var nameBytes = Encoding.ASCII.GetBytes(f.Name.ToUpperInvariant());
            Array.Copy(nameBytes, 0, header, basePos, Math.Min(nameBytes.Length, 11));
            header[basePos + 11] = (byte)char.ToUpperInvariant(f.Type);
            header[basePos + 16] = (byte)f.Length;
            header[basePos + 17] = (byte)f.Decimals;
        }

        header[32 + (fields.Count * 32)] = 0x0D;

        var hlBytes = BitConverter.GetBytes(headerLength);
        header[8] = hlBytes[0];
        header[9] = hlBytes[1];

        var rlBytes = BitConverter.GetBytes(recordLength);
        header[10] = rlBytes[0];
        header[11] = rlBytes[1];

        return header;
    }

    // ── Sredi radni staž ─────────────────────────────────────────────────────

    private bool ImaSelekcije() => Selektovani != null && NijeURezimuIzmene;

    [RelayCommand(CanExecute = nameof(ImaSelekcije))]
    private void SrediRadniStaz()
    {
        if (Selektovani == null) return;
        _ldradDbfPath ??= PronadjiLdradDbfPath(_folderPath);
        if (string.IsNullOrWhiteSpace(_ldradDbfPath) || !File.Exists(_ldradDbfPath))
        {
            Poruka = "ldrad.dbf nije pronađen.";
            return;
        }

        int noviStaz = IzracunajStaz(Selektovani);
        int stariStaz = Selektovani.Staz;

        var msg = $"Radnik: {Selektovani.Prezime} {Selektovani.Ime}\n" +
                  $"Dosadašnji staž: {stariStaz} god.\n" +
                  $"Izračunati staž:  {noviStaz} god.\n\n" +
                  $"Snimiti novi staž?";

        if (!ConfirmDialog.Pitaj(msg, "Sredi radni staž")) return;

        try
        {
            var diskZapisi = DbfReader.CitajSveZapise(_ldradDbfPath);
            var rawRec = PronadjiRawZapis(diskZapisi, Selektovani);
            if (rawRec == null) { Poruka = "Zapis nije pronađen na disku."; return; }

            rawRec["STAZ"] = (decimal)noviStaz;
            var schema = DbfTableWriter.LoadSchema(_ldradDbfPath);
            DbfTableWriter.WriteTable(_ldradDbfPath, schema, diskZapisi,
                static (r, name) => r.TryGetValue(name, out var v) ? v : null);

            Selektovani.Staz = noviStaz;
            Poruka = $"Staž ažuriran: {stariStaz} → {noviStaz} god. ({Selektovani.Prezime} {Selektovani.Ime})";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri snimanju staža: {ex.Message}";
        }
    }

    private static int IzracunajStaz(Radnik r)
    {
        // Prethodni staž: BrojGodina + BrojMeseci/12 + BrojDana/365
        int prevDani = r.BrojGodina * 365 + r.BrojMeseci * 30 + r.BrojDana;

        // Tekući staž: od datuma minulog rada do danas
        var refDatum = r.DatMinulogRada ?? r.DatumZasnivanja ?? r.DatumZaposlenja ?? r.DatumPrijema;
        int tekuciDani = refDatum.HasValue
            ? Math.Max(0, (int)(DateTime.Today - refDatum.Value).TotalDays)
            : 0;

        return (prevDani + tekuciDani) / 365;
    }

    // ── Kopiraj red ──────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(ImaSelekcije))]
    private void KopirajRed()
    {
        if (Selektovani == null) return;

        var noviBroj = PredloziNoviBroj();
        const int maxPokusaja = 100;
        int pokusaj = 0;
        while (true)
        {
            var kljuc = NapraviKljucZapisa(noviBroj, noviBroj.ToString(CultureInfo.InvariantCulture));
            if (PokusajZakljucavanje(kljuc, out _)) break;
            if (++pokusaj >= maxPokusaja)
            {
                Poruka = "Nije moguće kopirati — previše istovremenih korisnika.";
                return;
            }
            noviBroj++;
        }

        var kopija = Selektovani.Kopiraj();
        kopija.Broj = noviBroj;
        kopija.EvidBroj = noviBroj.ToString(CultureInfo.InvariantCulture);

        _sviRadnici.Add(kopija);
        FiltovaniRadnici.Add(kopija);
        Selektovani = kopija;
        _originalnaKopija = kopija.Kopiraj();
        _pocetniKljucZapisa = null;
        _pocetniPotpisZapisa = null;
        JeURezimuIzmene = true;
        Poruka = $"Kopija radnika — izmenite podatke i kliknite Sačuvaj.";
        AzurirajStatus();
    }

    // ── Export u Excel (CSV) ─────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(NijeURezimuIzmene))]
    private void ExportUExcel()
    {
        if (FiltovaniRadnici.Count == 0)
        {
            Poruka = "Nema radnika za export.";
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export radnika u Excel",
            Filter = "CSV fajl (*.csv)|*.csv",
            FileName = $"radnici_{DateTime.Today:yyyyMMdd}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Broj;Prezime;Ime;Evid.br.;JMBG;Radno mesto;Stepen;Staz;Koef;Osnov.bruto;Partija;Email");
            foreach (var r in FiltovaniRadnici)
            {
                sb.AppendLine(string.Join(";",
                    r.Broj.ToString(),
                    r.Prezime,
                    r.Ime,
                    r.EvidBroj,
                    r.MaticniBr,
                    r.RadnoMesto,
                    r.Stepen,
                    r.Staz.ToString(),
                    r.Koef.ToString("F3", CultureInfo.InvariantCulture),
                    r.OsnovBruto.ToString("F2", CultureInfo.InvariantCulture),
                    r.Partija,
                    r.Email));
            }

            File.WriteAllText(dlg.FileName, sb.ToString(), new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            Poruka = $"Exportovano {FiltovaniRadnici.Count} radnika → {Path.GetFileName(dlg.FileName)}";
            Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri exportu: {ex.Message}";
        }
    }
}

// ── Model ────────────────────────────────────────────────────────────────────

public class Radnik
{
    public int       Id               { get; set; }
    public int       Broj             { get; set; }
    public string    Prezime          { get; set; } = string.Empty;
    public string    Ime              { get; set; } = string.Empty;
    public string    EvidBroj         { get; set; } = string.Empty;
    public string    Sifra            { get; set; } = string.Empty;
    public string    MaticniBr        { get; set; } = string.Empty;
    public string    IdBroj           { get; set; } = string.Empty;
    public string    Pol              { get; set; } = string.Empty;
    public string    Adresa           { get; set; } = string.Empty;
    public string    Posta            { get; set; } = string.Empty;
    public string    Mesto            { get; set; } = string.Empty;
    public string    Telefon          { get; set; } = string.Empty;
    public string    RadnoMesto       { get; set; } = string.Empty;
    public string    Stepen           { get; set; } = string.Empty;
    public string    SkolSprema       { get; set; } = string.Empty;
    public string    VrstaZap         { get; set; } = string.Empty;
    public DateTime? DatumPrijema     { get; set; }
    public DateTime? DatumUgovora     { get; set; }
    public DateTime? DatumZasnivanja  { get; set; }
    public DateTime? DatumZaposlenja  { get; set; }
    public DateTime? DatumOtkaza      { get; set; }
    public decimal   Koef             { get; set; }
    public decimal   KoefDod          { get; set; }
    public decimal   KoefUkup         { get; set; }
    public decimal   StartBod         { get; set; }  // STARTBOD — bazno za obračun zarade (LDOBRACUN.PRG)
    public decimal   Osnovica         { get; set; }
    public decimal   OsnovBruto       { get; set; }
    public int       Staz             { get; set; }
    public decimal   BenProc          { get; set; }
    public decimal   ProcUvec         { get; set; }
    public decimal   MinProc          { get; set; }
    public string    Napomena         { get; set; } = string.Empty;
    public string    Napomena2        { get; set; } = string.Empty;
    public string    Partija          { get; set; } = string.Empty;
    public string    Brisanje         { get; set; } = string.Empty;

    // ── Organizacija ──────────────────────────────────────────────────
    public string    MestoPoreza      { get; set; } = string.Empty;  // MESTOPL
    public string    PoslJedinica     { get; set; } = string.Empty;  // PJ (Poslovna jedinica)
    public string    VrstaPrimanja    { get; set; } = string.Empty;  // VRSTAPRIM (B/P/I/R/U)
    public int       Grupa            { get; set; }                   // GRUPA
    public int       Grupa1           { get; set; }                   // GRUPA1
    public string    MestoTroskova    { get; set; } = string.Empty;  // MESTOTRO
    public string    SifraBanke       { get; set; } = string.Empty;  // SIFRABAN
    public string    ZiroRacun        { get; set; } = string.Empty;  // ZIRORAC

    // ── Obustave ──────────────────────────────────────────────────────
    public decimal   SolidProc        { get; set; }  // SOLPROC
    public decimal   AlimentProc      { get; set; }  // ALIMPROC
    public decimal   SindProc1        { get; set; }  // SIND1PROC
    public decimal   SindProc2        { get; set; }  // SIND2PROC
    public decimal   Kasa             { get; set; }  // KASA
    public decimal   KasaRata         { get; set; }  // KASARATA

    // ── Samodoprinos ──────────────────────────────────────────────────
    public int       SamodoprSifra    { get; set; }  // SAMSIF
    public decimal   SamodoprProc     { get; set; }  // SAMOPROC

    // ── Razno ─────────────────────────────────────────────────────────
    public string    Prevoz           { get; set; } = string.Empty;  // PREVOZ
    public DateTime? DatMinulogRada   { get; set; }  // DATMIN
    public int       BrojDana         { get; set; }  // BROJDANA
    public int       BrojMeseci       { get; set; }  // BROJMES
    public int       BrojGodina       { get; set; }  // BROJGOD
    public decimal   ProcUmanjenja    { get; set; }  // PROCUMANJ
    public decimal   PorezFondZar     { get; set; }  // POREZFOND
    public string    Neaktivan        { get; set; } = string.Empty;  // NEAKTIVAN
    public string    NepunoRadno      { get; set; } = string.Empty;  // MFP8NEPUN
    public DateTime? DatNezap         { get; set; }  // DATNEZAP
    public string    Porolaks         { get; set; } = string.Empty;  // POROLAKS
    public string    Email            { get; set; } = string.Empty;  // EMAIL
    public string    Roditelj         { get; set; } = string.Empty;  // RODITELJ

    // ── Osiguranje ────────────────────────────────────────────────────
    public string    LboBroj          { get; set; } = string.Empty;  // LBOBROJ
    public string    ZkBroj           { get; set; } = string.Empty;  // ZKBROJ
    public DateTime? DatumOsigOd      { get; set; }                   // DATOSIG0
    public DateTime? DatumOsigDo      { get; set; }                   // DATOSIG1
    public string    OsnovOsiguranja  { get; set; } = string.Empty;  // OSNOVOSIG
    public string    RegBrojSocijalno { get; set; } = string.Empty;  // REGSOC

    public Radnik Kopiraj() => (Radnik)MemberwiseClone();

    public void VratiIz(Radnik izvor)
    {
        Broj = izvor.Broj; Prezime = izvor.Prezime; Ime = izvor.Ime;
        EvidBroj = izvor.EvidBroj; Sifra = izvor.Sifra;
        MaticniBr = izvor.MaticniBr; IdBroj = izvor.IdBroj; Pol = izvor.Pol;
        Adresa = izvor.Adresa; Posta = izvor.Posta; Mesto = izvor.Mesto;
        Telefon = izvor.Telefon; RadnoMesto = izvor.RadnoMesto;
        Stepen = izvor.Stepen; SkolSprema = izvor.SkolSprema; VrstaZap = izvor.VrstaZap;
        DatumPrijema = izvor.DatumPrijema; DatumUgovora = izvor.DatumUgovora;
        DatumZasnivanja = izvor.DatumZasnivanja; DatumZaposlenja = izvor.DatumZaposlenja;
        DatumOtkaza = izvor.DatumOtkaza;
        Koef = izvor.Koef; KoefDod = izvor.KoefDod; KoefUkup = izvor.KoefUkup;
        StartBod = izvor.StartBod;
        Osnovica = izvor.Osnovica; OsnovBruto = izvor.OsnovBruto;
        Staz = izvor.Staz; BenProc = izvor.BenProc;
        ProcUvec = izvor.ProcUvec; MinProc = izvor.MinProc;
        Napomena = izvor.Napomena; Napomena2 = izvor.Napomena2;
        Partija = izvor.Partija; Brisanje = izvor.Brisanje;
        // Organizacija
        MestoPoreza = izvor.MestoPoreza; PoslJedinica = izvor.PoslJedinica;
        VrstaPrimanja = izvor.VrstaPrimanja; Grupa = izvor.Grupa; Grupa1 = izvor.Grupa1;
        MestoTroskova = izvor.MestoTroskova; SifraBanke = izvor.SifraBanke;
        ZiroRacun = izvor.ZiroRacun;
        // Obustave
        SolidProc = izvor.SolidProc; AlimentProc = izvor.AlimentProc;
        SindProc1 = izvor.SindProc1; SindProc2 = izvor.SindProc2;
        Kasa = izvor.Kasa; KasaRata = izvor.KasaRata;
        // Samodoprinos
        SamodoprSifra = izvor.SamodoprSifra; SamodoprProc = izvor.SamodoprProc;
        // Razno
        Prevoz = izvor.Prevoz; DatMinulogRada = izvor.DatMinulogRada;
        BrojDana = izvor.BrojDana; BrojMeseci = izvor.BrojMeseci; BrojGodina = izvor.BrojGodina;
        ProcUmanjenja = izvor.ProcUmanjenja; PorezFondZar = izvor.PorezFondZar;
        Neaktivan = izvor.Neaktivan; NepunoRadno = izvor.NepunoRadno;
        DatNezap = izvor.DatNezap; Porolaks = izvor.Porolaks;
        Email = izvor.Email; Roditelj = izvor.Roditelj;
        // Osiguranje
        LboBroj = izvor.LboBroj; ZkBroj = izvor.ZkBroj;
        DatumOsigOd = izvor.DatumOsigOd; DatumOsigDo = izvor.DatumOsigDo;
        OsnovOsiguranja = izvor.OsnovOsiguranja; RegBrojSocijalno = izvor.RegBrojSocijalno;
    }
}



