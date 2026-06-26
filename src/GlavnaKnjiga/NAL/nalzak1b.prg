parameters mkonp,mlenp,mdat0,mdat1,mnal1,mnal2,mnal3,mnal4,mdok,mmp,mmtr,mcifre,mnedovrseni,mautomatski,mmkonp,mlenkonp,knal
set exact on
knalizv=mdata01+'\'+'nalizv'+operater
knalizvx=mdata01+'\'+'nalizv'+operater+'.cdx'

kkocka=mdata01+'\'+'kocka'+operater
kkockap=mdata01+'\'+'kockap'+operater
kkocka1=mdata01+'\'+'kocka1'+operater
kkocka2=mdata01+'\'+'kocka2'+operater
kkocka3=mdata01+'\'+'kocka3'+operater
kkocka4=mdata01+'\'+'kocka4'+operater
kkocka5=mdata01+'\'+'kocka5'+operater
kkocka6=mdata01+'\'+'kocka6'+operater
kkocka7=mdata01+'\'+'kocka7'+operater
kkocka8=mdata01+'\'+'kocka8'+operater

kkockax=mdata01+'\'+'kocka'+operater+'.cdx'
kkockapx=mdata01+'\'+'kockap'+operater+'.cdx'
kkocka1x=mdata01+'\'+'kocka1'+operater+'.cdx'
kkocka2x=mdata01+'\'+'kocka2'+operater+'.cdx'
kkocka3x=mdata01+'\'+'kocka3'+operater+'.cdx'
kkocka4x=mdata01+'\'+'kocka4'+operater+'.cdx'
kkocka5x=mdata01+'\'+'kocka5'+operater+'.cdx'
kkocka6x=mdata01+'\'+'kocka6'+operater+'.cdx'
kkocka7x=mdata01+'\'+'kocka7'+operater+'.cdx'
kkocka8x=mdata01+'\'+'kocka8'+operater+'.cdx'

do idel with (knalizvx)
do idel with (kkockax)
do idel with (kkockapx)
do idel with (kkocka1x)
do idel with (kkocka2x)
do idel with (kkocka3x)
do idel with (kkocka4x)
do idel with (kkocka5x)
do idel with (kkocka6x)
do idel with (kkocka7x)
do idel with (kkocka8x)
use firma in 0
use &knal in 0 alias nal
select nal
*-----------pocetno stanje---------
copy structure to &kkocka
copy structure to &kkocka2
copy structure to &kkocka3

select * from nal where ;
                      (substr(konto,1,mlenkonp)=mmkonp.or.mkonp=space(8)).and.;
                      (dok=mdok.or.mdok=space(3)).and.;
                      (mp=mmp.or.mmp=space(2)).and.;
                      (mtr=mmtr.or.mmtr=0).and.;
                      (dtos(datdok)<=dtos(mdat0));
                      into table &kkocka
                      use
                      

if mnedovrseni='D'
   select * from nalpn where ;
                      (substr(konto,1,mlenkonp)=mmkonp.or.mkonp=space(8)).and.;
                      (dok=mdok.or.mdok=space(3)).and.;
                      (mp=mmp.or.mmp=space(2)).and.;
                      (mtr=mmtr.or.mmtr=0).and.;
                      (dtos(datdok)<=dtos(mdat0));
                      into table &kkocka2
                      use
endif

if mautomatski='D'
   select * from nalap where ;
                      (substr(konto,1,mlenkonp)=mmkonp.or.mkonp=space(8)).and.;
                      (dok=mdok.or.mdok=space(3)).and.;
                      (mp=mmp.or.mmp=space(2)).and.;
                      (mtr=mmtr.or.mmtr=0).and.;
                      (dtos(datdok)<=dtos(mdat0));
                      into table &kkocka3
                      use
endif

use &kkocka in 0 alias kocka exclusive
select kocka
IF MNAL1<>SPACE(6)
   delete all FOR brnal=mnal1
endif 
IF MNAL2<>SPACE(6)
   delete all FOR brnal=mnal2
