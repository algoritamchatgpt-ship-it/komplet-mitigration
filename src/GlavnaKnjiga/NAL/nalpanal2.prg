   *------PROVERA POSTOJANJA KONTA U ANALITICI-------
   PUSH KEY CLEAR
   SELECT NALP
*  SET STEP ON 
   MKONTO=KONTO
   
   PUBLIC MGBRNAL,MGDATDOK
   MGBRNAL=BRNAL
   MGDATDOK=DATDOK
   MGDATPRI=DATPRI
   MGDATPDV=DATPDV
   SELECT AAAN
   IF MKONTO<>SPACE(8)
   SEEK MKONTO
   
   IF FOUND()
      PUBLIC MDEFSIF
      MSIF=SIFARNIK
      IF MSIF<>SPACE(2)
         MDEFSIF=1
         KAN0='AN0'+ALLTRIM(MSIF)
      ELSE
         MDEFSIF=0
      ENDIF      
      *-----AKO POSTOJI ANALITIKA--------------------
      MREC=RECNO()
         
      PUBLIC TREDNI,TPNAZIV,TDP,TSIFARNIK,TKONTO

      MTREDNI=ALLTRIM(SIFPROD)
      TPNAZIV=ALLTRIM(PNAZIV)
      TDP=ALLTRIM(DP)
      TKONTO=ALLTRIM(KONTO)
      TSIFARNIK=SIFARNIK

      TKOJI='ANA'
      TANAL='ANP'

      KANALP=MDATA01+'\ANALP'+OPERATER
      KANAL='ANAL'+MTREDNI
      KKANALP='NALP'+OPERATER

      SELECT NALP
      SET RELATION TO
      SELECT KONTO
      USE   
*      USE &KANALK IN 0 EXCLU ALIAS ANALK
*      SELECT ANALK
*      DELETE ALL
*PACK

*      SET ORDER TO 1

      
      USE &KANALP EXCLU ALIAS ANALP IN 0
      
      SELECT ANALP
      DELETE ALL
      PACK

      APPEND BLANK
      REPLACE BRNAL WITH MGBRNAL
      REPLACE DATDOK WITH MGDATDOK
      REPLACE DATPDV WITH MGDATPDV
      REPLACE DATPRI WITH MGDATPRI
      REPLACE KONTO WITH KONTO

      
      USE &KANAL IN 0 ALIAS ANAL ORDER 2
   
      SELECT ANALP
      SET RELATION TO MP INTO MESTA ADDITIVE
      SET RELATION TO MTR INTO MTR ADDITIVE

      IF MDEFSIF=1 
         SELECT AN0
         USE 
         USE &KAN0 IN 0 ALIAS AN0 ORDER 1
         SELECT ANALP
         SET RELATION TO SIFRA INTO AN0 ADDITIVE
      ELSE
         SELECT ANALP 
         SET RELATION TO SIFRA INTO AN0 ADDITIVE
      ENDIF      
      GO TOP
      *----------------------------
      DO FORM ANP WITH TANAL       
      
      *----------------------------

      SELECT ANALP
      GO TOP
      DO WHILE.NOT.EOF()
         IF DUG<>0.OR.POT<>0
            SCATTER NAME POLJA
            REPLACE DUG WITH 0
            REPLACE POT WITH 0
            SELECT ANAL
            APPEND BLANK
            GATHER NAME POLJA
            SELECT ANALP
         ENDIF   
         SKIP
      ENDDO   
      SELECT ANALP
      SET RELATION TO
      USE
      SELECT ANAL
      SET ORDER TO 4
      SEEK MGBRNAL
      MDUG=0
      MPOT=0
      MDEVDUG=0
      MDEVPOT=0
      MDUGN=0
      MPOTN=0
      mdevdugN=0
      mdevpotN=0
      MDEVKURS=0
      MDEV='   '
      IF FOUND()
         mdev=dev
         mdevkurs=devkurs
         DO WHILE.NOT.EOF()
            IF BRNAL<>MGBRNAL
               EXIT
            ENDIF   
            IF DUG>0
               MDUG=MDUG+DUG
            ELSE
               MDUGN=MDUGN+DUG
            ENDIF
            IF POT>0   
               MPOT=MPOT+POT
            ELSE
               MPOTN=MPOTN+POT
            ENDIF
            IF DEVDUG>0
            mdevdug=mdevdug+devdug
            ELSE
            mdevdugN=mdevdugN+devdug
            ENDIF
            IF DEVPOT>0
               mdevpot=mdevpot+devpot
            else
               mdevpotN=mdevpotN+devpot
            ENDIF
            SKIP
         ENDDO      
      ENDIF
      SELECT ANAL
      USE
*      SELECT ANALK
*      USE
      SELECT NALP
      REPLACE DUG WITH MDUG+MDUGN
      REPLACE POT WITH MPOT+MPOTN
      replace devdug WITH mdevduG+MDEVDUGN
      replace devpot WITH mdevpot+MDEVPOTN
      replace dev WITH mdev
      replace devkurs WITH mdevkurs
      MEOPIS=OPIS
      MEBRNAL=BRNAL
      MEDATDOK=DATDOK
      MEDOK=DOK
      MKONTO=KONTO
   *   IF MDUGN<>0.OR.MPOTN<>0.OR.MDEVDUGN<>0.OR.MDEVPOTN<>0
   *      APPEND BLANK
   *      REPLACE DUG WITH MDUGN
   *      REPLACE POT WITH MPOTN
   *      replace devdug WITH mdevdugN
   *      replace devpot WITH mdevpotN
   *      replace dev WITH mdev
   *      replace devkurs WITH mdevkurs
   *      REPLACE BRNAL WITH MEBRNAL
   *      REPLACE DATDOK WITH MEDATDOK
   *      REPLACE DOK WITH MEDOK 
   *      REPLACE KONTO WITH MKONTO
   *      REPLACE OPIS WITH MEOPIS
   *   ENDIF
      *-------ZAVRSILI SMO ANALITIKU-------------
       
      TKOJI='NALGK'
      NALB='NAL'
      TNAL='NALP' 
      IF MDEFSIF=1 
         SELECT AN0
         USE 
         USE AN0 IN 0 ALIAS AN0 ORDER 1
      ENDIF      
      USE &KKONTO IN 0 ALIAS KONTO ORDER 1
      SELECT NALP
      SET RELATION TO BRNAL INTO NALBROJ  ADDITIVE
      SET RELATION TO KONTO INTO KONTo ADDITIVE
      SET RELATION TO MP INTO MESTA ADDITIVE
      SET RELATION TO MTR INTO MTR ADDITIVE
   ENDIF
      
   ENDIF
   SELECT NALP
*   NALP.GRD0.SETFOCUS
POP KEY
*   NALP.REFRESH
