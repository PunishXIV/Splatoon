## [WIP] [Script] Diamond Dust
It highlights positions.
Configuration:
- Set all spread positions based on the 1st Icicle Impact locations.
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P2%20Diamond%20Dust.cs
```

## [WIP] [Script] Mirror, Mirror
It highlights your next mirror positions.
Configuration:
- Set your first blue mirror.
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P2%20Mirror%20Mirror.cs
```

## [JP] [WIP] [Script] Light Rampant JP
It highlights your tower.
Configuration:
- Set player names and directions.
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P2%20Light%20Rampant%20JP.cs
```

### JP Box
the setup is as follows, with SW and SE, N and S swapping, and H2 and D4 making adjustments:
```
T1 T2 H1 H2
D1 D2 D3 D4
```
```
T1 - Put No name 
0 = NW // 1 = None // 2 = None

T2 - Put T1 name
0 = S // 1 = NW // 2 = None

H1 - T1 > T2
0 = NE // 1 = S // 2 =  NW

H2 -T1 > T2 > H1
0 = SW // 1 = NE // 2 = S
--------------------------
D1 - Put no name
0 = SE // 1 = None // 2 = None

D2 - Put D1 name
0 = N // 1 = SE // 2 = None

D3- D1 > D2
 0 = SW // 1 = N // 2 = SE

D4 -  D1 > D2 > D3
0 = NE// 1=  SW -// 2 = N
```

Thanks for Tora

## [WIP] [Script] AutoTargetCrystal
Auto-targets nearest light crystal, then auto-targets the center crystal after all light crystals are destroyed.
No configuration needed.
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P2%20AutoTargetCrystal.cs
```

## Flower-like explosions
Will show after knockback, to not obstruct your view. Feel free to edit that out but good luck seeing anything...
```
~Lv2~{"Name":"P2 - Flowers explosions","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"前後","type":3,"refY":40.0,"offY":-40.0,"radius":2.0,"color":4278190335,"fillIntensity":0.139,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40200],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":20.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"左右","type":3,"refY":40.0,"offY":-40.0,"radius":2.0,"color":4278190335,"fillIntensity":0.144,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40200],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":20.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":1.5707964,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"右斜","type":3,"refY":40.0,"offY":-40.0,"radius":2.0,"color":4278190335,"fillIntensity":0.144,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40200],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":20.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":0.7853982,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"左斜","type":3,"refY":40.0,"offY":-40.0,"radius":2.0,"color":4278190335,"fillIntensity":0.144,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40200],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":20.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":5.497787,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0}]}
```

## Axe/Scythe kick
Scythe kick is purple to help quickly differentiate them
```
~Lv2~{"Name":"P2 - Axe/Scythe kick","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"アクスキック","type":1,"radius":16.0,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40202],"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"サイスキック","type":1,"radius":4.0,"Donut":20.0,"color":3371237631,"fillIntensity":0.3,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40203],"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```

## Knockback safe spot
Saves safe knockback spot for future use. Does NOT resolves specific spot where you need to go for knockback.
```
~Lv2~{"Name":"P2 - first icicle impact","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"","type":1,"radius":4.93,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":7.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"","type":1,"offY":32.0,"radius":4.93,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":7.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Line 1","type":3,"refY":36.0,"offY":-4.0,"radius":0.0,"color":1679163136,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"KB point","type":1,"offY":16.0,"radius":0.0,"color":4279631616,"Filled":false,"fillIntensity":0.5,"thicc":10.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"Freezing":true,"FreezeFor":15.0,"IntervalBetweenFreezes":20.0}
```

## Spread/protean diagram
Diagram to indicate valid positions for flowers and proteans. Does NOT resolves your specific position, does NOT accounts for scythe/axe variants. 
```
~Lv2~{"Name":"P2 - first icicle impact - hints","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"Tower 1","type":1,"offX":5.5,"offY":10.5,"radius":1.0,"color":1677787125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Tower 1","type":1,"offX":-5.5,"offY":10.5,"radius":1.0,"color":1677787125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Tower 1","type":1,"offX":5.5,"offY":21.5,"radius":1.0,"color":1677787125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Tower 1","type":1,"offX":-5.5,"offY":21.5,"radius":1.0,"color":1677787125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Tower 2","type":1,"offX":13.5,"offY":2.5,"radius":1.0,"color":1677787125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Tower 2","type":1,"offX":-13.5,"offY":2.5,"radius":1.0,"color":1677787125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Tower 2","type":1,"offX":-13.5,"offY":29.5,"radius":1.0,"color":1677787125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Tower 2","type":1,"offX":13.5,"offY":29.5,"radius":1.0,"color":1677787125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Line 1","type":3,"refY":36.0,"offY":-4.0,"radius":0.0,"color":1679163136,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Line 1","type":3,"refX":-16.0,"refY":20.0,"offX":-16.0,"offY":-20.0,"radius":0.0,"color":1679163136,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":1.5707964,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"Freezing":true,"FreezeFor":8.5,"IntervalBetweenFreezes":20.0}
```

