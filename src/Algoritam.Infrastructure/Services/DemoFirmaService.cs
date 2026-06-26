using Algoritam.Application.Services;
using Algoritam.Domain.Entities;

namespace Algoritam.Infrastructure.Services;

/// <summary>
/// Demo servis sa hardkodovanim firmama.
/// ZAMENJUJE SE pravim DbFirmaService nakon migracije PUTANJE.DBF → SQLite.
/// </summary>
public class DemoFirmaService : IFirmaService
{
    private static readonly List<Firma> _firme =
    [
        new Firma
        {
            Id = 1,
            Naziv = "DOO PRIMER TRADE",
            Naziv2 = "Primer Trade d.o.o.",
            Ulica = "Bulevar Oslobođenja", BrojUlice = "123",
            PostanskiBroj = "21000", Mesto = "Novi Sad",
            Pib = "123456789", Maticni = "12345678",
            ZiroRacun = "160-12345678-01",
            Aktivna = true
        },
        new Firma
        {
            Id = 2,
            Naziv = "PREDUZETNIK PETAR PETROVIĆ",
            Naziv2 = "",
            Ulica = "Knez Mihailova", BrojUlice = "10",
            PostanskiBroj = "11000", Mesto = "Beograd",
            Pib = "987654321", Maticni = "87654321",
            ZiroRacun = "205-98765432-11",
            Aktivna = true
        },
        new Firma
        {
            Id = 3,
            Naziv = "AD STARA FIRMA",
            Naziv2 = "Stara Firma a.d.",
            Ulica = "Cara Dušana", BrojUlice = "5",
            PostanskiBroj = "18000", Mesto = "Niš",
            Pib = "111222333", Maticni = "11223344",
            ZiroRacun = "310-11122233-42",
            Aktivna = false
        },
        new Firma
        {
            Id = 4,
            Naziv = "DOO NOVA NADA",
            Naziv2 = "",
            Ulica = "Vojvode Mišića", BrojUlice = "7",
            PostanskiBroj = "31000", Mesto = "Užice",
            Pib = "444555666", Maticni = "44556677",
            ZiroRacun = "160-44455566-22",
            Aktivna = true
        },
    ];

    public Task<IReadOnlyList<Firma>> DajSveFirmeAsync()
        => Task.FromResult<IReadOnlyList<Firma>>(_firme);

    public Task<Firma?> DajFirmuAsync(int id)
        => Task.FromResult(_firme.FirstOrDefault(f => f.Id == id));

    public Task<Firma?> DodajFirmuAsync()
    {
        var nextId = _firme.Count == 0 ? 1 : _firme.Max(f => f.Id) + 1;
        var nova = new Firma
        {
            Id = nextId,
            Naziv = $"NOVA FIRMA {nextId}",
            Naziv2 = "Nova firma",
            Aktivna = true,
            FolderPath = $"F{nextId}"
        };

        _firme.Add(nova);
        return Task.FromResult<Firma?>(nova);
    }

    public Task<bool> ObrisiFirmuAsync(string folderPath)
    {
        var firma = _firme.FirstOrDefault(f =>
            string.Equals(f.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));

        if (firma is null) return Task.FromResult(false);
        _firme.Remove(firma);
        return Task.FromResult(true);
    }
}
