using Algoritam.Application.Services;
using Algoritam.Domain.Entities;

namespace Algoritam.Infrastructure.Services;

/// <summary>
/// Implementacija obračuna zarade — verna translacija FoxPro procedura:
///   LDOBRACUN.PRG  (glavni obračun + helper procedure)
///   OBRACSOC.PRG   (doprinosi i porez)
/// </summary>
public class ObracunService : IObracunService
{
    public void Obracunaj(
        LdObracunStavka s,
        Radnik r,
        LdParametar p,
        bool obracunatiNaknade = false,
        bool obracunOdUkupneObaveze = false)
    {
        // ── Parametri iz LDPARAM ─────────────────────────────────────────
        var procNoc = p.Procnoc;
        var procProd = p.Procprod;
        var procNed = p.Procned;
        var procBol = p.Procbol;
        var procPraz = p.Procpraz;
        var procPlac = p.Procplac;
        var procSus = p.Procsus;
        var procMin = p.Procmin;
        var czakon = p.Czakon;
        var minnac = p.Minnac;
        var ekoefs = p.Ekoefs == 0 ? 1m : p.Ekoefs;
        var procPor = p.Procpor;
        var neoporezP = p.Neoporezp;
        var neoporez = p.Neoporez;
        var srazpor = p.Srazpor;
        var sdin1 = p.Sdin1;
        var prosbruto = p.Prosbruto;
        var redispl = p.Redispl == 0 ? 1 : p.Redispl;
        var isplata = p.Isplata == 0 ? 1 : p.Isplata;
        var nakpos = p.Nakpos;
        var bkproc = p.Bkproc;
        var bkzastita = p.Bkzastita;
        var priprav = p.Priprav;

        var solporod1 = p.Solporod1;
        var solpordo1 = p.Solpordo1;
        var solporod2 = p.Solporod2;
        var solproc1 = p.Solproc1;
        var solproc2 = p.Solproc2;

        int mdec = string.IsNullOrWhiteSpace(p.Decimale) ? 0 : 2;

        // ── Kopiranje sa radnika ─────────────────────────────────────────
        s.Godina = p.Godina;
        s.Evidbroj = r.EvidencijskiBroj;
        s.ImePrez = r.ImePrezime;
        s.Maticnibr = r.MaticniBroj;
        s.Idbroj = r.IdBroj;
        s.Benproc = r.BeneficiraniProcenat;
        s.Grupa = r.Grupa;
        s.Grupa1 = r.Grupa1;
        s.Mtr = r.Mtr;
        s.Mesec = p.Mesec;

        // ── Cena rada i startni bod ──────────────────────────────────────
        // U FoxPro originalu (LDOBRACUN.PRG) koristi se isključivo LDRAD.STARTBOD,
        // koje je u Fox-u popunjeno kroz LDRAD.SCX form valid logiku. Kad korisnik
        // unosi radnika kroz C# WPF formu i upisuje samo "Koeficijent", STARTBOD
        // ostaje 0 i obračun pada. Fallback: ako STARTBOD nije postavljen, uzmi
        // koeficijent ukupni (ili osnovni koeficijent) — paritet sa Fox ponašanjem.
        decimal mc = p.Cenarada;
        decimal startbod = r.StartniBodovi;
        if (startbod == 0m)
        {
            startbod = r.KoeficijentUkupni != 0m
                ? r.KoeficijentUkupni
                : r.Koeficijent;
        }
        decimal sb = czakon != 0
            ? Math.Round(startbod * ekoefs / czakon, 8)
            : 0;

        s.Cenarada = mc;

        // ── Obračun časovi → dinari ──────────────────────────────────────
        if (obracunOdUkupneObaveze)
        {
            var edopf = p.Doppf1 + p.Dopzf1 + p.Dopnf1;
            var mfiksna1 = Math.Round(startbod * 100 / (100 + edopf), mdec);
            var mfiksna2 = mfiksna1 - s.Topli - s.Regres;
            var mfiksna3 = Math.Round(mfiksna2 * 100 / (100 + (procMin * r.Staz)), mdec);
            var sb2 = p.Czakon != 0 ? Math.Round(mfiksna3 / p.Czakon, 8) : 0m;

            s.Dinvr = Math.Round(s.Casvr * sb2, mdec);
            s.Dinuc = Math.Round(s.Casuc * sb2, mdec);
            s.Dinpriprav = Math.Round(s.Caspriprav * sb2 * priprav / 100, mdec);
            s.Dinnoc = Math.Round(s.Casnoc * sb2 * procNoc / 100, mdec);
            s.Dinprod = Math.Round(s.Casprod * sb2 * procProd / 100, mdec);
            s.Dinradnap = Math.Round(s.Casradnap * sb2 * procPraz / 100, mdec);
            s.Dinned = Math.Round(s.Casned * sb2 * procNed / 100, mdec);
            s.Dindor = Math.Round(s.Casdor * sb2, mdec);
            s.Dinsl = Math.Round(s.Cslput * sb2, mdec);

            // Naknade (SB2)
            s.Dinpraz = Math.Round(s.Caspraz * sb2, mdec);
            s.Dinbol = Math.Round(s.Casbol * sb2 * procBol / 100, mdec);
            s.Dinbol2 = Math.Round(s.Casbol2 * sb2, mdec);
            s.Dinplac = Math.Round(s.Casplac * sb2 * procPlac / 100, mdec);
            s.Dinplac2 = Math.Round(s.Casplac2 * sb2, mdec);
            s.Dingod = Math.Round(s.Casgod * sb2, mdec);
            s.Dinvv = Math.Round(s.Casvv * sb2, mdec);
        }
        else
        {
            s.Dinvr = Math.Round(s.Casuc * sb * mc, mdec);
            s.Dinuc = Math.Round(s.Casuc * sb * mc, mdec);
            s.Dinpriprav = Math.Round(s.Caspriprav * sb * mc * priprav / 100, mdec);
            s.Dinnoc = Math.Round(s.Casnoc * sb * mc * procNoc / 100, mdec);
            s.Dinprod = Math.Round(s.Casprod * sb * mc * procProd / 100, mdec);
            s.Dinradnap = Math.Round(s.Casradnap * sb * mc * procPraz / 100, mdec);
            s.Dinned = Math.Round(s.Casned * sb * mc * procNed / 100, mdec);
            s.Dindor = Math.Round(s.Casdor * sb * mc, mdec);
            s.Dinsl = Math.Round(s.Cslput * sb, mdec);

            // Naknade (opciono)
            if (obracunatiNaknade)
            {
                s.Dinpraz = Math.Round(s.Caspraz * sb * mc, mdec);
                s.Dinbol = Math.Round(s.Casbol * sb * mc * procBol / 100, mdec);
                s.Dinbol2 = Math.Round(s.Casbol2 * sb * mc, mdec);
                s.Dinplac = Math.Round(s.Casplac * sb * mc * procPlac / 100, mdec);
                s.Dinplac2 = Math.Round(s.Casplac2 * sb * mc, mdec);
                s.Dingod = Math.Round(s.Casgod * sb * mc, mdec);
                s.Dinvv = Math.Round(s.Casvv * sb * mc, mdec);
            }
        }

        // Fox ldobracun.prg 121-124: DIN1/DIN2/DIN3/DINSUS uvek SB*MC (posle if/endif)
        s.Din1   = Math.Round(s.Cas1   * sb * mc, mdec);
        s.Din2   = Math.Round(s.Cas2   * sb * mc, mdec);
        s.Din3   = Math.Round(s.Cas3   * sb * mc, mdec);
        s.Dinsus = Math.Round(s.Cassus * sb * mc * procSus / 100, mdec);

        // ── Minuli rad ───────────────────────────────────────────────────
        decimal mmin = s.Dinuc + s.Dinnoc + s.Dinpriprav + s.Dinprod + s.Dinradnap
                     + s.Dinpraz + s.Dinned + s.Dinbol + s.Dinbol2
                     + s.Dinplac + s.Dinplac2 + s.Dingod + s.Dinvv
                     + s.Dindor + s.Dinsl + s.Din1 + s.Din2 + s.Din3 + s.Dinsus;

        switch (minnac)
        {
            // Fox MINNAC=0: nema case → DINMIN ostaje 0 (bez minulog rada)
            case 1: // Fox MINNAC=1: minuli na sve
                s.Dinmin = Math.Round(mmin * procMin * r.Staz / 100, mdec);
                break;
            case 2: // Fox MINNAC=2: minuli samo na DINUC (redovni rad)
                s.Dinmin = Math.Round(s.Dinuc * procMin * r.Staz / 100, mdec);
                break;
            case 3: // Fox MINNAC=3: minuli na sve + FIKSNA + TOPLI
                s.Dinmin = Math.Round((mmin + s.Fiksna + s.Topli) * procMin * r.Staz / 100, mdec);
                break;
        }

        // ── SABERICAS ────────────────────────────────────────────────────
        if (nakpos != "D")
        {
            s.Casuk = s.Casuc + s.Caspraz + s.Casbol + s.Casbol2
                    + s.Casplac + s.Casplac2 + s.Casgod + s.Casdor + s.Cslput
                    + s.Cas1 + s.Cas2 + s.Cas3 + s.Casneplac;
        }
        else
        {
            s.Casuk = s.Casuc + s.Caspraz + s.Casbol + s.Casbol2
                    + s.Casplac + s.Casplac2 + s.Casgod + s.Casdor + s.Cslput
                    + s.Cas1 + s.Cas2 + s.Cas3 + s.Casneplac
                    + s.Casnoc + s.Casprod + s.Casned;
        }

        // ── SABERIDIN ────────────────────────────────────────────────────
        s.Dinuk = s.Dinuc + s.Dinnoc + s.Dinpriprav + s.Dinprod + s.Dinradnap
                + s.Dinned + s.Dinpraz + s.Dinbol + s.Dinbol2
                + s.Dinplac + s.Dinplac2 + s.Dingod + s.Dindor + s.Dinsl
                + s.Din1 + s.Din2 + s.Din3 + s.Dinmin;

        // ── OBRACSTIM ────────────────────────────────────────────────────
        // Fox ldobracun.prg 209-211: STIM = (DINUK - naknade) * STIMPROC / 100
        var dinUkZaStim = s.Dinuk - s.Dinpraz - s.Dinbol - s.Dinbol2
                        - s.Dinplac - s.Dinplac2 - s.Dingod;
        s.Stim1 = Math.Round(dinUkZaStim * s.Stim1proc / 100, mdec);
        s.Stim2 = Math.Round(dinUkZaStim * s.Stim2proc / 100, mdec);
        s.Stim3 = Math.Round(dinUkZaStim * s.Stim3proc / 100, mdec);

        // ── SABERIDOD ────────────────────────────────────────────────────
        s.Ldodaci = s.Dinnoc + s.Dinpriprav + s.Dinprod + s.Dinradnap
                  + s.Dinned + s.Dinmin + s.Topli + s.Regres + s.Terenski;

        // ── SABERINAK ────────────────────────────────────────────────────
        s.Naknade = s.Dinpraz + s.Dinbol + s.Dinbol2
                  + s.Dinplac + s.Dinplac2 + s.Dingod;

        // ── SABERIBRUTO ──────────────────────────────────────────────────
        s.Bruto = Math.Round(s.Dinuk + s.Stim1 + s.Stim2 + s.Stim3
                           + s.Topli + s.Regres + s.Terenski + s.Fiksna
                           + s.Dotacija + s.Dinsus, 2);

        // ── OBRACSOC — doprinosi i porez ─────────────────────────────────
        ObracunDoprinosiPorez(s, r, p, isplata, redispl, czakon, procPor,
            neoporezP, neoporez, srazpor, sdin1, prosbruto, mdec);

        // ── BK umanjenje ─────────────────────────────────────────────────
        // Fox: obracsocumanjenje.prg smanjuje BRUTO i ponovo računa ceo obracsoc.
        // Ovde: ObracunBkUmanjenje računa po Fox formuli i smanjuje s.Bruto,
        // zatim ponovo pozivamo ObracunDoprinosiPorez na smanjenom BRUTO.
        s.Bkumanj = 0;
        if (bkproc > 0 || bkzastita > 0)
        {
            ObracunBkUmanjenje(s, bkproc, bkzastita, czakon);
            // Re-run doprinosi+porez na smanjenom BRUTO (Fox: obracsocumanjenje ponovo računa sve)
            ObracunDoprinosiPorez(s, r, p, isplata, redispl, czakon, procPor,
                neoporezP, neoporez, srazpor, sdin1, prosbruto, mdec);
        }

        // ── OBRACBEN ─────────────────────────────────────────────────────
        decimal mmdinbol = s.Dinbol + s.Dinbol2;
        s.Bendin = Math.Round((s.Bruto - mmdinbol) * s.Benproc / 100, mdec);

        // ── OBRACSOCUMANJENJE (doprinosi firma) ──────────────────────────
        ObracunSocUmanjenje(s, mdec);

        // ── OBRACSOLPOR ──────────────────────────────────────────────────
        ObracunSolidarnPor(s, solporod1, solpordo1, solporod2, solproc1, solproc2);

        // ── OBRACOB — obustave ───────────────────────────────────────────
        s.Kasa = r.Kasa;
        s.Kasarata = r.KasaRata;
        s.Samodopr = Math.Round(s.Neto * r.SamodoprProcenat / 100, mdec);
        s.Sindikat1 = Math.Round(s.Neto * r.SindikatProcenat1 / 100, mdec);
        s.Sindikat2 = Math.Round(s.Neto * r.SindikatProcenat2 / 100, mdec);
        s.Solidarn = Math.Round(s.Neto * r.SolidarnostProcenat / 100, mdec);
        s.Aliment = Math.Round(s.Neto * r.AlimentacijaProcenat / 100, mdec);

        // ── OBRACKOM ─────────────────────────────────────────────────────
        s.Komorajd = Math.Round(s.Bruto * p.Komoraj / 100, 2);
        s.Komorasd = Math.Round(s.Bruto * p.Komoras / 100, 2);
        s.Komorard = Math.Round(s.Bruto * p.Komorar / 100, 2);

        // ── SABERIOB ─────────────────────────────────────────────────────
        // Fox ldobracun.prg SABERIOB: ACA IZBACIO OBUST5+OBUST6 (polja za razlike dotacija 08.08.2020)
        s.Ukobust = s.Krediti + s.Kreditia + s.Akontac + s.Prevoz + s.Aliment
                  + s.Kasa + s.Kasarata + s.Samodopr + s.Sindikat1 + s.Sindikat2
                  + s.Solidarn + s.Obust1 + s.Obust2 + s.Obust3 + s.Obust4
                  + s.Obustto + s.Solpor;
        s.Zaisplatu = Math.Round(s.Neto - s.Ukobust + s.Netoprev, mdec);
    }

