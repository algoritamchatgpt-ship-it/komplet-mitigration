using Algoritam.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Algoritam.Infrastructure.Data;

/// <summary>
/// EF Core DbContext za per-company SQLite bazu (algoritam.db).
/// Svaka firma (F1, F2...) dobija svoju bazu sa tabelama:
/// Korisnici, Firma, Radnici.
/// </summary>
public class FirmaDbContext : DbContext
{
    private readonly string _dbPath;

    /// <summary>
    /// Kreira kontekst koji će koristiti SQLite bazu na zadatoj putanji.
    /// </summary>
    /// <param name="dbPath">Puna putanja do .db fajla (npr. "C:\FIN\F1\zarade\algoritam.db").</param>
    public FirmaDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    /// <summary>
    /// Konstruktor za DI/options patern (opciono, za buduću upotrebu).
    /// </summary>
    public FirmaDbContext(DbContextOptions<FirmaDbContext> options)
        : base(options)
    {
        _dbPath = string.Empty; // Preuzima se iz options
    }

    public DbSet<Korisnik> Korisnici => Set<Korisnik>();
    public DbSet<Firma> Firma => Set<Firma>();
    public DbSet<LdParametar> LdParametri => Set<LdParametar>();
    public DbSet<LdKontoSablonStavka> LdKontoSablonStavke => Set<LdKontoSablonStavka>();
    public DbSet<LdKnjizenjeStavka> LdKnjizenjeStavke => Set<LdKnjizenjeStavka>();
    public DbSet<LdObracunStavka> LdObracunStavke => Set<LdObracunStavka>();
    public DbSet<LdPodStavka> LdPodStavke => Set<LdPodStavka>();
    public DbSet<LdSpisStavka> LdSpisStavke => Set<LdSpisStavka>();
    public DbSet<Radnik> Radnici => Set<Radnik>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_dbPath))
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        KonfigurisiKorisnika(modelBuilder);
        KonfigurisiFirmu(modelBuilder);
        KonfigurisiLdParametre(modelBuilder);
        KonfigurisiLdKontoSablonStavke(modelBuilder);
        KonfigurisiLdKnjizenjeStavke(modelBuilder);
        KonfigurisiLdObracunStavke(modelBuilder);
        KonfigurisiLdPodStavke(modelBuilder);
        KonfigurisiLdSpisStavke(modelBuilder);
        KonfigurisiRadnika(modelBuilder);
    }

    // ══════════════════════════════════════════════════════════════════
    //  KORISNIK (iz LOZINKE.DBF, 45 polja)
    // ══════════════════════════════════════════════════════════════════

    private static void KonfigurisiKorisnika(ModelBuilder mb)
    {
        var e = mb.Entity<Korisnik>();

        e.ToTable("Korisnici");
        e.HasKey(k => k.Id);
        e.Property(k => k.Id).ValueGeneratedOnAdd();

        // Legacy poslovni ključ — PAS je C(2) u DBF-u, čuvamo kao string
        e.Property(k => k.Pas).HasMaxLength(2).IsRequired();
        e.HasIndex(k => k.Pas).IsUnique();

        // Korisničko ime — unique
        e.Property(k => k.KorisnikIme).HasMaxLength(20).IsRequired();
        e.HasIndex(k => k.KorisnikIme).IsUnique();

        e.Property(k => k.KorisnikIme2).HasMaxLength(30);
        e.Property(k => k.Lozinka).HasMaxLength(10);

        // Nivo pristupa
        e.Property(k => k.PravaNivo).HasDefaultValue(0);

        // Prava pristupa — svi bool-ovi (stored as INTEGER 0/1 in SQLite)
        // EF Core automatski mapira bool -> INTEGER, ne treba specijalna konfiguracija.

        // Podešavanja
        e.Property(k => k.VremePocetka).HasMaxLength(10);
        e.Property(k => k.VremeKraja).HasMaxLength(10);
        e.Property(k => k.Slike).HasMaxLength(2);
        e.Property(k => k.Putanja).HasMaxLength(80);
        e.Property(k => k.Foxy).HasMaxLength(1);
        e.Property(k => k.PdfPrint).HasMaxLength(1);
        e.Property(k => k.Preneto).HasMaxLength(1);
    }

    // ══════════════════════════════════════════════════════════════════
    //  FIRMA (iz FIRMA.DBF, 115 polja)
    // ══════════════════════════════════════════════════════════════════

    private static void KonfigurisiFirmu(ModelBuilder mb)
    {
        var e = mb.Entity<Firma>();

        e.ToTable("Firma");
        e.HasKey(f => f.Id);

        // Runtime-only polja — ne čuvaju se u bazi
        e.Ignore(f => f.Aktivna);
        e.Ignore(f => f.FolderPath);

        // Naziv
        e.Property(f => f.Naziv).HasMaxLength(50).IsRequired();
        e.Property(f => f.Naziv2).HasMaxLength(50);
        e.Property(f => f.Baza).HasMaxLength(20);

        // Adresa
        e.Property(f => f.PostanskiBroj).HasMaxLength(5);
        e.Property(f => f.Mesto).HasMaxLength(25);
        e.Property(f => f.Ulica).HasMaxLength(25);
        e.Property(f => f.BrojUlice).HasMaxLength(10);
        e.Property(f => f.Republika).HasMaxLength(25);
        e.Property(f => f.Drzava).HasMaxLength(25);

        // Žiro računi
        e.Property(f => f.ZiroRacun).HasMaxLength(30);
        e.Property(f => f.ZiroRacun2).HasMaxLength(30);
        e.Property(f => f.ZiroRacun3).HasMaxLength(30);
        e.Property(f => f.ZiroRacun4).HasMaxLength(30);
        e.Property(f => f.ZiroRacun5).HasMaxLength(30);
        e.Property(f => f.ZiroRacun6).HasMaxLength(30);
        e.Property(f => f.ZiroRacunBolovanje).HasMaxLength(30);
        e.Property(f => f.ZiroRacunDevizni).HasMaxLength(30);

        // Kontrolni kodovi žiro računa
        e.Property(f => f.KontrolniKodZiro1).HasMaxLength(8);
        e.Property(f => f.KontrolniKodZiro2).HasMaxLength(8);
        e.Property(f => f.KontrolniKodZiro3).HasMaxLength(8);
        e.Property(f => f.KontrolniKodZiro4).HasMaxLength(8);
        e.Property(f => f.KontrolniKodZiro5).HasMaxLength(8);
        e.Property(f => f.KontrolniKodZiro6).HasMaxLength(8);
        e.Property(f => f.KontrolniKodZiroDevizni).HasMaxLength(8);
        e.Property(f => f.KontrolniKodZiroBolovanje).HasMaxLength(8);

        // Banke — nazivi
        e.Property(f => f.Banka1).HasMaxLength(30);
        e.Property(f => f.Banka2).HasMaxLength(30);
        e.Property(f => f.Banka3).HasMaxLength(30);
        e.Property(f => f.Banka4).HasMaxLength(30);
        e.Property(f => f.Banka5).HasMaxLength(30);
        e.Property(f => f.Banka6).HasMaxLength(30);
        e.Property(f => f.BankaBolovanje).HasMaxLength(30);
        e.Property(f => f.BankaDevizna).HasMaxLength(30);

        // Banke — šifre
        e.Property(f => f.BankaSifra1).HasMaxLength(8);
        e.Property(f => f.BankaSifra2).HasMaxLength(8);
        e.Property(f => f.BankaSifra3).HasMaxLength(8);
        e.Property(f => f.BankaSifra4).HasMaxLength(8);
        e.Property(f => f.BankaSifra5).HasMaxLength(8);
        e.Property(f => f.BankaSifra6).HasMaxLength(8);
        e.Property(f => f.BankaSifraBolovanje).HasMaxLength(8);
        e.Property(f => f.BankaSifraDevizna).HasMaxLength(8);

        // Kontakt
        e.Property(f => f.Telefon1).HasMaxLength(20);
        e.Property(f => f.Telefon2).HasMaxLength(20);
        e.Property(f => f.Telefon3).HasMaxLength(20);
        e.Property(f => f.Fax1).HasMaxLength(20);
        e.Property(f => f.Fax2).HasMaxLength(20);
        e.Property(f => f.Fax3).HasMaxLength(20);
        e.Property(f => f.Email).HasMaxLength(40);
        e.Property(f => f.Web).HasMaxLength(40);

        // Identifikatori
        e.Property(f => f.SifraDelatnosti).HasMaxLength(10);
        e.Property(f => f.NazivDelatnosti).HasMaxLength(30);
        e.Property(f => f.SdkKod).HasMaxLength(3);
        e.Property(f => f.Maticni).HasMaxLength(9);
        e.Property(f => f.Pib).HasMaxLength(16);
        e.Property(f => f.DopunskiPorBroj).HasMaxLength(24);
        e.Property(f => f.Vlasnik).HasMaxLength(30);
        e.Property(f => f.Agencija).HasMaxLength(50);
        e.Property(f => f.FpaSlrn).HasMaxLength(1);
        e.Property(f => f.Opstina).HasMaxLength(30);

        // Registracioni brojevi
        e.Property(f => f.RegBrojSocijalno).HasMaxLength(12);
        e.Property(f => f.RegBrojZdravstveno).HasMaxLength(12);
        e.Property(f => f.SudskiRegistar).HasMaxLength(20);
        e.Property(f => f.RegNaziv).HasMaxLength(30);

        // Vlasnik / odgovorno lice
        e.Property(f => f.PibSavetnika).HasMaxLength(9);
        e.Property(f => f.JmbgVlasnika1).HasMaxLength(13);
        e.Property(f => f.MbSavetnika).HasMaxLength(13);
        e.Property(f => f.JmbgVlasnika).HasMaxLength(13);
        e.Property(f => f.JmbgKontaktBroj).HasMaxLength(25);
        e.Property(f => f.RepublikaKod).HasMaxLength(3);
        e.Property(f => f.PorBrojRepublicke).HasMaxLength(24);
        e.Property(f => f.OdgovornoLice).HasMaxLength(27);
        e.Property(f => f.UplaceniKapital).HasMaxLength(20);
        e.Property(f => f.UpisaniKapital).HasMaxLength(20);
        e.Property(f => f.Naznaka).HasMaxLength(20);
        e.Property(f => f.SwiftKod).HasMaxLength(20);

        // Poreske oznake
        e.Property(f => f.PdvObveznik).HasMaxLength(1);
        e.Property(f => f.OrganizacioniOblik).HasMaxLength(20);
        e.Property(f => f.ZdravstvenaUstanova).HasMaxLength(40);
        e.Property(f => f.ProdajniCentar).HasMaxLength(1);
        e.Property(f => f.ZatvorenaGodina).HasMaxLength(1);
        e.Property(f => f.BudzetskiKorisnik).HasMaxLength(2);

        // Konta
        e.Property(f => f.KontoGotovina).HasMaxLength(10);
        e.Property(f => f.KontoCek).HasMaxLength(10);
        e.Property(f => f.KontoVirman).HasMaxLength(10);
        e.Property(f => f.KontoKartica).HasMaxLength(10);
        e.Property(f => f.KontoOstalo).HasMaxLength(10);

        // Ostalo
        e.Property(f => f.SifraFizickogLica).HasMaxLength(2);
        e.Property(f => f.Pumpe).HasMaxLength(1);
        e.Property(f => f.Sn).HasMaxLength(1);
        e.Property(f => f.OznakaJavnogPreduzeca).HasMaxLength(4);
        e.Property(f => f.Jbbk).HasMaxLength(5);

        // Latinica
        e.Property(f => f.NazivLatinican).HasMaxLength(50);
        e.Property(f => f.MestoLatinicom).HasMaxLength(25);
        e.Property(f => f.UlicaLatinicom).HasMaxLength(30);
        e.Property(f => f.RepublikaLatinicom).HasMaxLength(50);
        e.Property(f => f.OpstinaLatinicom).HasMaxLength(30);

        // System
        e.Property(f => f.Preneto).HasMaxLength(1);
        e.Property(f => f.Jbkjs).HasMaxLength(10);
        e.Property(f => f.BrojPoste).HasMaxLength(8);
    }

    private static void KonfigurisiLdParametre(ModelBuilder mb)
    {
        var e = mb.Entity<LdParametar>();

        e.ToTable("LdParametri");
        e.HasKey(p => p.Id);

        e.Property(p => p.Nazmes).HasMaxLength(10);
        e.Property(p => p.S1).HasMaxLength(3);
        e.Property(p => p.S3).HasMaxLength(3);
        e.Property(p => p.S4).HasMaxLength(3);
        e.Property(p => p.S5).HasMaxLength(3);
        e.Property(p => p.S6).HasMaxLength(3);
        e.Property(p => p.S71).HasMaxLength(3);
        e.Property(p => p.S72).HasMaxLength(3);
        e.Property(p => p.S8).HasMaxLength(3);
        e.Property(p => p.Snazmes).HasMaxLength(10);
        e.Property(p => p.Kd1).HasMaxLength(3);
        e.Property(p => p.Kd4).HasMaxLength(5);
        e.Property(p => p.Kd9).HasMaxLength(3);
        e.Property(p => p.Kd12).HasMaxLength(7);
        e.Property(p => p.Kd20).HasMaxLength(2);
        e.Property(p => p.Kd22).HasMaxLength(2);
        e.Property(p => p.Kd24).HasMaxLength(1);
        e.Property(p => p.Kd25).HasMaxLength(2);
        e.Property(p => p.Kd27).HasMaxLength(1);
        e.Property(p => p.Kd28).HasMaxLength(1);
        e.Property(p => p.Godina).HasMaxLength(4);
        e.Property(p => p.Nazp1).HasMaxLength(10);
        e.Property(p => p.Nazp2).HasMaxLength(10);
        e.Property(p => p.Nazp3).HasMaxLength(10);
        e.Property(p => p.Nazp4).HasMaxLength(10);
        e.Property(p => p.Nazp5).HasMaxLength(10);
        e.Property(p => p.Nazp5ter).HasMaxLength(10);
        e.Property(p => p.Nazo1).HasMaxLength(10);
        e.Property(p => p.Nazo2).HasMaxLength(10);
        e.Property(p => p.Nazo3).HasMaxLength(10);
        e.Property(p => p.Nazo4).HasMaxLength(10);
        e.Property(p => p.Nazo5).HasMaxLength(10);
        e.Property(p => p.Nazo6).HasMaxLength(10);
        e.Property(p => p.Decimale).HasMaxLength(1);
        e.Property(p => p.Srazpor).HasMaxLength(1);
        e.Property(p => p.Konacna).HasMaxLength(1);
        e.Property(p => p.Vrstaplate).HasMaxLength(1);
        e.Property(p => p.Arhiva).HasMaxLength(1);
        e.Property(p => p.Arhiva2).HasMaxLength(1);
        e.Property(p => p.Bknacin).HasMaxLength(1);
        e.Property(p => p.Nakpos).HasMaxLength(1);
        e.Property(p => p.Preneto).HasMaxLength(1);
    }

    private static void KonfigurisiLdSpisStavke(ModelBuilder mb)
    {
        var e = mb.Entity<LdSpisStavka>();

        e.ToTable("LdSpisStavke");
        e.HasKey(p => p.Id);
        e.Property(p => p.Id).ValueGeneratedOnAdd();

        e.Property(p => p.ImePrez).HasMaxLength(30);
        e.Property(p => p.Partija).HasMaxLength(20);
        e.Property(p => p.Sifra).HasMaxLength(5);
        e.Property(p => p.Preneto).HasMaxLength(1);
    }

    private static void KonfigurisiLdKontoSablonStavke(ModelBuilder mb)
    {
        var e = mb.Entity<LdKontoSablonStavka>();

        e.ToTable("LdKontoSablonStavke");
        e.HasKey(p => p.Id);
        e.Property(p => p.Id).ValueGeneratedOnAdd();

        e.Property(p => p.Vrsta).HasMaxLength(1);
        e.Property(p => p.Kod).HasMaxLength(7);
        e.Property(p => p.Opis).HasMaxLength(27);
        e.Property(p => p.Konto).HasMaxLength(10);
        e.Property(p => p.Kontop).HasMaxLength(10);
        e.Property(p => p.Preneto).HasMaxLength(1);
    }

    private static void KonfigurisiLdKnjizenjeStavke(ModelBuilder mb)
    {
        var e = mb.Entity<LdKnjizenjeStavka>();

        e.ToTable("LdKnjizenjeStavke");
        e.HasKey(p => p.Id);
        e.Property(p => p.Id).ValueGeneratedOnAdd();

        e.Property(p => p.Vrsta).HasMaxLength(1);
        e.Property(p => p.Kod).HasMaxLength(7);
        e.Property(p => p.Opis).HasMaxLength(27);
        e.Property(p => p.Konto).HasMaxLength(10);
        e.Property(p => p.Kontop).HasMaxLength(10);
        e.Property(p => p.Brnal).HasMaxLength(6);
        e.Property(p => p.Mp).HasMaxLength(2);
        e.Property(p => p.Preneto).HasMaxLength(1);
    }

    private static void KonfigurisiLdObracunStavke(ModelBuilder mb)
    {
        var e = mb.Entity<LdObracunStavka>();

        e.ToTable("LdObracunStavke");
        e.HasKey(p => p.Id);
        e.Property(p => p.Id).ValueGeneratedOnAdd();

        e.Property(p => p.Sifraprih).HasMaxLength(9);
        e.Property(p => p.ImePrez).HasMaxLength(30);
        e.Property(p => p.Nazmes).HasMaxLength(10);
        e.Property(p => p.Godina).HasMaxLength(4);
        e.Property(p => p.Vrsta).HasMaxLength(1);
        e.Property(p => p.Evidbroj).HasMaxLength(8);
        e.Property(p => p.Maticnibr).HasMaxLength(13);
        e.Property(p => p.Idbroj).HasMaxLength(11);
        e.Property(p => p.Dok).HasMaxLength(3);
        e.Property(p => p.Arhiva).HasMaxLength(1);
        e.Property(p => p.Arhiva2).HasMaxLength(1);
    }

    private static void KonfigurisiLdPodStavke(ModelBuilder mb)
    {
        var e = mb.Entity<LdPodStavka>();

        e.ToTable("LdPodStavke");
        e.HasKey(p => p.Id);
        e.Property(p => p.Id).ValueGeneratedOnAdd();

        e.Property(p => p.Kod).HasMaxLength(7);
        e.Property(p => p.Opis).HasMaxLength(27);
        e.Property(p => p.Vrsta).HasMaxLength(1);
        e.Property(p => p.Preneto).HasMaxLength(1);
    }

    // ══════════════════════════════════════════════════════════════════
    //  RADNIK (iz LDRAD.DBF, 158 polja)
    // ══════════════════════════════════════════════════════════════════

    private static void KonfigurisiRadnika(ModelBuilder mb)
    {
        var e = mb.Entity<Radnik>();

        e.ToTable("Radnici");
        e.HasKey(r => r.Id);
        e.Property(r => r.Id).ValueGeneratedOnAdd();

        // Poslovni ključ — broj radnika, unique
        e.HasIndex(r => r.Broj).IsUnique();

        // Identifikacija
        e.Property(r => r.ImePrezime).HasMaxLength(30);
        e.Property(r => r.Prezime).HasMaxLength(20);
        e.Property(r => r.Ime).HasMaxLength(20);
        e.Property(r => r.VrstaId).HasMaxLength(1);
        e.Property(r => r.MaticniBroj).HasMaxLength(13);
        e.Property(r => r.IdBroj).HasMaxLength(11);
        e.Property(r => r.EvidencijskiBroj).HasMaxLength(8);
        e.Property(r => r.Pol).HasMaxLength(1);

        // Adresa
        e.Property(r => r.Adresa).HasMaxLength(40);
        e.Property(r => r.Posta).HasMaxLength(5);
        e.Property(r => r.Mesto).HasMaxLength(25);
        e.Property(r => r.Telefon).HasMaxLength(20);
        e.Property(r => r.Drzava).HasMaxLength(3);
        e.Property(r => r.Opstina).HasMaxLength(3);
        e.Property(r => r.OpstinaRada).HasMaxLength(3);
        e.Property(r => r.Prebivaliste).HasMaxLength(3);

        // Organizacija
        e.Property(r => r.Sifra).HasMaxLength(5);
        e.Property(r => r.RadnoMesto).HasMaxLength(30);
        e.Property(r => r.RadnoMestoDetalj).HasMaxLength(40);
        e.Property(r => r.PoslovnaJedinica).HasMaxLength(2);
        e.Property(r => r.SifraOrganizacije).HasMaxLength(10);
        e.Property(r => r.IzvorFinansiranja).HasMaxLength(3);
        e.Property(r => r.GrupaVirmana).HasMaxLength(2);
        e.Property(r => r.IdProfesionalniKod).HasMaxLength(2);
        e.Property(r => r.IdSektor).HasMaxLength(2);
        e.Property(r => r.IdPodsektor).HasMaxLength(3);
        e.Property(r => r.IdLokacija).HasMaxLength(2);
        e.Property(r => r.IdLokacijaPod).HasMaxLength(2);
        e.Property(r => r.Dokument).HasMaxLength(3);
        e.Property(r => r.MestoPrimanja).HasMaxLength(2);
        e.Property(r => r.MestoPoreza).HasMaxLength(5);
        e.Property(r => r.MestoTroskova).HasMaxLength(10);

        // Stručna sprema
        e.Property(r => r.Stepen).HasMaxLength(3);
        e.Property(r => r.SkolskaSprema).HasMaxLength(15);
        e.Property(r => r.Sprema).HasMaxLength(3);
        e.Property(r => r.SifraZanimanja).HasMaxLength(8);

        // Vrsta zaposlenja
        e.Property(r => r.VrstaZaposlenja).HasMaxLength(2);
        e.Property(r => r.VrstaPrimanja).HasMaxLength(2);
        e.Property(r => r.OznakaVrstePrihoda).HasMaxLength(3);
        e.Property(r => r.OznakaOlaksica).HasMaxLength(2);
        e.Property(r => r.OznakaBeneficije).HasMaxLength(1);
        e.Property(r => r.TipSluzbe).HasMaxLength(4);
        e.Property(r => r.PlatnaGrupa).HasMaxLength(3);
        e.Property(r => r.GodinaNapredovanja).HasMaxLength(4);
        e.Property(r => r.GrupaNamestenja).HasMaxLength(4);
        e.Property(r => r.ProcenatAngazovanja).HasMaxLength(3);
        e.Property(r => r.Katalog).HasMaxLength(1);
        e.Property(r => r.Vrsta).HasMaxLength(1);

        // Decimal polja — koeficijenti i zarada
        // SQLite čuva REAL, EF Core konvertuje u decimal.
        // Za finansijsku preciznost: decimal u C# je dovoljan za lokalni desktop obračun.

        // M4 / fond
        e.Property(r => r.Dan).HasMaxLength(2);
        e.Property(r => r.Mesec).HasMaxLength(2);
        e.Property(r => r.Godina).HasMaxLength(4);
        e.Property(r => r.M4Mesec).HasMaxLength(2);
        e.Property(r => r.M4Dan).HasMaxLength(2);
        e.Property(r => r.M4Grad).HasMaxLength(2);

        // Osiguranje
        e.Property(r => r.LboBroj).HasMaxLength(11);
        e.Property(r => r.ZkBroj).HasMaxLength(11);
        e.Property(r => r.RegBrojSocijalno).HasMaxLength(10);
        e.Property(r => r.OsnovOsiguranja).HasMaxLength(10);

        // Bankovni
        e.Property(r => r.Partija).HasMaxLength(20);
        e.Property(r => r.SifraBanke).HasMaxLength(3);
        e.Property(r => r.ZiroRacun).HasMaxLength(20);

        // Umanjenja
        e.Property(r => r.Umanjenje).HasMaxLength(2);
        e.Property(r => r.Mfp8Nepuno).HasMaxLength(1);
        e.Property(r => r.Mfp9NajnizaOsnova).HasMaxLength(1);
        e.Property(r => r.Mfp10DvaVezana).HasMaxLength(1);

        // Oznake
        e.Property(r => r.Prevoz).HasMaxLength(1);
        e.Property(r => r.ToploObrok).HasMaxLength(1);
        e.Property(r => r.Ropnr).HasMaxLength(1);
        e.Property(r => r.Pripravnik).HasMaxLength(1);
        e.Property(r => r.Ocena).HasMaxLength(1);
        e.Property(r => r.PoreskeOlaksice).HasMaxLength(1);
        e.Property(r => r.Neaktivan).HasMaxLength(1);
        e.Property(r => r.Email).HasMaxLength(60);
        e.Property(r => r.Roditelj).HasMaxLength(20);
        e.Property(r => r.Bolovanje).HasMaxLength(2);
        e.Property(r => r.ObukaBrojNaredbe).HasMaxLength(2);
        e.Property(r => r.ObukaPp).HasMaxLength(2);
        e.Property(r => r.PripravnickiUgovor).HasMaxLength(20);

        // Napomene
        e.Property(r => r.Napomena1).HasMaxLength(60);
        e.Property(r => r.Napomena2).HasMaxLength(60);
        e.Property(r => r.Napomena3).HasMaxLength(60);
        e.Property(r => r.Napomena4).HasMaxLength(60);
        e.Property(r => r.Zadaci).HasMaxLength(60);

        // Sistemska polja
        e.Property(r => r.Brisanje).HasMaxLength(1);
        e.Property(r => r.Preneto).HasMaxLength(1);

        // Indeksi za brzo pretraživanje
        e.HasIndex(r => r.Prezime);
        e.HasIndex(r => r.MaticniBroj);
        e.HasIndex(r => r.PoslovnaJedinica);
    }
}
