using Algoritam.Application;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Data;
using Algoritam.Infrastructure.Dbf;

namespace Algoritam.Infrastructure.Migration;

/// <summary>
/// Servis za migraciju podataka iz DBF fajlova u SQLite bazu za jednu firmu.
///
/// Čita LOZINKE.DBF, FIRMA.DBF i LDRAD.DBF iz zadatog foldera,
/// kreira algoritam.db i insertuje sve zapise.
///
/// Koristi DbfReader.CitajSveZapise() za čitanje DBF strukture.
/// </summary>
public class DbfToSqliteMigrator
{
    /// <summary>
    /// Migrira sve tri tabele (lozinke, firma, radnici) iz foldera u SQLite bazu.
    /// Baza se kreira u folderu zarade: {FIN}\F#\zarade\algoritam.db
    /// Ako baza već postoji, briše je i pravi ponovo (clean migration).
    /// </summary>
    /// <param name="folderPath">Putanja do foldera firme (npr. "C:\FIN\F1").</param>
    public async Task MigrujFirmuAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder firme nije pronađen: {folderPath}");

        var dbPath = ZaradePaths.GetDbPath(folderPath);
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        // Obriši postojeću bazu ako postoji (clean migration)
        if (File.Exists(dbPath))
            File.Delete(dbPath);

        using var context = new FirmaDbContext(dbPath);

        // Kreiraj sve tabele
        await context.Database.EnsureCreatedAsync();

