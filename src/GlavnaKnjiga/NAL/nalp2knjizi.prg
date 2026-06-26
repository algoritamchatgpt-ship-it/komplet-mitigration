 *---------KNJIZENJE NALOGA DRUGI NACIN---------
         DO IDEL WITH (KKOCKAX)
         DO IDEL WITH (KKOCKAX2)         
         SELECT NALP
         COPY STRUCTURE TO &KKOCKA9
         COPY STRUCTURE TO &KKOCKA2         
         COPY STRUCTURE TO &KKOCKA3
         COPY STRUCTURE TO &KKOCKA4
         COPY STRUCTURE TO &KKOCKA5
         COPY STRUCTURE TO &KKOCKA6
         COPY STRUCTURE TO &KKOCKA7
                           
         USE &KKOCKA9 IN 0 ALIAS KOCKA9 EXCLUSIVE
         USE &KKOCKA2 IN 0 ALIAS KOCKA2 EXCLUSIVE
         USE &KKOCKA3 IN 0 ALIAS KOCKA3 EXCLUSIVE
         SELECT NALP
         SEEK MBRNAL
         DO WHILE.NOT.EOF()
            IF BRNAL<>MBRNAL
               EXIT
            ENDIF
            IF DUG<>0.OR.POT<>0
               SCAttER name POLJA
               MVRSTA=VRSTA
               DO CASE MVRSTA
                  CASE MVRSTA='AN'
                     SELECT KOCKA9
                     APPEND BLANK
                     GATHER NAME POLJA
                  CASE MVRSTA='MP'
                     SELECT KOCKA2
                     APPEND BLANK
                     GATHER NAME POLJA
                  OTHERWISE
                     SELECT KOCKA3
                     APPEND BLANK
                     GATHER NAME POLJA
               ENDCASE   
            ENDIF
            SELECT NALP
            SKIP
         ENDDO
         SELECT KOCKA9
         INDEX ON KONTO TAG KONTO
         TOTAL ON KONTO TO &KKOCKA4 FOR DUG>0 FIELDS DUG
         TOTAL ON konto TO &KKOCKA5 FOR DUG<0 FIELDS DUG         
         TOTAL ON konto TO &KKOCKA6 FOR POT>0 FIELDS POT
         TOTAL ON konto TO &KKOCKA7 FOR POT<0 FIELDS POT         
        * USE
         SELECT KOCKA2
         USE
         SELECT KOCKA3
         USE
         SELECT NALP
         DELETE ALL FOR BRNAL=MBRNAL
         PACK
         APPEND FROM &KKOCKA3
         APPEND FROM &KKOCKA4           
         APPEND FROM &KKOCKA5           
         APPEND FROM &KKOCKA6           
         APPEND FROM &KKOCKA7                             
         APPEND FROM &KKOCKA2                              
         SEEK MBRNAL
         DO WHILE.NOT.EOF()
            IF BRNAL<>MBRNAL
               EXIT
            ENDIF
            SCAttER name POLJA
            SELECT NAL
            APPEND BLANK
            GATHER NAME POLJA   
            REPLACE DATUM WITH DATE()
            REPLACE VREME WITH TIME()
            SELECT NALP
            SKIP
         ENDDO
         *-----------KNJIZENJE KEPU-----------
         USE &KKOCKA2 IN 0 ALIAS KOCKA2 EXCLUSIVE
         SELECT KOCKA2
         IF RECCOUNT()>0
            GO TOP
            DO WHILE.NOT.EOF()
               MIMETAB=ALLTRIM(IMETABELE)
               SCATTER NAME POLJA
               MUPLACENO=POT
               MDINRAZDUZ=DINRAZDUZ
               MOPIS=OPISU
               MDATRAZDUZ=DATRAZDUZ
               mmimetab=mimetab+'.dbf'
               IF FILE(mmimetab)
                  USE &MIMETAB IN 0 ALIAS IME
                  SELECT IME
                  APPEND BLANK
                  GATHER NAME POLJA
                  REPLACE UPLACENO WITH MUPLACENO
                  REPLACE OPIS WITH MOPIS
                  REPLACE VRSTA WITH ' '
                  IF MDINRAZDUZ<>0
                     APPEND BLANK
                     GATHER NAME POLJA
                     REPLACE DATDOK WITH MDATRAZDUZ
                     REPLACE OPIS WITH 'DNEVNI PROMET OD '+DTOC(MDATRAZDUZ)
                     REPLACE UPLACENO WITH 0
                     REPLACE VREDPROD WITH MDINRAZDUZ
                  ENDIF
                  USE
               endif   
               SELECT KOCKA2
               SKIP
            ENDDO   
         ENDIF
         SELECT KOCKA2
         USE
         *-----------KNJIZENJE ANALITIKE-----------
         SELECT KOCKA9
         IF RECCOUNT()>0
            GO TOP
            DO WHILE.NOT.EOF()
               IF PKNJAN='D'
                  IF PKANAUTOZ='D'
                     DO NALP2AUTOZATVAR
                  ELSE
                     MIMETAB=ALLTRIM(IMETABELE)
                     mmimetab=mimetab+'.dbf'
                     SCATTER NAME POLJA
                     IF FILE(MMIMETAB)
                        USE &MIMETAB IN 0 ALIAS IME
                        SELECT IME
                        APPEND BLANK
                        GATHER NAME POLJA
                        USE
                     ENDIF   
                  ENDIF
               ELSE    
                  MIMETAB=ALLTRIM(IMETABELE)
                  mmimetab=mimetab+'.dbf'
                  SCATTER NAME POLJA
                  IF FILE(MMIMETAB)
                     USE &MIMETAB IN 0 ALIAS IME
                     SELECT IME
                     APPEND BLANK
                     GATHER NAME POLJA
                     USE
                  ENDIF   
               ENDIF
               SELECT KOCKA9
               SKIP
            ENDDO   
         ENDIF
         SELECT KOCKA9
         USE
         SELECT NALP
         
         
