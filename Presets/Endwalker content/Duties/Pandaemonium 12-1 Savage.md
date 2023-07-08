[International] [Script] Wings unsafe spots. Accurately accounts for snapshot. Now with accurate pixel perfect line.
```
https://raw.githubusercontent.com/PunishXIV/Splatoon/main/SplatoonScripts/Duties/Endwalker/P12S%20Wing%20Cleaves.cs
```

[Internaltional] [Script] Limit Cut helper. Shows you when you're baiting puddles (assuming you're taking them in pairs, 13 24 57 68), and shows you when to go out for laser bait.
```
https://raw.githubusercontent.com/PunishXIV/Splatoon/main/SplatoonScripts/Duties/Endwalker/P12S%20Limit%20Cut.cs
```

[International] [Script] Superchain Theory visualiser. CLUTTTERS SCREEN HEAVILY. Includes spread buff visualisation (can be disabled).
```
https://raw.githubusercontent.com/PunishXIV/Splatoon/main/SplatoonScripts/Duties/Endwalker/P12S%20Superchain.cs
```

[International] [Script] Tether visualiser for Paradeigma 2/3 (Engravement 1/3), comes with 3 color modes (Dark/Light, Unstretched vs Stretched and 4 different colors).
```
https://raw.githubusercontent.com/PunishXIV/Splatoon/main/SplatoonScripts/Duties/Endwalker/P12S%20Tethers.cs
```

[International] Adds line AOE
```
~Lv2~{"Name":"P12S Adds","Group":"P12S","ZoneLockH":[1154],"ElementsL":[{"Name":"","type":3,"refY":45.0,"radius":5.0,"color":1342177535,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33518],"refActorUseCastTime":true,"refActorCastTimeMax":4.69,"includeRotation":true}]}
```

[International] Limit cut number extender
```
~Lv2~{"Name":"P12S Limit cut","Group":"P12S","ZoneLockH":[1154],"ElementsL":[{"Name":"1","type":1,"radius":0.0,"color":3371299322,"overlayBGColor":3137339392,"overlayTextColor":4294311819,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":" 1 ","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num01_s8p.avfx","refActorVFXMax":30000},{"Name":"2","type":1,"radius":0.0,"color":3371299322,"overlayBGColor":3137339392,"overlayTextColor":4278255601,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":" 2","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num02_s8p.avfx","refActorVFXMax":30000},{"Name":"3","type":1,"radius":0.56,"color":3371299322,"overlayBGColor":3137339392,"overlayTextColor":4294311819,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":" 3 ","refActorComparisonType":7,"Filled":true,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num03_s8p.avfx","refActorVFXMax":30000},{"Name":"4","type":1,"radius":0.56,"color":3371299322,"overlayBGColor":3137339392,"overlayTextColor":4278255601,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":" 4  ","refActorComparisonType":7,"Filled":true,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num04_s8p.avfx","refActorVFXMax":30000},{"Name":"5","type":1,"radius":0.56,"color":3371299322,"overlayBGColor":3137339392,"overlayTextColor":4294311819,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":" 5 ","refActorComparisonType":7,"Filled":true,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num05_s8t.avfx","refActorVFXMax":30000},{"Name":"6","type":1,"radius":0.56,"color":3371299322,"overlayBGColor":3137339392,"overlayTextColor":4278255601,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":" 6  ","refActorComparisonType":7,"Filled":true,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num06_s8t.avfx","refActorVFXMax":30000},{"Name":"7","type":1,"radius":0.56,"color":3371299322,"overlayBGColor":3137339392,"overlayTextColor":4294311819,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":" 7 ","refActorComparisonType":7,"Filled":true,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num07_s8t.avfx","refActorVFXMax":30000},{"Name":"8","type":1,"radius":0.56,"color":3371299322,"overlayBGColor":3137339392,"overlayTextColor":4278255601,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":" 8 ","refActorComparisonType":7,"Filled":true,"refActorVFXPath":"vfx/lockon/eff/sph_lockon2_num08_s8t.avfx","refActorVFXMax":30000}]}
```

[International] [Untested] Tower finder
```
~Lv2~{"Name":"find tower","Group":"P12S","ZoneLockH":[1154],"ElementsL":[{"Name":"tower","type":1,"radius":3.0,"thicc":7.5,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33550,33551,33554],"refActorCastTimeMax":55.800003}]}
```