    public void ObracunajOdNeta(
        LdObracunStavka s,
        Radnik r,
        LdParametar p,
        bool stari = false)
    {
        // ── Korak 1: prevedi časove u dinare ────────────────────────────
        // U Fox-u (ldobracunn0.prg) ovaj deo je identičan bruto putu sve do
        // izračunavanja MNETO2; zato ponovo iskoristimo postojeći Obracunaj
        // sa flag-om obracunatiNaknade=true (stari uvek računa naknade,
        // novi koristi vrednosti koje već postoje).
        Obracunaj(s, r, p, obracunatiNaknade: stari, obracunOdUkupneObaveze: false);

        // U ovom trenutku s.Bruto sadrži MNETO2 (zbir DINUK + stimulacije +
        // topli + regres + terenski + fiksna + dotacija + dinsus). Sad
        // primenjujemo SABERIBRUTON formulu: inflate u BRUTO.

        var procPor = p.Procpor;
        var neoporezP = p.Neoporezp;
        var srazpor = p.Srazpor;
        var sdin1 = p.Sdin1;
        var czakon = p.Czakon;
        var bkproc = p.Bkproc;
        var bkzastita = p.Bkzastita;
        int isplata = p.Isplata == 0 ? 1 : p.Isplata;
        int redispl = p.Redispl == 0 ? 1 : p.Redispl;
        int mdec = string.IsNullOrWhiteSpace(p.Decimale) ? 0 : 2;

        UcitajStopeDoprinosa(p, isplata,
            out var doppio, out var dopzdr, out var dopnez,
            out var dopfpio, out var dopfzdr, out var dopfnez);

        // mdopproc je u procentima (ne frakciji) — kao u Fox-u
        decimal mdopprocPct = Math.Round((doppio + dopzdr + dopnez) * 100m, 2);

        decimal mneto2 = s.Bruto;

        // m3mneoporezP — srazmeran neoporeziv iznos
        decimal m3mneoporezP = 0m;
        if (czakon != 0)
        {
            m3mneoporezP = Math.Round(neoporezP * s.Casuk / czakon, 2);
            if (m3mneoporezP > neoporezP) m3mneoporezP = neoporezP;
            if (srazpor != "D") m3mneoporezP = neoporezP;
        }

        // Predhodni iznosi (samo redispl > 1) — Poroslob* polja postoje u entitetu;
        // Preneto/Prestopa nisu modelovani pa ih tretiramo kao 0 (redispl=1 je default).
        decimal mprebruto = 0, mporoslob = 0, mprestopa = 0;
        switch (redispl)
        {
            case 2:
                mprebruto = s.Prebruto1;
                mporoslob = s.Poroslob1;
                break;
            case 3:
                mprebruto = s.Prebruto2;
                mporoslob = s.Poroslob2;
                break;
            case 4:
                mprebruto = s.Prebruto3;
                mporoslob = s.Poroslob3;
                break;
        }

        decimal moslob = Math.Round(m3mneoporezP * procPor / 100m, 2)
                       - Math.Round(mporoslob * mprestopa / 100m, 2);

        // BK umanjenje — samo "novi" obračun
        s.Bkumanj = 0;
        if (!stari && czakon != 0 && s.Casuk != 0 && (bkproc > 0 || bkzastita > 0))
        {
            decimal mbkzastita2 = Math.Round(bkzastita * s.Casuk / czakon, 2);
            decimal mbkumanj = mneto2 > mbkzastita2
                ? Math.Round(mneto2 * bkproc / 100m, 2)
                : 0m;

            if (Math.Round(mneto2 * czakon / s.Casuk, 2) > bkzastita)
            {
                if ((mneto2 - mbkumanj) < mbkzastita2)
                    mbkumanj = mneto2 - mbkzastita2;
            }
            s.Bkumanj = mbkumanj;
        }

        decimal moslob2 = Math.Round(m3mneoporezP * procPor / 100m, 2);

        // Glavna inflate-u-bruto formula
        decimal denom = 100m - procPor - mdopprocPct;
        decimal mosnov2 = denom != 0
            ? Math.Round((mneto2 - moslob2 - s.Bkumanj) * 100m / denom, 2)
            : mneto2;

        s.Bruto = mosnov2;

        // Provera minimalne zarade (ldobracunn0.prg lines 268–281)
        if (czakon != 0 && s.Casuk != 0)
        {
            decimal brutoNorm = Math.Round(s.Bruto * s.Casuk / czakon, 2);
            decimal sdin1Norm = Math.Round(sdin1 * s.Casuk / czakon, 2);
            if (brutoNorm < sdin1Norm)
            {
                decimal mdopsoc2 = Math.Round(sdin1 * s.Casuk / czakon * mdopprocPct / 100m, 0);
                decimal mnetodop = mneto2 + mdopsoc2 - m3mneoporezP;
                decimal mrazlika = (100m - procPor) != 0
                    ? mnetodop * procPor / (100m - procPor)
                    : 0m;
                s.Bruto = Math.Round(mneto2 + mdopsoc2 + mrazlika, 2);
            }
        }

        // ── Korak 2: ponovo izračunaj porez i doprinose nad novim BRUTO ─
        ObracunDoprinosiPorez(s, r, p, isplata, redispl, czakon, procPor,
            neoporezP, p.Neoporez, srazpor, sdin1, p.Prosbruto, mdec);

        // OBRACBEN — beneficirani staž (na novi bruto)
        // Fox ldobracunn0.prg calls obracbenn: mmdinbol = Round((dinBOL+dinBOL2)/0.701, 2)
        // ldobracunnstari.prg does NOT call obracbenn, uses raw dinbol+dinbol2
        decimal mmdinbol = stari
            ? s.Dinbol + s.Dinbol2
            : Math.Round((s.Dinbol + s.Dinbol2) / 0.701m, 2);
        s.Bendin = Math.Round((s.Bruto - mmdinbol) * s.Benproc / 100m, mdec);

        // Komore na novi bruto
        s.Komorajd = Math.Round(s.Bruto * p.Komoraj / 100m, 2);
        s.Komorasd = Math.Round(s.Bruto * p.Komoras / 100m, 2);
        s.Komorard = Math.Round(s.Bruto * p.Komorar / 100m, 2);

        // SABERIOB — obustave na novi neto
        s.Samodopr = Math.Round(s.Neto * r.SamodoprProcenat / 100m, mdec);
        s.Sindikat1 = Math.Round(s.Neto * r.SindikatProcenat1 / 100m, mdec);
        s.Sindikat2 = Math.Round(s.Neto * r.SindikatProcenat2 / 100m, mdec);
        s.Solidarn = Math.Round(s.Neto * r.SolidarnostProcenat / 100m, mdec);
        s.Aliment = Math.Round(s.Neto * r.AlimentacijaProcenat / 100m, mdec);

        // Fox ldobracun.prg SABERIOB: ACA IZBACIO OBUST5+OBUST6 (polja za razlike dotacija 08.08.2020)
        s.Ukobust = s.Krediti + s.Kreditia + s.Akontac + s.Prevoz + s.Aliment
                  + s.Kasa + s.Kasarata + s.Samodopr + s.Sindikat1 + s.Sindikat2
                  + s.Solidarn + s.Obust1 + s.Obust2 + s.Obust3 + s.Obust4
                  + s.Obustto + s.Solpor;
        s.Zaisplatu = Math.Round(s.Neto - s.Ukobust + s.Netoprev, mdec);
    }

