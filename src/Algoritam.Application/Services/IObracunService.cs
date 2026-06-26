using Algoritam.Domain.Entities;

namespace Algoritam.Application.Services;

/// <summary>
/// Servis za obračun zarade — prevod FoxPro procedura LDOBRACUN / OBRACSOC / SABERI*.
/// </summary>
public interface IObracunService
{
    /// <summary>
    /// Izvršava kompletni obračun za jednu stavku (jednog radnika u mesecu).
    /// Ekvivalent poziva: DO LDOBRACUN WITH cenarada, grupa, obrNaknade, obrUkupnaObaveza
    /// </summary>
    void Obracunaj(
        LdObracunStavka stavka,
        Radnik radnik,
        LdParametar param,
        bool obracunatiNaknade = false,
        bool obracunOdUkupneObaveze = false);

    /// <summary>
    /// Obračun NETO → BRUTO. Ekvivalent FoxPro procedure LDOBRACUNN + SABERIBRUTON
    /// (ldobracunn0.prg). Najpre prevodi časove u dinare po istoj logici kao bruto put,
    /// zatim izračunava MNETO2 = DINUK + stimulacije + dodaci, i otud računa BRUTO
    /// inflacijom: BRUTO = (MNETO2 - oslob - bkumanj) * 100 / (100 - procPor - dopProc).
    /// </summary>
    /// <param name="stari">
    /// Ako je TRUE — koristi staru varijantu (ldobracunnstari.prg): uvek računa naknade
    /// i ne primenjuje BK zaštitu. Ako je FALSE — nova varijanta sa BK zaštitom.
    /// </param>
    void ObracunajOdNeta(
        LdObracunStavka stavka,
        Radnik radnik,
        LdParametar param,
        bool stari = false);
}
