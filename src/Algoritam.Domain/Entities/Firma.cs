namespace Algoritam.Domain.Entities;

/// <summary>
/// Puni podaci o firmi. Migracija iz FIRMA.DBF (115 polja).
///
/// NAPOMENA O KOMPATIBILNOSTI:
///   Originalna Firma klasa (dizajnirana za PUTANJE.DBF) imala je:
///     Naziv, Naziv2, Ulica, BrojUlice, PostanskiBroj, Mesto, Drzava,
///     ZiroRacun, Pib, Maticni, Aktivna, FolderPath.
///   Svi ovi properties su sačuvani radi kompatibilnosti sa DbfFirmaService,
///   FirmaIzborViewModel, i drugim postojećim kodom.
///   Nova polja su DODANA za potpunu migraciju iz FIRMA.DBF.
///
/// U per-company SQLite bazi (algoritam.db) obično ima tačno 1 zapis.
/// U spisku firmi (PUTANJE.DBF čitanje) populiraju se samo osnovna polja.
/// </summary>
public class Firma
{
    /// <summary>Surogatni PK. U per-company bazi uvek 1. U spisku firmi — redni broj.</summary>
    public int Id { get; set; }

    // ── Naziv ───────────────────────────────────────────────────────

    /// <summary>Naziv firme, prvi red (FIME, C50).</summary>
    public string Naziv { get; set; } = string.Empty;

    /// <summary>Naziv firme, drugi red (FIME2, C50).</summary>
    public string Naziv2 { get; set; } = string.Empty;

    /// <summary>Baza / šifra (FBAZA, C20).</summary>
    public string Baza { get; set; } = string.Empty;

    // ── Adresa ──────────────────────────────────────────────────────

    /// <summary>Poštanski broj (FPOS, C5).</summary>
    public string PostanskiBroj { get; set; } = string.Empty;

    /// <summary>Mesto / grad (FMES, C25).</summary>
    public string Mesto { get; set; } = string.Empty;

    /// <summary>Ulica (FUL, C25).</summary>
    public string Ulica { get; set; } = string.Empty;

    /// <summary>Broj ulice (FULBR, C10).</summary>
    public string BrojUlice { get; set; } = string.Empty;

    /// <summary>Republika (FREPUB, C25).</summary>
    public string Republika { get; set; } = string.Empty;

    /// <summary>Država (FDRZAVA, C25).</summary>
    public string Drzava { get; set; } = string.Empty;

    // ── Žiro računi (do 6 + devizni + bolovanje) ────────────────────

    /// <summary>Žiro račun 1 — primarni (FZIRO, C30).
    /// Alias: postojeći kod koristi ZiroRacun za ovaj property.</summary>
    public string ZiroRacun { get; set; } = string.Empty;

    /// <summary>Žiro račun 2 (FZIRO2, C30).</summary>
    public string ZiroRacun2 { get; set; } = string.Empty;

    /// <summary>Žiro račun 3 (FZIRO3, C30).</summary>
    public string ZiroRacun3 { get; set; } = string.Empty;

    /// <summary>Žiro račun 4 (FZIRO4, C30).</summary>
    public string ZiroRacun4 { get; set; } = string.Empty;

    /// <summary>Žiro račun 5 (FZIRO5, C30).</summary>
    public string ZiroRacun5 { get; set; } = string.Empty;

    /// <summary>Žiro račun 6 (FZIRO6, C30).</summary>
    public string ZiroRacun6 { get; set; } = string.Empty;

    /// <summary>Žiro račun za bolovanje (FZIROBOL, C30).</summary>
    public string ZiroRacunBolovanje { get; set; } = string.Empty;

    /// <summary>Devizni račun (FZIRODEV, C30).</summary>
    public string ZiroRacunDevizni { get; set; } = string.Empty;

    // ── Kontrolni kodovi žiro računa ────────────────────────────────

