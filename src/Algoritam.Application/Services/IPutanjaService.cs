namespace Algoritam.Application.Services;

public interface IPutanjaService
{
    /// <summary>Vraća sačuvanu putanju do FIN root foldera, ili null ako nije postavljena.</summary>
    string? DajFinPutanju();

    /// <summary>Čuva putanju i vraća true ako je validna FIN instalacija.</summary>
    bool SnimiFinPutanju(string putanja);

    /// <summary>Proverava da li folder izgleda kao FIN instalacija.</summary>
    bool JeValidanFinFolder(string putanja);

    /// <summary>Da li je putanja već postavljena i validna.</summary>
    bool PutanjaPostavljena { get; }

    /// <summary>Vraća sačuvanu putanju za arhivu, ili null ako nije postavljena.</summary>
    string? DajArhivaPutanju();

    /// <summary>Čuva putanju za arhivu. Vraća true ako je uspelo.</summary>
    bool SnimiArhivaPutanju(string putanja);

    /// <summary>Vraća poslednju korišćenu putanju za izvoz tabela, ili null.</summary>
    string? DajIzvozPutanju();

    /// <summary>Čuva putanju za izvoz tabela. Vraća true ako je uspelo.</summary>
    bool SnimiIzvozPutanju(string putanja);
}
