using Algoritam.Core.Models;

namespace Algoritam.Core.Services;

public interface IFirmaService
{
    Task<List<Firma>> DajSveFirmeAsync();
    Task<Firma?> DodajFirmuAsync();
    Task<bool> ObrisiFirmuAsync(string folderPath);
}
