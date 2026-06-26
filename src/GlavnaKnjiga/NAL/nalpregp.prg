PARAMETERS sstampa
PUSH KEY clear
IF TNACINK<>'D'
   nalp.CMD0.setfocus
ELSE
   nalp2.CMD0.setfocus
endif

SELECT NALP
mbrnal=brnal
IF  TNACINK<>'D'
   SELECT AN0
   USE
ENDIF

   DO idel WITH (kkockax)   
   DO idel WITH (kkockax2)   



SELECT NALP
COPY STRU TO &KKOCKA

USE &KKOCKA IN 0 ALIAS KOCKA exclu   


KKNALIZV=MDATA01+'\NALIZV'+OPERATER+'.DBF'
KKNALIZVX=MDATA01+'\NALIZV'+OPERATER+'.CDX'
KNALIZV=MDATA01+'\NALIZV'+OPERATER
KNALIZVX=MDATA01+'\NALIZV'+OPERATER


DO IDEL WITH (KKNALIZV)
DO IDEL WITH (KKNALIZVX)
DO IBAZE WITH KNALIZV,'NALIZV'
DO IIND WITH KNALIZV,'KONTO+DTOS(DATDOK)','BRNAL','DTOS(DATDOK)',;
             'KONTO+SIFRA+DTOS(DATDOK)','SIFRA+DTOS(DATDOK)','SIFRA+BRRAC+DTOS(DATDOK)'


USE &KNALIZV IN 0 ALIAS NALIZV EXCLUSIVE 

SELECT NALP
SET ORDER TO 2
SEEK MBRNAL
IF FOUND()
   DO WHILE.NOT.EOF()
      IF BRNAL<>MBRNAL
         EXIT
      ENDIF   
      MKONTO=KONTO
      MDUG=DUG
      MPOT=POT
      MOPIS=OPIS
      MDEV=DEV
      MDEVKURS=DEVKURS
      MDEVDUG=DEVDUG
      MDEVPOT=DEVPOT
      MDATDOK=DATDOK
      MBRNAL=BRNAL
      MDOK=DOK
      MMP=MP
      MMTR=MTR 
      IF MDUG<>0.OR.MPOT<>0
         SELECT NALIZV
         APPEND BLANK
         REPLACE KONTO WITH MKONTO
         REPLACE DUG WITH MDUG
         REPLACE POT WITH MPOT
         REPLACE OPIS WITH MOPIS
         REPLACE DEV WITH MDEV
         REPLACE DEVKURS WITH MDEVKURS
         REPLACE DEVDUG WITH MDEVDUG
         REPLACE DEVPOT WITH MDEVPOT
         REPLACE BRNAL WITH MBRNAL
         REPLACE MP WITH MMP
         REPLACE MTR WITH MMTR
         REPLACE DOK WITH MDOK  
         REPLACE GRUPA WITH '00'
         replace datdok WITH mdatdok
         SELECT KOCKA
         APPEND BLANK
         REPLACE KONTO WITH MKONTO
      ENDIF
      SELECT NALP
      SKIP
   ENDDO    
ENDIF

IF TNACINK<>'D'
   SELECT KOCKA
   INDEX ON KONTO TAG KONTO
   SET ORDER TO 1
   TOTAL ON KONTO TO &KKOCKA2 FIELDS DUG,POT
   DELETE ALL
   PACK
   APPEND FROM &KKOCKA2
   SELECT KOCKA
   GO TOP
   DO WHILE.NOT.EOF()
      MKONTO=KONTO
      SELECT AAAN
      SEEK MKONTO
      IF FOUND()
         MSIFARNIK=SIFARNIK
         MSIFPROD=ALLTRIM(SIFPROD)
         MANAL='anal'+msifprod+'.dbf'
         IF FILE(manal)
            USE &manal IN 0 ALIAS ANAL ORDER 4
            IF MSIFARNIK<>SPACE(2)
               KAN0='AN0'+ALLTRIM(MSIFARNIK)+'.DBF'
            ELSE
               KAN0='AN0.DBF'
            ENDIF     
            USE &KAN0 IN 0 ORDER 1 ALIAS AN0
            SELECT ANAL
            SET RELATION TO SIFRA INTO AN0 ADDITIVE
            SEEK MBRNAL
            IF FOUND()
               DO WHILE.NOT.EOF()
                  IF BRNAL<>MBRNAL
                     EXIT
                  ENDIF
                  MSIFRA=SIFRA
                  mnaziv=an0.naziv
                  MBRRAC=BRRAC
                  MDATDOK=DATDOK
                  MVALUTA=VALUTA
                  MDUG=DUG
                  MPOT=POT
                  MDEV=DEV
                  mdevkurs=devkurs
                  MDEVDUG=DEVDUG
                  MDEVPOT=DEVPOT 
                  MDOK=DOK
                  MMTR=MTR
                  MMP=MP
                  IF MDUG<>0.OR.MPOT<>0
                     SELECT NALIZV
                     APPEND blank  
                     REPLACE SIFRA WITH MSIFRA
                     REPLACE BRRAC WITH MBRRAC
                     REPLACE DATDOK WITH MDATDOK
                     REPLACE VALUTA WITH MVALUTA
                     REPLACE DUG WITH MDUG
                     REPLACE POT WITH MPOT
                     REPLACE OPIS WITH MOPIS
                     REPLACE DEV WITH MDEV
                     REPLACE DEVKURS WITH MDEVKURS
                     REPLACE DEVDUG WITH MDEVDUG
                     REPLACE DEVPOT WITH MDEVPOT
                     REPLACE BRNAL WITH MBRNAL
                     REPLACE KONTO WITH MKONTO
                     replace k1naziv WITH mnaziv 
                     REPLACE DOK WITH MDOK
                     REPLACE MP WITH MMP
                     REPLACE MTR WITH MMTR
                     IF LEN(MSIFPROD)=1
                        REPLACE GRUPA WITH '0'+MSIFPROD
                     ELSE
                        REPLACE GRUPA WITH MSIFPROD 
                     ENDIF
                  ENDIF   
                  SELECT ANAL
                  SKIP
               ENDDO   
            ENDIF
            SELECT ANAL
            SET RELATION TO 
            USE
            SELECT an0 
            use
         endif 
      ENDIF
      SELECT KOCKA
      SKIP
   ENDDO   
   SELECT KOCKA
   USE

