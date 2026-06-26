using Algoritam.Application.Services;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using DomainRadnik = Algoritam.Domain.Entities.Radnik;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za karticu radnika - detaljni prikaz obracuna jednog radnika.
/// Ekvivalent FoxPro forme NALKARTICE.SCX.
/// </summary>
public partial class KarticaRadnikaViewModel : ObservableObject
{
    private readonly IObracunService _obracunService = new ObracunService();
    private readonly DomainRadnik? _radnik;
    private readonly LdObracunStavka _izvornaStavka;
    private LdObracunStavka _stavka;

    [ObservableProperty]
    private string _statusPoruka = string.Empty;

    public bool Potvrdjen { get; private set; }

    public LdObracunStavka Stavka
    {
        get => _stavka;
        private set
        {
            _stavka = value;
            OnPropertyChanged(string.Empty);
        }
    }

    public LdParametar? Param { get; }

    public string Naslov => $"KARTICA - {Stavka.ImePrez}  (br. {Stavka.Broj})";

    public string PeriodInfo => Param != null
        ? $"Mesec: {Param.Mesec}  Isplata: {Param.Isplata}  Godina: {Param.Godina}"
        : $"Mesec: {Stavka.Mesec}  Godina: {Stavka.Godina}";

    public decimal StartbodPrikaz => _radnik?.StartniBodovi ?? Stavka.Startbod;
    public int StazPrikaz => _radnik?.Staz ?? 0;
    public string StepenPrikaz => _radnik?.Stepen ?? string.Empty;

    public decimal Casuc => Stavka.Casuc;
    public decimal Casnoc => Stavka.Casnoc;
    public decimal Casprod => Stavka.Casprod;
    public decimal Casradnap => Stavka.Casradnap;
    public decimal Casned => Stavka.Casned;
    public decimal Casdor => Stavka.Casdor;
    public decimal Cslput => Stavka.Cslput;
    public decimal Caspraz => Stavka.Caspraz;
    public decimal Casbol => Stavka.Casbol;
    public decimal Casbol2 => Stavka.Casbol2;
    public decimal Casplac => Stavka.Casplac;
    public decimal Casplac2 => Stavka.Casplac2;
    public decimal Casgod => Stavka.Casgod;
    public decimal Casvv => Stavka.Casvv;
    public decimal Cassus => Stavka.Cassus;
    public decimal Casuk => Stavka.Casuk;

    public decimal Dinuc => Stavka.Dinuc;
    public decimal Dinnoc => Stavka.Dinnoc;
    public decimal Dinprod => Stavka.Dinprod;
    public decimal Dinradnap => Stavka.Dinradnap;
    public decimal Dinned => Stavka.Dinned;
    public decimal Dindor => Stavka.Dindor;
    public decimal Dinsl => Stavka.Dinsl;
    public decimal Dinpraz => Stavka.Dinpraz;
    public decimal Dinbol => Stavka.Dinbol;
    public decimal Dinbol2 => Stavka.Dinbol2;
    public decimal Dinplac => Stavka.Dinplac;
    public decimal Dinplac2 => Stavka.Dinplac2;
    public decimal Dingod => Stavka.Dingod;
    public decimal Dinvv => Stavka.Dinvv;
    public decimal Dinsus => Stavka.Dinsus;
    public decimal Dinmin => Stavka.Dinmin;
    public decimal Dinuk => Stavka.Dinuk;

    public decimal Stim1 => Stavka.Stim1;
    public decimal Stim2 => Stavka.Stim2;
    public decimal Stim3 => Stavka.Stim3;

    public decimal Topli
    {
        get => Stavka.Topli;
        set
        {
            if (Stavka.Topli == value) return;
            Stavka.Topli = value;
            OnPropertyChanged();
        }
    }

    public decimal Regres
    {
        get => Stavka.Regres;
        set
        {
            if (Stavka.Regres == value) return;
            Stavka.Regres = value;
            OnPropertyChanged();
        }
    }

    public decimal Terenski
    {
        get => Stavka.Terenski;
        set
        {
            if (Stavka.Terenski == value) return;
            Stavka.Terenski = value;
            OnPropertyChanged();
        }
    }

