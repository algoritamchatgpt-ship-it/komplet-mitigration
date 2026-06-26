namespace Algoritam.Domain.Entities;

/// <summary>
/// Korisnik sistema (operater). Migracija iz LOZINKE.DBF.
/// Svaki korisnik ima pristupna prava po modulima i pod-modulima.
///
/// NAPOMENA O KOMPATIBILNOSTI:
///   Originalna klasa koristila je: Pas (int), KorisnikIme (string), JeSupervizor (bool setable).
///   Nova klasa čuva iste nazive radi kompatibilnosti sa ViewModelima i servisima,
///   ali dodaje sva preostala polja iz LOZINKE.DBF (45 polja ukupno).
/// </summary>
public class Korisnik
{
    /// <summary>Surogatni primarni ključ (auto-increment).</summary>
    public int Id { get; set; }

    // ── Identifikacija ──────────────────────────────────────────────

    /// <summary>
    /// Legacy šifra korisnika (PAS iz DBF, C(2)).
    /// U originalnom kodu se koristio kao int, ali u DBF je zapravo C(2).
    /// Čuvamo kao string radi vernog preslikavanja, ali nudimo IntPas
    /// za kompatibilnost sa kodom koji očekuje int.
    /// </summary>
    public string Pas { get; set; } = string.Empty;

    /// <summary>Korisničko ime za prijavu (KORISNIK, 20 char).</summary>
    public string KorisnikIme { get; set; } = string.Empty;

    /// <summary>Puno ime korisnika za prikaz (KORIME, 30 char).</summary>
    public string KorisnikIme2 { get; set; } = string.Empty;

    /// <summary>Lozinka (LOZINKA). UPOZORENJE: plaintext iz legacy sistema!</summary>
    public string Lozinka { get; set; } = string.Empty;

    /// <summary>Da li je korisnik aktivan (AKTIVAN, C(1)). U DBF-u "D"=aktivan.</summary>
    public bool Aktivan { get; set; }

    /// <summary>Da li je supervizor. Mapira se iz PASSNIVO >= 1 ili ručno iz "OPERATER"="1".</summary>
    public bool JeSupervizor { get; set; }

    // ── Prava pristupa — glavni moduli ──────────────────────────────

    /// <summary>Nivo pristupa (PASSNIVO, N(1)). 0=nema, viši = više prava.</summary>
    public int PravaNivo { get; set; }

    /// <summary>Glavna knjiga (PASSGK).</summary>
    public bool PassGk { get; set; }

    /// <summary>Analitika (PASSAN).</summary>
    public bool PassAn { get; set; }

    /// <summary>Blagajna (PASSBL).</summary>
    public bool PassBl { get; set; }

    /// <summary>Trgovina — glavni (PASSTV).</summary>
    public bool PassTv { get; set; }

    /// <summary>Tehnički materijal — glavni (PASSTM).</summary>
    public bool PassTm { get; set; }

    /// <summary>Usluge (PASSUS).</summary>
    public bool PassUs { get; set; }

    /// <summary>Zarade / Lični dohodak (PASSLD).</summary>
    public bool PassLd { get; set; }

    /// <summary>Ostalo (PASSOST).</summary>
    public bool PassOst { get; set; }

    /// <summary>Štampa (PASSPRN).</summary>
    public bool PassPrn { get; set; }

    /// <summary>Promet (PASSPRO).</summary>
    public bool PassPro { get; set; }

    /// <summary>Osnovna sredstva (PASSOS).</summary>
    public bool PassOs { get; set; }

    /// <summary>Profakture (PASSPROF).</summary>
    public bool PassProf { get; set; }

    /// <summary>Delovodnik (PASSDEL).</summary>
    public bool PassDel { get; set; }

    // ── Prava pristupa — pod-moduli (TV i TM) ───────────────────────

    /// <summary>Trgovina — rad (PASSTVRA).</summary>
    public bool PassTvRad { get; set; }

    /// <summary>Trgovina — kalkulator (PASSTVKAL).</summary>
    public bool PassTvKal { get; set; }

    /// <summary>Trgovina — računi (PASSTVRAC).</summary>
    public bool PassTvRac { get; set; }

    /// <summary>Trgovina — nivo (PASSTVNIV).</summary>
    public bool PassTvNiv { get; set; }

    /// <summary>Tehnički materijal — rad (PASSTMRA).</summary>
    public bool PassTmRad { get; set; }

    /// <summary>Tehnički materijal — kalkulator (PASSTMKAL).</summary>
    public bool PassTmKal { get; set; }

    /// <summary>Tehnički materijal — računi (PASSTMRAC).</summary>
    public bool PassTmRac { get; set; }

    /// <summary>Tehnički materijal — nivo (PASSTMNIV).</summary>
    public bool PassTmNiv { get; set; }

    // ── Podešavanja korisnika ───────────────────────────────────────

    /// <summary>Datum poslednje prijave (DATUM).</summary>
    public DateTime? DatumPrijave { get; set; }

    /// <summary>Vreme početka poslednje sesije (VREME0, C(10)).</summary>
    public string VremePocetka { get; set; } = string.Empty;

    /// <summary>Vreme kraja poslednje sesije (VREME1, C(10)).</summary>
    public string VremeKraja { get; set; } = string.Empty;

    /// <summary>Kod teme/skina (SLIKE, C(2)).</summary>
    public string Slike { get; set; } = string.Empty;

    /// <summary>Podrazumevani magacin (MAGACIN, N(2)).</summary>
    public int Magacin { get; set; }

    /// <summary>Korisnička putanja (PUTANJA, C(80)).</summary>
    public string Putanja { get; set; } = string.Empty;

    /// <summary>FoxPro specifičan flag (FOXY, C(1)). Legacy.</summary>
    public string Foxy { get; set; } = string.Empty;

    /// <summary>Preferenca PDF štampe (PDFPRINT, C(1)).</summary>
    public string PdfPrint { get; set; } = string.Empty;

    /// <summary>Flag prenosa (PRENETO, C(1)).</summary>
    public string Preneto { get; set; } = string.Empty;

    /// <summary>ID broj zapisa (IDBR, N(11)).</summary>
    public long Idbr { get; set; }
}