## Usurper or Frost finder
Add that will cast twin stillness/silence
```
~Lv2~{"Name":"P2 - Usurper of Frost finder","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":2.0,"color":3372154890,"fillIntensity":0.5,"thicc":4.0,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[40208],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":23.300001,"refActorUseOvercast":true,"includeRotation":true,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```

## Twin stillness / Twin silence
```
~Lv2~{"Name":"P2 - Twin stillness","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"","type":4,"radius":40.0,"coneAngleMin":-145,"coneAngleMax":145,"fillIntensity":0.259,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40193],"refActorUseCastTime":true,"refActorCastTimeMax":3.0,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.259,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40193],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":6.0,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":3.1415927,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```
```
~Lv2~{"Name":"P2 - Twin silence","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"","type":4,"radius":40.0,"coneAngleMin":-145,"coneAngleMax":145,"fillIntensity":0.259,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40194],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":6.0,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.259,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40194],"refActorUseCastTime":true,"refActorCastTimeMax":3.0,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":3.1415927,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```

## [Partially EN] Twin stillness/silence KB helper
Shows early only for EN clients, requires translation
```
~Lv2~{"Name":"P2 - Twin silence/stillness KB helper","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"","type":3,"refY":32.0,"radius":0.0,"color":3356425984,"fillIntensity":0.345,"thicc":4.0,"refActorType":1,"includeRotation":true,"LineEndA":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.0,"Match":">40193)"},{"Type":2,"Duration":6.0,"Match":">40194)"},{"Type":2,"Duration":5.0,"MatchIntl":{"En":"The Light shall usher in peace!"},"MatchDelay":3.1}]}
```

## [EN] Shining armor helper
Don't look.
Shows only for EN clients, requires translation
```
~Lv2~{"Name":"P2 - Don't look","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"","type":4,"refY":10.0,"radius":20.0,"coneAngleMin":-46,"coneAngleMax":46,"color":3356425984,"fillIntensity":0.1,"overlayBGColor":4278190335,"overlayTextColor":4294967295,"overlayVOffset":3.0,"overlayText":"Don't intersect red!","refActorType":1,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"","type":1,"radius":0.0,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4293328640,"overlayFScale":3.0,"thicc":4.0,"overlayText":"!!! Don't LOOK !!!","refActorDataID":17823,"refActorComparisonType":3,"includeRotation":true,"tether":true,"LimitDistance":true,"LimitDistanceInvert":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":18.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4293328640,"overlayFScale":3.0,"thicc":4.0,"overlayText":"!!! Don't LOOK !!!","refActorDataID":17824,"refActorComparisonType":3,"includeRotation":true,"tether":true,"LimitDistance":true,"LimitDistanceInvert":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":18.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.2,"MatchIntl":{"En":"The Light shall usher in peace!"},"MatchDelay":2.0}]}
```

## Reflected scythe kick
```
~Lv2~{"Name":"P2 - Reflected scythe kick","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"","type":1,"radius":4.0,"Donut":10.0,"color":3371237631,"fillIntensity":0.292,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[40205],"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```

## Banish 3
```
~Lv2~{"Name":"P2 - Banish III Pair","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"二","type":1,"radius":5.5,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":5.0,"overlayText":"Pair","refActorComparisonType":1,"refActorType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.5,"Match":"(12809>40220)"}]}
```
```
~Lv2~{"Name":"P2 - Banish III Spread","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"近くにいるプレイヤー","type":1,"radius":6.0,"color":4278190335,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ー","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"Spread","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.5,"Match":"(12809>40221)"}],"MaxDistance":7.5,"UseDistanceLimit":true,"DistanceLimitType":1}
```

## Light rampant orb explosion
```
~Lv2~{"Name":"P2 - Light rampant orb explosion","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"","type":1,"radius":11.0,"refActorNPCNameID":9318,"refActorRequireCast":true,"refActorCastId":[40219],"refActorUseCastTime":true,"refActorCastTimeMin":2.0,"refActorCastTimeMax":4.7,"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```
