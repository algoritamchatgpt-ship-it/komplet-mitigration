using Algoritam.Domain.Entities;

namespace Algoritam.Application.Services;

public interface IFirmaService
{
    Task<IReadOnlyList<Firma>> DajSveFirmeAsync();
    Task<Firma?> DajFirmuAsync(int id);
    Task<Firma?> DodajFirmuAsync();
    Task<bool> ObrisiFirmuAsync(string folderPath);
}
