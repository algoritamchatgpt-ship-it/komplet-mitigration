namespace GlavnaKnjiga.Models;

public sealed class NalogPregledRow
{
    public string Konto { get; init; } = string.Empty;
    public decimal Dug { get; init; }
    public decimal Pot { get; init; }
    public string Opis { get; init; } = string.Empty;
    public DateTime? Datdok { get; init; }
    public string Brnal { get; init; } = string.Empty;
    public string Dok { get; init; } = string.Empty;
    public decimal Mp { get; init; }
    public decimal Mtr { get; init; }
    public string Dev { get; init; } = string.Empty;
    public decimal Devkurs { get; init; }
    public decimal Devdug { get; init; }
    public decimal Devpot { get; init; }
    public string Sifra { get; init; } = string.Empty;
    public string Brrac { get; init; } = string.Empty;
    public decimal Ulaz { get; init; }
    public decimal Izlaz { get; init; }
    public decimal Saldo { get; init; }
}
