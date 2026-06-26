PUSH KEY CLEAR
IF TNACINK<>'D'
   NALP.CMD0.SETFOCUS
ELSE
   NALP2.CMD0.SETFOCUS
endif
TBRISI=1
SELECT NALP
IF EOF()
   GO BOTTOM
ENDIF   
MBRNAL=BRNAL
SET ORDER TO 2
SEEK MBRNAL
MDUG=0
MPOT=0
MISPRAVNO=1
DO WHILE.NOT.EOF()
   IF BRNAL<>MBRNAL
      EXIT
   ENDIF
   IF KONTO=SPACE(10)
      IF dug<>0.or.pot<>0
         MISPRAVNO=0
      endif
   ENDIF
   MDUG=MDUG+DUG
   MPOT=MPOT+POT
   SKIP
ENDDO

IF MDUG=MPOT.AND.MISPRAVNO=1      
   
   PUBLIC DA_NE
   DA_NE='D'
      
   DO PORUKADANE WITH ' KNJIŽENJE NALOGA '+MBRNAL
   IF DA_NE='D'   
      GO TOP

      IF TGKREDNI='1'
         KNAL='NAL'
         USE NAL IN 0 ALIAS NAL
      ELSE
         KNAL='NAL'+TGKREDNI
         USE &KNAL IN 0 ALIAS NAL
      ENDIF 
      
      IF TNACINK<>'D'
         SELECT NALP
         SEEK MBRNAL
         DO WHILE.NOT.EOF()
            IF BRNAL<>MBRNAL
               EXIT
            ENDIF
            IF DUG<>0.OR.POT<>0
               SCAttER name POLJA
               SELECT NAL
               APPEND BLANK
               GATHER NAME POLJA   
               REPLACE DATUM WITH DATE()
               REPLACE VREME WITH TIME()
               SELECT NALP
            ENDIF
            SKIP
         ENDDO
      ELSE
      *-------AKO SE KNJIZI NA DRUGI NACIN---------
         DO NALP2KNJIZI
      ENDIF   
      
      SELECT NAL
      USE      
      SELECT NALBROJ
      SEEK MBRNAL
      IF FOUND()
         REPLACE DATKNJI WITH DATE()
      ENDIF   
             
      SELECT NALP
      USE
      IF TANAL=='NALP'
         USE &KNALPA IN 0 ALIAS NALP EXCLU
      ELSE
         USE NALAP IN 0 ALIAS NALP EXCLU
      ENDIF
      SELECT NALP      
      DELETE ALL FOR BRNAL=MBRNAL
      PACK
      IF TNACINK<>'D'
         NALP.RELEASE
      ELSE
         nalp2.release
      endif
      KEYBOARD '{ENTER}'
      
   ENDIF   
ELSE
   DO PORUKAU WITH ' NALOG NIJE U RAVNOTEŽI ILI NEMA KONTA '
   SEEK BRNAL
   SET ORDER TO
   IF TNACINK<>'D'
      NALP.GRD0.COLUMN2.SETFOCUS
   ELSE
      NALP2.GRD0.COLUMN2.SETFOCUS         
   endif   
ENDIF      

POP KEY