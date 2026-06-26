using Algoritam.Application.Services;
using Algoritam.Domain.Entities;

namespace Algoritam.Infrastructure.Services;

/// <summary>
/// Ako je postavljena FIN/workspace putanja, koristi DBF autentifikaciju.
/// Ako putanja nije postavljena, koristi demo korisnike.
/// </summary>
public class SmartAuthService : IAuthService
{
    private readonly IPutanjaService _putanjaService;
    private readonly DemoAuthService _demo = new();

    public SmartAuthService(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
    }

    public Task<Korisnik?> PrijavaAsync(string korisnikIme, string lozinka)
    {
        var finPutanja = _putanjaService.DajFinPutanju();
        if (string.IsNullOrWhiteSpace(finPutanja))
            return _demo.PrijavaAsync(korisnikIme, lozinka);

        FinWorkspaceResolver.EnsureLozinkeTables(finPutanja, out _, out _, out _);
        return new FinDbfAuthService(_putanjaService).PrijavaAsync(korisnikIme, lozinka);
    }
}
