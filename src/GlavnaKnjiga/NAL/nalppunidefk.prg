   USE nalpdefk IN 0 exclu
   SELECT nalpdefk 
   zap
   USE aaan IN 0
   SELECT aaan
   GO top
   DO while.not.eof()
      msifprod=sifprod
      mpnaziv=pnaziv
      mprikaz=prikaz
      mkonto=konto
      mdp=dp
      msifarnik=sifarnik
      mdevizNO=DEVIZNO
      mimetabele='anal'+ALLTRIM(STR(RECNO(),2,0))
      IF mprikaz<>'N'
         SELECT nalpdefk
         IF Mkonto<>SPACE(10)
               APPEND BLANK
               replace sifprod WITH msifprod
               replace pnaziv WITH mpnaziv
               replace konto WITH mkonto
               replace  IMETABELE WITH MIMETABELE
               replace vrsta WITH 'AN'
               REPLACE SIFARNIK WITH MSIFARNIK
               REPLACE DP WITH MDP
               REPLACE DEVIZNO WITH MDEVIZNO
         endif   
      endif   
      SELECT aaan
      SKIP
   enddo   
   SELECT aaan
   USE
   SELECT nalpdefk 
   USE aatv IN 0
   SELECT aatv
   GO top
   DO while.not.eof()
      msifprod=sifprod
      mpnaziv=pnaziv
      mprikaz=prikaz
      mkonto=kontopaz
      msifarnik=sifarnik
      mimetabele='tvtm'+ALLTRIM(STR(RECNO(),2,0))
      mdok='V'+ALLTRIM(STR(RECNO(),2,0))      
      IF mprikaz<>'N'
         SELECT nalpdefk
         IF Mkonto<>SPACE(10)
               APPEND BLANK
               replace sifprod WITH msifprod
               replace pnaziv WITH mpnaziv
               replace konto WITH mkonto
               replace  IMETABELE WITH MIMETABELE
               replace vrsta WITH 'VP'
               REPLACE SIFARNIK WITH MSIFARNIK
               REPLACE DOK WITH MDOK
         endif   
      endif   
      SELECT aaTV
      SKIP
   enddo   
   SELECT aaTV
   USE

   USE aatm IN 0
   SELECT aatm
   GO top
   DO while.not.eof()
      msifprod=sifprod
      mpnaziv=pnaziv
      mprikaz=prikaz
      mkonto=kontopaz
      msifarnik=sifarnik
      mimetabele='tm'+ALLTRIM(STR(RECNO(),2,0))
      mdok='M'+ALLTRIM(STR(RECNO(),2,0))      
      IF mprikaz<>'N'
         SELECT nalpdefk
         IF Mkonto<>SPACE(10)
               APPEND BLANK
               replace sifprod WITH msifprod
               replace pnaziv WITH mpnaziv
               replace konto WITH mkonto
               replace  IMETABELE WITH MIMETABELE
               replace vrsta WITH 'MP'
               REPLACE SIFARNIK WITH MSIFARNIK
               REPLACE DOK WITH MDOK
         endif   
      endif   
      SELECT aaTM
      SKIP
   enddo   
   SELECT aaTM
   USE
   SELECT NALPDEFK
   USE
