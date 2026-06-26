namespace Algoritam.Domain.Entities;

/// <summary>
/// Parametri zarada iz LDPARAM.DBF.
/// Fox forme: LDPAR.SCX i LDPAR2.SCX.
/// U bazi postoji jedan aktivan zapis.
/// </summary>
public class LdParametar
{
    public int Id { get; set; }

    public int RedniBr { get; set; }
    public int Mesec { get; set; }
    public string Nazmes { get; set; } = string.Empty;
    public int Redispl { get; set; }
    public int Dana { get; set; }
    public int Cmes { get; set; }
    public int Cpraz { get; set; }
    public int Czakon { get; set; }

    public decimal Procnoc { get; set; }
    public decimal Procprod { get; set; }
    public decimal Procpraz { get; set; }
    public decimal Procned { get; set; }
    public decimal Procmin { get; set; }
    public decimal Procsus { get; set; }
    public decimal Ekoefs { get; set; }
    public int Minnac { get; set; }
    public decimal Procbol { get; set; }
    public decimal Procplac { get; set; }
    public decimal Prosbruto { get; set; }
    public decimal Minimal { get; set; }

    public decimal Ben1 { get; set; }
    public decimal Ben2 { get; set; }
    public decimal Ben3 { get; set; }
    public decimal Ben4 { get; set; }

    public int Isplata { get; set; }
    public int Sisplata { get; set; }

    public decimal Doppr1 { get; set; }
    public decimal Dopzr1 { get; set; }
    public decimal Dopnr1 { get; set; }
    public decimal Doppf1 { get; set; }
    public decimal Dopzf1 { get; set; }
    public decimal Dopnf1 { get; set; }

    public decimal Doppr2 { get; set; }
    public decimal Dopzr2 { get; set; }
    public decimal Dopnr2 { get; set; }
    public decimal Doppf2 { get; set; }
    public decimal Dopzf2 { get; set; }
    public decimal Dopnf2 { get; set; }

    public decimal Doppr3 { get; set; }
    public decimal Dopzr3 { get; set; }
    public decimal Dopnr3 { get; set; }
    public decimal Doppf3 { get; set; }
    public decimal Dopzf3 { get; set; }
    public decimal Dopnf3 { get; set; }

    public decimal Doppr4 { get; set; }
    public decimal Dopzr4 { get; set; }
    public decimal Dopnr4 { get; set; }
    public decimal Doppf4 { get; set; }
    public decimal Dopzf4 { get; set; }
    public decimal Dopnf4 { get; set; }

    public decimal Doppr5 { get; set; }
    public decimal Dopzr5 { get; set; }
    public decimal Dopnr5 { get; set; }
    public decimal Doppf5 { get; set; }
    public decimal Dopzf5 { get; set; }
    public decimal Dopnf5 { get; set; }

    public decimal Procpor { get; set; }

    public string S1 { get; set; } = string.Empty;
    public decimal Sdin1 { get; set; }
    public string S3 { get; set; } = string.Empty;
    public decimal Sdin3 { get; set; }
    public string S4 { get; set; } = string.Empty;
    public decimal Sdin4 { get; set; }
    public string S5 { get; set; } = string.Empty;
    public decimal Sdin5 { get; set; }
    public string S6 { get; set; } = string.Empty;
    public decimal Sdin6 { get; set; }
    public string S71 { get; set; } = string.Empty;
    public decimal Sdin71 { get; set; }
    public string S72 { get; set; } = string.Empty;
    public decimal Sdin72 { get; set; }
    public string S8 { get; set; } = string.Empty;
    public decimal Sdin8 { get; set; }

    public decimal Komoraj { get; set; }
    public decimal Komoras { get; set; }
    public decimal Komorar { get; set; }

    public int Smesec { get; set; }
    public string Snazmes { get; set; } = string.Empty;
    public int Sredispl { get; set; }
    public decimal Cenarada { get; set; }

    public string Kd1 { get; set; } = string.Empty;
    public string Kd4 { get; set; } = string.Empty;
    public string Kd9 { get; set; } = string.Empty;
    public string Kd12 { get; set; } = string.Empty;
    public string Kd20 { get; set; } = string.Empty;
    public string Kd22 { get; set; } = string.Empty;
    public string Kd24 { get; set; } = string.Empty;
    public string Kd25 { get; set; } = string.Empty;
    public string Kd27 { get; set; } = string.Empty;
    public string Kd28 { get; set; } = string.Empty;

    public DateTime? Dat1 { get; set; }
    public DateTime? Dat2 { get; set; }
    public DateTime? Dat3 { get; set; }
    public DateTime? Dat4 { get; set; }
    public string Godina { get; set; } = string.Empty;

    public string Nazp1 { get; set; } = string.Empty;
    public string Nazp2 { get; set; } = string.Empty;
    public string Nazp3 { get; set; } = string.Empty;
    public string Nazp4 { get; set; } = string.Empty;
    public string Nazp5 { get; set; } = string.Empty;
    public string Nazp5ter { get; set; } = string.Empty;
    public string Nazo1 { get; set; } = string.Empty;
    public string Nazo2 { get; set; } = string.Empty;
    public string Nazo3 { get; set; } = string.Empty;
    public string Nazo4 { get; set; } = string.Empty;
    public string Nazo5 { get; set; } = string.Empty;
    public string Nazo6 { get; set; } = string.Empty;

    public decimal Neoporez { get; set; }
    public decimal Neoporezp { get; set; }
    public string Decimale { get; set; } = string.Empty;
    public int Aktivrac { get; set; }
    public DateTime? Datpocdel { get; set; }
    public int Brzap0 { get; set; }
    public int Brzap1 { get; set; }
    public int Brzap2 { get; set; }
    public string Srazpor { get; set; } = string.Empty;
    public decimal Dinsat { get; set; }
    public decimal Tosat { get; set; }
    public decimal Regsat { get; set; }
    public string Konacna { get; set; } = string.Empty;
    public string Vrstaplate { get; set; } = string.Empty;
    public string Arhiva { get; set; } = string.Empty;
    public string Arhiva2 { get; set; } = string.Empty;
    public DateTime? Datod { get; set; }
    public DateTime? Datdo { get; set; }

    public decimal Solporod1 { get; set; }
    public decimal Solpordo1 { get; set; }
    public decimal Solproc1 { get; set; }
    public decimal Solporod2 { get; set; }
    public decimal Solpordo2 { get; set; }
    public decimal Solproc2 { get; set; }

    public decimal Bkproc { get; set; }
    public decimal Bkzastita { get; set; }
    public string Bknacin { get; set; } = string.Empty;
    public string Nakpos { get; set; } = string.Empty;
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
    public decimal Priprav { get; set; }
    public decimal Priprav1 { get; set; }
    public decimal Priprav2 { get; set; }

    public decimal Najosn { get; set; }
}