    public string KontrolniKodZiro1 { get; set; } = string.Empty;  // FKONZIRO1
    public string KontrolniKodZiro2 { get; set; } = string.Empty;  // FKONZIRO2
    public string KontrolniKodZiro3 { get; set; } = string.Empty;  // FKONZIRO3
    public string KontrolniKodZiro4 { get; set; } = string.Empty;  // FKONZIRO4
    public string KontrolniKodZiro5 { get; set; } = string.Empty;  // FKONZIRO5
    public string KontrolniKodZiro6 { get; set; } = string.Empty;  // FKONZIRO6
    public string KontrolniKodZiroDevizni { get; set; } = string.Empty;   // FKONZIROD
    public string KontrolniKodZiroBolovanje { get; set; } = string.Empty; // FKONZIROB

    // ── Banke — nazivi ──────────────────────────────────────────────

    public string Banka1 { get; set; } = string.Empty;   // FBANKA
    public string Banka2 { get; set; } = string.Empty;   // FBANKA2
    public string Banka3 { get; set; } = string.Empty;   // FBANKA3
    public string Banka4 { get; set; } = string.Empty;   // FBANKA4
    public string Banka5 { get; set; } = string.Empty;   // FBANKA5
    public string Banka6 { get; set; } = string.Empty;   // FBANKA6
    public string BankaBolovanje { get; set; } = string.Empty;  // FBANKAB
    public string BankaDevizna { get; set; } = string.Empty;    // FBANKAD

    // ── Banke — šifre ───────────────────────────────────────────────

    public string BankaSifra1 { get; set; } = string.Empty;   // FBANSIF
    public string BankaSifra2 { get; set; } = string.Empty;   // FBANSIF2
    public string BankaSifra3 { get; set; } = string.Empty;   // FBANSIF3
    public string BankaSifra4 { get; set; } = string.Empty;   // FBANSIF4
    public string BankaSifra5 { get; set; } = string.Empty;   // FBANSIF5
    public string BankaSifra6 { get; set; } = string.Empty;   // FBANSIF6
    public string BankaSifraBolovanje { get; set; } = string.Empty;  // FBANSIFB
    public string BankaSifraDevizna { get; set; } = string.Empty;    // FBANSIFD

    // ── Kontakt ─────────────────────────────────────────────────────

    public string Telefon1 { get; set; } = string.Empty;   // FTEL
    public string Telefon2 { get; set; } = string.Empty;   // FTEL2
    public string Telefon3 { get; set; } = string.Empty;   // FTEL3
    public string Fax1 { get; set; } = string.Empty;       // FFAX
    public string Fax2 { get; set; } = string.Empty;       // FFAX2
    public string Fax3 { get; set; } = string.Empty;       // FFAX3
    public string Email { get; set; } = string.Empty;      // FEMAIL
    public string Web { get; set; } = string.Empty;        // FVEB

    // ── Poresko-pravni identifikatori ───────────────────────────────

    /// <summary>Šifra delatnosti (FSIF, C10).</summary>
    public string SifraDelatnosti { get; set; } = string.Empty;

    /// <summary>Naziv delatnosti (FNAZD, C30).</summary>
    public string NazivDelatnosti { get; set; } = string.Empty;

    /// <summary>SDK kod (FSDK, C3).</summary>
    public string SdkKod { get; set; } = string.Empty;

    /// <summary>Matični broj (FMAT, C9). Alias: postojeći kod koristi Maticni.</summary>
    public string Maticni { get; set; } = string.Empty;

    /// <summary>PIB / poreski identifikacioni broj (FPOR, C16).</summary>
    public string Pib { get; set; } = string.Empty;

    /// <summary>Dopunski poreski broj (FDOP, C24).</summary>
    public string DopunskiPorBroj { get; set; } = string.Empty;

    /// <summary>Vlasnik (FVLAST, C30).</summary>
    public string Vlasnik { get; set; } = string.Empty;

    /// <summary>Agencija / računovodstvena agencija (FAGENC, C50).</summary>
    public string Agencija { get; set; } = string.Empty;

    /// <summary>FPA/SLR/N flag (FFPASLRN, C1).</summary>
    public string FpaSlrn { get; set; } = string.Empty;

