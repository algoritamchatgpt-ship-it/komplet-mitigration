namespace Algoritam.Domain.Entities;

/// <summary>
/// Sablon konta za knjiženje zarada (LDKON00.DBF).
/// </summary>
public class LdKontoSablonStavka
{
    public int Id { get; set; }
    public string Vrsta { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public string Opis { get; set; } = string.Empty;
    public string Konto { get; set; } = string.Empty;
    public string Kontop { get; set; } = string.Empty;
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}