    /// <summary>
    /// Obračun doprinosa i poreza — translacija OBRACSOC.PRG za ISPLATA = 1/3/6 (najčešći slučaj).
    /// </summary>
    private static void ObracunDoprinosiPorez(
        LdObracunStavka s, Radnik r, LdParametar p,
        int isplata, int redispl, int czakon,
        decimal procPor, decimal neoporezP, decimal neoporez,
        string srazpor, decimal sdin1, decimal prosbruto, int mdec)
    {
        // Stope doprinosa — zavisno od isplate
        decimal doppio, dopzdr, dopnez, dopfpio, dopfzdr, dopfnez;
        UcitajStopeDoprinosa(p, isplata, out doppio, out dopzdr, out dopnez,
                             out dopfpio, out dopfzdr, out dopfnez);

        decimal mdopproc = doppio + dopzdr + dopnez;
        decimal mdopprocf = dopfpio + dopfzdr + dopfnez;

        // BUG 6 — šifre prihoda (Fox obracsoc.prg 79-84): primeni pre neoporezP
        var oznvrprih = r.OznakaVrstePrihoda?.Trim() ?? string.Empty;
        if (oznvrprih == "109") procPor = 0;
        if (oznvrprih == "108" || oznvrprih == "150" || oznvrprih == "151") neoporezP = 0;

        // Neoporezivi iznos srazmerno satima
        decimal mmneoporezP = 0;
        if (czakon != 0 && s.Bruto != 0)
        {
            decimal mmcasuk = s.Casuk > czakon ? czakon : s.Casuk;
            mmneoporezP = Math.Round(neoporezP * mmcasuk / czakon, 2);
            // oznvrprih '105': dvostruki neoporezivi iznos (Fox obracsoc.prg 100-101)
            if (oznvrprih == "105")
                mmneoporezP = Math.Round(neoporezP * mmcasuk * 2 / czakon, 2);
            if (mmneoporezP > neoporezP) mmneoporezP = neoporezP;
            if (srazpor != "D") mmneoporezP = neoporezP;
        }

        // Poreska oslobođenja
        s.Poroslob = mmneoporezP;
        decimal mneoporezivo;

        switch (redispl)
        {
            case 1:
            default:
                s.Poroslob1 = mmneoporezP > s.Bruto ? s.Bruto : mmneoporezP;
                s.Poroslob2 = 0; s.Poroslob3 = 0; s.Poroslob4 = 0;
                mneoporezivo = s.Poroslob1;
                break;
            case 2:
                var mporos2 = s.Poroslob - s.Poroslob1;
                var mbbruto2 = s.Bruto - s.Prebruto1;
                s.Poroslob2 = mporos2 > mbbruto2 ? mbbruto2 : mporos2;
                s.Poroslob3 = 0; s.Poroslob4 = 0;
                mneoporezivo = s.Poroslob1 + s.Poroslob2;
                break;
            case 3:
                var mporos3 = s.Poroslob - s.Poroslob1 - s.Poroslob2;
                var mbbruto3 = s.Bruto - s.Prebruto2;
                s.Poroslob3 = mporos3 > mbbruto3 ? mbbruto3 : mporos3;
                s.Poroslob4 = 0;
                mneoporezivo = s.Poroslob1 + s.Poroslob2 + s.Poroslob3;
                break;
            case 4:
                var mporos4 = s.Poroslob - s.Poroslob1 - s.Poroslob2 - s.Poroslob3;
                var mbbruto4 = s.Bruto - s.Prebruto3;
                s.Poroslob4 = mporos4 > mbbruto4 ? mbbruto4 : mporos4;
                mneoporezivo = s.Poroslob1 + s.Poroslob2 + s.Poroslob3 + s.Poroslob4;
                break;
        }

        // Obračun poreza
        s.Porezs = Math.Round((s.Bruto - mneoporezivo) * procPor / 100, mdec);
        s.Porez = s.Porezs;
        if (s.Porumanj != 0)
        {
            s.Porezu = Math.Round(s.Porezs - s.Porezs * (100 - s.Porumanj) / 100, mdec);
            s.Porez = s.Porezs - s.Porezu;
        }
        else
        {
            s.Porezu = 0;
        }

        s.Doposlob = neoporez;

        // Obračun doprinosa — zavisno od isplate
        if (isplata == 2)
        {
            // Fox obracsoc.prg 289-363: porodiljsko — dopsocr = BRUTO*mdopproc (bez neoporez)
            ObracunDoprinosiPorodiljsko(s, r, mdopproc, mdopprocf,
                doppio, dopzdr, dopnez, dopfpio, dopfzdr, dopfnez, mmneoporezP, mdec);
        }
        else if (isplata == 4)
        {
            // Fox obracsoc.prg 365-436: poseban put — POREZ se koriguje, dopsocr=(bruto-neoporez-porez)*mdopproc
            ObracunDoprinosiIsplata4(s, doppio, dopzdr, dopnez,
                dopfpio, dopfzdr, dopfnez, mdopproc, mdopprocf,
                mmneoporezP, neoporez, czakon, redispl, mdec);
        }
        else if (isplata == 5)
        {
            // Fox obracsoc.prg 439-end: penzioneri — dopsocr kao standard, NETO via Adopproc
            ObracunDoprinosiStandard(s, r, mdopproc, mdopprocf, doppio, dopzdr, dopnez,
                dopfpio, dopfzdr, dopfnez, czakon, sdin1, prosbruto, neoporez, mdec);
            ObracunDoprinosiPenzioneriNeto(s, r, p, czakon, sdin1, prosbruto, mdec);
        }
        else // isplata 1, 3, 6
        {
            ObracunDoprinosiStandard(s, r, mdopproc, mdopprocf, doppio, dopzdr, dopnez,
                dopfpio, dopfzdr, dopfnez, czakon, sdin1, prosbruto, neoporez, mdec);
        }

        // Osnovice poreza po isplatama
        if (procPor != 0)
        {
            switch (redispl)
            {
                case 1:
                    s.Osnovp1 = s.Bruto - s.Poroslob;
                    s.Osnovp2 = 0; s.Osnovp3 = 0; s.Osnovp4 = 0;
                    break;
                case 2:
                    s.Osnovp2 = s.Bruto - s.Osnovp1 - s.Poroslob;
                    s.Osnovp3 = 0; s.Osnovp4 = 0;
                    break;
                case 3:
                    s.Osnovp3 = s.Bruto - s.Osnovp1 - s.Osnovp2 - s.Poroslob;
                    s.Osnovp4 = 0;
                    break;
                case 4:
                    s.Osnovp4 = s.Bruto - s.Osnovp1 - s.Osnovp2 - s.Osnovp3 - s.Poroslob;
                    break;
            }
        }
    }

