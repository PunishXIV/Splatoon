[International] [Script] [Beta] Classical Concept solver (doesn't includes BPOG solver)
```
https://raw.githubusercontent.com/PunishXIV/Splatoon/main/SplatoonScripts/Duties/Endwalker/P12S%20Classical%20Concepts.cs
```

[International] [Script] [Beta] Pangenesis helper
```
https://raw.githubusercontent.com/PunishXIV/Splatoon/main/SplatoonScripts/Duties/Endwalker/P12S%20Pangenesis.cs
```

[International] Classical concepts cube AOE
```
~Lv2~{"Name":"P12S2 Cube AOE","Group":"P12S2","ZoneLockH":[1154],"ElementsL":[{"Name":"","type":1,"radius":4.0,"color":1509949695,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33587],"refActorUseCastTime":true,"refActorCastTimeMax":2.7,"Filled":true}]}
```

[International] [Beta] Gaiaochos line cast
```
~Lv2~{"Name":"P12S2 Gaiaochos line","Group":"P12S2","ZoneLockH":[1154],"ElementsL":[{"Name":"","type":3,"refY":17.0,"radius":3.0,"color":1342177535,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33584],"refActorUseCastTime":true,"refActorCastTimeMax":6.69,"includeRotation":true}]}
```

[International] [Beta] Geocentrism spin
```
~Lv2~{"Name":"P12S2 Geocentrism","Group":"P12S2","ZoneLockH":[1154],"ElementsL":[{"Name":"donut","type":1,"offY":10.0,"radius":2.0,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33578],"includeRotation":true},{"Name":"donut","type":1,"offY":10.0,"radius":3.0,"Donut":4.0,"color":2013266175,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33578],"FillStep":0.1,"includeRotation":true},{"Name":"Vertical","type":3,"refX":-5.0,"refY":17.0,"offX":-5.0,"offY":3.0,"radius":2.0,"color":2013266175,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33577],"includeRotation":true},{"Name":"Vertical","type":3,"refX":5.0,"refY":17.0,"offX":5.0,"offY":3.0,"radius":2.0,"color":2013266175,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33577],"includeRotation":true},{"Name":"Vertical","type":3,"refY":17.0,"offY":3.0,"radius":2.0,"color":2013266175,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33577],"includeRotation":true},{"Name":"Horizontal","type":3,"refY":7.0,"offY":3.0,"radius":7.0,"color":2013266175,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33579],"includeRotation":true},{"Name":"Horizontal","type":3,"refY":12.0,"offY":8.0,"radius":7.0,"color":2013266175,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33579],"includeRotation":true},{"Name":"Horizontal","type":3,"refY":17.0,"offY":13.0,"radius":7.0,"color":2013266175,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33579],"includeRotation":true}]}
```

[International] Palladian ray spots
```
~Lv2~{"Name":"P12S2 Palladian Ray","Group":"P12S2","ZoneLockH":[1154],"ElementsL":[{"Name":"","type":1,"offX":8.0,"offY":12.0,"radius":2.0,"color":4278253567,"overlayText":"BAIT","refActorDataID":16181,"refActorRequireCast":true,"refActorCastId":[33571],"refActorUseCastTime":true,"refActorCastTimeMax":4.7,"refActorUseOvercast":true,"refActorComparisonType":3,"includeRotation":true,"onlyTargetable":true,"Filled":true},{"Name":"","type":1,"offX":-8.0,"offY":12.0,"radius":2.0,"color":4278253567,"overlayText":"BAIT","refActorDataID":16181,"refActorRequireCast":true,"refActorCastId":[33571],"refActorUseCastTime":true,"refActorCastTimeMax":4.7,"refActorUseOvercast":true,"refActorComparisonType":3,"includeRotation":true,"onlyTargetable":true,"Filled":true}]}
```

[International] [Beta] Debuff assist (classical concept alpha/beta, calorie theory wind aoe radius)
```
~Lv2~{"Name":"P12S2 Debuffs","Group":"P12S2","ZoneLockH":[1154],"ElementsL":[{"Name":"Wind AOE","type":1,"radius":6.1,"color":3356425984,"thicc":4.0,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[3591],"refActorUseBuffTime":true,"refActorBuffTimeMax":10.0,"onlyTargetable":true},{"Name":"Alpha","type":1,"radius":0.0,"overlayBGColor":4278190248,"overlayTextColor":4294967295,"overlayVOffset":1.54,"thicc":0.0,"overlayText":"Alpha (triangle)","refActorRequireBuff":true,"refActorBuffId":[3560],"refActorType":1},{"Name":"Beta","type":1,"radius":0.0,"overlayBGColor":4278217069,"overlayTextColor":4294967295,"overlayVOffset":1.54,"thicc":0.0,"overlayText":"Beta (square)","refActorRequireBuff":true,"refActorBuffId":[3561],"refActorType":1}]}
```

[International] Exaflare movement predictor. Synchronized with visual, not snapshot.

First exaflare:
```
~Lv2~{"Name":"P12S2 Exa 1","Group":"P12S2","ZoneLockH":[1154],"ElementsL":[{"Name":"","type":1,"radius":6.0,"color":1342177535,"overlayBGColor":4278190080,"overlayTextColor":4278190335,"overlayFScale":2.0,"overlayText":"Warning!","refActorName":"*","refActorRequireCast":true,"refActorCastId":[33567],"refActorUseCastTime":true,"refActorCastTimeMax":5.69,"includeRotation":true}]}
```
Sequential exaflares
```
~Lv2~{"Name":"P12S2 Exa 2","Group":"P12S2","ZoneLockH":[1154],"ElementsL":[{"Name":"","type":1,"offY":8.0,"radius":6.0,"color":1342177535,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33567],"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":8.3}
```
```
~Lv2~{"Name":"P12S2 Exa 3","Group":"P12S2","ZoneLockH":[1154],"ElementsL":[{"Name":"","type":1,"offY":16.0,"radius":6.0,"color":1342177535,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33567],"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":10.3,"FreezeDisplayDelay":6.3}
```
```
~Lv2~{"Name":"P12S2 Exa 4","Group":"P12S2","ZoneLockH":[1154],"ElementsL":[{"Name":"","type":1,"offY":24.0,"radius":6.0,"color":1342177535,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33567],"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":12.3,"FreezeDisplayDelay":8.3}
```
```
~Lv2~{"Name":"P12S2 Exa 5","Group":"P12S2","ZoneLockH":[1154],"ElementsL":[{"Name":"","type":1,"offY":32.0,"radius":6.0,"color":1342177535,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[33567],"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":14.3,"FreezeDisplayDelay":10.3}
```

[International] Exaflare spread radius checker
```
~Lv2~{"Name":"P12S2 Exa Spread","Group":"P12S2","ZoneLockH":[1154],"DCond":5,"ElementsL":[{"Name":"self","type":1,"radius":6.0,"color":687802368,"refActorType":1,"Filled":true},{"Name":"party","type":1,"radius":6.0,"color":4294903808,"thicc":5.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.8,"Match":"(12382>33566)","MatchDelay":11.0}],"MaxDistance":6.1,"UseDistanceLimit":true,"DistanceLimitType":1}
```