        // Migriraj svaku tabelu
        await MigrujKorisnikeAsync(context, folderPath);
        await MigrujFirmuAsync(context, folderPath);
        await MigrujLdParametarAsync(context, folderPath);
        await MigrujLdObracunAsync(context, folderPath);
        await MigrujLdPodAsync(context, folderPath);
        await MigrujLdSpisAsync(context, folderPath);
        await MigrujLdKontoSablonAsync(context, folderPath);
        await MigrujLdKnjizenjeAsync(context, folderPath);
        await MigrujRadnikeAsync(context, folderPath);
    }

    // ══════════════════════════════════════════════════════════════════
    //  LOZINKE.DBF -> Korisnici
    // ══════════════════════════════════════════════════════════════════

    private static async Task MigrujKorisnikeAsync(FirmaDbContext context, string folderPath)
    {
        var putanja = PronadjiDbf(folderPath, "lozinke.dbf");
        if (putanja == null) return; // Tabela ne postoji u ovom folderu — preskačemo

        var zapisi = DbfReader.CitajSveZapise(putanja);

        foreach (var z in zapisi)
        {
            var korisnik = new Korisnik
            {
                Pas           = z.Str("PAS"),
                KorisnikIme   = z.Str("KORISNIK"),
                KorisnikIme2  = z.Str("KORIME"),
                Lozinka       = z.Str("LOZINKA"),
                Aktivan       = z.Str("AKTIVAN").Equals("D", StringComparison.OrdinalIgnoreCase),
                JeSupervizor  = z.Int("PASSNIVO") >= 1,

                // Nivo pristupa
                PravaNivo = z.Int("PASSNIVO"),

                // Prava pristupa — N(1) u DBF-u, 0 ili 1
                PassGk    = z.Int("PASSGK") != 0,
                PassAn    = z.Int("PASSAN") != 0,
                PassBl    = z.Int("PASSBL") != 0,
                PassTv    = z.Int("PASSTV") != 0,
                PassTvRad = z.Int("PASSTVRA") != 0,
                PassTvKal = z.Int("PASSTVKAL") != 0,
                PassTvRac = z.Int("PASSTVRAC") != 0,
                PassTvNiv = z.Int("PASSTVNIV") != 0,
                PassTm    = z.Int("PASSTM") != 0,
                PassTmRad = z.Int("PASSTMRA") != 0,
                PassTmKal = z.Int("PASSTMKAL") != 0,
                PassTmRac = z.Int("PASSTMRAC") != 0,
                PassTmNiv = z.Int("PASSTMNIV") != 0,
                PassUs    = z.Int("PASSUS") != 0,
                PassOs    = z.Int("PASSOS") != 0,
                PassLd    = z.Int("PASSLD") != 0,
                PassPro   = z.Int("PASSPRO") != 0,
                PassOst   = z.Int("PASSOST") != 0,
                PassPrn   = z.Int("PASSPRN") != 0,
                PassProf  = z.Int("PASSPROF") != 0,
                PassDel   = z.Int("PASSDEL") != 0,

                // Podešavanja
                DatumPrijave = z.Dat("DATUM"),
                VremePocetka = z.Str("VREME0"),
                VremeKraja   = z.Str("VREME1"),
                Slike        = z.Str("SLIKE"),
                Magacin      = z.Int("MAGACIN"),
                Putanja      = z.Str("PUTANJA"),
                Foxy         = z.Str("FOXY"),
                PdfPrint     = z.Str("PDFPRINT"),
                Preneto      = z.Str("PRENETO"),
                Idbr         = z.Long("IDBR"),
            };

            context.Korisnici.Add(korisnik);
        }

        await context.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════════════════════════
    //  FIRMA.DBF -> Firma
    // ══════════════════════════════════════════════════════════════════

    private static async Task MigrujFirmuAsync(FirmaDbContext context, string folderPath)
    {
        var putanja = PronadjiDbf(folderPath, "firma.dbf");
        if (putanja == null) return;

        var zapisi = DbfReader.CitajSveZapise(putanja);

        // Firma.dbf obično ima tačno 1 zapis
        foreach (var z in zapisi)
        {
            var firma = new Firma
            {
                Id = 1, // Uvek 1 za jedini zapis

                // Naziv
                Naziv  = z.Str("FIME"),
                Naziv2 = z.Str("FIME2"),
                Baza   = z.Str("FBAZA"),

                // Adresa
                PostanskiBroj = z.Str("FPOS"),
                Mesto         = z.Str("FMES"),
                Ulica         = z.Str("FUL"),
                BrojUlice     = z.Str("FULBR"),
                Republika     = z.Str("FREPUB"),
                Drzava        = z.Str("FDRZAVA"),

                // Žiro računi
                ZiroRacun          = z.Str("FZIRO"),
                ZiroRacun2         = z.Str("FZIRO2"),
                ZiroRacun3         = z.Str("FZIRO3"),
                ZiroRacun4         = z.Str("FZIRO4"),
                ZiroRacun5         = z.Str("FZIRO5"),
                ZiroRacun6         = z.Str("FZIRO6"),
                ZiroRacunBolovanje = z.Str("FZIROBOL"),
                ZiroRacunDevizni   = z.Str("FZIRODEV"),

                // Kontrolni kodovi
                KontrolniKodZiro1         = z.Str("FKONZIRO1"),
                KontrolniKodZiro2         = z.Str("FKONZIRO2"),
                KontrolniKodZiro3         = z.Str("FKONZIRO3"),
                KontrolniKodZiro4         = z.Str("FKONZIRO4"),
                KontrolniKodZiro5         = z.Str("FKONZIRO5"),
                KontrolniKodZiro6         = z.Str("FKONZIRO6"),
                KontrolniKodZiroDevizni   = z.Str("FKONZIROD"),
                KontrolniKodZiroBolovanje = z.Str("FKONZIROB"),

                // Banke — nazivi
                Banka1         = z.Str("FBANKA"),
                Banka2         = z.Str("FBANKA2"),
                Banka3         = z.Str("FBANKA3"),
                Banka4         = z.Str("FBANKA4"),
                Banka5         = z.Str("FBANKA5"),
                Banka6         = z.Str("FBANKA6"),
                BankaBolovanje = z.Str("FBANKAB"),
                BankaDevizna   = z.Str("FBANKAD"),

                // Banke — šifre
                BankaSifra1         = z.Str("FBANSIF"),
                BankaSifra2         = z.Str("FBANSIF2"),
                BankaSifra3         = z.Str("FBANSIF3"),
                BankaSifra4         = z.Str("FBANSIF4"),
                BankaSifra5         = z.Str("FBANSIF5"),
                BankaSifra6         = z.Str("FBANSIF6"),
                BankaSifraBolovanje = z.Str("FBANSIFB"),
                BankaSifraDevizna   = z.Str("FBANSIFD"),

                // Kontakt
                Telefon1 = z.Str("FTEL"),
                Telefon2 = z.Str("FTEL2"),
                Telefon3 = z.Str("FTEL3"),
                Fax1     = z.Str("FFAX"),
                Fax2     = z.Str("FFAX2"),
                Fax3     = z.Str("FFAX3"),
                Email    = z.Str("FEMAIL"),
                Web      = z.Str("FVEB"),

                // Identifikatori
                SifraDelatnosti  = z.Str("FSIF"),
                NazivDelatnosti  = z.Str("FNAZD"),
                SdkKod           = z.Str("FSDK"),
                Maticni          = z.Str("FMAT"),
                Pib              = z.Str("FPOR"),
                DopunskiPorBroj  = z.Str("FDOP"),
                Vlasnik          = z.Str("FVLAST"),
                Agencija         = z.Str("FAGENC"),
                FpaSlrn          = z.Str("FFPASLRN"),
                Opstina          = z.Str("FOPS"),

                // Datumi
                DatumOsnivanja         = z.Dat("FDAT0"),
                DatumPrestanka         = z.Dat("FDAT1"),
                DatumObrade            = z.Dat("FDATOBR"),
                DatumRegistracije      = z.Dat("FDATREG"),
                DatumUpisa             = z.Dat("FDATUPIS"),
                DatumPdv               = z.Dat("FDATPDV"),

                // Registracioni brojevi
                RegBrojSocijalno   = z.Str("FREGSOC"),
                RegBrojZdravstveno = z.Str("FREGZDR"),
                SudskiRegistar     = z.Str("FREGSUD"),
                RegNaziv           = z.Str("FREGNAZ"),

                // Vlasnik
                PibSavetnika      = z.Str("FPIBSAV"),
                JmbgVlasnika1     = z.Str("FJMBG1"),
                MbSavetnika       = z.Str("FMBSAV"),
                JmbgVlasnika      = z.Str("FJMBG"),
                JmbgKontaktBroj   = z.Str("FJMBGKONBR"),
                RepublikaKod      = z.Str("FREP"),
                PorBrojRepublicke = z.Str("FPORREP"),
                OdgovornoLice     = z.Str("FOSOBA"),
                UplaceniKapital   = z.Str("FUPLACENI"),
                UpisaniKapital    = z.Str("FUPISANI"),
                Naznaka           = z.Str("FNAZNAKA"),
                SwiftKod          = z.Str("FSWIFT"),

                // Poreske oznake
                PdvObveznik           = z.Str("FPDV"),
                OrganizacioniOblik    = z.Str("FOBLIK"),
                ZdravstvenaUstanova   = z.Str("FZDRAVUST"),
                ProdajniCentar        = z.Str("FPRODC"),
                ZatvorenaGodina       = z.Str("FZATVGOD"),
                DatumZatvaranjaPerioda = z.Dat("FZATVPER"),
                BudzetskiKorisnik     = z.Str("FBUDZKOR"),

                // Konta
                KontoGotovina = z.Str("KGOTOVINA"),
                KontoCek      = z.Str("KCEK"),
                KontoVirman   = z.Str("KVIRMAN"),
                KontoKartica  = z.Str("KKARTICA"),
                KontoOstalo   = z.Str("KOSTALO"),

                // Ostalo
                SifraFizickogLica     = z.Str("SIFFIZLI"),
                Pumpe                 = z.Str("FPUMPE"),
                Sn                    = z.Str("FSN"),
                OznakaJavnogPreduzeca = z.Str("OZNAKAJP"),
                Jbbk                  = z.Str("JBBK"),

                // Latinica
                NazivLatinican     = z.Str("FIMEC"),
                MestoLatinicom     = z.Str("FMESC"),
                UlicaLatinicom     = z.Str("FULC"),
                RepublikaLatinicom = z.Str("FREPUBC"),
                OpstinaLatinicom   = z.Str("FOPSC"),

                // System
                Preneto   = z.Str("PRENETO"),
                Idbr      = z.Long("IDBR"),
                Jbkjs     = z.Str("JBKJS"),
                BrojPoste = z.Str("FBRPOSTA"),
            };

            context.Firma.Add(firma);
            break; // Očekujemo samo 1 zapis
        }

        await context.SaveChangesAsync();
    }

    private static async Task MigrujLdParametarAsync(FirmaDbContext context, string folderPath)
    {
        var putanja = PronadjiDbf(folderPath, "ldparam.dbf");
        if (putanja == null)
        {
            context.LdParametri.Add(LdParametarDefaults.Kreiraj());
            await context.SaveChangesAsync();
            return;
        }

        var zapisi = DbfReader.CitajSveZapise(putanja);
        var z = zapisi.FirstOrDefault();
        if (z == null)
        {
            context.LdParametri.Add(LdParametarDefaults.Kreiraj());
            await context.SaveChangesAsync();
            return;
        }

        var p = new LdParametar
        {
            Id = 1,
            RedniBr = z.Int("REDNIBR"),
            Mesec = z.Int("MESEC"),
            Nazmes = z.Str("NAZMES"),
            Redispl = z.Int("REDISPL"),
            Dana = z.Int("DANA"),
            Cmes = z.Int("CMES"),
            Cpraz = z.Int("CPRAZ"),
            Czakon = z.Int("CZAKON"),
            Procnoc = z.Dec("PROCNOC") ?? 0m,
            Procprod = z.Dec("PROCPROD") ?? 0m,
            Procpraz = z.Dec("PROCPRAZ") ?? 0m,
            Procned = z.Dec("PROCNED") ?? 0m,
            Procmin = z.Dec("PROCMIN") ?? 0m,
            Procsus = z.Dec("PROCSUS") ?? 0m,
            Ekoefs = z.Dec("EKOEFS") ?? 0m,
            Minnac = z.Int("MINNAC"),
            Procbol = z.Dec("PROCBOL") ?? 0m,
            Procplac = z.Dec("PROCPLAC") ?? 0m,
            Prosbruto = z.Dec("PROSBRUTO") ?? 0m,
            Minimal = z.Dec("MINIMAL") ?? 0m,
            Ben1 = z.Dec("BEN1") ?? 0m,
            Ben2 = z.Dec("BEN2") ?? 0m,
            Ben3 = z.Dec("BEN3") ?? 0m,
            Ben4 = z.Dec("BEN4") ?? 0m,
            Isplata = z.Int("ISPLATA"),
            Sisplata = z.Int("SISPLATA"),
            Doppr1 = z.Dec("DOPPR1") ?? 0m,
            Dopzr1 = z.Dec("DOPZR1") ?? 0m,
            Dopnr1 = z.Dec("DOPNR1") ?? 0m,
            Doppf1 = z.Dec("DOPPF1") ?? 0m,
            Dopzf1 = z.Dec("DOPZF1") ?? 0m,
            Dopnf1 = z.Dec("DOPNF1") ?? 0m,
            Doppr2 = z.Dec("DOPPR2") ?? 0m,
            Dopzr2 = z.Dec("DOPZR2") ?? 0m,
            Dopnr2 = z.Dec("DOPNR2") ?? 0m,
            Doppf2 = z.Dec("DOPPF2") ?? 0m,
            Dopzf2 = z.Dec("DOPZF2") ?? 0m,
            Dopnf2 = z.Dec("DOPNF2") ?? 0m,
            Doppr3 = z.Dec("DOPPR3") ?? 0m,
            Dopzr3 = z.Dec("DOPZR3") ?? 0m,
            Dopnr3 = z.Dec("DOPNR3") ?? 0m,
            Doppf3 = z.Dec("DOPPF3") ?? 0m,
            Dopzf3 = z.Dec("DOPZF3") ?? 0m,
            Dopnf3 = z.Dec("DOPNF3") ?? 0m,
            Doppr4 = z.Dec("DOPPR4") ?? 0m,
            Dopzr4 = z.Dec("DOPZR4") ?? 0m,
            Dopnr4 = z.Dec("DOPNR4") ?? 0m,
            Doppf4 = z.Dec("DOPPF4") ?? 0m,
            Dopzf4 = z.Dec("DOPZF4") ?? 0m,
            Dopnf4 = z.Dec("DOPNF4") ?? 0m,
            Doppr5 = z.Dec("DOPPR5") ?? 0m,
            Dopzr5 = z.Dec("DOPZR5") ?? 0m,
            Dopnr5 = z.Dec("DOPNR5") ?? 0m,
            Doppf5 = z.Dec("DOPPF5") ?? 0m,
            Dopzf5 = z.Dec("DOPZF5") ?? 0m,
            Dopnf5 = z.Dec("DOPNF5") ?? 0m,
            Procpor = z.Dec("PROCPOR") ?? 0m,
            S1 = z.Str("S1"),
            Sdin1 = z.Dec("SDIN1") ?? 0m,
            S3 = z.Str("S3"),
            Sdin3 = z.Dec("SDIN3") ?? 0m,
            S4 = z.Str("S4"),
            Sdin4 = z.Dec("SDIN4") ?? 0m,
            S5 = z.Str("S5"),
            Sdin5 = z.Dec("SDIN5") ?? 0m,
            S6 = z.Str("S6"),
            Sdin6 = z.Dec("SDIN6") ?? 0m,
            S71 = z.Str("S71"),
            Sdin71 = z.Dec("SDIN71") ?? 0m,
            S72 = z.Str("S72"),
            Sdin72 = z.Dec("SDIN72") ?? 0m,
            S8 = z.Str("S8"),
            Sdin8 = z.Dec("SDIN8") ?? 0m,
            Komoraj = z.Dec("KOMORAJ") ?? 0m,
            Komoras = z.Dec("KOMORAS") ?? 0m,
            Komorar = z.Dec("KOMORAR") ?? 0m,
            Smesec = z.Int("SMESEC"),
            Snazmes = z.Str("SNAZMES"),
            Sredispl = z.Int("SREDISPL"),
            Cenarada = z.Dec("CENARADA") ?? 0m,
            Kd1 = z.Str("KD1"),
            Kd4 = z.Str("KD4"),
            Kd9 = z.Str("KD9"),
            Kd12 = z.Str("KD12"),
            Kd20 = z.Str("KD20"),
            Kd22 = z.Str("KD22"),
            Kd24 = z.Str("KD24"),
            Kd25 = z.Str("KD25"),
            Kd27 = z.Str("KD27"),
            Kd28 = z.Str("KD28"),
            Dat1 = z.Dat("DAT1"),
            Dat2 = z.Dat("DAT2"),
            Dat3 = z.Dat("DAT3"),
            Dat4 = z.Dat("DAT4"),
            Godina = z.Str("GODINA"),
            Nazp1 = z.Str("NAZP1"),
            Nazp2 = z.Str("NAZP2"),
            Nazp3 = z.Str("NAZP3"),
            Nazp4 = z.Str("NAZP4"),
            Nazp5 = z.Str("NAZP5"),
            Nazp5ter = z.Str("NAZP5TER"),
            Nazo1 = z.Str("NAZO1"),
            Nazo2 = z.Str("NAZO2"),
            Nazo3 = z.Str("NAZO3"),
            Nazo4 = z.Str("NAZO4"),
            Nazo5 = z.Str("NAZO5"),
            Nazo6 = z.Str("NAZO6"),
            Neoporez = z.Dec("NEOPOREZ") ?? 0m,
            Neoporezp = z.Dec("NEOPOREZP") ?? 0m,
            Decimale = z.Str("DECIMALE"),
            Aktivrac = z.Int("AKTIVRAC"),
            Datpocdel = z.Dat("DATPOCDEL"),
            Brzap0 = z.Int("BRZAP0"),
            Brzap1 = z.Int("BRZAP1"),
            Brzap2 = z.Int("BRZAP2"),
            Srazpor = z.Str("SRAZPOR"),
            Dinsat = z.Dec("DINSAT") ?? 0m,
            Tosat = z.Dec("TOSAT") ?? 0m,
            Regsat = z.Dec("REGSAT") ?? 0m,
            Konacna = z.Str("KONACNA"),
            Vrstaplate = z.Str("VRSTAPLATE"),
            Arhiva = z.Str("ARHIVA"),
            Arhiva2 = z.Str("ARHIVA2"),
            Datod = z.Dat("DATOD"),
            Datdo = z.Dat("DATDO"),
            Solporod1 = z.Dec("SOLPOROD1") ?? 0m,
            Solpordo1 = z.Dec("SOLPORDO1") ?? 0m,
            Solproc1 = z.Dec("SOLPROC1") ?? 0m,
            Solporod2 = z.Dec("SOLPOROD2") ?? 0m,
            Solpordo2 = z.Dec("SOLPORDO2") ?? 0m,
            Solproc2 = z.Dec("SOLPROC2") ?? 0m,
            Bkproc = z.Dec("BKPROC") ?? 0m,
            Bkzastita = z.Dec("BKZASTITA") ?? 0m,
            Bknacin = z.Str("BKNACIN"),
            Nakpos = z.Str("NAKPOS"),
            Preneto = z.Str("PRENETO"),
            Idbr = z.Long("IDBR"),
            Priprav = z.Dec("PRIPRAV") ?? 0m,
            Priprav1 = z.Dec("PRIPRAV1") ?? 0m,
            Priprav2 = z.Dec("PRIPRAV2") ?? 0m
        };

        context.LdParametri.Add(p);
        await context.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════════════════════════
    //  LDRAD.DBF -> Radnici
    // ══════════════════════════════════════════════════════════════════

    private static async Task MigrujLdObracunAsync(FirmaDbContext context, string folderPath)
    {
        var putanja = PronadjiDbf(folderPath, "ld.dbf");
        if (putanja == null)
            return;

        var zapisi = DbfReader.CitajSveZapise(putanja);
        foreach (var z in zapisi)
        {
            context.LdObracunStavke.Add(new LdObracunStavka
            {
                Broj = z.Int("BROJ"),
                Sifraprih = z.Str("SIFRAPRIH"),
                ImePrez = z.Str("IME_PREZ"),
                Bruto = z.Dec("BRUTO") ?? 0m,
                Porez = z.Dec("POREZ") ?? 0m,
                Neto = z.Dec("NETO") ?? 0m,
                Prevoz = z.Dec("PREVOZ") ?? 0m,
                Zaisplatu = z.Dec("ZAISPLATU") ?? 0m,
                Mesec = z.Int("MESEC"),
                Isplata = z.Int("ISPLATA"),
                Nazmes = z.Str("NAZMES"),
                Datum = z.Dat("DATUM"),
                Godina = z.Str("GODINA"),
                Vrsta = z.Str("VRSTA"),
                Evidbroj = z.Str("EVIDBROJ"),
                Maticnibr = z.Str("MATICNIBR"),
                Idbroj = z.Str("IDBROJ"),
                Dok = z.Str("DOK"),
                Grupa = z.Int("GRUPA"),
                Grupa1 = z.Int("GRUPA1"),
                Arhiva = z.Str("ARHIVA"),
                Arhiva2 = z.Str("ARHIVA2"),
                Idbr = z.Long("IDBR")
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task MigrujLdPodAsync(FirmaDbContext context, string folderPath)
    {
        var putanja = PronadjiDbf(folderPath, "ldpod.dbf");
        if (putanja == null)
            return;

        var zapisi = DbfReader.CitajSveZapise(putanja);
        foreach (var z in zapisi)
        {
            context.LdPodStavke.Add(new LdPodStavka
            {
                Kod = z.Str("KOD"),
                Opis = z.Str("OPIS"),
                S1a = z.Dec("S1A") ?? 0m,
                Sv1a = z.Dec("SV1A") ?? 0m,
                S1b = z.Dec("S1B") ?? 0m,
                Sv1b = z.Dec("SV1B") ?? 0m,
                S1c = z.Dec("S1C") ?? 0m,
                Sv1c = z.Dec("SV1C") ?? 0m,
                S1u = z.Dec("S1U") ?? 0m,
                Sv1u = z.Dec("SV1U") ?? 0m,
                S2a = z.Dec("S2A") ?? 0m,
                Sv2a = z.Dec("SV2A") ?? 0m,
                S2b = z.Dec("S2B") ?? 0m,
                Sv2b = z.Dec("SV2B") ?? 0m,
                S2c = z.Dec("S2C") ?? 0m,
                Sv2c = z.Dec("SV2C") ?? 0m,
                S2u = z.Dec("S2U") ?? 0m,
                Sv2u = z.Dec("SV2U") ?? 0m,
                S3a = z.Dec("S3A") ?? 0m,
                Sv3a = z.Dec("SV3A") ?? 0m,
                S3b = z.Dec("S3B") ?? 0m,
                Sv3b = z.Dec("SV3B") ?? 0m,
                S3c = z.Dec("S3C") ?? 0m,
                Sv3c = z.Dec("SV3C") ?? 0m,
                S3u = z.Dec("S3U") ?? 0m,
                Sv3u = z.Dec("SV3U") ?? 0m,
                S4a = z.Dec("S4A") ?? 0m,
                Sv4a = z.Dec("SV4A") ?? 0m,
                S4b = z.Dec("S4B") ?? 0m,
                Sv4b = z.Dec("SV4B") ?? 0m,
                S4c = z.Dec("S4C") ?? 0m,
                Sv4c = z.Dec("SV4C") ?? 0m,
                S4u = z.Dec("S4U") ?? 0m,
                Sv4u = z.Dec("SV4U") ?? 0m,
                Su = z.Dec("SU") ?? 0m,
                Svu = z.Dec("SVU") ?? 0m,
                Mesec = z.Int("MESEC"),
                Isplata = z.Int("ISPLATA"),
                Vrsta = z.Str("VRSTA"),
                Preneto = z.Str("PRENETO"),
                Idbr = z.Long("IDBR")
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task MigrujLdSpisAsync(FirmaDbContext context, string folderPath)
    {
        var putanja = PronadjiDbf(folderPath, "ldspis.dbf");
        if (putanja == null)
            return;

        var zapisi = DbfReader.CitajSveZapise(putanja);
        foreach (var z in zapisi)
        {
            context.LdSpisStavke.Add(new LdSpisStavka
            {
                Broj = z.Int("BROJ"),
                ImePrez = z.Str("IME_PREZ"),
                Partija = z.Str("PARTIJA"),
                Iznos = z.Dec("IZNOS") ?? 0m,
                Sifra = z.Str("SIFRA"),
                Preneto = z.Str("PRENETO"),
                Idbr = z.Long("IDBR")
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task MigrujLdKontoSablonAsync(FirmaDbContext context, string folderPath)
    {
        var putanja = PronadjiDbf(folderPath, "ldkon00.dbf");
        if (putanja == null)
            return;

        var zapisi = DbfReader.CitajSveZapise(putanja);
        foreach (var z in zapisi)
        {
            context.LdKontoSablonStavke.Add(new LdKontoSablonStavka
            {
                Vrsta = z.Str("VRSTA"),
                Kod = z.Str("KOD"),
                Opis = z.Str("OPIS"),
                Konto = z.Str("KONTO"),
                Kontop = z.Str("KONTOP"),
                Preneto = z.Str("PRENETO"),
                Idbr = z.Long("IDBR")
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task MigrujLdKnjizenjeAsync(FirmaDbContext context, string folderPath)
    {
        var putanja = PronadjiDbf(folderPath, "ldkon.dbf");
        if (putanja == null)
            return;

        var zapisi = DbfReader.CitajSveZapise(putanja);
        foreach (var z in zapisi)
        {
            context.LdKnjizenjeStavke.Add(new LdKnjizenjeStavka
            {
                Vrsta = z.Str("VRSTA"),
                Kod = z.Str("KOD"),
                Opis = z.Str("OPIS"),
                Konto = z.Str("KONTO"),
                Kontop = z.Str("KONTOP"),
                Iznos = z.Dec("IZNOS") ?? 0m,
                Datdok = z.Dat("DATDOK"),
                Brnal = z.Str("BRNAL"),
                Mp = z.Str("MP"),
                Mtr = z.Int("MTR"),
                Preneto = z.Str("PRENETO"),
                Idbr = z.Long("IDBR")
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task MigrujRadnikeAsync(FirmaDbContext context, string folderPath)
    {
        var putanja = PronadjiDbf(folderPath, "ldrad.dbf");
        if (putanja == null) return;

        var zapisi = DbfReader.CitajSveZapise(putanja);

        foreach (var z in zapisi)
        {
            // Preskoči logički obrisane zapise
            if (z.Str("BRISANJE").Equals("D", StringComparison.OrdinalIgnoreCase))
                continue;

            var radnik = new Radnik
            {
                // Identifikacija
                Broj             = z.Int("BROJ"),
                ImePrezime       = z.Str("IME_PREZ"),
                Prezime          = z.Str("PREZIME"),
                Ime              = z.Str("IME"),
                VrstaId          = z.Str("VRSTAID"),
                MaticniBroj      = z.Str("MATICNIBR"),
                IdBroj           = z.Str("IDBROJ"),
                EvidencijskiBroj = z.Str("EVIDBROJ"),
                Pol              = z.Str("POL"),

                // Adresa
                Adresa       = z.Str("ADRESA"),
                Posta        = z.Str("POSTA"),
                Mesto        = z.Str("MESTO"),
                Telefon      = z.Str("TELEFON"),
                Drzava       = z.Str("DRZAVA"),
                Opstina      = z.Str("OPSTINA"),
                OpstinaRada  = z.Str("OPSTINAR"),
                Prebivaliste = z.Str("PREBIVAL"),

                // Organizacija
                Sifra              = z.Str("SIFRA"),
                RadnoMesto         = z.Str("RADNOMES"),
                RadnoMestoDetalj   = z.Str("RMESTO"),
                PoslovnaJedinica   = z.Str("PJ"),
                SifraOrganizacije  = z.Str("SIFRAORG"),
                IzvorFinansiranja  = z.Str("IZVORFIN"),
                GrupaVirmana       = z.Str("GRUPAVIRM"),
                IdProfesionalniKod = z.Str("IDPROFC"),
                IdSektor           = z.Str("IDSEKTOR"),
                IdPodsektor        = z.Str("IDPODSEK"),
                IdLokacija         = z.Str("IDLOKAC"),
                IdLokacijaPod      = z.Str("IDLOKACP"),
                Dokument           = z.Str("DOK"),
                MestoPrimanja      = z.Str("MP"),

                // Stručna sprema
                Stepen         = z.Str("STEPEN"),
                SkolskaSprema  = z.Str("SKOSPREMA"),
                Sprema         = z.Str("SPREMA"),
                SifraZanimanja = z.Str("SIFRAZANIM"),

                // Vrsta zaposlenja
                VrstaZaposlenja     = z.Str("VRSTAZAP"),
                VrstaPrimanja       = z.Str("VRSTAPRIM"),
                OznakaVrstePrihoda  = z.Str("OZNVRPRIH"),
                OznakaOlaksica      = z.Str("OZNOLAKS"),
                OznakaBeneficije    = z.Str("OZNBEN"),
                TipSluzbe           = z.Str("TIPSLUZB"),
                PlatnaGrupa         = z.Str("PLATNAGR"),
                GodinaNapredovanja  = z.Str("GODNAPRED"),
                GrupaNamestenja     = z.Str("GRNAMEST"),
                ProcenatAngazovanja = z.Str("PROCANGAZ"),
                Katalog             = z.Str("KATALOG"),
                Vrsta               = z.Str("VRSTA"),
                Grupa               = z.Int("GRUPA"),
                Grupa1              = z.Int("GRUPA1"),

                // Datumi
                DatumPrijave        = z.Dat("DATPRI"),
                DatumUgovora        = z.Dat("DATUGOVOR"),
                DatumZasnivanja     = z.Dat("DATZASNIV"),
                DatumZaposlenja     = z.Dat("DATZAPOS"),
                DatumOtkaza         = z.Dat("DATOTKAZ"),
                UgovorOd            = z.Dat("UGOVDAT0"),
                UgovorDo            = z.Dat("UGOVDAT1"),
                ProduzenjeOd        = z.Dat("PRODDAT0"),
                ProduzenjeDo        = z.Dat("PRODDAT1"),
                DatumNezaposlenosti = z.Dat("DATNEZAP"),
                DatumMinulogRada    = z.Dat("DATMIN"),

                // Koeficijenti i zarada
                Koeficijent        = z.Dec("KOEF") ?? 0m,
                KoeficijentDodatni = z.Dec("KOEFDOD") ?? 0m,
                KoeficijentUkupni  = z.Dec("KOEFUKUP") ?? 0m,
                Osnovica           = z.Dec("OSNOVICA") ?? 0m,
                OsnovBruto         = z.Dec("OSNOVBRUTO") ?? 0m,
                ProcenatUvecanja   = z.Dec("PROCUVEC") ?? 0m,
                StartniBodovi      = z.Dec("STARTBOD") ?? 0m,
                Kosovo             = z.Dec("KOSOVO") ?? 0m,

                // Staž
                Staz                 = z.Int("STAZ"),
                BeneficiraniProcenat = z.Dec("BENPROC") ?? 0m,
                BeneficiraniStaz     = z.Dec("BENSTAZ") ?? 0m,
                StazJubilej          = z.Dec("STAZJUBIL") ?? 0m,
                ProcenatMinulogRada  = z.Dec("MINPROC") ?? 0m,

                // Obustave
                Kasa                 = z.Dec("KASA") ?? 0m,
                KasaRata             = z.Dec("KASARATA") ?? 0m,
                SindikatProcenat1    = z.Dec("SIND1PROC") ?? 0m,
                SindikatProcenat2    = z.Dec("SIND2PROC") ?? 0m,
                SolidarnostProcenat  = z.Dec("SOLPROC") ?? 0m,
                Dnevnica             = z.Dec("DNEVNICA") ?? 0m,
                AlimentacijaProcenat = z.Dec("ALIMPROC") ?? 0m,
                KolektivniKorak      = z.Dec("KOLKOR") ?? 0m,

                // Satnica
                DinarskaSatnica1      = z.Dec("DINSAT1") ?? 0m,
                DinarskaSatnica2      = z.Dec("DINSAT2") ?? 0m,
                DinarskaSatnica3      = z.Dec("DINSAT3") ?? 0m,
                DinarskaSatnicaUkupno = z.Dec("DINSATSVE") ?? 0m,
                CasovnaSatnica1       = z.Dec("CASSAT1") ?? 0m,
                CasovnaSatnica2       = z.Dec("CASSAT2") ?? 0m,
                CasovnaSatnica3       = z.Dec("CASSAT3") ?? 0m,
                CasovnaSatnicaUkupno  = z.Dec("CASSATSVE") ?? 0m,

                // Stimulacije
                StimulacijaMin      = z.Dec("STIMIN") ?? 0m,
                StimulacijaGodisnja = z.Dec("STIMGOD") ?? 0m,
                Stimulacija1        = z.Dec("STIM1") ?? 0m,
                Stimulacija2        = z.Dec("STIM2") ?? 0m,
                Stimulacija3        = z.Dec("STIM3") ?? 0m,
                Destimulacija1      = z.Dec("DESTIM1") ?? 0m,
                Destimulacija2      = z.Dec("DESTIM2") ?? 0m,
                Destimulacija3      = z.Dec("DESTIM3") ?? 0m,

                // Fond / M4
                FondZarada = z.Dec("FONDZ") ?? 0m,
                Mtr        = z.Int("MTR"),
                Dan        = z.Str("DAN"),
                Mesec      = z.Str("MESEC"),
                Godina     = z.Str("GODINA"),
                M4Mesec    = z.Str("M4MES"),
                M4Dan      = z.Str("M4DAN"),
                M4Grad     = z.Str("M4GRAD"),

                // Osiguranje
                LboBroj           = z.Str("LBOBROJ"),
                ZkBroj            = z.Str("ZKBROJ"),
                DatumOsiguranjaOd = z.Dat("DATOSIG0"),
                DatumOsiguranjaDo = z.Dat("DATOSIG1"),
                RegBrojSocijalno  = z.Str("REGSOC"),
                OsnovOsiguranja   = z.Str("OSNOVOSIG"),

                // Godišnji odmor
                GodisnjeDanaUkupno        = z.Int("GODUK"),
                GodisnjeDanaIskorisceno   = z.Int("GODISKOR"),
                GodisnjeDanaNeiskorisceno = z.Int("GODNEISKOR"),

                // Bankovni
                Partija          = z.Str("PARTIJA"),
                SamodoprSifra    = z.Int("SAMSIF"),
                SamodoprProcenat = z.Dec("SAMOPROC") ?? 0m,

                // Umanjenja / MFP
                ProcenatUmanjenja   = z.Dec("PROCUMANJ") ?? 0m,
                Umanjenje           = z.Str("UMANJENJE"),
                PorskoUmanjenje     = z.Dec("PORUMANJ") ?? 0m,
                DoprinosnoUmanjenje = z.Dec("DOPUMANJ") ?? 0m,
                PioUmanjenjeRadnik  = z.Dec("PIOUMANJR") ?? 0m,
                PioUmanjenjeFirma   = z.Dec("PIOUMANJF") ?? 0m,
                Mfp3Procenat        = z.Dec("MFP3PROC") ?? 0m,
                Mfp6                = z.Dec("MFP6") ?? 0m,
                Mfp7                = z.Dec("MFP7") ?? 0m,
                Mfp8Nepuno          = z.Str("MFP8NEPUN"),
                Mfp9NajnizaOsnova   = z.Str("MFP9NAJOSN"),
                Mfp10DvaVezana      = z.Str("MFP10DVEZ"),

                // Oznake
                Prevoz             = z.Str("PREVOZ"),
                ToploObrok         = z.Str("TOPLI"),
                Ropnr              = z.Str("ROPNR"),
                Pripravnik         = z.Str("PRIPRAV"),
                Ocena              = z.Str("OCENA"),
                PoreskeOlaksice    = z.Str("POROLAKS"),
                Roditelj           = z.Str("RODITELJ"),
                Bolovanje          = z.Str("BOLOVANJE"),
                ObukaBrojNaredbe   = z.Str("OBUCBRNR"),
                ObukaPp            = z.Str("OBUCPP"),
                SanitarniPregled   = z.Dat("SANITARNI"),
                PripravnickiUgovor = z.Str("PRIPUG"),
                PripravnickiDatum  = z.Dat("PRIPDAT"),

                // Napomene
                Napomena1 = z.Str("NAPOMENA"),
                Napomena2 = z.Str("NAPOMENA2"),
                Napomena3 = z.Str("NAPOMENA3"),
                Napomena4 = z.Str("NAPOMENA4"),
                Zadaci    = z.Str("ZADACI"),

                // Sistemska polja
                Brisanje = z.Str("BRISANJE"),
                Preneto  = z.Str("PRENETO"),
                Idbr     = z.Long("IDBR"),
            };

            context.Radnici.Add(radnik);
        }

        await context.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════════════════════════
    //  POMOĆNE METODE
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traži DBF fajl case-insensitive (DBF fajlovi mogu biti LOZINKE.DBF ili lozinke.dbf).
    /// </summary>
    private static string? PronadjiDbf(string folderPath, string fileName)
    {
        // Prvo probaj tačan naziv
        var putanja = Path.Combine(folderPath, fileName);
        if (File.Exists(putanja)) return putanja;

        // Probaj UPPERCASE
        putanja = Path.Combine(folderPath, fileName.ToUpperInvariant());
        if (File.Exists(putanja)) return putanja;

        // Probaj case-insensitive pretragu
        var fajlovi = Directory.GetFiles(folderPath, "*.dbf");
        var nadjen = fajlovi.FirstOrDefault(f =>
            Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));

        return nadjen;
    }
}

// ══════════════════════════════════════════════════════════════════════
//  EXTENSION METODE ZA ČITANJE IZ DBF ZAPISA
//
//  DbfReader vraća Dictionary<string, object?> gde su:
//    - C polja -> string
//    - N polja -> decimal?
//    - D polja -> DateTime?
//    - L polja -> bool
//
//  Ove metode bezbedno konvertuju vrednosti.
// ══════════════════════════════════════════════════════════════════════

internal static class MigratorZapisExtensions
{
    /// <summary>Čita string vrednost iz zapisa. Vraća prazan string ako ne postoji.</summary>
    public static string Str(this Dictionary<string, object?> z, string polje)
        => z.TryGetValue(polje, out var v) && v is string s ? s.Trim() : string.Empty;

    /// <summary>Čita decimal vrednost iz zapisa. Vraća null ako ne postoji.</summary>
    public static decimal? Dec(this Dictionary<string, object?> z, string polje)
        => z.TryGetValue(polje, out var v) && v is decimal d ? d : null;

    /// <summary>Čita int vrednost iz zapisa (iz decimal). Vraća 0 ako ne postoji.</summary>
    public static int Int(this Dictionary<string, object?> z, string polje)
        => z.TryGetValue(polje, out var v) && v is decimal d ? (int)d : 0;

    /// <summary>Čita long vrednost iz zapisa (iz decimal). Vraća 0 ako ne postoji.</summary>
    public static long Long(this Dictionary<string, object?> z, string polje)
        => z.TryGetValue(polje, out var v) && v is decimal d ? (long)d : 0L;

    /// <summary>Čita DateTime? vrednost iz zapisa. Vraća null ako ne postoji ili je prazno.</summary>
    public static DateTime? Dat(this Dictionary<string, object?> z, string polje)
        => z.TryGetValue(polje, out var v) && v is DateTime dt ? dt : null;

    /// <summary>Čita bool vrednost iz zapisa. Vraća false ako ne postoji.</summary>
    public static bool Bool(this Dictionary<string, object?> z, string polje)
        => z.TryGetValue(polje, out var v) && v is true;
}