    /// <summary>Opština (FOPS, C30).</summary>
    public string Opstina { get; set; } = string.Empty;

    // ── Datumi registracije ─────────────────────────────────────────

    /// <summary>Datum osnivanja (FDAT0).</summary>
    public DateTime? DatumOsnivanja { get; set; }

    /// <summary>Datum prestanka (FDAT1).</summary>
    public DateTime? DatumPrestanka { get; set; }

    /// <summary>Datum obrade (FDATOBR).</summary>
    public DateTime? DatumObrade { get; set; }

    /// <summary>Datum registracije (FDATREG).</summary>
    public DateTime? DatumRegistracije { get; set; }

    /// <summary>Datum upisa (FDATUPIS).</summary>
    public DateTime? DatumUpisa { get; set; }

    /// <summary>Datum PDV registracije (FDATPDV).</summary>
    public DateTime? DatumPdv { get; set; }

    // ── Registracioni brojevi ───────────────────────────────────────

    /// <summary>Registracioni broj socijalnog (FREGSOC, C12).</summary>
    public string RegBrojSocijalno { get; set; } = string.Empty;

    /// <summary>Registracioni broj zdravstvenog (FREGZDR, C12).</summary>
    public string RegBrojZdravstveno { get; set; } = string.Empty;

    /// <summary>Sudski registar (FREGSUD, C20).</summary>
    public string SudskiRegistar { get; set; } = string.Empty;

    /// <summary>Registracioni naziv (FREGNAZ, C30).</summary>
    public string RegNaziv { get; set; } = string.Empty;

    // ── Vlasnik / odgovorno lice ────────────────────────────────────

    /// <summary>PIB savetnika / zastupnika (FPIBSAV, C9).</summary>
    public string PibSavetnika { get; set; } = string.Empty;

    /// <summary>JMBG vlasnika 1 (FJMBG1, C13).</summary>
    public string JmbgVlasnika1 { get; set; } = string.Empty;

    /// <summary>MB savetnika (FMBSAV, C13).</summary>
    public string MbSavetnika { get; set; } = string.Empty;

    /// <summary>JMBG vlasnika / odgovornog lica (FJMBG, C13).</summary>
    public string JmbgVlasnika { get; set; } = string.Empty;

    /// <summary>JMBG kontakt broj (FJMBGKONBR, C25).</summary>
    public string JmbgKontaktBroj { get; set; } = string.Empty;

    /// <summary>Republika kod (FREP, C3).</summary>
    public string RepublikaKod { get; set; } = string.Empty;

    /// <summary>Poreski broj republičke (FPORREP, C24).</summary>
    public string PorBrojRepublicke { get; set; } = string.Empty;

    /// <summary>Odgovorno lice / osoba (FOSOBA, C27).</summary>
    public string OdgovornoLice { get; set; } = string.Empty;

    /// <summary>Uplaćeni kapital (FUPLACENI, C20).</summary>
    public string UplaceniKapital { get; set; } = string.Empty;

    /// <summary>Upisani kapital (FUPISANI, C20).</summary>
    public string UpisaniKapital { get; set; } = string.Empty;

    /// <summary>Naznaka (FNAZNAKA, C20).</summary>
    public string Naznaka { get; set; } = string.Empty;

    /// <summary>SWIFT kod (FSWIFT, C20).</summary>
    public string SwiftKod { get; set; } = string.Empty;

    // ── Poreske i računovodstvene oznake ────────────────────────────

    /// <summary>Da li je obveznik PDV-a (FPDV, C1). "D"=Da.</summary>
    public string PdvObveznik { get; set; } = string.Empty;

    /// <summary>Organizacioni oblik (FOBLIK, C20).</summary>
    public string OrganizacioniOblik { get; set; } = string.Empty;

    /// <summary>Zdravstvena ustanova (FZDRAVUST, C40).</summary>
    public string ZdravstvenaUstanova { get; set; } = string.Empty;

    /// <summary>Prodajni centar flag (FPRODC, C1).</summary>
    public string ProdajniCentar { get; set; } = string.Empty;

