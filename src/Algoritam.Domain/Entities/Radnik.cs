namespace Algoritam.Domain.Entities;

/// <summary>
/// Radnik (zaposleni) — migracija iz LDRAD.DBF (158 polja).
/// Šifarnik zaposlenih za modul Zarade (LD).
/// BROJ je poslovni ključ (redni broj radnika unutar firme).
/// </summary>
public class Radnik
{
    /// <summary>Surogatni primarni ključ (auto-increment).</summary>
    public int Id { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  IDENTIFIKACIJA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Broj radnika — poslovni ključ (BROJ, N4). Unique.</summary>
    public int Broj { get; set; }

    /// <summary>Ime i prezime spojeno (IME_PREZ, C30). Legacy display field.</summary>
    public string ImePrezime { get; set; } = string.Empty;

    /// <summary>Prezime (PREZIME, C20).</summary>
    public string Prezime { get; set; } = string.Empty;

    /// <summary>Ime (IME, C20).</summary>
    public string Ime { get; set; } = string.Empty;

    /// <summary>Vrsta identifikacionog dokumenta (VRSTAID, C1).</summary>
    public string VrstaId { get; set; } = string.Empty;

    /// <summary>JMBG — jedinstveni matični broj građana (MATICNIBR, C13).</summary>
    public string MaticniBroj { get; set; } = string.Empty;

    /// <summary>ID broj — poreski identifikacioni broj (IDBROJ, C11).</summary>
    public string IdBroj { get; set; } = string.Empty;

    /// <summary>Evidencioni broj (EVIDBROJ, C8).</summary>
    public string EvidencijskiBroj { get; set; } = string.Empty;

    /// <summary>Pol (POL, C1). "M" ili "Z".</summary>
    public string Pol { get; set; } = string.Empty;

    // ══════════════════════════════════════════════════════════════════
    //  ADRESA I KONTAKT
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Adresa (ADRESA, C40).</summary>
    public string Adresa { get; set; } = string.Empty;

    /// <summary>Poštanski broj (POSTA, C5).</summary>
    public string Posta { get; set; } = string.Empty;

    /// <summary>Mesto (MESTO, C25).</summary>
    public string Mesto { get; set; } = string.Empty;

    /// <summary>Telefon (TELEFON, C20).</summary>
    public string Telefon { get; set; } = string.Empty;

    /// <summary>Država (DRZAVA, C3). Šifra.</summary>
    public string Drzava { get; set; } = string.Empty;

    /// <summary>Opština prebivališta (OPSTINA, C3).</summary>
    public string Opstina { get; set; } = string.Empty;

    /// <summary>Opština rada (OPSTINAR, C3).</summary>
    public string OpstinaRada { get; set; } = string.Empty;

    /// <summary>Prebivalište kod (PREBIVAL, C3).</summary>
    public string Prebivaliste { get; set; } = string.Empty;

    // ══════════════════════════════════════════════════════════════════
    //  ORGANIZACIJA I KLASIFIKACIJA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Šifra radnog mesta / pozicije (SIFRA, C5). Potencijalni FK.</summary>
    public string Sifra { get; set; } = string.Empty;

    /// <summary>Radno mesto — opis (RADNOMES, C30).</summary>
    public string RadnoMesto { get; set; } = string.Empty;

    /// <summary>Radno mesto — detaljni opis (RMESTO, C40).</summary>
    public string RadnoMestoDetalj { get; set; } = string.Empty;

    /// <summary>Poslovna jedinica (PJ, C2). Potencijalni FK.</summary>
    public string PoslovnaJedinica { get; set; } = string.Empty;

    /// <summary>Šifra organizacione jedinice (SIFRAORG, C10).</summary>
    public string SifraOrganizacije { get; set; } = string.Empty;

    /// <summary>Izvor finansiranja (IZVORFIN, C3).</summary>
    public string IzvorFinansiranja { get; set; } = string.Empty;

    /// <summary>Grupa virmana (GRUPAVIRM, C2).</summary>
    public string GrupaVirmana { get; set; } = string.Empty;

    /// <summary>Profesionalni kod (IDPROFC, C2).</summary>
    public string IdProfesionalniKod { get; set; } = string.Empty;

    /// <summary>Sektor (IDSEKTOR, C2).</summary>
    public string IdSektor { get; set; } = string.Empty;

    /// <summary>Podsektor (IDPODSEK, C3).</summary>
    public string IdPodsektor { get; set; } = string.Empty;

    /// <summary>Lokacija (IDLOKAC, C2).</summary>
    public string IdLokacija { get; set; } = string.Empty;

    /// <summary>Lokacija — podlokacija (IDLOKACP, C2).</summary>
    public string IdLokacijaPod { get; set; } = string.Empty;

    /// <summary>Dokument (DOK, C3).</summary>
    public string Dokument { get; set; } = string.Empty;

    /// <summary>Mesto primanja (MP, C2).</summary>
    public string MestoPrimanja { get; set; } = string.Empty;

    /// <summary>Mesto plaćanja poreza (MESTOPL, C5).</summary>
    public string MestoPoreza { get; set; } = string.Empty;

    /// <summary>Mesto troškova (MESTOTRO, C10).</summary>
    public string MestoTroskova { get; set; } = string.Empty;

    // ══════════════════════════════════════════════════════════════════
    //  STRUČNA SPREMA I KVALIFIKACIJE
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Stepen stručne spreme (STEPEN, C3).</summary>
    public string Stepen { get; set; } = string.Empty;

    /// <summary>Školska sprema — tekst (SKOSPREMA, C15).</summary>
    public string SkolskaSprema { get; set; } = string.Empty;

    /// <summary>Sprema — šifra (SPREMA, C3).</summary>
    public string Sprema { get; set; } = string.Empty;

    /// <summary>Šifra zanimanja (SIFRAZANIM, C8).</summary>
    public string SifraZanimanja { get; set; } = string.Empty;

    // ══════════════════════════════════════════════════════════════════
    //  VRSTA ZAPOSLENJA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Vrsta zaposlenja (VRSTAZAP, C2). Šifra.</summary>
    public string VrstaZaposlenja { get; set; } = string.Empty;

    /// <summary>Vrsta primanja (VRSTAPRIM, C2).</summary>
    public string VrstaPrimanja { get; set; } = string.Empty;

    /// <summary>Oznaka vrste prihoda (OZNVRPRIH, C3).</summary>
    public string OznakaVrstePrihoda { get; set; } = string.Empty;

    /// <summary>Oznaka olakšica (OZNOLAKS, C2).</summary>
    public string OznakaOlaksica { get; set; } = string.Empty;

    /// <summary>Oznaka beneficiranog staža (OZNBEN, C1).</summary>
    public string OznakaBeneficije { get; set; } = string.Empty;

    /// <summary>Tip službe (TIPSLUZB, C4).</summary>
    public string TipSluzbe { get; set; } = string.Empty;

    /// <summary>Platna grupa (PLATNAGR, C3).</summary>
    public string PlatnaGrupa { get; set; } = string.Empty;

    /// <summary>Godina napredovanja (GODNAPRED, C4).</summary>
    public string GodinaNapredovanja { get; set; } = string.Empty;

    /// <summary>Grupa namestenja (GRNAMEST, C4).</summary>
    public string GrupaNamestenja { get; set; } = string.Empty;

    /// <summary>Procenat angažovanja (PROCANGAZ, C3).</summary>
    public string ProcenatAngazovanja { get; set; } = string.Empty;

    /// <summary>Katalog flag (KATALOG, C1).</summary>
    public string Katalog { get; set; } = string.Empty;

    /// <summary>Vrsta radnog odnosa (VRSTA, C1).</summary>
    public string Vrsta { get; set; } = string.Empty;

    /// <summary>Grupa (GRUPA, N4).</summary>
    public int Grupa { get; set; }

    /// <summary>Grupa 1 (GRUPA1, N4).</summary>
    public int Grupa1 { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  DATUMI ZAPOSLENJA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Datum prijave / prijema (DATPRI).</summary>
    public DateTime? DatumPrijave { get; set; }

    /// <summary>Datum ugovora (DATUGOVOR).</summary>
    public DateTime? DatumUgovora { get; set; }

    /// <summary>Datum zasnivanja radnog odnosa (DATZASNIV).</summary>
    public DateTime? DatumZasnivanja { get; set; }

    /// <summary>Datum zaposlenja (DATZAPOS). Može biti ista kao DATZASNIV.</summary>
    public DateTime? DatumZaposlenja { get; set; }

    /// <summary>Datum otkaza / prestanka (DATOTKAZ).</summary>
    public DateTime? DatumOtkaza { get; set; }

    /// <summary>Početak ugovora na određeno (UGOVDAT0).</summary>
    public DateTime? UgovorOd { get; set; }

    /// <summary>Kraj ugovora na određeno (UGOVDAT1).</summary>
    public DateTime? UgovorDo { get; set; }

    /// <summary>Početak produženja (PRODDAT0).</summary>
    public DateTime? ProduzenjeOd { get; set; }

    /// <summary>Kraj produženja (PRODDAT1).</summary>
    public DateTime? ProduzenjeDo { get; set; }

    /// <summary>Datum nezaposlenosti (DATNEZAP).</summary>
    public DateTime? DatumNezaposlenosti { get; set; }

    /// <summary>Datum minulog rada (DATMIN).</summary>
    public DateTime? DatumMinulogRada { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  KOEFICIJENTI I ZARADA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Koeficijent osnovni (KOEF, N12.3).</summary>
    public decimal Koeficijent { get; set; }

    /// <summary>Koeficijent dodatni (KOEFDOD, N12.3).</summary>
    public decimal KoeficijentDodatni { get; set; }

    /// <summary>Koeficijent ukupni (KOEFUKUP, N12.3).</summary>
    public decimal KoeficijentUkupni { get; set; }

    /// <summary>Osnovica za obračun (OSNOVICA, N12.3).</summary>
    public decimal Osnovica { get; set; }

    /// <summary>Osnov bruto zarade (OSNOVBRUTO, N12.3).</summary>
    public decimal OsnovBruto { get; set; }

    /// <summary>Procenat uvećanja (PROCUVEC, N12.3).</summary>
    public decimal ProcenatUvecanja { get; set; }

    /// <summary>Startni bodovi (STARTBOD, N18.6).</summary>
    public decimal StartniBodovi { get; set; }

    /// <summary>Kosovo dodatak (KOSOVO, N12.3).</summary>
    public decimal Kosovo { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  STAŽ
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Ukupan staž u mesecima (STAZ, N10).</summary>
    public int Staz { get; set; }

    /// <summary>Beneficirani procenat (BENPROC, N9.4).</summary>
    public decimal BeneficiraniProcenat { get; set; }

    /// <summary>Beneficirani staž (BENSTAZ, N12.2).</summary>
    public decimal BeneficiraniStaz { get; set; }

    /// <summary>Jubilej staž (STAZJUBIL, N12.2).</summary>
    public decimal StazJubilej { get; set; }

    /// <summary>Procenat minulog rada (MINPROC, N12.2).</summary>
    public decimal ProcenatMinulogRada { get; set; }

    /// <summary>Broj dana staža (BROJDANA, N4).</summary>
    public int BrojDana { get; set; }

    /// <summary>Broj meseci staža (BROJMES, N4).</summary>
    public int BrojMeseci { get; set; }

    /// <summary>Broj godina staža (BROJGOD, N4).</summary>
    public int BrojGodina { get; set; }

    /// <summary>Porez na fond zarada (POREZFOND, N12.2).</summary>
    public decimal PorezFondZarada { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  OBUSTAVE I ODBITCI
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Kasa / kredit (KASA, N12.2).</summary>
    public decimal Kasa { get; set; }

    /// <summary>Rata kase (KASARATA, N12.2).</summary>
    public decimal KasaRata { get; set; }

    /// <summary>Procenat sindikata 1 (SIND1PROC, N12.4).</summary>
    public decimal SindikatProcenat1 { get; set; }

    /// <summary>Procenat sindikata 2 (SIND2PROC, N12.4).</summary>
    public decimal SindikatProcenat2 { get; set; }

    /// <summary>Solidarnost procenat (SOLPROC, N12.4).</summary>
    public decimal SolidarnostProcenat { get; set; }

    /// <summary>Dnevnica (DNEVNICA, N12.4).</summary>
    public decimal Dnevnica { get; set; }

    /// <summary>Alimentacija procenat (ALIMPROC, N12.4).</summary>
    public decimal AlimentacijaProcenat { get; set; }

    /// <summary>Kolektivni korak (KOLKOR, N12.2).</summary>
    public decimal KolektivniKorak { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  SATNICA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Dinarska satnica 1 (DINSAT1, N12.2).</summary>
    public decimal DinarskaSatnica1 { get; set; }

    /// <summary>Dinarska satnica 2 (DINSAT2, N12.2).</summary>
    public decimal DinarskaSatnica2 { get; set; }

    /// <summary>Dinarska satnica 3 (DINSAT3, N12.2).</summary>
    public decimal DinarskaSatnica3 { get; set; }

    /// <summary>Dinarska satnica — ukupno (DINSATSVE, N12.2).</summary>
    public decimal DinarskaSatnicaUkupno { get; set; }

    /// <summary>Časovna satnica 1 (CASSAT1, N12.2).</summary>
    public decimal CasovnaSatnica1 { get; set; }

    /// <summary>Časovna satnica 2 (CASSAT2, N12.2).</summary>
    public decimal CasovnaSatnica2 { get; set; }

    /// <summary>Časovna satnica 3 (CASSAT3, N12.2).</summary>
    public decimal CasovnaSatnica3 { get; set; }

    /// <summary>Časovna satnica — ukupno (CASSATSVE, N12.2).</summary>
    public decimal CasovnaSatnicaUkupno { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  STIMULACIJE / BONUSI
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Stimulacija — minimalna (STIMIN, N9.2).</summary>
    public decimal StimulacijaMin { get; set; }

    /// <summary>Stimulacija — godišnja (STIMGOD, N9.2).</summary>
    public decimal StimulacijaGodisnja { get; set; }

    /// <summary>Stimulacija 1 (STIM1, N9.2).</summary>
    public decimal Stimulacija1 { get; set; }

    /// <summary>Stimulacija 2 (STIM2, N9.2).</summary>
    public decimal Stimulacija2 { get; set; }

    /// <summary>Stimulacija 3 (STIM3, N9.2).</summary>
    public decimal Stimulacija3 { get; set; }

    /// <summary>Destimulacija 1 (DESTIM1, N9.2).</summary>
    public decimal Destimulacija1 { get; set; }

    /// <summary>Destimulacija 2 (DESTIM2, N9.2).</summary>
    public decimal Destimulacija2 { get; set; }

    /// <summary>Destimulacija 3 (DESTIM3, N9.2).</summary>
    public decimal Destimulacija3 { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  FOND RADNIH SATI I M4
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Fond radnih sati (FONDZ, N10.2).</summary>
    public decimal FondZarada { get; set; }

    /// <summary>Mesečni tip rada (MTR, N5).</summary>
    public int Mtr { get; set; }

    /// <summary>Dan tekućeg obračuna (DAN, C2).</summary>
    public string Dan { get; set; } = string.Empty;

    /// <summary>Mesec tekućeg obračuna (MESEC, C2).</summary>
    public string Mesec { get; set; } = string.Empty;

    /// <summary>Godina tekućeg obračuna (GODINA, C4).</summary>
    public string Godina { get; set; } = string.Empty;

    /// <summary>M4 mesec (M4MES, C2).</summary>
    public string M4Mesec { get; set; } = string.Empty;

    /// <summary>M4 dan (M4DAN, C2).</summary>
    public string M4Dan { get; set; } = string.Empty;

    /// <summary>M4 grad (M4GRAD, C2).</summary>
    public string M4Grad { get; set; } = string.Empty;

    // ══════════════════════════════════════════════════════════════════
    //  OSIGURANJE
    // ══════════════════════════════════════════════════════════════════

    /// <summary>LBO broj (LBOBROJ, C11).</summary>
    public string LboBroj { get; set; } = string.Empty;

    /// <summary>Broj zdravstvene knjižice (ZKBROJ, C11).</summary>
    public string ZkBroj { get; set; } = string.Empty;

    /// <summary>Datum osiguranja od (DATOSIG0).</summary>
    public DateTime? DatumOsiguranjaOd { get; set; }

    /// <summary>Datum osiguranja do (DATOSIG1).</summary>
    public DateTime? DatumOsiguranjaDo { get; set; }

    /// <summary>Registracioni broj socijalnog (REGSOC, C10).</summary>
    public string RegBrojSocijalno { get; set; } = string.Empty;

    /// <summary>Osnov osiguranja (OSNOVOSIG, C10).</summary>
    public string OsnovOsiguranja { get; set; } = string.Empty;

    // ══════════════════════════════════════════════════════════════════
    //  GODIŠNJI ODMOR
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Godišnji odmor — ukupan broj dana (GODUK, N9).</summary>
    public int GodisnjeDanaUkupno { get; set; }

    /// <summary>Godišnji odmor — iskorišćeno (GODISKOR, N9).</summary>
    public int GodisnjeDanaIskorisceno { get; set; }

    /// <summary>Godišnji odmor — neiskorišćeno (GODNEISKOR, N9).</summary>
    public int GodisnjeDanaNeiskorisceno { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  BANKOVNI PODACI
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Partija / broj tekućeg računa (PARTIJA, C20).</summary>
    public string Partija { get; set; } = string.Empty;

    /// <summary>Šifra banke (SIFRABAN, C3).</summary>
    public string SifraBanke { get; set; } = string.Empty;

    /// <summary>Žiro račun (ZIRORAC, C20).</summary>
    public string ZiroRacun { get; set; } = string.Empty;

    /// <summary>Samodoprinos šifra (SAMSIF, N4).</summary>
    public int SamodoprSifra { get; set; }

    /// <summary>Samodoprinos procenat (SAMOPROC, N8.4).</summary>
    public decimal SamodoprProcenat { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  UMANJENJA I MFP (MINIMALNA FISKALNA PLAĆANJA)
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Procenat umanjenja (PROCUMANJ, N9.4).</summary>
    public decimal ProcenatUmanjenja { get; set; }

    /// <summary>Vrsta umanjenja (UMANJENJE, C2).</summary>
    public string Umanjenje { get; set; } = string.Empty;

    /// <summary>Porsko umanjenje (PORUMANJ, N9.4).</summary>
    public decimal PorskoUmanjenje { get; set; }

    /// <summary>Doprinosno umanjenje (DOPUMANJ, N9.4).</summary>
    public decimal DoprinosnoUmanjenje { get; set; }

    /// <summary>PIO umanjenje — radnik (PIOUMANJR, N9.4).</summary>
    public decimal PioUmanjenjeRadnik { get; set; }

    /// <summary>PIO umanjenje — firma (PIOUMANJF, N9.4).</summary>
    public decimal PioUmanjenjeFirma { get; set; }

    /// <summary>MFP3 procenat (MFP3PROC, N9.2).</summary>
    public decimal Mfp3Procenat { get; set; }

    /// <summary>MFP6 iznos (MFP6, N12.2).</summary>
    public decimal Mfp6 { get; set; }

    /// <summary>MFP7 iznos (MFP7, N12.2).</summary>
    public decimal Mfp7 { get; set; }

    /// <summary>MFP8 nepuno radno vreme (MFP8NEPUN, C1).</summary>
    public string Mfp8Nepuno { get; set; } = string.Empty;

    /// <summary>MFP9 najniža osnova (MFP9NAJOSN, C1).</summary>
    public string Mfp9NajnizaOsnova { get; set; } = string.Empty;

    /// <summary>MFP10 dva vezana (MFP10DVEZ, C1).</summary>
    public string Mfp10DvaVezana { get; set; } = string.Empty;

    // ══════════════════════════════════════════════════════════════════
    //  RAZNE OZNAKE I FLAGOVI
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Prevoz (PREVOZ, C1).</summary>
    public string Prevoz { get; set; } = string.Empty;

    /// <summary>Topli obrok (TOPLI, C1).</summary>
    public string ToploObrok { get; set; } = string.Empty;

    /// <summary>ROPNR flag (ROPNR, C1).</summary>
    public string Ropnr { get; set; } = string.Empty;

    /// <summary>Pripravnik (PRIPRAV, C1).</summary>
    public string Pripravnik { get; set; } = string.Empty;

    /// <summary>Ocena rada (OCENA, C1).</summary>
    public string Ocena { get; set; } = string.Empty;

    /// <summary>Poreske olakšice (POROLAKS, C1).</summary>
    public string PoreskeOlaksice { get; set; } = string.Empty;

    /// <summary>Neaktivan (NEAKTIVAN, C1). "D" = neaktivan.</summary>
    public string Neaktivan { get; set; } = string.Empty;

    /// <summary>Email adresa (EMAIL, C60).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Roditelj (RODITELJ, C20).</summary>
    public string Roditelj { get; set; } = string.Empty;

    /// <summary>Bolovanje kod (BOLOVANJE, C2).</summary>
    public string Bolovanje { get; set; } = string.Empty;

    /// <summary>Obuka broj naredbe (OBUCBRNR, C2).</summary>
    public string ObukaBrojNaredbe { get; set; } = string.Empty;

    /// <summary>Obuka PP (OBUCPP, C2).</summary>
    public string ObukaPp { get; set; } = string.Empty;

    /// <summary>Sanitarni pregled datum (SANITARNI).</summary>
    public DateTime? SanitarniPregled { get; set; }

    /// <summary>Pripravnički ugovor (PRIPUG, C20).</summary>
    public string PripravnickiUgovor { get; set; } = string.Empty;

    /// <summary>Datum pripravničkog ugovora (PRIPDAT).</summary>
    public DateTime? PripravnickiDatum { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  NAPOMENE I ZADACI
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Napomena 1 (NAPOMENA, C60).</summary>
    public string Napomena1 { get; set; } = string.Empty;

    /// <summary>Napomena 2 (NAPOMENA2, C60).</summary>
    public string Napomena2 { get; set; } = string.Empty;

    /// <summary>Napomena 3 (NAPOMENA3, C60).</summary>
    public string Napomena3 { get; set; } = string.Empty;

    /// <summary>Napomena 4 (NAPOMENA4, C60).</summary>
    public string Napomena4 { get; set; } = string.Empty;

    /// <summary>Zadaci (ZADACI, C60).</summary>
    public string Zadaci { get; set; } = string.Empty;

    // ══════════════════════════════════════════════════════════════════
    //  SISTEMSKA POLJA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Flag brisanja (BRISANJE, C1). "D" = obrisan.</summary>
    public string Brisanje { get; set; } = string.Empty;

    /// <summary>Flag prenosa (PRENETO, C1).</summary>
    public string Preneto { get; set; } = string.Empty;

    /// <summary>ID broj zapisa (IDBR, N11).</summary>
    public long Idbr { get; set; }
}
