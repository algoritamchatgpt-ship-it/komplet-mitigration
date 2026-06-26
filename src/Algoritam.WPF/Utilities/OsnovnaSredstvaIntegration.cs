using System.IO;
using AlgoritamAppState = Algoritam.Application.AppState;
using AlgoritamFirma = Algoritam.Domain.Entities.Firma;
using AlgoritamKorisnik = Algoritam.Domain.Entities.Korisnik;
using AlgoritamPutanjaService = Algoritam.Application.Services.IPutanjaService;
using OsAppState = OsnovnaSredstva.Services.AppState;
using OsFirma = OsnovnaSredstva.Models.Firma;
using OsKorisnik = OsnovnaSredstva.Models.Korisnik;
using OsPutanjaService = OsnovnaSredstva.Services.IPutanjaService;

namespace Algoritam.WPF.Utilities;

internal static class OsnovnaSredstvaIntegration
{
    public static OsAppState CreateAppState(AlgoritamAppState appState)
    {
        var osAppState = new OsAppState
        {
            AktivnaGodina = appState.AktivnaGodina
        };

        if (appState.TrenutniKorisnik is { } korisnik)
        {
            osAppState.Prijavi(MapKorisnik(korisnik));
        }

        if (appState.AktivnaFirma is { } firma)
        {
            osAppState.PostaviFirmu(MapFirma(firma));
        }

        return osAppState;
    }

    public static OsPutanjaService CreatePutanjaService(AlgoritamPutanjaService putanjaService)
        => new PutanjaServiceAdapter(putanjaService);

    private static OsFirma MapFirma(AlgoritamFirma firma)
    {
        return new OsFirma
        {
            FolderPath = firma.FolderPath,
            FolderIme = DajImeFoldera(firma.FolderPath),
            Aktivna = firma.Aktivna,
            Naziv = firma.Naziv,
            Naziv2 = firma.Naziv2,
            NazivLatinican = firma.NazivLatinican,
            Baza = firma.Baza,
            Vlasnik = firma.Vlasnik,
            OdgovornoLice = firma.OdgovornoLice,
            OrganizacioniOblik = firma.OrganizacioniOblik,
            Maticni = firma.Maticni,
            MatBr = firma.Maticni,
            Pib = firma.Pib,
            PdvObveznik = firma.PdvObveznik,
            SifraDelatnosti = firma.SifraDelatnosti,
            NazivDelatnosti = firma.NazivDelatnosti,
            PostanskiBroj = firma.PostanskiBroj,
            Mesto = firma.Mesto,
            Ulica = firma.Ulica,
            BrojUlice = firma.BrojUlice,
            Opstina = firma.Opstina,
            Republika = firma.Republika,
            Drzava = firma.Drzava,
            Telefon1 = firma.Telefon1,
            Telefon2 = firma.Telefon2,
            Fax1 = firma.Fax1,
            Email = firma.Email,
            Web = firma.Web,
            Agencija = firma.Agencija,
            ZiroRacun = firma.ZiroRacun,
            ZiroRacun2 = firma.ZiroRacun2,
            ZiroRacunDevizni = firma.ZiroRacunDevizni,
            ZiroRacunBolovanje = firma.ZiroRacunBolovanje,
            Banka1 = firma.Banka1,
            Banka2 = firma.Banka2,
            BankaDevizna = firma.BankaDevizna,
            BankaBolovanje = firma.BankaBolovanje,
            SwiftKod = firma.SwiftKod,
            DatumOsnivanja = firma.DatumOsnivanja,
            DatumRegistracije = firma.DatumRegistracije,
            DatumUpisa = firma.DatumUpisa,
            DatumPdv = firma.DatumPdv,
            RegBrojSocijalno = firma.RegBrojSocijalno,
            RegBrojZdravstveno = firma.RegBrojZdravstveno,
            SudskiRegistar = firma.SudskiRegistar
        };
    }

    private static OsKorisnik MapKorisnik(AlgoritamKorisnik korisnik)
    {
        return new OsKorisnik
        {
            Pas = korisnik.Pas,
            KorisnikIme = korisnik.KorisnikIme,
            KorisnikIme2 = korisnik.KorisnikIme2,
            Lozinka = korisnik.Lozinka,
            Aktivan = korisnik.Aktivan,
            JeSupervizor = korisnik.JeSupervizor,
            PravaNivo = korisnik.PravaNivo,
            PassGk = korisnik.PassGk,
            PassAn = korisnik.PassAn,
            PassBl = korisnik.PassBl,
            PassTv = korisnik.PassTv,
            PassTm = korisnik.PassTm,
            PassUs = korisnik.PassUs,
            PassLd = korisnik.PassLd,
            PassOst = korisnik.PassOst,
            PassPrn = korisnik.PassPrn,
            PassPro = korisnik.PassPro,
            PassOs = korisnik.PassOs,
            PassProf = korisnik.PassProf,
            PassDel = korisnik.PassDel
        };
    }

    private static string DajImeFoldera(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath)) return string.Empty;

        var ociscenaPutanja = folderPath.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);

        return Path.GetFileName(ociscenaPutanja);
    }

    private sealed class PutanjaServiceAdapter(AlgoritamPutanjaService inner) : OsPutanjaService
    {
        public string? DajFinPutanju() => inner.DajFinPutanju();

        public bool SnimiFinPutanju(string putanja) => inner.SnimiFinPutanju(putanja);

        public bool JeValidanFinFolder(string putanja) => inner.JeValidanFinFolder(putanja);

        public string? DajArhivaPutanju() => inner.DajArhivaPutanju();

        public bool SnimiArhivaPutanju(string putanja) => inner.SnimiArhivaPutanju(putanja);

        public string? DajIzvozPutanju() => inner.DajIzvozPutanju();

        public bool SnimiIzvozPutanju(string putanja) => inner.SnimiIzvozPutanju(putanja);
    }
}