[International] Party in/out
```
~Lv2~{"Name":"P12S Party In/Out","Group":"P12S","ZoneLockH":[1154],"ElementsL":[{"Name":"Party Out","type":1,"radius":9.0,"color":1677721855,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayFScale":2.0,"overlayText":"<<< PARTY OUT >>>","refActorName":"*","refActorRequireCast":true,"refActorCastId":[33535],"onlyTargetable":true,"Filled":true},{"Name":"Party In","type":1,"radius":7.0,"Donut":25.0,"overlayTextColor":4294967295,"overlayFScale":2.0,"thicc":3.0,"overlayText":">>> Party IN <<<","refActorName":"*","refActorRequireCast":true,"refActorCastId":[33534],"onlyTargetable":true}]}
```

[International] Simple debuff helper (tower drop/laser)
```
~Lv2~{"Name":"P12S Debuff assist","Group":"P12S","ZoneLockH":[1154],"ElementsL":[{"Name":"Dark laser","type":1,"radius":0.0,"overlayBGColor":4278190080,"overlayTextColor":4293525759,"overlayVOffset":2.0,"overlayFScale":1.5,"overlayPlaceholders":true,"thicc":0.0,"overlayText":"Dark laser\\n<<< LEFT","refActorRequireBuff":true,"refActorBuffId":[3582],"refActorType":1},{"Name":"Light laser","type":1,"radius":0.0,"overlayBGColor":4278190080,"overlayTextColor":4278253567,"overlayVOffset":2.0,"overlayFScale":1.5,"overlayPlaceholders":true,"thicc":0.0,"overlayText":"Light laser\\nRIGHT >>>","refActorRequireBuff":true,"refActorBuffId":[3581],"refActorType":1},{"Name":"Light tower","type":1,"radius":2.5,"Donut":0.5,"color":3355503359,"overlayBGColor":4278190080,"overlayTextColor":4278253567,"overlayVOffset":2.0,"overlayFScale":1.5,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[3579]},{"Name":"Dark tower","type":1,"radius":2.5,"Donut":0.5,"color":3372155119,"overlayBGColor":4278190080,"overlayTextColor":4278253567,"overlayVOffset":2.0,"overlayFScale":1.5,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[3580]},{"Name":"Light tower","type":1,"radius":2.5,"Donut":0.5,"color":3355503359,"overlayBGColor":4278190080,"overlayTextColor":4278253567,"overlayFScale":1.5,"overlayText":"WARNING!","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[3579],"refActorUseBuffTime":true,"refActorBuffTimeMax":2.0},{"Name":"Dark tower","type":1,"radius":2.5,"Donut":0.5,"color":3372155119,"overlayBGColor":4278190080,"overlayTextColor":4294902005,"overlayFScale":1.5,"overlayText":"WARNING!","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[3580],"refActorUseBuffTime":true,"refActorBuffTimeMax":2.0}]}
```

[International] Unsafe tile visualiser
```
~Lv2~{"Name":"P12S Platform break","Group":"P12S","ZoneLockH":[1154],"DCond":5,"ElementsL":[{"Name":"","type":3,"refY":5.0,"offY":-5.0,"radius":10.0,"color":603979775,"refActorDataID":16229,"refActorComparisonType":3,"includeRotation":true,"onlyVisible":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.7,"Match":">33503)"}]}
```

[International] Boss line AOE
```
~Lv2~{"Name":"P12S Boss line","Group":"P12S","ZoneLockH":[1154],"ElementsL":[{"Name":"","type":3,"refY":20.0,"offY":-20.0,"radius":7.5,"color":2013329407,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33539],"refActorUseCastTime":true,"refActorCastTimeMax":4.69,"refActorUseOvercast":true,"includeRotation":true}]}
```

[International] First adds line AOE for tank invuln strat for non-tanks
```
~Lv2~{"Name":"P12S First adds (tank invuln)","Group":"P12S","ZoneLockH":[1154],"DCond":5,"ElementsL":[{"Name":"","type":3,"refY":45.0,"radius":5.0,"color":1342242805,"refActorDataID":16172,"refActorComparisonType":3,"includeRotation":true,"onlyVisible":true}],"UseTriggers":true,"Triggers":[{"Duration":42.0}]}
```

[International] Dark and Light debuffs as a dot on your body (works for phase 2 too)
```
~Lv2~{"Name":"P12S DarkLight","Group":"P12S","ZoneLockH":[693,1154],"ElementsL":[{"Name":"Light","type":1,"offZ":0.5,"radius":0.0,"color":3355505151,"thicc":16.4,"refActorRequireBuff":true,"refActorBuffId":[3576],"refActorType":1},{"Name":"Dark","type":1,"offZ":0.5,"radius":0.0,"color":3372155131,"thicc":16.4,"refActorRequireBuff":true,"refActorBuffId":[3577],"refActorType":1}]}
```
