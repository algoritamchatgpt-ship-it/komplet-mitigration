namespace Algoritam.WPF.Models;

public sealed class PregledTabelaStavka
{
    public string Sifra { get; init; } = string.Empty;
    public string Naziv { get; init; } = string.Empty;
    public decimal Iznos1 { get; init; }
    public decimal Iznos2 { get; init; }
}