    public decimal Fiksna
    {
        get => Stavka.Fiksna;
        set
        {
            if (Stavka.Fiksna == value) return;
            Stavka.Fiksna = value;
            OnPropertyChanged();
        }
    }

    public decimal Ldodaci => Stavka.Ldodaci;
    public decimal Naknade => Stavka.Naknade;

    public decimal Bruto => Stavka.Bruto;
    public decimal Neto => Stavka.Neto;
    public decimal Neto2 => Stavka.Neto2;
    public decimal Zaisplatu => Stavka.Zaisplatu;

    public decimal Dopsocr => Stavka.Dopsocr;
    public decimal Dopsocf => Stavka.Dopsocf;
    public decimal Doppr => Stavka.Doppr;
    public decimal Dopzr => Stavka.Dopzr;
    public decimal Dopnr => Stavka.Dopnr;
    public decimal Doppf => Stavka.Doppf;
    public decimal Dopzf => Stavka.Dopzf;
    public decimal Dopnf => Stavka.Dopnf;

    public decimal Porez => Stavka.Porez;
    public decimal Porezs => Stavka.Porezs;
    public decimal Porezu => Stavka.Porezu;
    public decimal Poroslob => Stavka.Poroslob;
    public decimal Osnovica => Stavka.Osnovica;

    public decimal Krediti => Stavka.Krediti;

    public decimal Prevoz
    {
        get => Stavka.Prevoz;
        set
        {
            if (Stavka.Prevoz == value) return;
            Stavka.Prevoz = value;
            OnPropertyChanged();
        }
    }

    public decimal Samodopr => Stavka.Samodopr;
    public decimal Sindikat1 => Stavka.Sindikat1;
    public decimal Sindikat2 => Stavka.Sindikat2;
    public decimal Solidarn => Stavka.Solidarn;
    public decimal Aliment => Stavka.Aliment;
    public decimal Kasa => Stavka.Kasa;
    public decimal Kasarata => Stavka.Kasarata;
    public decimal Ukobust => Stavka.Ukobust;
    public decimal Solpor => Stavka.Solpor;

    public decimal Komorajd => Stavka.Komorajd;
    public decimal Komorasd => Stavka.Komorasd;
    public decimal Bendin => Stavka.Bendin;
    public decimal Benproc => Stavka.Benproc;

    public KarticaRadnikaViewModel(
        LdObracunStavka stavka,
        DomainRadnik? radnik,
        LdParametar? param)
    {
        _radnik = radnik;
        _izvornaStavka = stavka;
        _stavka = CloneStavka(stavka);
        Param = param;
    }

    [RelayCommand]
    private void Potvrdi()
    {
        Sacuvaj(promeniINovobracunaj: false);
    }

    [RelayCommand]
    private void PotvrdiSaObracunom()
    {
        Sacuvaj(promeniINovobracunaj: true);
    }

    private void Sacuvaj(bool promeniINovobracunaj)
    {
        try
        {
            if (promeniINovobracunaj)
            {
                if (Param != null && _radnik != null)
                {
                    _obracunService.Obracunaj(Stavka, _radnik, Param, obracunatiNaknade: true);
                }
                else
                {
                    StatusPoruka = "Sačuvano bez obračuna (nedostaju podaci radnika ili parametri).";
                }
            }

            KopirajStavku(Stavka, _izvornaStavka);
            OnPropertyChanged(string.Empty);
            Potvrdjen = true;
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greska pri cuvanju: {ex.Message}";
        }
    }

    private static LdObracunStavka CloneStavka(LdObracunStavka izvor)
    {
        var kopija = new LdObracunStavka();
        KopirajStavku(izvor, kopija);
        return kopija;
    }

    private static void KopirajStavku(LdObracunStavka izvor, LdObracunStavka cilj)
    {
        foreach (var prop in typeof(LdObracunStavka).GetProperties())
        {
            if (!prop.CanRead || !prop.CanWrite) continue;
            prop.SetValue(cilj, prop.GetValue(izvor));
        }
    }
}
