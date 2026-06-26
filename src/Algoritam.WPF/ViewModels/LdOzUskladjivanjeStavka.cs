namespace Algoritam.WPF.ViewModels;

public sealed class LdOzUskladjivanjeStavka
{
    public decimal K1zarada { get; set; }
    public decimal K1sati { get; set; }
    public decimal K1posatu { get; set; }
    public decimal K1mogsati { get; set; }
    public decimal K1prosmes { get; set; }
    public decimal K1pzarada { get; set; }
    public decimal K2zarada { get; set; }
    public decimal K2sati { get; set; }
    public decimal K2posatu { get; set; }
    public decimal K2mogsati { get; set; }
    public decimal K2prosmes { get; set; }
    public decimal K2pzarada { get; set; }
    public decimal Koef { get; set; }
    public string Mesec1 { get; set; } = string.Empty;
    public string Mesec2 { get; set; } = string.Empty;
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}
