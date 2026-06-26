using Algoritam.Application.Services;

namespace Algoritam.Infrastructure.Services;

public class FileLoginSessionService : ILoginSessionService
{
    private readonly IPutanjaService _putanjaService;

    public FileLoginSessionService(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
    }

    public bool TryAcquireSession(string korisnikIme, out IDisposable? handle, out string message)
    {
        handle = null;
        message = string.Empty;

        var finPutanja = _putanjaService.DajFinPutanju();
        if (string.IsNullOrWhiteSpace(finPutanja))
            return true;

        return FinWorkspaceResolver.TryAcquireUserSessionLock(finPutanja, korisnikIme, out handle, out message);
    }
}