endif 
IF MNAL3<>SPACE(6)
   delete all FOR brnal=mnal3
endif 
IF MNAL4<>SPACE(6)
   delete all FOR brnal=mnal4
endif 
pack                      

index on konto tag konto
set order to 1
total on konto to &kkocka4 fields dug,pot
zap
append from &kkocka4

use &kkocka2 in 0 alias kocka2 exclusive
select kocka2
IF MNAL1<>SPACE(6)
   delete all FOR brnal=mnal1
endif 
IF MNAL2<>SPACE(6)
   delete all FOR brnal=mnal2
endif 
IF MNAL3<>SPACE(6)
   delete all FOR brnal=mnal3
endif 
IF MNAL4<>SPACE(6)
   delete all FOR brnal=mnal4
endif 
pack                      

index on konto tag konto
set order to 1
total on konto to &kkocka4 fields dug,pot
zap
append from &kkocka4
use

use &kkocka3 in 0 alias kocka3 exclusive
select kocka3
IF MNAL1<>SPACE(6)
   delete all FOR brnal=mnal1
endif 
IF MNAL2<>SPACE(6)
   delete all FOR brnal=mnal2
endif 
IF MNAL3<>SPACE(6)
   delete all FOR brnal=mnal3
endif 
IF MNAL4<>SPACE(6)
   delete all FOR brnal=mnal4
endif 
pack                      

index on konto tag konto
set order to 1
total on konto to &kkocka4 fields dug,pot
zap
append from &kkocka4
use

select kocka
append from &kkocka2
append from &kkocka3

use

*-----------unos prometa---------
select nal
copy structure to &kkocka2
copy structure to &kkocka3
copy structure to &kkocka5

select * from nal where ;
                      (substr(konto,1,mlenkonp)=mmkonp.or.mkonp=space(8)).and.;
                      (dok=mdok.or.mdok=space(3)).and.;
                      (mp=mmp.or.mmp=space(2)).and.;
                      (mtr=mmtr.or.mmtr=0).and.;
                      (dtos(datdok)>dtos(mdat0).and.dtos(datdok)<=dtos(mdat1));
                      into table &kkocka5
                      use
                      

if mnedovrseni='D'
   select * from nalpn where ;
                      (substr(konto,1,mlenkonp)=mmkonp.or.mkonp=space(8)).and.;
                      (dok=mdok.or.mdok=space(3)).and.;
                      (mp=mmp.or.mmp=space(2)).and.;
                      (mtr=mmtr.or.mmtr=0).and.;
                      (dtos(datdok)>dtos(mdat0).and.dtos(datdok)<=dtos(mdat1));
                      into table &kkocka2
                      use
endif

if mautomatski='D'
   select * from nalap where ;
                      (substr(konto,1,mlenkonp)=mmkonp.or.mkonp=space(8)).and.;
                      (dok=mdok.or.mdok=space(3)).and.;
                      (mp=mmp.or.mmp=space(2)).and.;
                      (mtr=mmtr.or.mmtr=0).and.;
                      (dtos(datdok)>dtos(mdat0).and.dtos(datdok)<=dtos(mdat1));
                      into table &kkocka3
                      use
endif

use &kkocka5 in 0 alias kocka5 exclusive
select kocka5

IF MNAL1<>SPACE(6)
   delete all FOR brnal=mnal1
endif 
IF MNAL2<>SPACE(6)
   delete all FOR brnal=mnal2
endif 
IF MNAL3<>SPACE(6)
   delete all FOR brnal=mnal3
endif 
IF MNAL4<>SPACE(6)
   delete all FOR brnal=mnal4
endif 
pack                      

index on konto tag konto
set order to 1
total on konto to &kkocka4 fields dug,pot
zap
append from &kkocka4

use &kkocka2 in 0 alias kocka2 exclusive
select kocka2
IF MNAL1<>SPACE(6)
   delete all FOR brnal=mnal1
endif 
IF MNAL2<>SPACE(6)
   delete all FOR brnal=mnal2
