# Multi-user setup (Windows LAN)

Ovaj projekat sada ima osnovnu zastitu od konflikta upisa za DBF tabele:
- `Radnici` koristi optimistic check po zapisu + record lock fajl.
- `Platni spisak` koristi optimistic check po fajlu + lock pri snimanju.

Ako drugi korisnik pokusa da snimi stare podatke preko novijih, aplikacija prikazuje poruku o konfliktu i trazi osvezavanje.

## 1) Glavni racunar (host)

1. Publish aplikaciju u zajednicki folder (primer: `D:\AlgoritamShared\App`).
2. Pokreni PowerShell kao Administrator.
3. Pokreni:

```powershell
.\tools\setup-network-share.ps1 `
  -AppFolder "D:\AlgoritamShared\App" `
  -ShareName "AlgoritamApp" `
  -ExeRelativePath "Algoritam.WPF.exe" `
  -CreatePublicDesktopShortcut
```

To uradi sledece:
- SMB share sa `ChangeAccess Everyone`
- NTFS `Modify` za `Everyone`
- opcioni shortcut na `Public Desktop`

## 2) Klijentski racunari

Na svakom klijentu korisnici mogu da naprave shortcut ka:

`\\HOSTNAME\AlgoritamApp\Algoritam.WPF.exe`

Ako je aplikacija na istom racunaru za vise lokalnih korisnika, `Public Desktop` shortcut je dovoljan za sve profile.

## 3) Operativna preporuka

- Za poruku konflikta: korisnik treba da klikne `Osvezi` i ponovi unos.
- Ne drzati isti ekran otvoren satima bez osvezavanja.
- Backup DBF foldera raditi redovno (npr. dnevno).

## 4) Brzi test sa 2 racunara

1. Na host racunaru pokreni publish:

```powershell
.\tools\publish-office-build.ps1
```

2. Na host racunaru podeli folder preko mreze:

```powershell
.\tools\setup-network-share.ps1 `
  -AppFolder "C:\Workspace\algoritam-migration\newproject\instalacije\AlgoritamOffice" `
  -ShareName "AlgoritamApp" `
  -ExeRelativePath "Algoritam.WPF.exe" `
  -CreatePublicDesktopShortcut
```

3. Na klijentskom racunaru napravi ikonicu:

```powershell
powershell -ExecutionPolicy Bypass -File "\\HOSTNAME\AlgoritamApp\create-network-shortcut.ps1" -TargetUncExePath "\\HOSTNAME\AlgoritamApp\Algoritam.WPF.exe"
```

4. Istovremeni test:
- Racunar A: otvori isti record u `Radnici`, klikni `Izmeni` i ostavi otvoreno.
- Racunar B: pokusaj `Izmeni` isti record.
- Ocekivano: B dobija poruku da je zapis zakljucan.

5. Test "prvi upis ima prioritet":
- A i B otvore isti period u `Platni spisak`.
- A sacuva izmene.
- B bez osvezavanja pokusa da sacuva svoje.
- Ocekivano: B dobija poruku konflikta i trazi se osvezavanje.
