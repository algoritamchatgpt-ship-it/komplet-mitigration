namespace Algoritam.Domain.Entities;

/// <summary>
/// Stavka obračuna zarade — puni red iz LD.DBF tabele.
/// Svaki red predstavlja jednog radnika u jednom mesecu/isplati.
/// Fox forme: LD.SCX (platni spisak), LDA.SCX (alternativni).
/// </summary>
public class LdObracunStavka
{
    public int Id { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  IDENTIFIKACIJA
    // ══════════════════════════════════════════════════════════════════

    public int Broj { get; set; }
    public string Sifraprih { get; set; } = string.Empty;
    public string ImePrez { get; set; } = string.Empty;
    public string Evidbroj { get; set; } = string.Empty;
    public string Maticnibr { get; set; } = string.Empty;
    public string Idbroj { get; set; } = string.Empty;
    public string Dok { get; set; } = string.Empty;
    public int Grupa { get; set; }
    public int Grupa1 { get; set; }
    public int Mtr { get; set; }
    public string Vrsta { get; set; } = string.Empty;

    // ══════════════════════════════════════════════════════════════════
    //  PERIOD
    // ══════════════════════════════════════════════════════════════════

    public int Mesec { get; set; }
    public int Isplata { get; set; }
    public string Nazmes { get; set; } = string.Empty;
    public string Godina { get; set; } = string.Empty;
    public DateTime? Datum { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  ČASOVI (iz Fox: CASxx polja)
    // ══════════════════════════════════════════════════════════════════

    public decimal Casvr { get; set; }
    public decimal Casuc { get; set; }
    public decimal Casnoc { get; set; }
    public decimal Casprod { get; set; }
    public decimal Casradnap { get; set; }
    public decimal Casned { get; set; }
    public decimal Casdor { get; set; }
    public decimal Cslput { get; set; }
    public decimal Caspraz { get; set; }
    public decimal Casbol { get; set; }
    public decimal Casbol2 { get; set; }
    public decimal Casplac { get; set; }
    public decimal Casplac2 { get; set; }
    public decimal Casgod { get; set; }
    public decimal Casvv { get; set; }
    public decimal Cas1 { get; set; }
    public decimal Cas2 { get; set; }
    public decimal Cas3 { get; set; }
    public decimal Cassus { get; set; }
    public decimal Casneplac { get; set; }
    public decimal Caspriprav { get; set; }
    public decimal Casuk { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  DINARI (iz Fox: DINxx polja — obračunate vrednosti)
    // ══════════════════════════════════════════════════════════════════

    public decimal Dinvr { get; set; }
    public decimal Dinuc { get; set; }
    public decimal Dinnoc { get; set; }
    public decimal Dinprod { get; set; }
    public decimal Dinradnap { get; set; }
    public decimal Dinned { get; set; }
    public decimal Dindor { get; set; }
    public decimal Dinsl { get; set; }
    public decimal Dinpraz { get; set; }
    public decimal Dinbol { get; set; }
    public decimal Dinbol2 { get; set; }
    public decimal Dinplac { get; set; }
    public decimal Dinplac2 { get; set; }
    public decimal Dingod { get; set; }
    public decimal Dinvv { get; set; }
    public decimal Din1 { get; set; }
    public decimal Din2 { get; set; }
    public decimal Din3 { get; set; }
    public decimal Dinsus { get; set; }
    public decimal Dinmin { get; set; }
    public decimal Dinuk { get; set; }
    public decimal Dinpriprav { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  STIMULACIJE
    // ══════════════════════════════════════════════════════════════════

    public decimal Stim1 { get; set; }
    public decimal Stim2 { get; set; }
    public decimal Stim3 { get; set; }
    public decimal Stim1proc { get; set; }
    public decimal Stim2proc { get; set; }
    public decimal Stim3proc { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  DODACI
    // ══════════════════════════════════════════════════════════════════

    public decimal Topli { get; set; }
    public decimal Regres { get; set; }
    public decimal Terenski { get; set; }
    public decimal Fiksna { get; set; }
    public decimal Dotacija { get; set; }
    public decimal Ldodaci { get; set; }
    public decimal Naknade { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  BRUTO / NETO
    // ══════════════════════════════════════════════════════════════════

    public decimal Bruto { get; set; }
    public decimal Neto { get; set; }
    public decimal Neto2 { get; set; }
    public decimal Netosve { get; set; }
    public decimal Netoprev { get; set; }
    public decimal Netoost { get; set; }
    public decimal Cenarada { get; set; }
    public decimal Startbod { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  DOPRINOSI
    // ══════════════════════════════════════════════════════════════════

    public decimal Dopsocr { get; set; }
    public decimal Dopsocf { get; set; }
    public decimal Doppr { get; set; }
    public decimal Dopzr { get; set; }
    public decimal Dopnr { get; set; }
    public decimal Doppf { get; set; }
    public decimal Dopzf { get; set; }
    public decimal Dopnf { get; set; }
    public decimal Doppru { get; set; }
    public decimal Doppfu { get; set; }
    public decimal Dopzfu { get; set; }
    public decimal Dopnfu { get; set; }
    public decimal Doppfs { get; set; }
    public decimal Dopzfs { get; set; }
    public decimal Dopnfs { get; set; }
    public decimal Doposlob { get; set; }
    public decimal Dopumanj { get; set; }
    public decimal Pioumanjr { get; set; }
    public decimal Pioumanjf { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  POREZ
    // ══════════════════════════════════════════════════════════════════

    public decimal Porez { get; set; }
    public decimal Porezs { get; set; }
    public decimal Porezu { get; set; }
    public decimal Poroslob { get; set; }
    public decimal Poroslob1 { get; set; }
    public decimal Poroslob2 { get; set; }
    public decimal Poroslob3 { get; set; }
    public decimal Poroslob4 { get; set; }
    public decimal Porumanj { get; set; }
    public decimal Osnovica { get; set; }
    public decimal Propisana { get; set; }
    public int Skala { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  OBUSTAVE
    // ══════════════════════════════════════════════════════════════════

    public decimal Krediti { get; set; }
    public decimal Kreditia { get; set; }
    public decimal Akontac { get; set; }
    public decimal Prevoz { get; set; }
    public decimal Kasa { get; set; }
    public decimal Kasarata { get; set; }
    public decimal Samodopr { get; set; }
    public decimal Sindikat1 { get; set; }
    public decimal Sindikat2 { get; set; }
    public decimal Solidarn { get; set; }
    public decimal Aliment { get; set; }
    public decimal Obust1 { get; set; }
    public decimal Obust2 { get; set; }
    public decimal Obust3 { get; set; }
    public decimal Obust4 { get; set; }
    public decimal Obust5 { get; set; }
    public decimal Obust6 { get; set; }
    public decimal Obustto { get; set; }
    public decimal Solpor { get; set; }
    public decimal Ukobust { get; set; }
    public decimal Zaisplatu { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  BENEFICIJE I KOMORE
    // ══════════════════════════════════════════════════════════════════

    public decimal Benproc { get; set; }
    public decimal Bendin { get; set; }
    public decimal Komorajd { get; set; }
    public decimal Komorasd { get; set; }
    public decimal Komorard { get; set; }
    public decimal Bkumanj { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  REDOVNA ISPLATA POLJA (za višestruke isplate)
    // ══════════════════════════════════════════════════════════════════

    public decimal Prebruto1 { get; set; }
    public decimal Prebruto2 { get; set; }
    public decimal Prebruto3 { get; set; }
    public decimal Prepor1 { get; set; }
    public decimal Prepor2 { get; set; }
    public decimal Prepor3 { get; set; }
    public decimal Osnovp1 { get; set; }
    public decimal Osnovp2 { get; set; }
    public decimal Osnovp3 { get; set; }
    public decimal Osnovp4 { get; set; }

    // ══════════════════════════════════════════════════════════════════
    //  SISTEMSKA POLJA
    // ══════════════════════════════════════════════════════════════════

    public string Arhiva { get; set; } = string.Empty;
    public string Arhiva2 { get; set; } = string.Empty;
    public long Idbr { get; set; }
}
