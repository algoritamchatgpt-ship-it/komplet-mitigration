namespace GlavnaKnjiga.Models;

public sealed class NalslagRow
{
    public string Brnal { get; init; } = string.Empty;
    public decimal Dug { get; init; }
    public decimal Pot { get; init; }
    public decimal Razlika => Dug - Pot;
    public bool JeSlozen => Dug == Pot;
    public string Status => JeSlozen ? "SLOŽEN" : "NIJE SLOŽEN";
}
