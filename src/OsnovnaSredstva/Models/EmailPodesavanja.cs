namespace OsnovnaSredstva.Models;

public class EmailPodesavanja
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool KoristiTls { get; set; } = true;
    public string KorisnickoIme { get; set; } = string.Empty;
    public string Lozinka { get; set; } = string.Empty;
    public string PosiljaoceEmail { get; set; } = string.Empty;
    public string PosiljaoceIme { get; set; } = string.Empty;
}