    /// <summary>Zatvorena godina flag (FZATVGOD, C1).</summary>
    public string ZatvorenaGodina { get; set; } = string.Empty;

    /// <summary>Datum zatvaranja perioda (FZATVPER).</summary>
    public DateTime? DatumZatvaranjaPerioda { get; set; }

    /// <summary>Budžetski korisnik kod (FBUDZKOR, C2).</summary>
    public string BudzetskiKorisnik { get; set; } = string.Empty;

    // ── Konta za gotovinski promet ──────────────────────────────────

    /// <summary>Konto gotovine (KGOTOVINA, C10).</summary>
    public string KontoGotovina { get; set; } = string.Empty;

    /// <summary>Konto čeka (KCEK, C10).</summary>
    public string KontoCek { get; set; } = string.Empty;

    /// <summary>Konto virmana (KVIRMAN, C10).</summary>
    public string KontoVirman { get; set; } = string.Empty;

    /// <summary>Konto kartice (KKARTICA, C10).</summary>
    public string KontoKartica { get; set; } = string.Empty;

    /// <summary>Konto ostalo (KOSTALO, C10).</summary>
    public string KontoOstalo { get; set; } = string.Empty;

    // ── Ostalo ──────────────────────────────────────────────────────

    /// <summary>Šifra fizičkog lica (SIFFIZLI, C2).</summary>
    public string SifraFizickogLica { get; set; } = string.Empty;

    /// <summary>Pumpe flag (FPUMPE, C1).</summary>
    public string Pumpe { get; set; } = string.Empty;

    /// <summary>SN flag (FSN, C1).</summary>
    public string Sn { get; set; } = string.Empty;

    /// <summary>Oznaka javnog preduzeća (OZNAKAJP, C4).</summary>
    public string OznakaJavnogPreduzeca { get; set; } = string.Empty;

    /// <summary>JBBK kod (JBBK, C5).</summary>
    public string Jbbk { get; set; } = string.Empty;

    // ── Latinični nazivi ────────────────────────────────────────────

    /// <summary>Naziv firme — latinica (FIMEC, C50).</summary>
    public string NazivLatinican { get; set; } = string.Empty;

    /// <summary>Mesto — latinica (FMESC, C25).</summary>
    public string MestoLatinicom { get; set; } = string.Empty;

    /// <summary>Ulica — latinica (FULC, C30).</summary>
    public string UlicaLatinicom { get; set; } = string.Empty;

    /// <summary>Republika — latinica (FREPUBC, C50).</summary>
    public string RepublikaLatinicom { get; set; } = string.Empty;

    /// <summary>Opština — latinica (FOPSC, C30).</summary>
    public string OpstinaLatinicom { get; set; } = string.Empty;

    // ── Sistemska polja ─────────────────────────────────────────────

    /// <summary>Flag prenosa (PRENETO, C1).</summary>
    public string Preneto { get; set; } = string.Empty;

    /// <summary>ID broj zapisa (IDBR, N11).</summary>
    public long Idbr { get; set; }

    /// <summary>JBKJS — jedinstveni broj korisnika javnih sredstava (JBKJS, C10).</summary>
    public string Jbkjs { get; set; } = string.Empty;

    /// <summary>Broj pošte (FBRPOSTA, C8).</summary>
    public string BrojPoste { get; set; } = string.Empty;

    // ── Kompatibilnost sa postojećim kodom (iz originalne Firma klase) ─────

    /// <summary>
    /// Da li je firma aktivna. Koristi se u FirmaIzborViewModel za filtriranje.
    /// Ovo polje NE POSTOJI u firma.dbf — popunjava se programski
    /// (npr. DbfFirmaService uvek postavlja true).
    /// </summary>
    public bool Aktivna { get; set; } = true;

    /// <summary>
    /// Putanja do foldera firme (npr. "C:\FIN\F1").
    /// Ne čuva se u SQLite — popunjava se pri učitavanju.
    /// Koristi se u ZaradeMenuViewModel i drugim mestima.
    /// </summary>
    public string FolderPath { get; set; } = string.Empty;
}
