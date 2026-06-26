namespace Algoritam.Domain.Entities;

/// <summary>
/// Stavka spiska za isplatu iz LDSPIS.DBF.
/// Fox forma: LDSPIS.SCX.
/// </summary>
public class LdSpisStavka
{
    public int Id { get; set; }
    public int Broj { get; set; }
    public string ImePrez { get; set; } = string.Empty;
    public string Partija { get; set; } = string.Empty;
    public decimal Iznos { get; set; }
    public string Sifra { get; set; } = string.Empty;
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}
