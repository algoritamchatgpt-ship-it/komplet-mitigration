namespace Algoritam.Domain.Entities;

/// <summary>
/// Radna tabela knjiženja zarada (LDKON.DBF).
/// </summary>
public class LdKnjizenjeStavka
{
    public int Id { get; set; }
    public string Vrsta { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public string Opis { get; set; } = string.Empty;
    public string Konto { get; set; } = string.Empty;
    public string Kontop { get; set; } = string.Empty;
    public decimal Iznos { get; set; }
    public DateTime? Datdok { get; set; }
    public string Brnal { get; set; } = string.Empty;
    public string Mp { get; set; } = string.Empty;
    public int Mtr { get; set; }
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}
