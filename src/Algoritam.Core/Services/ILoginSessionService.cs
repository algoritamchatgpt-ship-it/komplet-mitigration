namespace Algoritam.Core.Services;

public interface ILoginSessionService
{
    bool TryAcquireSession(string korisnikIme, out IDisposable? lockHandle, out string? poruka);
    void ReleaseSession(string korisnikIme);
}