endif 
IF MNAL3<>SPACE(6)
   delete all FOR brnal=mnal3
endif 
IF MNAL4<>SPACE(6)
   delete all FOR brnal=mnal4
endif 
pack                      
index on konto tag konto
set order to 1
total on konto to &kkocka4 fields dug,pot
zap
append from &kkocka4
use

use &kkocka3 in 0 alias kocka3 exclusive
select kocka3
IF MNAL1<>SPACE(6)
   delete all FOR brnal=mnal1
endif 
IF MNAL2<>SPACE(6)
   delete all FOR brnal=mnal2
endif 
IF MNAL3<>SPACE(6)
   delete all FOR brnal=mnal3
endif 
IF MNAL4<>SPACE(6)
   delete all FOR brnal=mnal4
endif 
pack                      
index on konto tag konto
set order to 1
total on konto to &kkocka4 fields dug,pot
zap
append from &kkocka4
use

select kocka5
append from &kkocka2
append from &kkocka3

use

on error
use &knalizv alias nalizv exclu in 0
select nalizv
zap
use
do iind with knalizv,'k1','k2','k3','k4','k5','k6','konto'
use &knalizv alias nalizv exclu in 0
select nalizv
on error do greske with ;
   error( ), message( ), message(1), program( ), lineno( )

select nalizv
set order to 7
go top
append from &kkocka
replace all pdug with dug
replace all ppot with pot
replace all dug with 0
replace all pot with 0
append from &kkocka5

do idel with (kkockax)
select nalizv

total on konto fields pdug,ppot,dug,pot to &kkocka
zap
append from &kkocka
replace all k1 with substr(konto,1,1)
replace all k2 with substr(konto,1,2)
replace all k3 with substr(konto,1,3)
replace all k4 with substr(konto,1,4)
replace all k5 with substr(konto,1,5)
replace all k6 with substr(konto,1,6)
replace all psaldo with pdug-ppot
replace all saldo with dug-pot
replace all udug with pdug+dug
replace all upot with ppot+pot
replace all usaldo with udug-upot

use &kkon1 alias konto in 0 order 1 exclu
select konto
select nalizv
set relation to k1 into konto additive
replace all k1naziv with konto->naziv
set relation to
select konto
use
use &kkon2 alias konto in 0 order 1 exclu
select konto
select nalizv
set relation to k2 into konto additive
replace all k2naziv with konto->naziv
set relation to
select konto
use

use &kkon3 alias konto in 0 order 1 exclu
select konto
select nalizv
set relation to k3 into konto additive
replace all k3naziv with konto->naziv
set relation to
select konto
use

use &kkon4 alias konto in 0 order 1 exclu
select konto
select nalizv
set relation to k4 into konto additive
replace all k4naziv with konto->naziv
set relation to
select konto
use

use &kkon5 alias konto in 0 order 1 exclu
select konto
select nalizv
set relation to k5 into konto additive
replace all k5naziv with konto->naziv
set relation to
select konto
use

use &kkon6 alias konto in 0 order 1 exclu
select konto
select nalizv
set relation to k6 into konto additive
replace all k6naziv with konto->naziv
set relation to
select konto
use

use &kkonto alias konto in 0 order 1
select konto
select nalizv
set relation to konto into konto additive
replace all knaziv with konto->naziv
set relation to
select konto
use
select nalizv
if mcifre='D'
   replace all pdug with round(pdug/1000,2)
   replace all ppot with round(ppot/1000,2)
   replace all psaldo with round(psaldo/1000,2)
   replace all dug with round(dug/1000,2)
   replace all pot with round(pot/1000,2)
   replace all saldo with round(saldo/1000,2)
   replace all udug with round(udug/1000,2)
   replace all upot with round(upot/1000,2)
   replace all usaldo with round(usaldo/1000,2)
   replace all pddug with round(pddug/1000,2)
   replace all pdpot with round(pdpot/1000,2)
   replace all pdsaldo with round(pdsaldo/1000,2)

endif

do form nalzak2
set exact off

pop key