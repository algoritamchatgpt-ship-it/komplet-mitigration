namespace Algoritam.Domain.Entities;

/// <summary>
/// Jedna stavka rekapitulacije zarade (ekvivalent reda u LDREK.DBF).
/// Fox forma: ldrekap2.prg — 34 stavke sa KOD, OPIS, PRE, SADA, RAZLIKA.
/// </summary>
public class RekapitulacijaStavka
{
    public int RedniBroj { get; set; }
    public string Kod { get; set; } = string.Empty;
    public string Opis { get; set; } = string.Empty;
    public decimal Pre { get; set; }
    public decimal Sada { get; set; }
    public decimal Razlika { get; set; }
    public bool ImaLiniju { get; set; }
}
