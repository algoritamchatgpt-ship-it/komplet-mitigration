namespace Algoritam.Domain.Entities;

/// <summary>
/// Zbirni podaci o platama iz LDPOD.DBF.
/// Fox forma: LDPOD / LDPOD00.
/// </summary>
public class LdPodStavka
{
    public int Id { get; set; }
    public string Kod { get; set; } = string.Empty;
    public string Opis { get; set; } = string.Empty;

    public decimal S1a { get; set; }
    public decimal Sv1a { get; set; }
    public decimal S1b { get; set; }
    public decimal Sv1b { get; set; }
    public decimal S1c { get; set; }
    public decimal Sv1c { get; set; }
    public decimal S1u { get; set; }
    public decimal Sv1u { get; set; }

    public decimal S2a { get; set; }
    public decimal Sv2a { get; set; }
    public decimal S2b { get; set; }
    public decimal Sv2b { get; set; }
    public decimal S2c { get; set; }
    public decimal Sv2c { get; set; }
    public decimal S2u { get; set; }
    public decimal Sv2u { get; set; }

    public decimal S3a { get; set; }
    public decimal Sv3a { get; set; }
    public decimal S3b { get; set; }
    public decimal Sv3b { get; set; }
    public decimal S3c { get; set; }
    public decimal Sv3c { get; set; }
    public decimal S3u { get; set; }
    public decimal Sv3u { get; set; }

    public decimal S4a { get; set; }
    public decimal Sv4a { get; set; }
    public decimal S4b { get; set; }
    public decimal Sv4b { get; set; }
    public decimal S4c { get; set; }
    public decimal Sv4c { get; set; }
    public decimal S4u { get; set; }
    public decimal Sv4u { get; set; }

    public decimal Su { get; set; }
    public decimal Svu { get; set; }

    public int Mesec { get; set; }
    public int Isplata { get; set; }
    public string Vrsta { get; set; } = string.Empty;
    public string Preneto { get; set; } = string.Empty;
    public int Numred { get; set; }
    public long Idbr { get; set; }
}
