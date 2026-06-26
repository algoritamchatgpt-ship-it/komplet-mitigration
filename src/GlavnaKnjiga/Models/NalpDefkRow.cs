namespace GlavnaKnjiga.Models;

public class NalpDefkRow
{
    public decimal Sifprod    { get; set; }
    public string  Konto      { get; set; } = string.Empty;
    public string  Pnaziv     { get; set; } = string.Empty;
    public string  Devizno    { get; set; } = string.Empty;
    public string  Sifarnik   { get; set; } = string.Empty;
    public string  Dok        { get; set; } = string.Empty;
    public string  Vrsta      { get; set; } = string.Empty;
    public string  Imetabele  { get; set; } = string.Empty;
    public string  Dp         { get; set; } = string.Empty;
    public string  Preneto    { get; set; } = string.Empty;
    public decimal Numred     { get; set; }
    public decimal Idbr       { get; set; }
}
