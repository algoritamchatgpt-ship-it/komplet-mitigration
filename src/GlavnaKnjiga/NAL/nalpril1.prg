PUSH KEY CLEAR
   DO idel WITH (kkockax)
   DO idel WITH (kkockax2)
   DO idel WITH (kkockax3)
   DO idel WITH (kkockax4)

USE AAAN IN 0 ORDER 1

SELECT DATUMI
MDAT0=DAT0
MDAT1=DAT1
SELECT NALPRIL0
GO TOP
DO WHILE.NOT.EOF()
   MKONTO=KONTO
   MDP=DP
   SELECT AAAN
   LOCATE FOR KONTO=MKONTO
   IF FOUND()
      MPNAZIV=PNAZIV
      MSIFPROD=SIFPROD
      KANAL='ANAL'+ALLTRIM(MSIFPROD)
      KKANAL='ANAL'+ALLTRIM(MSIFPROD)+'.DBF'
      IF FILE(KKANAL)
         USE &kanal IN 0 ALIAS anal
         SELECT anal
         IF mdp='D'
            COPY TO &KKOCKA FOR VALUTA<MDAT0.AND.ZATVAR=' '.AND.DUG<>0
            COPY TO &KKOCKA3 FOR VALUTA>=MDAT0.AND.VALUTA<=MDAT1.AND.ZATVAR=' '.AND.DUG<>0
         ELSE
            COPY TO &KKOCKA FOR VALUTA<MDAT0.AND.ZATVAR=' '.AND.POT<>0
            COPY TO &KKOCKA3 FOR VALUTA>=MDAT0.AND.VALUTA<=MDAT1.AND.ZATVAR=' '.AND.POT<>0
         ENDIF   
            USE &KKOCKA3 IN 0 ALIAS KOCKA3 EXCLU
            SELECT KOCKA3
            INDEX ON SIFRA+BRRAC TAG SIFRAC3
            SET ORDER TO 1
            IF MDP='D'
            TOTAL ON SIFRA+BRRAC TO &KKOCKA4 FIELDS DUG
            ELSE
            TOTAL ON SIFRA+BRRAC TO &KKOCKA4 FIELDS POT
            ENDIF
            ZAP
            APPEND FROM &KKOCKA4
            USE &KKOCKA IN 0 ALIAS KOCKA EXCLU
            SELECT KOCKA
            INDEX ON SIFRA+BRRAC TAG SIFRAC
            SET ORDER TO 1
            IF MDP='D'
            TOTAL ON SIFRA+BRRAC TO &KKOCKA2 FIELDS DUG
            ELSE
            TOTAL ON SIFRA+BRRAC TO &KKOCKA2 FIELDS POT
            ENDIF
            DELETE ALL
PACK

            APPEND FROM &KKOCKA2
            GO TOP
            DO WHILE.NOT.EOF()
               MSIFRAC=SIFRA+BRRAC
               SELECT ANAL
               SET ORDER TO 3
               SEEK MSIFRAC 
               IF FOUND()
                  MDUG=0
                  MPOT=0
                  DO WHILE.NOT.EOF()_
                     IF SIFRA+BRRAC<>MSIFRAC
                        EXIT
                     ENDIF   
                     IF MDP='D'
                     MPOT=MPOT+POT
                     ELSE
                     MDUG=MDUG+DUG
                     ENDIF
                     SKIP
                  ENDDO   
                  SELECT KOCKA
                  IF MDP='D'
                     REPLACE POT WITH MPOT
                  ELSE
                     REPLACE DUG WITH MDUG
                  ENDIF
               ENDIF
               SELECT KOCKA
               SKIP
            ENDDO   
            SELECT KOCKA
            GO TOP
            DO WHILE.NOT.EOF()
               MSIFRA=SIFRA
               MBRRAC=BRRAC
               IF MDP='D'
                  MDUGPRE=DUG-POT
               ELSE
                  MPOTPRE=POT-DUG
               ENDIF
               SELECT NALPRIL
               APPEND BLANK
               REPLACE SIFRA WITH MSIFRA
               IF MDP='D'
                  REPLACE DUGPRE WITH MDUGPRE
               ELSE
                  REPLACE POTPRE WITH MPOTPRE
               ENDIF      
               REPLACE KONTO WITH MKONTO
               REPLACE DAT0 WITH MDAT0
               REPLACE DAT1 WITH MDAT1
               REPLACE PAUTO WITH '*'
               REPLACE OPIS WITH MPNAZIV
               REPLACE BRRAC WITH MBRRAC
               SELECT KOCKA
               SKIP
            ENDDO   
            USE
            *-----------OCEKIVANI PRILIV ZA PERIOD
            SELECT KOCKA3
            GO TOP
            DO WHILE.NOT.EOF()
               MSIFRAC=SIFRA+BRRAC
               SELECT ANAL
               SET ORDER TO 3
               SEEK MSIFRAC 
               IF FOUND()
                  MDUG=0
                  MPOT=0
                  DO WHILE.NOT.EOF()_
                     IF SIFRA+BRRAC<>MSIFRAC
                        EXIT
                     ENDIF   
                     IF MDP='D'
                        MPOT=MPOT+POT
                     ELSE
                        MDUG=MDUG+DUG
                     ENDIF
                     SKIP
                  ENDDO   
                  SELECT KOCKA3
                  IF MDP='D'
                     REPLACE POT WITH MPOT
                  ELSE
                     REPLACE DUG WITH MDUG
                  ENDIF
               ENDIF
               SELECT KOCKA3
               SKIP
            ENDDO   
            SELECT KOCKA3
            GO TOP
            DO WHILE.NOT.EOF()
               MSIFRA=SIFRA
               MBRRAC=BRRAC
               IF MDP='D'
                  MDUG=DUG-POT
               ELSE
                  MPOT=POT-DUG
               ENDIF
               SELECT NALPRIL
               APPEND BLANK
               REPLACE SIFRA WITH MSIFRA
               IF MDP='D'
                  REPLACE DUG WITH MDUG
               ELSE
                  REPLACE POT WITH MPOT
               ENDIF   
               REPLACE PAUTO WITH '*'
               REPLACE OPIS WITH MPNAZIV
               REPLACE KONTO WITH MKONTO
               REPLACE DAT0 WITH MDAT0
               REPLACE DAT1 WITH MDAT1
               REPLACE BRRAC WITH MBRRAC
               SELECT KOCKA3
               SKIP
            ENDDO   
            USE
         ENDIF 
         SELECT ANAL
         USE   
      ENDIF
  
   SELECT NALPRIL0
   SKIP
ENDDO   
SELECT AAAN
USE
USE AN0 IN 0 ORDER 1
SELECT NALPRIL
SET RELATION TO SIFRA INTO AN0 ADDITIVE
REPLACE ALL NAZIV WITH AN0.NAZIV
SET RELATION TO
SELECT AN0
USE
SELECT NALPRIL
POP KEY