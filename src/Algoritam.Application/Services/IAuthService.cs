using Algoritam.Domain.Entities;

namespace Algoritam.Application.Services;

public interface IAuthService
{
    Task<Korisnik?> PrijavaAsync(string korisnikIme, string lozinka);
}
