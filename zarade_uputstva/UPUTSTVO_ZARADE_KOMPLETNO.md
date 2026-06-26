# UPUTSTVO ZA RAD SA ZARADAMA — KOMPLETNO
## Algoritam — Program za obračun zarada i ličnih primanja
### Verzija: jun 2026 | Sa konkretnim primerima podataka za unos

---

## SADRŽAJ

1. [Tok obračuna — pregled koraka](#1-tok-obracuna)
2. [Parametri 1 — osnovna podešavanja](#2-parametri-1)
3. [Evidencija zaposlenih](#3-evidencija-zaposlenih)
4. [Parametri 2 — mesečna podešavanja](#4-parametri-2)
5. [Unos radnika i unos časova](#5-unos-radnika-i-casova)
6. [Platni spisak — Kartica F7](#6-platni-spisak-kartica-f7)
7. [Obračun BRUTO](#7-obracun-bruto)
8. [Obračun NETO (budžetski)](#8-obracun-neto)
9. [Krediti](#9-krediti)
10. [Obračun naknada (12-mesečni prosek)](#10-obracun-naknada)
11. [Prevoz](#11-prevoz)
12. [Dotacija do minimalne zarade](#12-dotacija)
13. [Noćni rad i prekovremeni rad](#13-nocni-i-prekovremeni-rad)
14. [Bolovanje do 30 dana](#14-bolovanje-do-30-dana)
15. [Suspenzija](#15-suspenzija)
16. [Bonus — isplata](#16-bonus)
17. [Olakšice za novozaposlene](#17-olaksice)
18. [Više isplata u jednom mesecu](#18-vise-isplata)
19. [PPP-PD prijava](#19-ppp-pd-prijava)
20. [Arhiviranje i rasknjižavanje](#20-arhiviranje-i-rasknjiavanje)

---

## PRIMER FIRMA (koristimo kroz celo uputstvo)

```
Naziv:  DOO "MONTA" BEOGRAD
PIB:    101234567
MB:     12345678
Račun:  205-1234567890-12  (Komercijalna banka)
```

### Zaposleni u primerima

| Br. | Ime i prezime      | Koeficijent | Staž | Posebno                     |
|-----|--------------------|-------------|------|-----------------------------|
| 1   | Petrović Marko     | 1.30        | 16 g | Standardan obračun          |
| 2   | Nikolić Ana        | 1.00        | 8 g  | Bolovanje 5 dana u januaru  |
| 3   | Jovanović Dragan   | 1.10        | 22 g | Noćni rad 32h, prekovremeni 8h |
| 4   | Stojanović Milica  | 1.00        | 5 g  | Kredit 5.000/mes, alimentacija 15% |
| 5   | Ilić Jovan         | 0.90        | 3 g  | Suspenzija 2 dana           |

### Važeći podaci za januar 2026 (koristimo u svim primerima)

| Parametar                  | Vrednost      |
|----------------------------|---------------|
| Bruto cena rada             | 90.000,00 RSD |
| Neoporezivi iznos (UPO)    | 26.300,00 RSD |
| Najniža osnovica doprinosa | 28.400,00 RSD |
| Minimalna bruto zarada     | 47.000,00 RSD |
| Prosečna bruto plata       | 125.000,00 RSD |
| Radnih sati u januaru      | 176 h         |
| Dani januara               | 31             |
| Praznici u januaru         | 2 dana = 16 h |
| Radni sati bez praznika    | 160 h         |

### Stope doprinosa i poreza

| Vrsta                   | Radnik | Firma  |
|-------------------------|--------|--------|
| PIO                     | 14,00% | 12,00% |
| Zdravstvo               | 5,15%  | 5,15%  |
| Nezaposlenost           | 0,75%  | 0,75%  |
| **Ukupno doprinosi**    | **19,90%** | **17,90%** |
| Porez na zaradu         | 10,00% | —      |

---

## 1. TOK OBRAČUNA

### Redosled koraka — ne preskakati!

```
┌─────────────────────────────────────────────────────────────┐
│  1. RADNICI — Proveriti evidenciju, staž, novoporimljene    │
│  2. KREDITI — Proveriti rasknjižavanje prethodnog meseca    │
│  3. PARAMETRI 1 — Proveriti/ažurirati iznose i stope        │
│  4. PARAMETRI 2 — Uneti mesec, datum, časove praznika       │
│  5. UNOS RADNIKA — Kliknuti POTVRĐUJEM                      │
│  6. UNOS ČASOVA — Kliknuti POTVRĐUJEM                       │
│  7. PLATNI SPISAK — Uneti naknade, preneti kredite          │
│  8. OBRAČUN BRUTO/NETO — Uneti cenu rada, POTVRĐUJEM        │
│  9. SPISKOVI — Kliknuti PRENOS za isplatu                   │
│  10. PPP-PD — Formirati i predati XML prijavu               │
│  11. KREDITI — Rasknjižiti nakon isplate                    │
└─────────────────────────────────────────────────────────────┘
```

**VAŽNO — Zlatna pravila:**

- Ako radiš **jednu (konačnu) isplatu** — obračun možeš ponavljati bezbroj puta
- Ako radiš **više isplata** (akontacija + konačna) — NIKADA ne ponavljaj obračun za istu isplatu; pre svake nove isplate obavezno uradi ARHIVIRANJE
- Pre svakog obračuna proveri: `PARAMETRI 2 → KONAČNA ISPLATA D/N` — pogrešan podatak ruši ceo obračun

---

## 2. PARAMETRI 1

**Putanja:** Glavni meni → PARAMETRI 1

Ove podatke menjaš retko — samo kada se promeni zakon ili minimalna zarada.

### 2.1 Procenti po zakonu

| Polje                    | Vrednost  | Napomena                                          |
|--------------------------|-----------|---------------------------------------------------|
| % ZA NOĆNI RAD           | `26`      | Uvećanje od 26% — samo na procenat, ne na sate    |
| % ZA PRODUŽEN RAD        | `26`      | Uvećanje od 26% — sati moraju biti u učinku       |
| % ZA PRAZNIK             | `110`     | 110% cene rada po satu                            |
| % ZA RAD NEDELJOM        | `110`     | 110% — sati moraju biti u učinku                  |
| % ZA MINULI RAD          | `0.4`     | 0,4% po godini staža                              |
| % ZA BOLOVANJE           | `65`      | Bolovanje do 30 dana na teret firme               |
| % ZA PLAĆENO ODSUSTVO    | `45`      | Program koristi 80%                               |
| % ZA SUSPENZIJU          | (po firmi) | Npr. `50` — prema odluci firme                   |
| % ZA BENEFICIJU 12 mes.  | `3.4`     |                                                   |
| % ZA BENEFICIJU 13 mes.  | `5.4`     |                                                   |
| % ZA BENEFICIJU 14 mes.  | `6.9`     |                                                   |
| % ZA BENEFICIJU 15 mes.  | `10.3`    |                                                   |

### 2.2 Ostala polja

| Polje                  | Primer vrednosti    | Objašnjenje                                                                   |
|------------------------|---------------------|-------------------------------------------------------------------------------|
| KOEFICIJENT STARTA     | (prazno ili `1`)    | Koristi se samo ako firma ima koef. starta × koef. RM u Pravilniku o radu    |
| MINULI RAD             | `0`                 | 0=NA START (preporuka za bruto), 1=NA SVA PRIMANJA, 2=NA PRIMANJA, 3=NETO-BUDŽ |
| MINIMALNA ZARADA       | `47000`             | Bruto za bruto obračun, neto za neto obračun. Menja se sa zakonom!            |
| PROSEČNA BRUTO PLATA   | `125000`            | **OBAVEZNO!** Za ograničenje najviše osnovice (5×ovaj iznos). Ažurirati mesečno |
| ZARADA PO SATU         | (samo neto obračun) | Ostaviti prazno za bruto obračun                                              |

#### Stope doprinosa (levi deo tabele)

Ove kolone menjaj samo kada se izmeni zakon:

```
                    NA TERET RADNIKA    NA TERET FIRME
PIO:                    14.00%              12.00%
Zdravstvo:               5.15%               5.15%
Nezaposlenost:           0.75%               0.75%
```

### 2.3 Opcija MINULI RAD — detaljno

| Oznaka | Naziv              | Kada koristiti                                      |
|--------|--------------------|-----------------------------------------------------|
| `0`    | NA START           | Bruto obračun — minuli na osnovnu zaradu po učinku  |
| `1`    | NA SVA PRIMANJA    | Bruto i neto — minuli na sva primanja               |
| `2`    | NA PRIMANJA        | Bruto i neto — minuli na osnovnu zaradu             |
| `3`    | NETO OBRAČUN BUDŽ  | Isključivo neto obračun budžetskih                  |

> **Preporuka za firme:** Bruto obračun → koristiti **oznaku 0** (NA START). Neto obračun → **oznaku 2** (NA PRIMANJA).

---

## 3. EVIDENCIJA ZAPOSLENIH

**Putanja:** Glavni meni → RADNICI

Klikni **OTVORI TABELU** za direktan unos u kolone ili **DODAJ +** za unos po kartici.

### 3.1 Obavezna polja — uvek popuniti

| Polje        | Primer — Petrović Marko    | Napomena                                    |
|--------------|----------------------------|---------------------------------------------|
| BROJ         | `1`                        | Redni broj (auto)                           |
| IME I PREZIME | `PETROVIC MARKO`          | Bez dijakritičkih znakova za PPP-PD!        |
| PREZIME      | `PETROVIC`                 | Bez č,ć,š,ž,đ                              |
| IME          | `MARKO`                    | Bez č,ć,š,ž,đ                              |
| MATICNIBR    | `0101985710023`            | JMBG — 13 cifara                            |
| VRSTAID      | `1`                        | 1=JMBG, 2=izbegl., 3=pasoš, 9=ostalo       |
| OZNVRPRIH    | `101`                      | Šifra vrste prihoda iz kataloga PU          |
| STARTBOD     | `1.30`                     | Koeficijent radnog mesta                    |
| STAZ         | `16`                       | Godine staža. Automatski: SREDI RADNI STAŽ  |
| DATMIN       | `15.03.2009`               | Datum zaposlenja iz ugovora o radu          |
| VRSTA        | (prazno)                   | Prazno za redovnu zaradu                    |

### 3.2 Polja za PPP-PD (obavezna za poresku prijavu)

| Polje    | Primer           | Šta znači                                    |
|----------|------------------|----------------------------------------------|
| PREBIVAL | `7020`           | Šifra opštine stanovanja (iz šifrarnika PU)  |
| KATALOG  | `1`              | Uvek 1 kod svih vrsta prihoda                |
| VRSTAPRIM | `101`           | Šifra prihoda — redovna zarada               |
| OZNOLAKS | (prazno)         | Prazno ako nema olakšica                     |
| OZNBEN   | (prazno)         | Prazno ako nema beneficiranog staža          |
| STEPEN   | `7`              | Stepen SS (4=SSS, 6=VŠS, 7=VSS, itd.)       |

### 3.3 Polja za uplatu na račun radnika

| Polje   | Primer              | Napomena                                            |
|---------|---------------------|-----------------------------------------------------|
| SIFRA   | `101`               | Šifra iz šifrarnika partnera (banka/firma radnika)  |
| PARTIJA | `205-9876543210-88` | Broj tekućeg računa radnika                         |

### 3.4 Polja za posebne obustave

| Polje     | Primer       | Objašnjenje                                      |
|-----------|--------------|--------------------------------------------------|
| ALIMPROC  | `15`         | Procenat alimentacije (Stojanović Milica: `15`)  |
| KASA      | `300`        | Mesečna članarina kase (fiksno)                  |
| KASARATA  | `2000`       | Rata za kasu                                     |
| SIND1PROC | `1`          | Procenat sindikata 1                             |
| SIND2PROC | (prazno)     | Procenat sindikata 2 (ako je u dva)              |
| SOLPROC   | `0.5`        | Procenat solidarnosti                            |
| SAMSIF    | `7020`       | Šifra opštine za samodoprinos                    |
| SAMOPROC  | `3`          | Procenat samodoprinosa                           |
| PREVOZ    | `*`          | Zvezdica = radnik prima prevoz                   |

### 3.5 Polje VRSTA — kada se puni

| Vrednost | Za koga                                         |
|----------|-------------------------------------------------|
| (prazno) | Redovna zarada — svi standardni radnici         |
| `B`      | Bolovanje PREKO 30 dana                         |
| `P`      | Porodilje                                       |
| `R`      | Penzioneri                                      |
| `I`      | Invalidi                                        |
| `U`      | Ugovor o delu                                   |

### 3.6 Polja za obračun sa olakšicama

| Polje     | Vrednost | Opis                                               |
|-----------|----------|----------------------------------------------------|
| PROCUMANJ | `65`     | Procenat umanjenja poreza (za olakšicu čl. 21v)    |
| UMANJENJE | `08`     | Oznaka tipa olakšice (08=novozaposleni čl. 21v)    |
| DOPUMANJ  | `65`     | Procenat umanjenja doprinosa na teret firme        |

### 3.7 Primer — unos sva 4 radnika

#### Petrović Marko (standardan)
```
BROJ:        1         IME:    MARKO      PREZIME:   PETROVIC
MATICNIBR:   0101985710023               VRSTAID:   1
PREBIVAL:    7011      (Beograd - Stari grad)
OZNVRPRIH:   101       KATALOG: 1
STARTBOD:    1.30      STAZ:    16        STEPEN:    7
DATMIN:      15.03.2009
VRSTA:       (prazno)
PARTIJA:     205-1111111111-11
```

#### Nikolić Ana (bolovanje u januaru)
```
BROJ:        2         IME:    ANA        PREZIME:   NIKOLIC
MATICNIBR:   1505990785031               VRSTAID:   1
PREBIVAL:    7019      (Beograd - Savski venac)
OZNVRPRIH:   101       KATALOG: 1
STARTBOD:    1.00      STAZ:    8         STEPEN:    6
DATMIN:      10.02.2018
VRSTA:       (prazno)
PARTIJA:     205-2222222222-22
```

#### Stojanović Milica (kredit + alimentacija)
```
BROJ:        4         IME:    MILICA     PREZIME:   STOJANOVIC
MATICNIBR:   2203991745051               VRSTAID:   1
PREBIVAL:    7000      (Beograd)
OZNVRPRIH:   101       KATALOG: 1
STARTBOD:    1.00      STAZ:    5         STEPEN:    4
DATMIN:      01.06.2021
VRSTA:       (prazno)
ALIMPROC:    15
PARTIJA:     205-4444444444-44
```

### 3.8 Dugme SREDI RADNI STAŽ

Koristi se za automatsko računanje staža od datuma zaposlenja do danas.

**Kada koristiti:** Svaki mesec (ili bar jednom godišnje) da bi program imao tačan broj godina za minuli rad.

**Uslov:** Polje `DATMIN` (datum zaposlenja) mora biti popunjeno.

**Postupak:**
1. Kliknuti dugme **SREDI RADNI STAŽ**
2. Program pita: `Da li hoćeš da sređuješ radni staž?` → **DA**
3. Staž se automatski upisuje u kolonu **STAZ** za sve zaposlene

> Ako postoji prethodni staž u istoj firmi sa prekidom, ručno popuni polja **DAN**, **MESEC**, **GODINA** (broj dana/meseci/godina prethodnog staža) pre klika na dugme.

---

## 4. PARAMETRI 2

**Putanja:** Glavni meni → PARAMETRI 2

Ovo menjasz svaki mesec! Najvažniji korak pre unosa radnika.

### 4.1 Osnovna polja — januar 2026

| Polje                   | Vrednost za januar 2026 | Napomena                                        |
|-------------------------|-------------------------|--------------------------------------------------|
| VRSTA ISPLATE           | `1`                     | 1=Redovna, 2=Porodilje, 3=Bolovanje, 4=Invalidi, 5=Penzioneri, 6=Ugovor o delu |
| REDNI BROJ MESECA       | `1`                     | Januar=1, Februar=2, ... Decembar=12            |
| NAZIV MESECA            | auto                    | Popunjava se automatski                          |
| DATUM PRVE ISPLATE      | `31.01.2026`            | Datum isplate (ili datum knjiženja)              |
| DATUM DRUGE ISPLATE     | (prazno)                | Samo ako ima 2. isplate                         |
| GODINA                  | `2026`                  | Menja se 1× godišnje                            |
| KALENDARSKI BROJ DANA   | `31`                    | Auto — popunjava se uz mesec                    |
| ČASOVI PRAZNIKA         | `16`                    | Januar ima 2 praznika = 16h. Ručno uneti!       |
| ZAKONSKI BROJ SATI      | `176`                   | Auto — zakonski fond                            |
| RADNI SATI U MESECU     | `160`                   | = 176 − 16 (korigovati za praznike ručno!)      |
| KONAČNA ISPLATA D/N     | `D`                     | D=konačna, N=nije konačna                       |

### 4.2 Napredna polja

| Polje                          | Vrednost         | Napomena                                         |
|--------------------------------|------------------|--------------------------------------------------|
| OZNAKA 1                       | `1`              | Uvek ostaje 1                                    |
| NAJNIŽA OSNOVICA               | `28400`          | Važeći propisani iznos                           |
| POČETNI DATUM                  | `01.01.2026`     | Uvek 1. januar tekuće godine                    |
| ZADNJI DATUM                   | `31.12.2026`     | Uvek 31. decembar tekuće godine                 |
| AKTIVNI TEKUĆI RAČUN           | `1`              | Šifra računa iz Podataka o firmi (ako ima više)  |
| IZNOS ZARADE BEZ DOPRINOSA     | (prazno)         | Trenutno ne postoji propisan iznos               |
| IZNOS ZARADE BEZ POREZA        | `26300`          | Neoporezivi iznos — **OBAVEZNO uneti!**          |
| OSLOBAĐANJE SRAZMERNO ČASOVIMA | `D`              | D = proporcionalno radnim satima                 |
| DECIMALE *=D                   | `*`              | Preporuka PU — uvek unositi *                    |
| DATUM POČETKA DELATNOSTI       | `15.07.2003`     | Datum upisa u APR — jednom uneti, ne menjati    |

### 4.3 Nazivi primanja i obustava (opciono)

Popunjavaš kada imaš jednokratna ili periodična primanja/obustave:

```
PRIMANJA 1:        Topli obrok            (ako ne koristiš auto prenos)
PRIMANJA 2:        Jubilarna nagrada
PRIMANJA 3:        (prazno)
PRIMANJA 4:        (prazno)
TERENSKI:          Terenski dodatak       (naziv ostaje)

OSTALE OBUSTAVE 1: Sudska taksa
OSTALE OBUSTAVE 2: (prazno)
```

### 4.4 REDNI BROJ MESECA — posebni slučajevi

Ako radiš 4 odvojena obračuna iste vrste za isti mesec (npr. 4 grupe radnika), koristi:

| Mesec    | Grupa 1 | Grupa 2 | Grupa 3 | Grupa 4 |
|----------|---------|---------|---------|---------|
| Januar   | 1       | 13      | 25      | 37      |
| Februar  | 2       | 14      | 26      | 38      |
| Mart     | 3       | 15      | 27      | 39      |
| April    | 4       | 16      | 28      | 40      |
| Maj      | 5       | 17      | 29      | 41      |
| Jun      | 6       | 18      | 30      | 42      |
| Jul      | 7       | 19      | 31      | 43      |
| Avgust   | 8       | 20      | 32      | 44      |
| Septembar| 9       | 21      | 33      | 45      |
| Oktobar  | 10      | 22      | 34      | 46      |
| Novembar | 11      | 23      | 35      | 47      |
| Decembar | 12      | 24      | 36      | 48      |

**Primer upotrebe:** Radnici sa olakšicama moraju ići u posebnu PPP-PD prijavu. Ako za januar radiš i redovne i radnike sa olakšicom:
- Redovni: mesec = `1`
- Sa olakšicom: mesec = `13`

---

## 5. UNOS RADNIKA I ČASOVA

### 5.1 Unos radnika u platni spisak

**Putanja:** Glavni meni → UNOS RADNIKA

Otvara se prozor **UNOS RADNIKA U PLATNI SPISAK**.

| Polje   | Vrednost | Šta znači                                                                      |
|---------|----------|--------------------------------------------------------------------------------|
| GRUPA 1 | `0`      | 0 = unosi SVE radnike iste VRSTE (prazno polje VRSTA u evidenciji za redovne) |
| GRUPA 1 | `1`      | Unosi samo radnike kojima si u evidenciji upisao GRUPA1 = 1                   |

**Klikni POTVRĐUJEM → IZLAZ**

> Radnici su sada u platnom spisku za januar.

### 5.2 Unos časova

**Putanja:** Glavni meni → UNOS ČASOVA

Klikni **POTVRĐUJEM → IZLAZ**

Ovim si preneo u platni spisak:
- Časove PO VREMENU iz Parametri 2 (= RADNI SATI = 160h za januar)
- Časove PO UČINKU = isto 160h
- Časove PRAZNIKA = 16h (što si uneo u Parametri 2)

---

## 6. PLATNI SPISAK — KARTICA F7

**Putanja:** Glavni meni → PLATNI SPISAK

Otvara se tabela sa svim radnicima. Klikni na radnika, zatim **KARTICA F7** da otvoriš karticu.

### 6.1 Struktura Kartice F7

Kartica ima 3 kolone: **VRSTA PRIMANJA** | **ČASOVI** | **DINARI**

### 6.2 Primer — Petrović Marko (standardan, bez posebnosti)

```
Cena rada: 90.000 RSD | Koef: 1.30 | Staž: 16 god
Mesec: januar 2026 | Radni sati: 160h
```

| Vrsta primanja      | Časovi  | Dinari        | Napomena                              |
|---------------------|---------|---------------|---------------------------------------|
| PO VREMENU          | 160,00  | —             | Auto iz Unos časova                   |
| PO UČINKU           | 160,00  | —             | Auto iz Unos časova                   |
| PRAZNIK             | 16,00   | —             | Auto iz Parametri 2                   |
| MINULI RAD          | —       | auto          | 16 god × 0,4% = 6,4% na bruto        |
| NOĆNI RAD           | 0,00    | —             | Nije radio noćno                      |
| PRODUŽEN RAD        | 0,00    | —             | Nije radio prekovremeno               |
| TOPLI OBROK         | —       | `8.000,00`    | Uneti ručno ako nema auto prenosa     |
| REGRES              | —       | (prazno)      | Ako se ne isplaćuje u januaru         |

**UKUPNO ČASOVA:** 160 + 16 = 176h  
**Posle obračuna — rezultati (auto):**

| Polje                   | Iznos          | Formula                               |
|-------------------------|----------------|---------------------------------------|
| BRUTO ZARADA            | 121.368,00     | 90.000×1.30 + minuli 6,4% + praznik  |
| POREZ                   | 9.506,80       | (121.368 − 26.300) × 10%             |
| PIO radnik (14%)        | 17.011,52      | 121.368 × 14%                         |
| ZDRAVSTVO radnik (5.15%)| 6.250,45       | 121.368 × 5,15%                       |
| NEZAP. radnik (0.75%)   | 910,26         | 121.368 × 0,75%                       |
| **NETO ZARADA**         | **87.689,97**  | 121.368 − 9.506,80 − 24.172,23       |
| PIO firma (12%)         | 14.564,16      | 121.368 × 12%                         |
| ZDRAVSTVO firma (5.15%) | 6.250,45       | 121.368 × 5,15%                       |
| NEZAP. firma (0.75%)    | 910,26         | 121.368 × 0,75%                       |
| **UKUPAN TROŠAK**       | **142.092,87** | Bruto + svi firmski doprinosi         |

> **Napomena o prazniku:** Cena rada × koeficijent × 160h / 160h = osnovna zarada. Praznik: 90.000 × 1.30 / 160 × 16 × 2.10 = uvećanje za 110%. Minuli: 6,4% na bruto.

### 6.3 Primer — Nikolić Ana (bolovanje 5 dana = 40h)

```
Cena rada: 90.000 RSD | Koef: 1.00 | Staž: 8 god (minuli 3.2%)
Bolovanje: 5 radnih dana = 40h (od 20.01.2026. do 24.01.2026.)
Radila: 160 − 40 = 120h (učinak), 16h praznik ostaje
```

| Vrsta primanja      | Časovi   | Dinari  | Napomena                                     |
|---------------------|----------|---------|----------------------------------------------|
| PO VREMENU          | 120,00   | —       | Korigovati: 160 − 40 = 120                   |
| PO UČINKU           | 120,00   | —       | Korigovati: 160 − 40 = 120                   |
| PRAZNIK             | 16,00    | —       | Ostaje nepromenjen                           |
| BOLOVANJE (65%)     | 40,00    | —       | Uneti 40h. Smanjuje učinak.                  |
| MINULI RAD          | —        | auto    | 8 god × 0,4% = 3,2% na bruto                |
| TOPLI OBROK         | —        | 8.000   |                                              |

**VAŽNO:** Kada unosiš 40h bolovanja — učinak moraš smanjiti sa 160 na 120. Bolovanje ulazi u ukupan broj sati.

### 6.4 Primer — Jovanović Dragan (noćni rad 32h + prekovremeni 8h)

```
Cena rada: 90.000 RSD | Koef: 1.10 | Staž: 22 god (minuli 8.8%)
Noćni rad: 32h | Prekovremeni: 8h
```

| Vrsta primanja      | Časovi  | Dinari  | Napomena                                                       |
|---------------------|---------|---------|----------------------------------------------------------------|
| PO VREMENU          | 160,00  | —       | Ostaje 160h — noćni su već u njemu                            |
| PO UČINKU           | 168,00  | —       | 160 + 8 prekovremenih (prekovremeni MORAJU biti u učinku)     |
| PRAZNIK             | 16,00   | —       | Ostaje                                                         |
| NOĆNI RAD           | 32,00   | —       | Samo uvećanje 26% se obračunava. Ne ulazi u ukupno.           |
| PRODUŽEN RAD        | 8,00    | —       | Samo uvećanje 26%. MORA biti u učinku. Ne ulazi u ukupno.     |
| MINULI RAD          | —       | auto    | 22 god × 0,4% = 8,8%                                          |

**Razlika:** Noćni i prekovremeni se ne dodaju na ukupno sate — program samo obračunava *procenat uvećanja*. Zato učinak mora sadržati te sate.

### 6.5 Primer — Stojanović Milica (kredit + alimentacija)

```
Cena rada: 90.000 RSD | Koef: 1.00 | Staž: 5 god (minuli 2.0%)
Kredit: 5.000 RSD/mes | Alimentacija: 15% od neto
```

Kartica F7 — primanja unosi se normalno (160h učinak, 16h praznik).

Obustave (desna kolona kartice) — unose se auto:

| Vrsta obustave     | Iznos      | Kako se popunjava                                  |
|--------------------|------------|----------------------------------------------------|
| KREDITI            | `5.000,00` | Auto — kroz PRENOSI → PRENOS KREDITA               |
| ALIMENTACIJA       | auto       | Auto — program uzima 15% od neto (iz evidencije)   |

### 6.6 Primer — Ilić Jovan (suspenzija 2 dana = 16h)

```
Cena rada: 90.000 RSD | Koef: 0.90 | Staž: 3 god (minuli 1.2%)
Suspenzija: 2 radna dana = 16h | Procenat suspenzije: 50% (u Parametri 1)
```

| Vrsta primanja  | Časovi   | Dinari | Napomena                                     |
|-----------------|----------|--------|----------------------------------------------|
| PO UČINKU       | 144,00   | —      | 160 − 16 = 144 (smanjiti za suspension sate) |
| PRAZNIK         | 16,00    | —      |                                              |
| SUSPENZIJA      | `-16,00` | —      | **Uneti sa negativnim predznakom!**          |

> Program obračunava procenat suspenzije (50%) na vrednost tih sati i oduzima od zarade.

### 6.7 Polje FIKSNA PLATA — kada koristiti

Koristi se za zaradu koja se ne računa kroz cenu rada × koeficijent:
- Direktori sa ugovorenom fiksnom zaradom
- Honorari koji se oporezuju kao zarada

```
Primer: Direktor sa fiksnom bruto zaradom 200.000 RSD
→ PO UČINKU: 0 (ne popunjavati)
→ FIKSNA PLATA: 200.000,00 (bruto za bruto obračun)
→ CENA RADA pri obračunu: 0 (nula!)
```

### 6.8 Topli obrok i regres

Unosimo ručno ili kroz prenos:

```
TOPLI OBROK:  8.000,00 RSD mesečno (bruto iznos)
REGRES:       10.000,00 RSD (najčešće jednom godišnje — jul/avgust)
```

**Auto prenos:** PLATNI SPISAK → PRENOSI → TOPLI OBROK I REGRES → unesi iznose → OBRAČUN

### 6.9 Stimulacije

U polja STIMULAC 1, 2, 3 unosiš **procenat** (ne iznos!) stimulacije:

```
Radnik X ima stimulaciju 10% od bruto zarade.
→ STIMULAC 1: 10
→ Program automatski obračunava 10% × bruto zarada
```

---

## 7. OBRAČUN BRUTO

**Putanja:** PLATNI SPISAK → klik OBRAČUN BRUTO-F10

Otvara se prozor **OBRAČUN PLATA**.

### 7.1 Polja za unos

| Polje                          | Primer vrednosti | Napomena                                                      |
|--------------------------------|------------------|---------------------------------------------------------------|
| CENA RADA                      | `90000`          | Bruto cena rada iz ugovora. 0 ako koristiš fiksnu platu.     |
| GRUPA                          | (prazno)         | Popuniti samo ako imaš više grupa sa različitim cenama rada  |
| OBRAČUNATI NAKNADE             | `N`              | N = zadržati već obračunate naknade; D = obračunaj po ceni rada |
| OBRAČUNATI OD UKUPNE OBAVEZE   | `N`              | Uvek N (osim posebnog slučaja sa koeficijentom ukupnog troška) |
| IZNOS ZARADE BEZ DOPRINOSA     | (auto iz P2)     | Trenutno 0 — ne postoji propisani iznos                      |
| IZNOS ZARADE BEZ POREZA        | (auto iz P2)     | `26.300` — automatski iz Parametri 2                         |

### 7.2 Kada uneti OBRAČUNATI NAKNADE = D vs N

| Situacija                               | Vrednost |
|-----------------------------------------|----------|
| Nema naknada (bolovanje, godišnji...)   | `N`      |
| Koristio si poseban OBRAČUN NAKNADA     | `N`      |
| Imaš naknade ali NISI radio obračun naknada | `D`  |

### 7.3 Opcija OBRAČUNATI OD UKUPNE OBAVEZE

Poseban slučaj: U evidenciji radnika u polje KOEFICIJENT uneseš **ukupan trošak** firme za zaradu (bruto + svi doprinosi firme).

```
Primer: Firma želi da isplati zaposlenom tako da ukupan trošak bude 150.000 RSD.
→ U evidenciji: STARTBOD = 150000 (koeficijent = ukupan trošak)
→ CENA RADA = 1
→ OBRAČUNATI OD UKUPNE OBAVEZE = D
```

**Ograničenje:** Ne koristi se ako je zarada manja od najniže ili veća od najviše osnovice!

### 7.4 Klikni POTVRĐUJEM

Obračun je završen. Proveravaj kartice F7 za svaki radnik.

---

## 8. OBRAČUN NETO (samo budžetski korisnici)

**Putanja:** PLATNI SPISAK → OBRAČUN NETO

| Polje              | Primer   | Napomena                                          |
|--------------------|----------|---------------------------------------------------|
| CENA RADA (neto)   | `55000`  | Neto iznos iz ugovora                            |
| OBRAČUNATI NAKNADE | `N`/`D`  | Isto kao kod bruta                               |
| PO SATU            | (auto)   | Iz Parametri 1 — za obračun po satu             |

Dugme za pokretanje: **DODAJ MINULI RAD** (a ne POTVRĐUJEM kao kod bruta).

Dugme **ODUZMI MINULI RAD** — koristi se samo ako je neto cena rada ugovorena sa uračunatim minulim radom.

---

## 9. KREDITI

### 9.1 Unos novog kredita

**Putanja:** Glavni meni → KREDITI → DODAJ +

```
Primer: Stojanović Milica uzima kredit od banke (Komercijalna)
Iznos: 500.000 RSD | Rate: 100 | Rata: 5.000/mes
```

| Polje           | Vrednost                 | Kako uneti                                       |
|-----------------|--------------------------|--------------------------------------------------|
| BROJ            | `4`                      | Klikni u polje → RADNICI F7 → pronađi Miličin   |
| ŠIFRA           | `5`                      | Klikni → PARTNERI F8 → izaberi Komercijalnu banku |
| PARTIJA         | `205-8888888888-88`      | Broj žiro računa banke za uplatu rate            |
| IZNOS           | `500000`                 | Ukupan iznos kredita                             |
| KOLIKO          | `100`                    | Broj rata                                        |
| PRVARATA        | `5000`                   | Prva rata                                        |
| OSTALERATE      | `5000`                   | Ostale rate                                      |
| ZAODBITAK       | `*`                      | Zvezdica = aktivan kredit za odbitak             |
| AKTIVRATA       | `5000`                   | Iznos koji ide u konačni obračun                 |
| AKONTRATA       | (prazno)                 | Popuniti samo ako ima akontacije                 |
| DATDOK          | `15.01.2026`             | Datum ugovora o kreditu                          |

**Nakon unosa:** Klikni **SREĐIVANJE KREDITA** → **U REDU** da otvoriš praćenje otplata.

### 9.2 Prenos kredita u platni spisak

**Putanja:** PLATNI SPISAK → PRENOSI

| Situacija                    | Opcija                   |
|------------------------------|--------------------------|
| Jedna isplata (konačna)      | PRENOS KREDITA → **ŽELIM PRENOS KREDITA** |
| Više isplata — 1. isplata    | PRENOS KREDITA → **ŽELIM PRENOS AKONTACIJE** |
| Više isplata — konačna       | PRENOS KREDITA → **ŽELIM PRENOS KREDITA** |

### 9.3 Rasknjižavanje (evidencija otplate)

**Kada:** Nakon što je zarada isplaćena (ne pre!).

**Putanja:** Glavni meni → KREDITI

```
1. Klikni RASKNJIŽAVANJE PLATE (ili AKONTACIJE)
2. Unesi datum isplate (isti kao u Parametri 2)
3. Klikni U REDU
4. Klikni SREĐIVANJE KREDITA
```

Program automatski:
- Upisuje iznos odbijene rate u kolonu ODBIJENO
- Smanjuje OSTATAK
- Briše zvezdicu iz ZAODBITAK ako je kredit otplaćen
- Upisuje `*` u ARHIVA za završene kredite

### 9.4 Pregled stanja kredita

| Dugme          | Šta prikazuje                                     |
|----------------|---------------------------------------------------|
| SALDO RADNIKA  | Preostalo dugovanje po radniku                    |
| SALDO FIRMI    | Aktivne rate za sve banke/firme                   |
| PREGLED OTPLATA | Istorija otplata (koristiti za kontrolu)         |
| LISTIĆ RADNIK  | Pregled za jednog radnika — štampati pre obračuna |

---

## 10. OBRAČUN NAKNADA (12-mesečni prosek)

Koristi se kada zaposlenima treba obračunati bolovanje, porodiljsko i sl. naknade na osnovu proseka prethodnih 12 meseci.

### 10.1 Koraci

**Korak 1 — Formirati arhivu 12 meseci**

Putanja: Glavni meni → ODRŽAVANJE → ARHIVIRANJE

Formiraj arhivu 12 meseci koji prethode mesecu za koji radiš obračun:
```
Primer: Obračun za januar 2026 → arhivirati: januar–decembar 2025
```

**Korak 2 — Proveriti SALDO ZA NAKNADE**

Putanja: PLATNI SPISAK → SALDO ZA NAKNADE

Prikazuje cenu rada po satu za svakog radnika. Proveri da li su svi iznosi veći od minimalne zarade.

**Korak 3 — Uneti radnike i časove naknada**

U Parametri 2 podesi vrstu obračuna i mesec. Unesi radnike. Unesi časove naknada u Kartici F7:
```
Primer: Ana Nikolić — bolovanje 40h = uneti u polje BOLOVANJE: 40
         Učinak smanjiti: 160 → 120
```

**Korak 4 — Pokrenuti OBRAČUN NAKNADA**

Putanja: PLATNI SPISAK → OBRAČUN NAKNADA

```
1. Klikni PRENOS IZ ARHIVE
2. Provjeri iznose — svi moraju biti ≥ minimalna zarada po satu
3. Klikni OBRAČUN NAKNADA
```

Program prenosi obračunate naknade u platni spisak.

**Korak 5 — Pokrenuti OBRAČUN BRUTO**

Unesi cenu rada i u polje OBRAČUNATI NAKNADE unesi **N** (da zadržiš naknade iz obračuna naknada!).

### 10.2 Kada koristiti obračun naknada vs. direktan unos

| Situacija                              | Pristup                                       |
|----------------------------------------|-----------------------------------------------|
| Bolovanje do 30 dana, mala firma       | Direktan unos časova u Karticu F7             |
| Bolovanje do 30 dana, promenljive cene | Obračun naknada (12-mes. prosek)              |
| Bolovanje PREKO 30 dana                | **OBAVEZNO** obračun naknada + posebni obrazci |
| Godišnji odmor                         | Direktan unos časova u Karticu F7             |

---

## 11. PREVOZ

### 11.1 Priprema — evidencija zaposlenih

U Kartici F7 svakog radnika koji prima prevoz, uneti zvezdicu u polje **PREVOZ**.

Alternativno: Klikni **OTVORI TABELU** i u kolonu PREVOZ unesi `*` kod svih.

### 11.2 Unos podataka za prevoz

**Putanja:** Glavni meni → PREVOZ (4. kolona, 4. dugme)

```
1. Klikni PREUZMI IZ SPISKA — preuzima radnike sa * u prevoz
2. Klikni POPUNJAVANJE — otvara se pomoćna tabela
```

| Polje           | Primer vrednosti  | Napomena                                           |
|-----------------|-------------------|----------------------------------------------------|
| DANA U MESECU   | `20`              | Broj dana za koje plaćaš prevoz (bez slobodnih)    |
| DNEVNA KARTA    | `250`             | Cena karte za 1 dan (povratna)                    |
| NEOPOREZIVO     | `5.000`           | Važeći neoporezivi mesečni iznos za prevoz         |
| DATUM           | `31.01.2026`      |                                                    |
| MESEC           | `1`               | Mora biti isti kao u Parametri 2                  |
| NAZIV           | `JANUAR`          |                                                    |

Klikni **UNOS** → potvrdi svakog radnika → Klikni u kolonu **ISPLATA** → unesi redni broj isplate.

**Ako ima različitih cena karata:** Ručno izmeni DNEVNA KARTA za pojedinog radnika direktno u tabeli.

**Ako radnik nije bio ceo mesec:** Srazmerno smanji neoporezivi iznos:
```
Radnik bio 15 od 20 dana → neoporezivo = 5.000 × 15/20 = 3.750 RSD
```

### 11.3 Čuvanje i prenos

```
1. Klikni SAČUVAJ (obavezno!)
2. Klikni IZLAZ
3. Klikni SPISAK → BRIŠI (ako ima starih podataka) → PRENOS PREVOZA
```

### 11.4 PPP-PD za prevoz

Posebna PPP-PD prijava — odvojena od zarade.

Parametri PPP-PD: Redni broj isplate i mesec moraju biti isti kao u obračunu prevoza.

---

## 12. DOTACIJA DO MINIMALNE ZARADE

Kada jedan ili više zaposlenih ima zaradu nižu od minimalne, dodaje se dotacija.

### 12.1 Kada se koristi

- Radnici sa manjim koeficijentom i malo sati
- Radnici koji su bili na neplaćenom ili suspenziji
- Radnici sa delimičnim radnim vremenom

### 12.2 Postupak

**Uslov:** Pre dotacije obavezno:
- U Parametri 1: popuniti **MINIMALNA ZARADA** (bruto ili neto, prema tipu obračuna)
- U Parametri 1: **MINULI RAD = 3** (za neto) ili odgovarajuće za bruto

```
Korak 1: Uradi obračun (bruto ili neto)
Korak 2: Utvrdi koji radnici imaju zaradu < minimalna
Korak 3: PLATNI SPISAK → PRENOSI → DOTACIJA → SVI RADNICI DO NETA
Korak 4: Program prikazuje: "Završeno je računanje dotacije."
Korak 5: Ponovi obračun da se preračuna minuli rad na dotaciju
```

### 12.3 Rezultat u Kartici F7

| Polje                         | Primer iznosa | Šta znači                                   |
|-------------------------------|---------------|---------------------------------------------|
| DOTACIJA NA OSNOVNU ZARADU    | `3.200,00`    | Dopuna za časove po učinku                  |
| RAZLIKA DOTACIJE NA DODATKE   | `450,00`      | Dopuna za časove dodataka (topli obrok...)  |
| RAZLIKA DOTACIJE NA NAKNADE   | `680,00`      | Dopuna za časove naknada (bolovanje...)     |

**VAŽNO:** Ako menjate časove ili parametre nakon dodate dotacije → najpre uradi **BRISANJE DOTACIJE**, pa ponovi sve korake.

---

## 13. NOĆNI RAD I PREKOVREMENI RAD

### 13.1 Noćni rad (26% uvećanje)

```
Situacija: Dragan je radio 32h noćno od ukupnih 160h rada.
```

| Polje            | Unos   | Objašnjenje                                                      |
|------------------|--------|------------------------------------------------------------------|
| PO UČINKU        | 160,00 | Pun fond — noćni su već u njemu                                 |
| NOĆNI RAD        | 32,00  | Program obračunava samo 26% uvećanje na ovih 32h                |

**Formula uvećanja noćnog rada:**
```
Cena po satu = 90.000 × 1.10 / 160 = 618,75 RSD/h
Uvećanje = 618,75 × 26% = 160,88 RSD/h
Noćni dodatak = 160,88 × 32h = 5.148,00 RSD
```

> Noćni sati **NE ulaze** u ukupno sate (već su u učinku). Program samo obračunava procenat.

### 13.2 Prekovremeni rad (26% uvećanje)

```
Dragan je radio 8h prekovremeno = 168h ukupno.
```

| Polje          | Unos   | Objašnjenje                                                        |
|----------------|--------|---------------------------------------------------------------------|
| PO UČINKU      | 168,00 | 160 + 8 prekovremenih — MORAJU biti u učinku!                      |
| PRODUŽEN RAD   | 8,00   | Program obračunava samo 26% uvećanje na ovih 8h                    |

> Prekovremeni sati **NE ulaze** u ukupno sate — već su u učinku.

### 13.3 Rad na praznik (110% uvećanje)

```
Radnik je radio praznik (16h) umesto da ga koristio kao slobodan dan.
```

| Polje            | Unos   | Objašnjenje                                                       |
|------------------|--------|-------------------------------------------------------------------|
| PO UČINKU        | 176,00 | Učinak uključuje i praznik — ne smanjuješ!                       |
| PRAZNIK (u P2)   | 0      | Ne unosi se u Parametri 2 za ovog radnika                        |
| RAD NA PRAZNIK   | 16,00  | Unosi u Kartici F7 — **ne ulazi** u ukupno sate                  |

### 13.4 Rad nedeljom (110% uvećanje)

| Polje          | Unos  | Objašnjenje                                             |
|----------------|-------|---------------------------------------------------------|
| PO UČINKU      | 168   | Sadrži i nedeljne sate                                  |
| RAD NEDELJOM   | 8     | Program obračunava 110% na ovih 8h. Ne ulazi u ukupno. |

### 13.5 Primer — prekovremeni na praznik (najkompleksniji slučaj)

```
Radnik radio na praznik (16h) I prekovremeno (8h) u istom mesecu.
Učinak = 160 + 8 prekovremenih = 168h (praznik je u ovom delu!)
```

| Polje          | Unos   |
|----------------|--------|
| PO UČINKU      | 168,00 |
| PRODUŽEN RAD   | 8,00   |
| RAD NA PRAZNIK | 16,00  |

---

## 14. BOLOVANJE DO 30 DANA

Bolovanje do 30 dana ide **na teret firme** — 65% cene rada za te sate.

### 14.1 Pravila unosa

```
Ana Nikolić: bolovanje 5 dana (40h), ostatak 120h redovnog rada
```

| Polje           | Unos   | Objašnjenje                                                   |
|-----------------|--------|---------------------------------------------------------------|
| PO UČINKU       | 120,00 | 160 − 40 = 120 (smanjiti za sate bolovanja!)                  |
| BOLOVANJE (65%) | 40,00  | Uneti sate bolovanja. **Ulaze** u ukupno sate.                |

**Formula bolovanja:**
```
Zarada po satu = 90.000 × 1.00 / 160 = 562,50 RSD/h
Bolovanje = 562,50 × 65% = 365,63 RSD/h
Naknada bolovanje = 365,63 × 40h = 14.625,00 RSD
```

### 14.2 Bolovanje 100%

Koristi se samo u posebnim slučajevima (npr. povrede na radu).

| Polje               | Unos   | Napomena                           |
|---------------------|--------|------------------------------------|
| PO UČINKU           | 120,00 | Smanjiti                           |
| BOLOVANJE 100%      | 40,00  | Obračunava 100% cene rada          |

### 14.3 Bolovanje PREKO 30 dana

Ovo je znatno komplikovaniji proces sa posebnim obrascima (OZ-7, OZ-10).

**U evidenciji zaposlenih:** Polje **VRSTA** = `B`

Za ovaj obračun koristiti:
- Poseban obrazac bolovanja
- Obračun naknada na osnovu 12-mesečnog proseka
- Vrsta isplate `3` u Parametri 2

---

## 15. SUSPENZIJA

Privremeno udaljenje radnika — prima smanjenu zaradu.

### 15.1 Priprema

U Parametri 1 → **PROCENAT SUSPENZIJE**: unesi procenat koji firma plaća za vreme suspenzije.

```
Primer: Firma plaća 50% zarade za vreme suspenzije
→ PROCENAT SUSPENZIJE: 50
```

### 15.2 Unos u Karticu F7

```
Jovan Ilić: suspenzija 2 radna dana = 16h
```

**Bruto obračun:**

| Polje       | Unos    | Napomena                                        |
|-------------|---------|--------------------------------------------------|
| PO UČINKU   | 144,00  | 160 − 16 = 144 (smanjiti za suspension sate)   |
| SUSPENZIJA  | `-16,00` | **OBAVEZNO** sa negativnim predznakom!         |

**Neto obračun:**

| Polje       | Unos        | Napomena                                               |
|-------------|-------------|--------------------------------------------------------|
| PO UČINKU   | 144,00      |                                                        |
| SUSPENZIJA  | `-16,00`    | Sati sa minusom                                        |
| SUSPENZIJA  | `-XXXXXX`   | Plus neto iznos suspenzije sa minusom (ručno izračunati) |

---

## 16. BONUS

### 16.1 Bonus kroz redovnu isplatu za decembar

Najjednostavniji slučaj — bonus se isplaćuje zajedno sa decembarskom zaradom.

```
Radniku se isplaćuje bonus 50.000 RSD bruto uz decembarsku zaradu.
```

**Priprema:**
- Proveri Parametri 1: **PROSEČNA BRUTO PLATA** — važeći iznos (za ograničenje najviše osnovice)

**Unos u Kartici F7:**

| Polje        | Unos      | Napomena                                     |
|--------------|-----------|----------------------------------------------|
| FIKSNA PLATA | 50.000,00 | Bruto iznos bonusa                           |

Obračun ide normalno. U polje OSNOVICA DOPRINOSA proveravas:
- Ako bruto zarada + bonus < najviša osnovica (5 × prosečna) → doprinosi na pun bruto
- Ako bruto zarada + bonus > najviša osnovica → doprinosi na najvišu osnovicu

### 16.2 Bonus nakon isplaćene konačne decembarske zarade

Kompleksna procedura — bonus zahteva posebnu PPP-PD prijavu.

```
Korak 1: U Evidenciji zaposlenih → GRUPA1: upiši 99 kod svih koji primaju bonus

Korak 2: Parametri 2:
  VRSTA ISPLATE: 1
  REDNI BROJ MESECA: 24 (jer je 12 već iskorišćen za decembar)
  REDNI BROJ ISPLATE: 1
  DATUM PRVE ISPLATE: datum
  KONAČNA ISPLATA: N
  IZNOS ZARADE BEZ POREZA: iznos iz ORIGINALNE decembarske isplate

Korak 3: Unos radnika → GRUPA1: 99

Korak 4: Platni spisak → uradi IDENTIČAN obračun kao konačna decembarska

Korak 5: Parametri 1 → ažuriraj PROSEČNA BRUTO PLATA na datum isplate bonusa

Korak 6: Parametri 2:
  REDNI BROJ ISPLATE: 2
  DATUM DRUGE ISPLATE: datum isplate bonusa
  KONAČNA ISPLATA: D
  IZNOS ZARADE BEZ POREZA: iznos na datum isplate bonusa

Korak 7: Platni spisak → PRENOSI → PRENOS ZARADE II ISPLATA + PRENOS AKONTACIJE
         → U Kartici F7: FIKSNA PLATA = bruto iznos bonusa
         → Ponovi obračun

Korak 8: PPP-PD:
  GODINA: ista kao konačna
  ISPLATA ZA MESEC: isti mesec kao konačna
  REDNI BROJ ISPLATE: 2
  KONAČNA: K
  DATUM OBAVEZE: datum plaćanja iz već predate konačne prijave
  DATUM PLAĆANJA: datum predaje ove prijave
```

---

## 17. OLAKŠICE ZA NOVOZAPOSLENE

### 17.1 Oznake u evidenciji zaposlenih

| Polje     | Vrednost | Vrsta olakšice                                       |
|-----------|----------|------------------------------------------------------|
| OZNOLAKS  | `08`     | Povraćaj 65% poreza i doprinosa — čl. 21v            |
| PROCUMANJ | `65`     | Procenat umanjenja poreza                            |
| DOPUMANJ  | `65`     | Procenat umanjenja doprinosa na teret firme          |
| OZNOLAKS  | `37`     | Oslobođenje 70% poreza i 100% PIO — čl. 21z          |
| OZNOLAKS  | `38`     | Zaposleni (ne novozaposleni) čl. 21i                 |

### 17.2 Posebna PPP-PD prijava

Radnici sa olakšicama MORAJU ići u posebnu prijavu, odvojenu od redovnih.

```
Kako razdvojiti:
1. U Parametri 2: za redovne → mesec = 1
                  za olakšice → mesec = 13 (drugi obračun)
2. Poseban platni spisak za svaki obračun
3. Posebna PPP-PD za svaki obračun
```

---

## 18. VIŠE ISPLATA U JEDNOM MESECU

### 18.1 Akontacija + konačna isplata

```
Firma isplaćuje 1. i 20. u mesecu.
1. isplata (akontacija) = 20.01.2026.
2. isplata (konačna) = 31.01.2026.
```

**Parametri 2 za 1. isplatu:**
```
DATUM PRVE ISPLATE: 20.01.2026
KONAČNA ISPLATA: N       ← NIJE konačna!
```

**Pre 2. isplate — OBAVEZNO ARHIVIRANJE:**
```
Putanja: Glavni meni → ODRŽAVANJE → ARHIVIRANJE
```

**Parametri 2 za 2. isplatu (konačna):**
```
DATUM DRUGE ISPLATE: 31.01.2026
KONAČNA ISPLATA: D       ← Konačna
```

**Platni spisak — prenos podataka:**
```
PRENOSI → PRENOS ZARADE II ISPLATA → ŽELIM PRENOS
PRENOSI → PRENOS AKONTACIJE → ŽELIM PRENOS
```

### 18.2 Krediti kod više isplata

| Kada odbijati kredit    | Kolona     | Opcija prenosa            |
|-------------------------|------------|---------------------------|
| Samo kroz konačnu       | AKTIVRATA  | ŽELIM PRENOS KREDITA      |
| Samo kroz akontaciju    | AKONTRATA  | ŽELIM PRENOS AKONTACIJE   |
| Kroz obe isplate        | Oba polja  | Odgovarajuća opcija svaki put |

**NIKADA ne ponavljaj obračun za istu isplatu u više-isplatnom sistemu — gubiš podatke prethodne isplate!**

---

## 19. PPP-PD PRIJAVA

**Putanja:** Glavni meni → IV XML-TXT → 1.XML → PORESKA PRIJAVA PPP-PD → PARAMETRI

### 19.1 Parametri PPP-PD

| Polje                | Vrednost              | Napomena                                           |
|----------------------|-----------------------|----------------------------------------------------|
| TIP ISPLATE          | `2`                   | Uvek 2 za zaradu                                  |
| VRSTA ID ISPLATIOCA  | `0`                   | 0=PIB (uvek)                                      |
| PROPISANA OSNOVICA   | `28400`               | Može ostaviti prazno                              |
| ŠIFRA OPŠTINE        | `7011`                | Opština sedišta firme                             |
| PIB ILI JMBG         | `101234567`           | PIB firme                                         |
| NAZIV                | `DOO MONTA BEOGRAD`   |                                                   |
| SEDIŠTE              | `BEOGRAD`             |                                                   |
| TELEFON              | `011-123-4567`        |                                                   |
| ULICA I BROJ         | `Bulevar Oslobođenja 100` |                                               |
| EMAIL                | `info@monta.rs`       |                                                   |

### 19.2 Polja za redovnu prijavu

| Polje                | Vrednost        | Napomena                                               |
|----------------------|-----------------|--------------------------------------------------------|
| DEKLARACIJA BR. PRIJAVE | `1`          | Redni broj u nizu prijava za ovaj mesec               |
| VRSTA PRIJAVE        | `1`             | 1=opšta prijava (redovna)                             |
| GODINA               | `2026`          |                                                        |
| ISPLATA ZA MESEC     | `01`            | Sa nulom ispred! Januar = `01`                        |
| REDNI BROJ ISPLATE   | `1`             | Redni broj isplate u mesecu                           |
| KONAČNA              | `K`             | K=konačna; prazno=nije konačna                        |
| DATUM OBAVEZE        | (prazno)        | Ne popunjava se za opštu prijavu                      |
| DATUM PLAĆANJA       | `31.01.2026`    | Najkasniji planirani datum isplate                    |
| BROJ DANA            | `31`            | Kalendarski broj dana januara                         |
| FOND SATI            | `160`           | Radnih sati u mesecu                                  |
| BROJ ZAPOSLENIH      | `5`             | Broj primaoca prihoda u prijavi                       |

### 19.3 Preuzimanje podataka iz obračuna

```
1. Klikni IZLAZ (iz Parametara)
2. Klikni PORESKA PRIJAVA PPP-PD
3. Ako je tabela popunjena: BRIŠI SVE
4. Klikni PARAMETRI PRENOSA → BRIŠI SVE → DODAJ SVE
5. U tabeli pronađi red REDOVNA ZARADA → unesi:
   ISPLATA: 1    MESEC: 1
6. Klikni krstić da se vratiš
7. Klikni PREUZMI ZARADE
8. Provjeri podatke
9. Klikni NAPRAVI XML
10. Pošalji XML poreskoj upravi
```

### 19.4 Izmenjena prijava

Kada radiš izmenu već predate prijave, popuni DODATNA POLJA:

| Polje                  | Vrednost | Napomena                                     |
|------------------------|----------|----------------------------------------------|
| VRSTA IZMENE           | `1`      | 1=izmena broja primatelja, 2=izmena iznosa... |
| IDENTIFIKACIJA IZMENE  | `123456` | ID broj originalne prijave (iz poreske)      |
| BROJ REŠENJA           | `RS-001` | Broj rešenja na osnovu koga menjate          |
| OSNOV PODNOŠENJA       | `1`      |                                              |

---

## 20. ARHIVIRANJE I RASKNJIŽAVANJE

### 20.1 Arhiviranje zarada

**Putanja:** Glavni meni → ODRŽAVANJE → ARHIVIRANJE

**Kada obavezno arhivirati:**
- Pre svake 2., 3., 4. isplate za isti mesec
- Nakon konačnog obračuna svakog meseca (čuvanje podataka)
- Pre obračuna naknada (potrebna arhiva 12 meseci)

### 20.2 Vraćanje zarade iz arhive

**Putanja:** Putanja iz uputstva `Vraćanje zarada iz arhive`

Koristi se kada treba ispraviti grešku u već završenom obračunu.

```
NAPOMENA: Vraćanje iz arhive radi isključivo programer.
Čuvaj arhive uredno — jednom mesečno minimum!
```

### 20.3 Rasknjižavanje kredita

Uvek posle svake isplate:

```
1. Glavni meni → KREDITI
2. RASKNJIŽAVANJE PLATE (za konačnu)
   ili RASKNJIŽAVANJE AKONTACIJE (za prvu isplatu)
3. Unesi datum isplate
4. Klikni U REDU
5. Klikni SREĐIVANJE KREDITA
6. Klikni PREGLED OTPLATA — proveri stanja
```

---

## BRZI KONTROLNI LISTA (checklista za svaki mesec)

```
PRE OBRAČUNA:
□ Proverena evidencija radnika (novorimljeni, odjavljeni, VRSTA)
□ Urađeno SREDI RADNI STAŽ
□ Provereni parametri Parametri 1 (minimalna zarada, prosečna bruto)
□ Parametri 2 — uneti mesec, datum, praznici, konačna D/N
□ Krediti — provereni, rasknjižen prethodni mesec

OBRAČUN:
□ Unos radnika — POTVRĐUJEM
□ Unos časova — POTVRĐUJEM
□ Platni spisak — korigovani časovi za sve posebne slučajeve
□ Prenosi (krediti, naknade, prevoz, topli obrok)
□ Obračun BRUTO (uneta cena rada, POTVRĐUJEM)
□ Proverene kartice F7 — bar 3-4 radnika spot-check

POSLE OBRAČUNA:
□ Spiskovi → PRENOS (za isplatu)
□ Prevoz → posebna PPP-PD
□ Zarada → PPP-PD → NAPRAVI XML → predaja
□ Virman → isplata
□ Krediti → RASKNJIŽAVANJE PLATE → SREĐIVANJE KREDITA
□ Arhiviranje (ako ima više isplata ili za arhivu)
```

---

## NAJČEŠĆE GREŠKE I REŠENJA

| Greška                                | Uzrok                              | Rešenje                                                   |
|---------------------------------------|------------------------------------|-----------------------------------------------------------|
| Porez se ne obračunava                | Prazno polje IZNOS ZARADE BEZ POREZA u P2 | Uneti neoporezivi iznos u Parametri 2            |
| Doprinosi na minimalnu umesto na bruto | PROSEČNA BRUTO PLATA nije uneta   | Obavezno uneti u Parametri 1                             |
| Minuli rad nije obračunat             | Polje STAZ = 0 ili prazno          | SREDI RADNI STAŽ ili ručno uneti STAZ                    |
| Radnik nije u platnom spisku          | Polje VRSTA ne odgovara vrsti isplate | Proveriti VRSTA u evidenciji (prazno=redovna)           |
| Kredit nije odbijen                   | AKTIVRATA prazna ili ZAODBITAK nema * | Uneti AKTIVRATA i zvezdicu u ZAODBITAK                 |
| PPP-PD XML se ne može predati         | Pogrešan BROJ DANA ili FOND SATI  | Proveriti da li su u skladu sa mesecom                   |
| Dupla obustava alimentacije           | ALIMPROC i ručni unos istovremeno | Koristiti samo jedno — program auto ako je u evidenciji  |
| Obračun se ne može ponoviti           | Više isplata — KONAČNA = D za 1. isplatu | Postaviti N za prvu, D samo za konačnu             |
| Neto zarada manja od minimalne        | Zarada zaista manja — bez dotacije | Koristiti DOTACIJA → SVI RADNICI DO NETA                |

---

*Dokument pripremljen na osnovu originalnih uputstava iz programa Algoritam.*
*Svi primeri su ilustrativni — proveravaj važeće zakonske iznose pre svakog obračuna.*
