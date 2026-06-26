PUSH KEY CLEAR
TBRISI=1
DO IDEL WITH (KKOCKAX)
SELECT NALP
IF EOF()
   GO BOTTOM
ENDIF   
      GO TOP
      IF TGKREDNI='1'
         KNAL='NAL'
         USE NAL IN 0 ALIAS NAL
      ELSE
         KNAL='NAL'+MAANAL
         USE &KNAL IN 0 ALIAS NAL
      ENDIF 
      SELECT NALP
      SET ORDER TO 2
      TOTAL ON BRNAL TO &KKOCKA  FIELDS DUG,POT FOR KONTO<>SPACE(10)
      USE &KKOCKA IN 0 ALIAS KOCKA EXCLUSIVE
      SELECT KOCKA
      GO TOP
      DO WHILE.NOT.EOF()
         MBRNAL=BRNAL
         IF DUG=POT
            SELECT NALP
            SEEK MBRNAL
            IF FOUND()
               DO WHILE.NOT.EOF()
                  IF BRNAL<>MBRNAL
                     EXIT
                  ENDIF   
                  IF KONTO<>'XXXXXXXXXX'
                     IF DUG<>0.OR.POT<>0
                        SCAttER name POLJA
                        SELECT NAL
                        APPEND BLANK
                        GATHER NAME POLJA   
                        REPLACE DATUM WITH DATE()
                        REPLACE VREME WITH TIME()
                        SELECT NALBROJ
                        SEEK MBRNAL
                        IF FOUND()
                           REPLACE DATKNJI WITH DATE()
                        ENDIF   
                     ENDIF
                     SELECT NALP
                     REPLACE KONTO WITH 'XXXXXXXXXX'
                  ENDIF
                  SKIP
               ENDDO
            ENDIF
         ENDIF   
         SELECT KOCKA
         SKIP
      ENDDO   
      SELECT KOCKA
      USE     
      SELECT NAL
      USE      
      SELECT NALP
      USE
      INKEY(3)
      IF TANAL=='NALP'
         USE &KNALPA IN 0 ALIAS NALP EXCLU
      ELSE
         USE NALAP IN 0 ALIAS NALP EXCLU
      ENDIF
      SELECT NALP      
      DELETE ALL FOR KONTO='XXXXXXXXXX'
      PACK
      POP KEY
      NALP.RELEASE
      KEYBOARD '{ENTER}'
