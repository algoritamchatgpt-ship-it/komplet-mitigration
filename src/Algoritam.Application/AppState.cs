using Algoritam.Domain.Entities;

namespace Algoritam.Application;

public class AppState
{
    private IDisposable? _aktivnaSesijaLock;

    public Korisnik? TrenutniKorisnik { get; private set; }
    public Firma? AktivnaFirma { get; private set; }
    public bool AktivnaFirmaJeStandalone { get; private set; }
    public int AktivnaGodina { get; set; } = DateTime.Now.Year;

    public bool JePrijavljen => TrenutniKorisnik != null;
    public bool JeSupervizor => TrenutniKorisnik?.JeSupervizor ?? false;
    public bool ImaFirmu => AktivnaFirma != null;

    /// <summary>Putanja do SQLite baze aktivne firme. Null ako firma nije izabrana.</summary>
    public string? DbPath => AktivnaFirma is { FolderPath: { Length: > 0 } fp }
        ? ZaradePaths.GetDbPath(fp)
        : null;

    public void Prijavi(Korisnik korisnik) => TrenutniKorisnik = korisnik;

    public void PostaviSesijaLock(IDisposable? handle)
    {
        OslobodiSesijaLock();
        _aktivnaSesijaLock = handle;
    }

    public void OslobodiSesijaLock()
    {
        if (_aktivnaSesijaLock is null)
            return;

        try
        {
            _aktivnaSesijaLock.Dispose();
        }
        catch
        {
            // Session lock release is best-effort.
        }
        finally
        {
            _aktivnaSesijaLock = null;
        }
    }

    public void Odjavi()
    {
        OslobodiSesijaLock();
        TrenutniKorisnik = null;
        AktivnaFirma = null;
        AktivnaFirmaJeStandalone = false;
    }

    public void PostaviFirmu(Firma firma, bool standalone = false)
    {
        AktivnaFirma = firma;
        AktivnaFirmaJeStandalone = standalone;
    }
}
