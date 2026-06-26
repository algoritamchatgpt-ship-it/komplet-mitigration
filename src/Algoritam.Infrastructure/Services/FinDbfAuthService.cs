using Algoritam.Application.Services;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Dbf;

namespace Algoritam.Infrastructure.Services;

/// <summary>
/// Autentifikacija korisnika direktno iz LOZINKE.DBF tabele.
/// </summary>
public class FinDbfAuthService : IAuthService
{
    private readonly IPutanjaService _putanjaService;

    public FinDbfAuthService(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
    }

    public static string? PronadjiLozinkeFajl(string? finPutanja)
        => FinWorkspaceResolver.ResolveLozinkeTablePath(finPutanja);

    public Task<Korisnik?> PrijavaAsync(string korisnikIme, string lozinka)
    {
        var finPutanja = _putanjaService.DajFinPutanju();
        var lozinkeFajl = PronadjiLozinkeFajl(finPutanja);
        if (string.IsNullOrWhiteSpace(lozinkeFajl))
            return Task.FromResult<Korisnik?>(null);

        try
        {
            var zapisi = DbfReader.CitajSveZapise(lozinkeFajl);
            var zapis = zapisi.FirstOrDefault(z =>
                string.Equals(z.Str("KORISNIK"), korisnikIme, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(z.Str("LOZINKA"), lozinka, StringComparison.Ordinal));

            if (zapis is null)
            {
                Serilog.Log.Warning("Login fail: korisnik '{K}' nije pronađen u {F}", korisnikIme, lozinkeFajl);
                return Task.FromResult<Korisnik?>(null);
            }
            if (!zapis.JeAktivan())
            {
                Serilog.Log.Warning("Login fail: korisnik '{K}' nije aktivan (AKTIVAN='{A}')", korisnikIme, zapis.Str("AKTIVAN"));
                return Task.FromResult<Korisnik?>(null);
            }

            var pravaNivo = (int)(zapis.Dec("PASSNIVO") ?? 0m);

            var korisnik = new Korisnik
            {
                Pas = zapis.Str("PAS"),
                KorisnikIme = zapis.Str("KORISNIK"),
                Lozinka = zapis.Str("LOZINKA"),
                KorisnikIme2 = zapis.Str("KORIME"),
                Aktivan = true,
                JeSupervizor = zapis.Str("OPERATER") == "1" || pravaNivo > 0,
                PravaNivo = pravaNivo,

                PassGk = zapis.Bool("PASSGK"),
                PassAn = zapis.Bool("PASSAN"),
                PassBl = zapis.Bool("PASSBL"),
                PassTv = zapis.Bool("PASSTV"),
                PassTm = zapis.Bool("PASSTM"),
                PassUs = zapis.Bool("PASSUS"),
                PassLd = zapis.Bool("PASSLD"),
                PassOst = zapis.Bool("PASSOST"),
                PassPrn = zapis.Bool("PASSPRN"),
                PassPro = zapis.Bool("PASSPRO"),
                PassOs = zapis.Bool("PASSOS"),
                PassProf = zapis.Bool("PASSPROF"),
                PassDel = zapis.Bool("PASSDEL"),

                PassTvRad = zapis.Bool("PASSTVRA"),
                PassTvKal = zapis.Bool("PASSTVKAL"),
                PassTvRac = zapis.Bool("PASSTVRAC"),
                PassTvNiv = zapis.Bool("PASSTVNIV"),
                PassTmRad = zapis.Bool("PASSTMRA"),
                PassTmKal = zapis.Bool("PASSTMKAL"),
                PassTmRac = zapis.Bool("PASSTMRAC"),
                PassTmNiv = zapis.Bool("PASSTMNIV"),
            };

            return Task.FromResult<Korisnik?>(korisnik);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Login greška pri čitanju {F}", lozinkeFajl);
            return Task.FromResult<Korisnik?>(null);
        }
    }
}

internal static class ZapisExtensions
{
    public static string Str(this Dictionary<string, object?> zapis, string polje)
        => zapis.TryGetValue(polje, out var value)
            ? Convert.ToString(value)?.Trim() ?? string.Empty
            : string.Empty;

    public static decimal? Dec(this Dictionary<string, object?> zapis, string polje)
        => zapis.TryGetValue(polje, out var value) switch
        {
            true when value is decimal d => d,
            true when value is int i => i,
            true when value is long l => l,
            true when decimal.TryParse(Convert.ToString(value), out var parsed) => parsed,
            _ => null
        };

    public static bool Bool(this Dictionary<string, object?> zapis, string polje)
    {
        if (!zapis.TryGetValue(polje, out var value) || value is null)
            return false;

        return value switch
        {
            bool b => b,
            decimal d => d != 0m,
            int i => i != 0,
            long l => l != 0,
            string s => JeTruthyString(s),
            _ => JeTruthyString(Convert.ToString(value) ?? string.Empty)
        };
    }

    public static bool JeAktivan(this Dictionary<string, object?> zapis)
    {
        if (!zapis.TryGetValue("AKTIVAN", out var value) || value is null)
            return true;

        if (value is string s)
        {
            var trimmed = s.Trim();
            if (string.IsNullOrEmpty(trimmed)) return true;
            return JeTruthyString(trimmed) || string.Equals(trimmed, "D", StringComparison.OrdinalIgnoreCase);
        }

        return value switch
        {
            bool b => b,
            decimal d => d != 0m,
            int i => i != 0,
            _ => JeTruthyString(Convert.ToString(value) ?? string.Empty)
        };
    }

    private static bool JeTruthyString(string value)
    {
        var norm = value.Trim().ToUpperInvariant();
        return norm is "T" or "Y" or ".T." or "1" or "D" or "TRUE";
    }
}