    private static void ObracunDoprinosiStandard(
        LdObracunStavka s, Radnik r,
        decimal mdopproc, decimal mdopprocf,
        decimal doppio, decimal dopzdr, decimal dopnez,
        decimal dopfpio, decimal dopfzdr, decimal dopfnez,
        int czakon, decimal sdin1, decimal prosbruto, decimal neoporez, int mdec)
    {
        decimal mkorekc = czakon != 0 ? s.Casuk / czakon : 0;
        if (mkorekc > 1) mkorekc = 1;

        decimal mosnov = Math.Round(sdin1 * mkorekc, mdec);
        decimal mosnov5 = Math.Round(prosbruto * mkorekc * 5, mdec);

        if (r.Ropnr == "1" && mosnov < sdin1)
            mosnov = sdin1;

        if (s.Bruto < mosnov)
        {
            s.Dopsocr = Math.Round(mosnov * mdopproc, mdec);
            s.Dopsocf = Math.Round(mosnov * mdopprocf, mdec);
            s.Osnovica = mosnov;
            s.Propisana = Math.Round(sdin1 * mkorekc, mdec);
            s.Skala = 0;
        }
        else
        {
            s.Dopsocr = Math.Round((s.Bruto - neoporez) * mdopproc, mdec);
            s.Dopsocf = Math.Round((s.Bruto - neoporez) * mdopprocf, mdec);
            s.Osnovica = s.Bruto;
            s.Propisana = sdin1;
            s.Skala = 1;
        }

        if (s.Bruto > mosnov5 && mosnov5 > 0)
        {
            s.Dopsocr = Math.Round(mosnov5 * mdopproc, mdec);
            s.Dopsocf = Math.Round(mosnov5 * mdopprocf, mdec);
            s.Osnovica = mosnov5;
            s.Propisana = sdin1;
            s.Skala = 5;
        }

        // Raspodela doprinosa radnik
        if (mdopproc != 0)
        {
            s.Doppr = Math.Round(s.Dopsocr * doppio / mdopproc, mdec);
            s.Dopzr = Math.Round(s.Dopsocr * dopzdr / mdopproc, mdec);
            s.Dopnr = Math.Round(s.Dopsocr * dopnez / mdopproc, mdec);
            // Korekcija zaokruživanja
            if (s.Dopsocr != s.Doppr + s.Dopzr + s.Dopnr)
            {
                if (s.Dopnr != 0) s.Dopnr = s.Dopsocr - s.Doppr - s.Dopzr;
                else if (s.Doppr != 0) s.Doppr = s.Dopsocr - s.Dopzr;
            }
        }

        // Raspodela doprinosa firma
        if (mdopprocf != 0)
        {
            s.Doppf = Math.Round(s.Dopsocf * dopfpio / mdopprocf, mdec);
            s.Dopzf = Math.Round(s.Dopsocf * dopfzdr / mdopprocf, mdec);
            s.Dopnf = Math.Round(s.Dopsocf * dopfnez / mdopprocf, mdec);
            if (s.Dopsocf != s.Doppf + s.Dopzf + s.Dopnf)
            {
                if (s.Dopnf != 0) s.Dopnf = s.Dopsocf - s.Doppf - s.Dopzf;
                else if (s.Doppf != 0) s.Doppf = s.Dopsocf - s.Dopzf;
            }

            // Sačuvane (full) stope
            s.Doppfs = Math.Round(s.Dopsocf * dopfpio / mdopprocf, mdec);
            s.Dopzfs = Math.Round(s.Dopsocf * dopfzdr / mdopprocf, mdec);
            s.Dopnfs = Math.Round(s.Dopsocf * dopfnez / mdopprocf, mdec);
        }

        // Umanjenja doprinosa
        if (s.Dopumanj != 0 && mdopprocf != 0)
        {
            s.Doppfu = Math.Round(s.Dopsocf * dopfpio * (100 - s.Dopumanj) / mdopprocf / 100, mdec);
            s.Dopzfu = Math.Round(s.Dopsocf * dopfzdr * (100 - s.Dopumanj) / mdopprocf / 100, mdec);
            s.Dopnfu = Math.Round(s.Dopsocf * dopfnez * (100 - s.Dopumanj) / mdopprocf / 100, mdec);
        }

        if (s.Pioumanjr != 0)
            s.Doppru = Math.Round(s.Doppr * s.Pioumanjr / 100, mdec);
        if (s.Pioumanjf != 0)
            s.Doppfu = Math.Round(s.Doppf * s.Pioumanjf / 100, mdec);

        // Neto
        if (s.Porumanj != 0)
        {
            s.Neto = s.Bruto - s.Dopsocr - s.Porezs;
        }
        else
        {
            s.Neto = s.Bruto - s.Dopsocr - s.Porez;
            if (r.Ropnr == "1")
                s.Neto = s.Bruto - s.Doppr - s.Dopzr - s.Dopnr - s.Porez;
        }

        s.Netosve = s.Neto + s.Netoprev;
    }

