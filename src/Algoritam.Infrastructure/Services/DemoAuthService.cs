using Algoritam.Application.Services;
using Algoritam.Domain.Entities;

namespace Algoritam.Infrastructure.Services;

/// <summary>
/// Demo auth servis sa hardkodovanim korisnicima.
/// ZAMENJUJE SE pravim DbAuthService nakon migracije LOZINKE.DBF -> SQLite.
/// </summary>
public class DemoAuthService : IAuthService
{
    private static readonly List<Korisnik> _korisnici =
    [
        new Korisnik
        {
            Pas = "1",
            KorisnikIme = "admin",
            Lozinka = "admin",
            Aktivan = true,
            JeSupervizor = true,
            PravaNivo = 1,
            PassGk = true, PassAn = true, PassBl = true, PassTv = true,
            PassTm = true, PassUs = true, PassLd = true, PassOst = true,
            PassPrn = true, PassPro = true, PassOs = true, PassProf = true,
            PassDel = true
        },
        new Korisnik
        {
            Pas = "2",
            KorisnikIme = "korisnik",
            Lozinka = "korisnik",
            Aktivan = true,
            JeSupervizor = false,
            PravaNivo = 0,
            PassGk = true, PassAn = true, PassBl = false, PassTv = false,
            PassTm = false, PassUs = false, PassLd = false, PassOst = true,
            PassPrn = true, PassPro = false, PassOs = false, PassProf = false,
            PassDel = false
        }
    ];

    public Task<Korisnik?> PrijavaAsync(string korisnikIme, string lozinka)
    {
        var korisnik = _korisnici.FirstOrDefault(k =>
            string.Equals(k.KorisnikIme, korisnikIme, StringComparison.OrdinalIgnoreCase) &&
            k.Lozinka == lozinka &&
            k.Aktivan);

        return Task.FromResult(korisnik);
    }
}
