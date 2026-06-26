using Algoritam.Core.Models;

namespace Algoritam.Core.Services;

public record PrijavaRezultat(Korisnik? Korisnik, string Poruka = "")
{
    public bool Uspelo => Korisnik != null;
}

public interface IAuthService
{
    Task<PrijavaRezultat> PrijavaAsync(string korisnikIme, string lozinka);
}