    // Fox obracsoc.prg CASE MISPLATA=2 (289-363): porodiljsko
    private static void ObracunDoprinosiPorodiljsko(
        LdObracunStavka s, Radnik r,
        decimal mdopproc, decimal mdopprocf,
        decimal doppio, decimal dopzdr, decimal dopnez,
        decimal dopfpio, decimal dopfzdr, decimal dopfnez,
        decimal mmneoporezP, int mdec)
    {
        s.Dopsocr  = Math.Round(s.Bruto * mdopproc, mdec);
        s.Dopsocf  = Math.Round(s.Bruto * mdopprocf, mdec);
        s.Osnovica = s.Bruto - mmneoporezP;
        s.Propisana = 0;
        s.Skala    = 1;

        if (mdopproc != 0)
        {
            s.Doppr = Math.Round(s.Dopsocr * doppio / mdopproc, mdec);
            s.Dopzr = Math.Round(s.Dopsocr * dopzdr / mdopproc, mdec);
            s.Dopnr = Math.Round(s.Dopsocr * dopnez / mdopproc, mdec);
            if (s.Dopsocr != s.Doppr + s.Dopzr + s.Dopnr)
            {
                if (s.Dopnr != 0) s.Dopnr = s.Dopsocr - s.Doppr - s.Dopzr;
                else if (s.Doppr != 0) s.Doppr = s.Dopsocr - s.Dopzr;
            }
        }
        else { s.Dopsocr = 0; s.Doppr = 0; s.Dopzr = 0; s.Dopnr = 0; }

        if (mdopprocf != 0)
        {
            s.Doppf = Math.Round(s.Dopsocf * dopfpio / mdopprocf, mdec);
            s.Dopzf = Math.Round(s.Dopsocf * dopfzdr / mdopprocf, mdec);
            s.Dopnf = Math.Round(s.Dopsocf * dopfnez / mdopprocf, mdec);
            if (s.Dopsocf != s.Doppf + s.Dopzf + s.Dopnf)
            {
                if (s.Dopnf != 0) s.Dopnf = s.Dopsocf - s.Doppf - s.Dopzf;
                else if (s.Doppf != 0) s.Doppf = s.Dopsocf - s.Dopzf;
            }
            s.Doppfs = Math.Round(s.Dopsocf * dopfpio / mdopprocf, mdec);
            s.Dopzfs = Math.Round(s.Dopsocf * dopfzdr / mdopprocf, mdec);
            s.Dopnfs = Math.Round(s.Dopsocf * dopfnez / mdopprocf, mdec);
        }
        else { s.Dopsocf = 0; s.Doppf = 0; s.Dopzf = 0; s.Dopnf = 0; }

        if (s.Dopumanj != 0 && mdopprocf != 0)
        {
            s.Doppfu = Math.Round(s.Dopsocf * dopfpio * (100 - s.Dopumanj) / mdopprocf / 100, mdec);
            s.Dopzfu = Math.Round(s.Dopsocf * dopfzdr * (100 - s.Dopumanj) / mdopprocf / 100, mdec);
            s.Dopnfu = Math.Round(s.Dopsocf * dopfnez * (100 - s.Dopumanj) / mdopprocf / 100, mdec);
        }
        if (s.Pioumanjr != 0) s.Doppru = Math.Round(s.Doppr * s.Pioumanjr / 100, mdec);
        if (s.Pioumanjf != 0) s.Doppfu = Math.Round(s.Doppf * s.Pioumanjf / 100, mdec);

        // Neto — bez ROPNR='1' special case (Fox obracsoc.prg 358-362)
        s.Neto    = s.Porumanj != 0
            ? s.Bruto - s.Dopsocr - s.Porezs
            : s.Bruto - s.Dopsocr - s.Porez;
        s.Netosve = s.Neto + s.Netoprev;
    }

