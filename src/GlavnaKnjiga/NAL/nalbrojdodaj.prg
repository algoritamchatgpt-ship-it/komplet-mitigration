push key clear
   select nalbroj
   append blank
   if nalb='TVK'
      replace datum with kaldatdok
   endif
   if nalb='TVK'
      replace datum with kaldatdok
   endif
   if tkoji='U'
      replace datum with kaldatdok
   endif
nalbroj.grd0.setfocus
nalbroj.refresh
do form nalbrojk
nalbroj.grd0.setfocus
pop key
nalbroj.refresh
