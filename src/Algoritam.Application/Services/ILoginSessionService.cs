namespace Algoritam.Application.Services;

public interface ILoginSessionService
{
    bool TryAcquireSession(string korisnikIme, out IDisposable? handle, out string message);
}
