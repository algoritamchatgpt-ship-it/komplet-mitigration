*----------AUTOMATSKO ZATVARANJE RACUNA------------
*SET STEP ON 
   SELECT KOCKA9
   MDP=DP
   MKONTO=KONTO
   MGDATDOK=DATDOK
   MGBRNAL=BRNAL
   MSIFRA=SIFRA
   MIMETAB=ALLTRIM(IMETABELE)
   KANALB=MDATA01+'\ANALB'+OPERATER
   KANALK=MDATA01+'\ANALK'+OPERATER
   USE &KANALB EXCLU ALIAS ANALP IN 0
   SELECT ANALP
   DELETE ALL
   PACK      
   DO idel WITH (kkockax)
   DO idel WITH (kkockax2)
   DO idel WITH (kkockax3)
   DO idel WITH (kkockax4)
   SELECT ANALP
   COPY STRUCTURE TO &KKOCKA
   USE
   USE &KKOCKA IN 0 ALIAS KOCKA EXCLUSIVE
   IF MDP='D'
      SELECT KOCKA9
      MUPLATA=POT-DUG
      SCATTER NAME POLJA
      SELECT KOCKA
      APPEND BLANK
      GATHER NAME POLJA
      REPLACE UPLATA WITH MUPLATA
      SELECT KOCKA
      COPY STRUCTURE TO &KKOCKA2
      
      USE &MIMETAB IN 0 ALIAS ANAL ORDER 8
      
      USE &KKOCKA2 IN 0 ALIAS KOCKA2 EXCLU
      SELECT KOCKA
      *----------OVDE NALAZIMO NEZATVORENE RACUNE-----------
      *----------KUPCI--------------------------------------
      GO TOP
      MDUG=0
      MPOT=0
      DO WHILE.NOT.EOF()
         MSIFRA=SIFRA
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
         SELECT KOCKA
         SKIP
      ENDDO   
   ELSE
      SELECT KOCKA9
      MUPLATA=DUG-POT
      SCATTER NAME POLJA
      SELECT KOCKA
      APPEND BLANK
      GATHER NAME POLJA
      REPLACE UPLATA WITH MUPLATA
      SELECT KOCKA
      COPY STRUCTURE TO &KKOCKA2
      
      USE &MIMETAB IN 0 ALIAS ANAL ORDER 8
      
      USE &KKOCKA2 IN 0 ALIAS KOCKA2 EXCLU
      SELECT KOCKA
      *----------OVDE NALAZIMO NEZATVORENE RACUNE-----------
      *----------KUPCI--------------------------------------
      GO TOP
      MDUG=0
      MPOT=0
      DO WHILE.NOT.EOF()
         MSIFRA=SIFRA
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
         SELECT KOCKA
         SKIP
      ENDDO   
      *----------KRAJ KNJIZENJA DOBAVLJACA-----------   
   ENDIF
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
   USE
   SELECT KOCKA9
*----KRAJ AKO JE AUTOMATSKO ZATVARANJE----------------
