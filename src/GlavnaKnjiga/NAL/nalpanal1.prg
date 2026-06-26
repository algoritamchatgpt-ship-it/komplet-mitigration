PUSH KEY CLEAR
   SELECT NALP
   MKONTO=KONTO
   PUBLIC MGBRNAL,MGDATDOK
   MGBRNAL=BRNAL
   MGDATDOK=DATDOK
   SELECT AAAN
   SEEK MKONTO
   IF FOUND()
      PUBLIC MDEFSIF,MDP
      MSIF=SIFARNIK
      MDP=DP
      IF MSIF<>SPACE(2)
         MDEFSIF=1
         KAN0='AN0'+ALLTRIM(MSIF)+'.DBF'
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
      TKOJI='ANP2'
      TANAL='ANP2'
      KANALB=MDATA01+'\ANALB'+OPERATER
      KANAL='ANAL'+MTREDNI
      KANALK=MDATA01+'\ANALK'+OPERATER
      KKANALP='NALP'+OPERATER
      SELECT NALP
      SET RELATION TO
      USE &KANALB EXCLU ALIAS ANALP IN 0
      SELECT ANALP
      DELETE ALL
      PACK
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
      SELECT KONTO
      USE
      SELECT ANALP
      GO TOP
      *----------------------------
      DO FORM ANP2 WITH TANAL       
      *----------------------------
      *----------AUTOMATSKO ZATVARANJE RACUNA------------
      IF PKANAUTOZ='D'
         SELECT ANALP
         DO idel WITH (kkockax)
         DO idel WITH (kkockax2)
         DO idel WITH (kkockax3)
         DO idel WITH (kkockax4)
         SELECT ANALP
         COPY STRUCTURE TO &KKOCKA
         USE &KKOCKA IN 0 ALIAS KOCKA EXCLUSIVE
         SELECT KOCKA
         INDEX ON SIFRA TAG SIFRA
         SELECT ANALP
         GO TOP
         DO WHILE.NOT.EOF()
            IF UPLATA<>0
               SCATTER NAME POLJA
               REPLACE uplata WITH 0
               SELECT KOCKA
               APPEND BLANK
               GATHER NAME POLJA
               SELECT ANALP
            ENDIF   
            SKIP
         ENDDO   
         SELECT ANAL
         SET ORDER TO 8
         *---PRAVIMO TOTAL SVIH UPLATA NA ISTO SIFRI
         SELECT KOCKA 
         TOTAL ON SIFRA TO &KKOCKA2 FIELDS UPLATA,DUPLATA
         DELETE ALL
         PACK

         APPEND FROM &KKOCKA2
     
         SELECT KOCKA
         COPY STRUCTURE TO &KKOCKA2
         USE &KKOCKA2 IN 0 ALIAS KOCKA2 EXCLU
         SELECT KOCKA
         *----------OVDE NALAZIMO NEZATVORENE RACUNE-----------
         *----------KUPCI--------------------------------------
         GO TOP
         MDUG=0
         MPOT=0
         DO WHILE.NOT.EOF()
            MSIFRA=SIFRA
            IF MDP='D'
               MPOT=UPLATA
               SELECT ANAL
               SET EXACT OFF
               SEEK MSIFRA
               IF FOUND()
                  *----KOPIRAMO NEZATVORENE RACUNE U KOCKA3
                  COPY TO &KKOCKA3 FOR ZATVAR=' '.AND.SIFRA=MSIFRA
                  USE &KKOCKA3 IN 0 ALIAS KOCKA3 EXCLU
                  SELECT KOCKA3
                  INDEX ON BRRAC TAG RAC
                  INDEX ON DTOS(VALUTA)+BRRAC TAG RACVAL
                  SET ORDER TO 1
                  *-----RADIMO TOTAL VALUTA+BROJ RACUNA----
                  TOTAL ON BRRAC TO &KKOCKA4 FIELDS DUG,POT
                  ZAP
                  APPEND FROM &KKOCKA4
                  DELETE ALL FOR POT>=DUG
                  PACK
                  *----PRAVIMO DUGOVNU STRANU KOLIKO JE PREOSTALO-----
                  REPLACE ALL DUG WITH DUG-POT
                  REPLACE ALL POT WITH 0
                  *----KOCKA 3 SADRZI RACUNE REDOM NEZATVORENE------
                  SELECT KOCKA3   
                  SET ORDER TO 2
                  GO TOP               
                  DO WHILE.NOT.EOF()
                     MDUG=DUG
                     MBRRAC=BRRAC
                     MDOK=DOK
                     IF MPOT>0
                        *-----AKO JE IZNOS UPLATE MANJI OD NEZATVORENE STAVKE 
                        DO CASE
                        CASE MDUG>MPOT
                           SELECT KOCKA2
                           APPEND BLANK
                           REPLACE SIFRA WITH MSIFRA
                           REPLACE DATDOK WITH MGDATDOK
                           REPLACE VALUTA WITH MGDATDOK
                           REPLACE POT WITH MPOT
                           REPLACE BRRAC WITH MBRRAC
                           REPLACE DOK WITH MDOK
                           REPLACE BRNAL WITH MGBRNAL
                           MPOT=0
                           SELECT KOCKA
                        CASE MDUG=MPOT
                           SELECT KOCKA2
                           APPEND BLANK
                           REPLACE SIFRA WITH MSIFRA
                           REPLACE DATDOK WITH MGDATDOK
                           REPLACE VALUTA WITH MGDATDOK
                           REPLACE POT WITH MPOT
                           REPLACE BRRAC WITH MBRRAC
                           REPLACE DOK WITH MDOK
                           REPLACE BRNAL WITH MGBRNAL
                           REPLACE ZATVAR WITH '*'
                           MPOT=0
                           SELECT KOCKA3
                           REPLACE ZATVAR WITH '*'
                        CASE MDUG<MPOT
                           SELECT KOCKA2
                           APPEND BLANK
                           REPLACE SIFRA WITH MSIFRA
                           REPLACE DATDOK WITH MGDATDOK
                           REPLACE VALUTA WITH MGDATDOK
                           REPLACE POT WITH MDUG
                           REPLACE BRRAC WITH MBRRAC
                           REPLACE DOK WITH MDOK
                           REPLACE BRNAL WITH MGBRNAL
                           REPLACE ZATVAR WITH '*'
                           MPOT=MPOT-MDUG
                           SELECT KOCKA3
                           REPLACE ZATVAR WITH '*'
                        ENDCASE                         
                        SELECT KOCKA
                        REPLACE UPLATA WITH MPOT
                        SELECT KOCKA3
                        SKIP
                     ELSE
                        EXIT
                     ENDIF       
                  ENDDO   
                  SELECT KOCKA3
                  IF RECCOUNT()>0
                     GO TOP
                     DO WHILE.NOT.EOF()
                        MSIFRAC=SIFRA+BRRAC
                        MZATVAR=ZATVAR
                        SELECT ANAL
                        SET ORDER TO 3
                        SEEK MSIFRAC
                        IF FOUND()
                           DO WHILE.NOT.EOF()
                              IF SIFRA+BRRAC<>MSIFRAC
                                 EXIT
                              ENDIF
                              REPLACE ZATVAR WITH MZATVAR   
                              SKIP
                           ENDDO   
                        ENDIF
                        SELECT KOCKA3
                        SKIP
                     ENDDO   
                  ENDIF
                  SELECT KOCKA3
                  USE
               ENDIF
            ELSE
               *-----KNJIZENJE DOBAVLJACA
               MDUG=UPLATA
               SELECT ANAL
               SET EXACT OFF
               SEEK MSIFRA
               IF FOUND()
                  *----KOPIRAMO NEZATVORENE RACUNE U KOCKA3
                  COPY TO &KKOCKA3 FOR ZATVAR=' '.AND.SIFRA=MSIFRA
                  USE &KKOCKA3 IN 0 ALIAS KOCKA3 EXCLU
                  SELECT KOCKA3
                  INDEX ON BRRAC TAG RAC
                  INDEX ON DTOS(VALUTA)+BRRAC TAG RACVAL
                  SET ORDER TO 1
                  *-----RADIMO TOTAL VALUTA+BROJ RACUNA----
                  TOTAL ON BRRAC TO &KKOCKA4 FIELDS DUG,POT
                  ZAP
                  APPEND FROM &KKOCKA4
                  DELETE ALL FOR DUG>=POT
                  PACK
                  *----PRAVIMO DUGOVNU STRANU KOLIKO JE PREOSTALO-----
                  REPLACE ALL POT WITH POT-DUG
                  REPLACE ALL DUG WITH 0
                  *----KOCKA 3 SADRZI RACUNE REDOM NEZATVORENE------
                  SELECT KOCKA3   
                  SET ORDER TO 2
                  GO TOP               
                  DO WHILE.NOT.EOF()
                     MPOT=POT
                     MBRRAC=BRRAC
                     MDOK=DOK
                     IF MDUG>0
                        *-----AKO JE IZNOS UPLATE MANJI OD NEZATVORENE STAVKE 
                        DO CASE
                        CASE MPOT>MDUG
                           SELECT KOCKA2
                           APPEND BLANK
                           REPLACE SIFRA WITH MSIFRA
                           REPLACE DATDOK WITH MGDATDOK
                           REPLACE VALUTA WITH MGDATDOK
                           REPLACE DUG WITH MDUG
                           REPLACE BRRAC WITH MBRRAC
                           REPLACE DOK WITH MDOK
                           REPLACE BRNAL WITH MGBRNAL
                           MDUG=0
                           SELECT KOCKA
                        CASE MPOT=MDUG
                           SELECT KOCKA2
                           APPEND BLANK
                           REPLACE SIFRA WITH MSIFRA
                           REPLACE DATDOK WITH MGDATDOK
                           REPLACE VALUTA WITH MGDATDOK
                           REPLACE DUG WITH MDUG
                           REPLACE BRRAC WITH MBRRAC
                           REPLACE DOK WITH MDOK
                           REPLACE BRNAL WITH MGBRNAL
                           REPLACE ZATVAR WITH '*'
                           MDUG=0
                           SELECT KOCKA3
                           REPLACE ZATVAR WITH '*'
                        CASE MPOT<MDUG
                           SELECT KOCKA2
                           APPEND BLANK
                           REPLACE SIFRA WITH MSIFRA
                           REPLACE DATDOK WITH MGDATDOK
                           REPLACE VALUTA WITH MGDATDOK
                           REPLACE DUG WITH MPOT
                           REPLACE BRRAC WITH MBRRAC
                           REPLACE DOK WITH MDOK
                           REPLACE BRNAL WITH MGBRNAL
                           REPLACE ZATVAR WITH '*'
                           MDUG=MDUG-MPOT
                           SELECT KOCKA3
                           REPLACE ZATVAR WITH '*'
                        ENDCASE                         
                        SELECT KOCKA
                        REPLACE UPLATA WITH MDUG
                        SELECT KOCKA3
                        SKIP
                     ELSE
                       EXIT
                     ENDIF       
                  ENDDO   
                  SELECT KOCKA3
                  IF RECCOUNT()>0
                     GO TOP
                     DO WHILE.NOT.EOF()
                        MSIFRAC=SIFRA+BRRAC
                        MZATVAR=ZATVAR
                        SELECT ANAL
                        SET ORDER TO 3
                        SEEK MSIFRAC
                        IF FOUND()
                           DO WHILE.NOT.EOF()
                              IF SIFRA+BRRAC<>MSIFRAC
                                 EXIT
                              ENDIF
                              REPLACE ZATVAR WITH MZATVAR   
                              SKIP
                           ENDDO   
                        ENDIF
                        SELECT KOCKA3
                        SKIP
                     ENDDO   
                  ENDIF
                  SELECT KOCKA3
                  USE
               ENDIF
               *----------KRAJ KNJIZENJA DOBAVLJACA-----------   
            ENDIF
            SELECT KOCKA
            SKIP
         ENDDO   
         SELECT KOCKA
         DELETE ALL FOR UPLATA=0
         PACK
         IF MDP='D'
            replace ALL POT WITH uplata
         ELSE
            REPLACE ALL DUG WITH UPLATA
         ENDIF
         REPLACE ALL KONTO WITH MKONTO
         REPLACE ALL DATDOK WITH MGDATDOK
         REPLACE ALL VALUTA WITH MGDATDOK
         REPLACE ALL BRNAL WITH MGBRNAL
         REPLACE ALL BRRAC WITH 'NEMA'
         USE
         SELECT KOCKA2
         REPLACE ALL KONTO WITH MKONTO
         REPLACE ALL DATDOK WITH MGDATDOK
         REPLACE ALL VALUTA WITH MGDATDOK
         REPLACE ALL BRNAL WITH MGBRNAL
         USE
         SELECT ANAL
         APPEND FROM &KKOCKA
         APPEND FROM &KKOCKA2
         *----KRAJ AKO JE AUTOMATSKO ZATVARANJE----------------
      ELSE
         *------AKO NIJE AUTOMATSKO---------------------------- 
         SELECT ANALP
         GO TOP
         DO WHILE.NOT.EOF()
            IF UPLATA<>0
               SCATTER NAME POLJA
               IF TDP='D'
                  MPOT=UPLATA
                  MDUG=0
               ELSE
                  MDUG=UPLATA   
                  MPOT=0
               ENDIF   
               SELECT ANAL
               APPEND BLANK
               GATHER NAME POLJA
               REPLACE DUG WITH MDUG
               REPLACE POT WITH MPOT
               REPLACE KONTO WITH MKONTO
               REPLACE DATDOK WITH MGDATDOK
               REPLACE VALUTA WITH MGDATDOK
               REPLACE BRNAL WITH MGBRNAL
               SELECT ANALP
            ENDIF   
            SKIP
         ENDDO   
      ENDIF
      *----------KRAJ NEZATVORENIH RACUNA-------------------
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
      SELECT NALP
      REPLACE DUG WITH MDUG
      REPLACE POT WITH MPOT
      replace devdug WITH mdevdug
      replace devpot WITH mdevpot
      replace dev WITH mdev
      replace devkurs WITH mdevkurs
      MEBRNAL=BRNAL
      MEDATDOK=DATDOK
      MEDOK=DOK
      MKONTO=KONTO
      IF MDUGN<>0.OR.MPOTN<>0.OR.MDEVDUGN<>0.OR.MDEVPOTN<>0
         APPEND BLANK
         REPLACE DUG WITH -MPOTN
         REPLACE POT WITH -MDUGN
         replace devdug WITH -mdevPOTN
         replace devpot WITH -mdevDUGN
         replace dev WITH mdev
         replace devkurs WITH mdevkurs
         REPLACE BRNAL WITH MEBRNAL
         REPLACE DATDOK WITH MEDATDOK
         REPLACE DOK WITH MEDOK 
         REPLACE KONTO WITH MKONTO
      ENDIF
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
      SET RELATION TO KONTO INTO KONTO ADDITIVE
      SET RELATION TO MP INTO MESTA ADDITIVE
      SET RELATION TO MTR INTO MTR ADDITIVE
   ENDIF   
   SELECT NALP
   IF TNACINK<>'D'
      NALP.GRD0.SETFOCUS
      NALP.REFRESH
   ELSE
      NALP2.GRD0.SETFOCUS
      NALP2.REFRESH
   ENDIF   
   
   
POP KEY