ELSE
*------AKO JE NACIN KNJIZENJA=DA------------------
   SELECT KOCKA
   USE
   SELECT NALP
   GO TOP
   DO WHILE.NOT.EOF()
      IF BRNAL=MBRNAL
         IF VRSTA='AN'
            MSIFRA=SIFRA
            mnaziv=an0.naziv
            MBRRAC=BRRAC
            MDATDOK=DATDOK
            MVALUTA=VALUTA
            MDUG=DUG
            MPOT=POT
            MDEV=DEV
            mdevkurs=devkurs
            MDEVDUG=DEVDUG
            MDEVPOT=DEVPOT 
            MDOK=DOK
            MMTR=MTR
            MMP=MP
            MSIFPROD=SIFPROD
            MKONTO=KONTO
            IF MDUG<>0.OR.MPOT<>0
               SELECT NALIZV
               APPEND blank  
               REPLACE SIFRA WITH MSIFRA
               REPLACE BRRAC WITH MBRRAC
               REPLACE DATDOK WITH MDATDOK
               REPLACE VALUTA WITH MVALUTA
               REPLACE DUG WITH MDUG
               REPLACE POT WITH MPOT
               REPLACE OPIS WITH MOPIS
               REPLACE DEV WITH MDEV
               REPLACE DEVKURS WITH MDEVKURS
               REPLACE DEVDUG WITH MDEVDUG
               REPLACE DEVPOT WITH MDEVPOT
               REPLACE BRNAL WITH MBRNAL
               REPLACE KONTO WITH MKONTO
               replace k1naziv WITH mnaziv 
               REPLACE DOK WITH MDOK
               REPLACE MP WITH MMP
               REPLACE MTR WITH MMTR
               IF LEN(MSIFPROD)=1
                  REPLACE GRUPA WITH '0'+MSIFPROD
               ELSE
                  REPLACE GRUPA WITH MSIFPROD 
               ENDIF
               SELECT NALP
            ENDIF   
         ENDIF
      ENDIF      
      SELECT NALP
      SKIP
   ENDDO   
ENDIF
   
SELECT NALIZV
SET ORDER TO 
GO TOP
SET CENTURY off
DO CASE sstampa
case sstampa=1
   *REPORT FORM NALOG6 PREVIEW
   mfile='NALOG6'
   uslov=""
   DO printer_bullzip WITH mdata02,mfile,uslov
   
CASE sstampa=2
   *REPORT FORM NALOG6eko PREVIEW
   mfile='NALOG6EKO'
   uslov=""
   DO printer_bullzip WITH mdata02,mfile,uslov
   
otherwise
   *REPORT FORM NALOG3DEV PREVIEW
      mfile='NALOG3DEV'
   uslov=""
   DO printer_bullzip WITH mdata02,mfile,uslov

endcase
SET CENTURY on
SELECT NALIZV
USE
IF TNACINK<>'D'
   USE AN0 IN 0 ORDER 1 ALIAS AN0
ENDIF
SELECT NALP
SET ORDER TO 2
SEEK mbrnal
SET ORDER TO 
IF TNACINK<>'D'
   nalp.grd0.COLUMN2.setfocus
ELSE
   nalp2.grd0.COLUMN2.setfocus
endif   
POP key