    // Fox obracsoc.prg CASE MISPLATA=4 (365-436)
    private static void ObracunDoprinosiIsplata4(
        LdObracunStavka s,
        decimal doppio, decimal dopzdr, decimal dopnez,
        decimal dopfpio, decimal dopfzdr, decimal dopfnez,
        decimal mdopproc, decimal mdopprocf,
        decimal mmneoporezP, decimal neoporez, int czakon,
        int redispl, int mdec)
    {
        decimal mbporez = redispl switch { 2 => s.Prepor1, 3 => s.Prepor2, 4 => s.Prepor3, _ => 0m };
        s.Porez = s.Porezs - s.Porezu + mbporez;

        // Fox: mosnov = Round((BRUTO-POREZ-MMNEOPOREZP)*mkorekc, MDEC) → NETO
        decimal mkorekc = czakon != 0 && s.Casuk != 0
            ? Math.Min(s.Casuk / (decimal)czakon, 1m)
            : 1m;
        s.Neto    = Math.Round((s.Bruto - s.Porez - mmneoporezP) * mkorekc, mdec);
        s.Netosve = s.Neto + s.Netoprev;

        // Fox: dopsocr = (bruto - MNEOPOREZ - POREZ) * mdopproc
        s.Osnovica = s.Bruto - neoporez - s.Porez;
        s.Dopsocr  = Math.Round(s.Osnovica * mdopproc, mdec);
        s.Dopsocf  = Math.Round(s.Osnovica * mdopprocf, mdec);
        // Fox: REPLACE OSNOVICA WITH MOSNOV (overwrites with mosnov = NETO)
        s.Osnovica = s.Neto;
        s.Skala    = 1;

        if (mdopproc != 0)
        {
            s.Doppr = Math.Round(s.Dopsocr * doppio / mdopproc, mdec);
            s.Dopzr = Math.Round(s.Dopsocr * dopzdr / mdopproc, mdec);
            s.Dopnr = Math.Round(s.Dopsocr * dopnez / mdopproc, mdec);
            if (s.Dopsocr != s.Doppr + s.Dopzr + s.Dopnr)
            {
                if (s.Dopnr != 0) s.Dopnr = s.Dopsocr - s.Doppr - s.Dopzr;
                else if (s.Doppr != 0) s.Doppr = s.Dopsocr - s.Dopzr;
            }
        }
        if (mdopprocf != 0)
        {
            s.Doppf = Math.Round(s.Dopsocf * dopfpio / mdopprocf, mdec);
            s.Dopzf = Math.Round(s.Dopsocf * dopfzdr / mdopprocf, mdec);
            s.Dopnf = Math.Round(s.Dopsocf * dopfnez / mdopprocf, mdec);
            if (s.Dopsocf != s.Doppf + s.Dopzf + s.Dopnf)
            {
                if (s.Dopnf != 0) s.Dopnf = s.Dopsocf - s.Doppf - s.Dopzf;
                else if (s.Doppf != 0) s.Doppf = s.Dopsocf - s.Dopzf;
            }
            s.Doppfs = Math.Round(s.Dopsocf * dopfpio / mdopprocf, mdec);
            s.Dopzfs = Math.Round(s.Dopsocf * dopfzdr / mdopprocf, mdec);
            s.Dopnfs = Math.Round(s.Dopsocf * dopfnez / mdopprocf, mdec);
        }
        if (s.Dopumanj != 0 && mdopprocf != 0)
        {
            s.Doppfu = Math.Round(s.Dopsocf * dopfpio * (100 - s.Dopumanj) / mdopprocf / 100, mdec);
            s.Dopzfu = Math.Round(s.Dopsocf * dopfzdr * (100 - s.Dopumanj) / mdopprocf / 100, mdec);
            s.Dopnfu = Math.Round(s.Dopsocf * dopfnez * (100 - s.Dopumanj) / mdopprocf / 100, mdec);
        }
        // Fox 430-435: PIOUMANJ za isplata=4 koristi raw stope (DOPPIO/DOPFPIO), ne iznose
        if (s.Pioumanjr != 0) s.Doppru = Math.Round(doppio * s.Pioumanjr / 100, mdec);
        if (s.Pioumanjf != 0) s.Doppfu = Math.Round(dopfpio * s.Pioumanjf / 100, mdec);

    }

