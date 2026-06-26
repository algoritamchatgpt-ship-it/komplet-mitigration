using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using System.Collections.ObjectModel;
using KolDef = OsnovnaSredstva.Services.OsStampacHelper.KolDef;

namespace OsnovnaSredstva.ViewModels;

public partial class OsSifarnikPopisnaListaViewModel : ObservableObject
{
    public enum Tip { VrsteOs, AmortGrupe, AmortPodgrupe, IzvoriFinansiranja, OsnKoriscenja }

    [ObservableProperty] private string _naslov = "";
    [ObservableProperty] private string _poruka = "";

    // Samo jedan od ovih listova je aktivan — ostali su prazni
    public ObservableCollection<OsVrstaStavka>  VrsteOs           { get; } = [];
    public ObservableCollection<OsAgStavka>     AmortGrupe        { get; } = [];
    public ObservableCollection<OsAgPodStavka>  AmortPodgrupe     { get; } = [];
    public ObservableCollection<OsIzvorStavka>  IzvoriFinansiranja{ get; } = [];
    public ObservableCollection<OsOsnKStavka>   OsnKoriscenja     { get; } = [];

    public Tip AktivniTip { get; }

    public bool JeVrste   => AktivniTip == Tip.VrsteOs;
    public bool JeGrupe   => AktivniTip == Tip.AmortGrupe;
    public bool JePodgrupe=> AktivniTip == Tip.AmortPodgrupe;
    public bool JeIzvori  => AktivniTip == Tip.IzvoriFinansiranja;
    public bool JeOsnovi  => AktivniTip == Tip.OsnKoriscenja;

    public OsSifarnikPopisnaListaViewModel(
        Tip tip,
        IEnumerable<OsVrstaStavka>  vrste,
        IEnumerable<OsAgStavka>     grupe,
        IEnumerable<OsAgPodStavka>  podgrupe,
        IEnumerable<OsIzvorStavka>  izvori,
        IEnumerable<OsOsnKStavka>   osnovi)
    {
        AktivniTip = tip;

        Naslov = tip switch
        {
            Tip.VrsteOs            => "POPISNA LISTA — VRSTE OSNOVNIH SREDSTAVA",
            Tip.AmortGrupe         => "POPISNA LISTA — AMORTIZACIONE GRUPE",
            Tip.AmortPodgrupe      => "POPISNA LISTA — PODGRUPE AMORTIZACIJE",
            Tip.IzvoriFinansiranja => "POPISNA LISTA — IZVORI FINANSIRANJA",
            Tip.OsnKoriscenja      => "POPISNA LISTA — OSNOVI KORIŠĆENJA",
            _                      => "POPISNA LISTA"
        };

        foreach (var x in vrste)    VrsteOs.Add(x);
        foreach (var x in grupe)    AmortGrupe.Add(x);
        foreach (var x in podgrupe) AmortPodgrupe.Add(x);
        foreach (var x in izvori)   IzvoriFinansiranja.Add(x);
        foreach (var x in osnovi)   OsnKoriscenja.Add(x);

        var broj = tip switch
        {
            Tip.VrsteOs            => VrsteOs.Count,
            Tip.AmortGrupe         => AmortGrupe.Count,
            Tip.AmortPodgrupe      => AmortPodgrupe.Count,
            Tip.IzvoriFinansiranja => IzvoriFinansiranja.Count,
            Tip.OsnKoriscenja      => OsnKoriscenja.Count,
            _                      => 0
        };
        Poruka = $"Ukupno zapisa: {broj}";
    }

    [RelayCommand]
    private void Stampa()
    {
        switch (AktivniTip)
        {
            case Tip.VrsteOs:
                OsStampacHelper.Stampaj(Naslov,
                    [new("VRSTA", 110, false), new("NAZIV VRSTE SREDSTAVA", 330, false), new("PRENETO", 70, false), new("ID BR", 55)],
                    VrsteOs.Select(x => new[] { x.Vrsta, x.Naziv, x.Preneto, x.IDBr.ToString() }).ToList(),
                    onGotov: m => Poruka = m);
                break;
            case Tip.AmortGrupe:
                OsStampacHelper.Stampaj(Naslov,
                    [new("AG", 75, false), new("STOPA %", 80), new("OPIS GRUPE", 300, false), new("VRSTA", 65, false), new("PRENETO", 65, false), new("ID BR", 50)],
                    AmortGrupe.Select(x => new[] { x.Ag, x.AgStopa.ToString("N2"), x.Opis, x.Vrsta, x.Preneto, x.IDBr.ToString() }).ToList(),
                    onGotov: m => Poruka = m);
                break;
            case Tip.AmortPodgrupe:
                OsStampacHelper.Stampaj(Naslov,
                    [new("PODGRUPA", 100, false), new("AG", 75, false), new("OPIS PODGRUPE", 330, false), new("PRENETO", 65, false), new("ID BR", 50)],
                    AmortPodgrupe.Select(x => new[] { x.AgPod, x.Ag, x.Opis, x.Preneto, x.IDBr.ToString() }).ToList(),
                    onGotov: m => Poruka = m);
                break;
            case Tip.IzvoriFinansiranja:
                OsStampacHelper.Stampaj(Naslov,
                    [new("IZVOR", 110, false), new("NAZIV IZVORA FINANSIRANJA", 330, false), new("PRENETO", 70, false), new("ID BR", 55)],
                    IzvoriFinansiranja.Select(x => new[] { x.Izvor, x.Naziv, x.Preneto, x.IDBr.ToString() }).ToList(),
                    onGotov: m => Poruka = m);
                break;
            case Tip.OsnKoriscenja:
                OsStampacHelper.Stampaj(Naslov,
                    [new("OSNOV", 110, false), new("NAZIV OSNOVA KORIŠĆENJA", 330, false), new("PRENETO", 70, false), new("ID BR", 55)],
                    OsnKoriscenja.Select(x => new[] { x.OsnovKor, x.Naziv, x.Preneto, x.IDBr.ToString() }).ToList(),
                    onGotov: m => Poruka = m);
                break;
        }
    }
}
