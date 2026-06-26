push key clear
select nalvrsta
append blank
replace vrnal with str(recno(),3,0)
nalvrsta.refresh

do form nalvrstak

nalvrsta.grd0.setfocus
pop key
nalvrsta.refresh
