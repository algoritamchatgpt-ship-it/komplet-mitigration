namespace GlavnaKnjiga.Models;

public sealed class NalprilPregledRow
{
    public string Grupa { get; set; } = string.Empty;
    public string Konto { get; set; } = string.Empty;
    public decimal Dug { get; set; }
    public decimal Pot { get; set; }
    public decimal Dugpre { get; set; }
    public decimal Potpre { get; set; }
    public decimal Saldo => Dugpre - Potpre + Dug - Pot;
}