    // Fox obracsoc.prg CASE MISPLATA=5 (439-end): NETO override via Adopproc (isplata=1 stope)
    private static void ObracunDoprinosiPenzioneriNeto(
        LdObracunStavka s, Radnik r, LdParametar p,
        int czakon, decimal sdin1, decimal prosbruto, int mdec)
    {
        decimal adoppio = p.Doppr1 / 100;
        decimal adopzdr = p.Dopzr1 / 100;
        decimal adopnez = p.Dopnr1 / 100;
        // Fox 460-464: VRSTA='R' → bez DOPNEZ za penzionere-radnike
        decimal adopproc = string.Equals(r.Vrsta, "R", StringComparison.OrdinalIgnoreCase)
            ? adoppio + adopzdr
            : adoppio + adopzdr + adopnez;

        decimal mkorekc = czakon != 0 ? s.Casuk / (decimal)czakon : 0m;
        if (mkorekc > 1) mkorekc = 1;
        decimal mosnov  = Math.Round(sdin1 * mkorekc, mdec);
        decimal mosnov5 = Math.Round(prosbruto * mkorekc * 5, mdec);
        if (r.Ropnr == "1" && mosnov < sdin1) mosnov = sdin1;

        decimal adopsocr = s.Bruto < mosnov
            ? Math.Round(mosnov * adopproc, 2)
            : Math.Round(s.Bruto * adopproc, 2);
        if (s.Bruto > mosnov5 && mosnov5 > 0)
            adopsocr = Math.Round(mosnov5 * adopproc, 2);

        s.Neto = Math.Round(s.Bruto - s.Porez - adopsocr, mdec);
        // Fox obracsocumanjenje.prg 586-588: ROPNR='1' override za penzionere
        if (r.Ropnr == "1")
            s.Neto = s.Bruto - s.Doppr - s.Porez;
        s.Netosve = s.Neto + s.Netoprev;
    }

