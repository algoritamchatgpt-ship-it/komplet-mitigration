PUSH KEY CLEAR
   *------PROVERA POSTOJANJA KONTA U ANALITICI-------
   *SET STEP ON    
   mkonto=konto
   SELECT nalpdefk
   LOCATE FOR konto=mkonto   
   IF FOUND()
      *---zavisno od situacije odredjujemo formu************
      mAsifprod=sifprod
      mApnaziv=pnaziv                                          
      mAdevizno=devizno
      mAsifarnik=sifarnik
      mAdok=dok
      mAvrsta=vrsta
      mAimetab=imetabele
      mAdp=dp
      SELECT NALP
*      ttanal=tanal 

      IF MAVRSTA='AN'
          DO FORM NALP2KART WITH MADEVIZNO,maimetab,MAVRSTA,mAsifprod,madp
      ENDIF    
      IF MAVRSTA='MP'
          DO FORM NALP2KARTMP WITH MADEVIZNO,maimetab,MAVRSTA,mAsifprod
      ENDIF    
      
      SELECT NALP
*      TANAL=ttanal
*      KEYBOARD '{RIGHTARROW}'
*      KEYBOARD '{LEFTARROW}'
   ELSE
      SELECT nalp
   ENDIF
      
POP KEY