    private static void ObracunSocUmanjenje(LdObracunStavka s, int mdec)
    {
        // obracsocU — umanjenje doprinosa firma
        if (s.Dopumanj != 0)
        {
            var mdoppfu = Math.Round(s.Doppf * s.Dopumanj / 100, mdec);
            var mdopzfu = Math.Round(s.Dopzf * s.Dopumanj / 100, mdec);
            var mdopnfu = Math.Round(s.Dopnf * s.Dopumanj / 100, mdec);
            s.Doppfu = mdoppfu;
            s.Dopzfu = mdopzfu;
            s.Dopnfu = mdopnfu;
            s.Doppf -= mdoppfu;
            s.Dopzf -= mdopzfu;
            s.Dopnf -= mdopnfu;
            s.Dopsocf = s.Doppf + s.Dopzf + s.Dopnf;
        }

        if (s.Pioumanjr != 0)
            s.Doppru = Math.Round(s.Doppr * s.Pioumanjr / 100, mdec);
        if (s.Pioumanjf != 0)
            s.Doppfu = Math.Round(s.Doppf * s.Pioumanjf / 100, mdec);
    }

    private static void ObracunBkUmanjenje(LdObracunStavka s, decimal bkproc, decimal bkzastita, int czakon)
    {
        if (czakon == 0 || s.Casuk == 0) return;

        decimal mbkzastita2 = Math.Round(bkzastita * s.Casuk / czakon, 2);
        if (s.Neto > mbkzastita2)
        {
            s.Bkumanj = Math.Round(s.Neto * bkproc / 100, 2);
        }

        if (Math.Round(s.Neto * czakon / s.Casuk, 2) > bkzastita)
        {
            if (s.Neto - s.Bkumanj < mbkzastita2)
                s.Bkumanj = s.Neto - mbkzastita2;
        }

        if (s.Bkumanj != 0m)
            s.Bruto = Math.Round(s.Bruto - s.Bkumanj, 2);
    }

    private static void ObracunSolidarnPor(LdObracunStavka s,
        decimal solporod1, decimal solpordo1, decimal solporod2,
        decimal solproc1, decimal solproc2)
    {
        s.Neto2 = 0;
        s.Solpor = 0;
        if (solporod1 <= 0) return;

        decimal msolpor1 = 0, msolpor2 = 0;
        decimal netoOst = s.Neto + s.Netoost;

        if (netoOst > solporod1 && netoOst <= solpordo1)
            msolpor1 = Math.Round(solproc1 * (s.Neto - solporod1) / 100, 2);

        if (netoOst > solporod2)
        {
            msolpor1 = Math.Round(solproc1 * (solpordo1 - solporod1) / 100, 2);
            msolpor2 = Math.Round(solproc2 * (netoOst - solpordo1) / 100, 2);
        }

        s.Solpor = msolpor1 + msolpor2;
        s.Neto2 = s.Neto - s.Solpor;
    }

    private static void UcitajStopeDoprinosa(LdParametar p, int isplata,
        out decimal doppio, out decimal dopzdr, out decimal dopnez,
        out decimal dopfpio, out decimal dopfzdr, out decimal dopfnez)
    {
        switch (isplata)
        {
            case 2:
                doppio = Math.Round(p.Doppr2 / 100, 6); dopzdr = Math.Round(p.Dopzr2 / 100, 6); dopnez = Math.Round(p.Dopnr2 / 100, 6);
                dopfpio = Math.Round(p.Doppf2 / 100, 6); dopfzdr = Math.Round(p.Dopzf2 / 100, 6); dopfnez = Math.Round(p.Dopnf2 / 100, 6);
                break;
            case 3:
                doppio = Math.Round(p.Doppr3 / 100, 6); dopzdr = Math.Round(p.Dopzr3 / 100, 6); dopnez = Math.Round(p.Dopnr3 / 100, 6);
                dopfpio = Math.Round(p.Doppf3 / 100, 6); dopfzdr = Math.Round(p.Dopzf3 / 100, 6); dopfnez = Math.Round(p.Dopnf3 / 100, 6);
                break;
            case 4:
                doppio = Math.Round(p.Doppr4 / 100, 6); dopzdr = Math.Round(p.Dopzr4 / 100, 6); dopnez = Math.Round(p.Dopnr4 / 100, 6);
                dopfpio = Math.Round(p.Doppf4 / 100, 6); dopfzdr = Math.Round(p.Dopzf4 / 100, 6); dopfnez = Math.Round(p.Dopnf4 / 100, 6);
                break;
            case 5:
                doppio = Math.Round(p.Doppr5 / 100, 6); dopzdr = Math.Round(p.Dopzr5 / 100, 6); dopnez = Math.Round(p.Dopnr5 / 100, 6);
                dopfpio = Math.Round(p.Doppf5 / 100, 6); dopfzdr = Math.Round(p.Dopzf5 / 100, 6); dopfnez = Math.Round(p.Dopnf5 / 100, 6);
                break;
            default: // 1 i 6
                doppio = Math.Round(p.Doppr1 / 100, 6); dopzdr = Math.Round(p.Dopzr1 / 100, 6); dopnez = Math.Round(p.Dopnr1 / 100, 6);
                dopfpio = Math.Round(p.Doppf1 / 100, 6); dopfzdr = Math.Round(p.Dopzf1 / 100, 6); dopfnez = Math.Round(p.Dopnf1 / 100, 6);
                break;
        }
    }
}
