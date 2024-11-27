## Warning! These presets are WORK IN PROGRESS. They may change frequently, and they may not fully cover mechanic/cover mechanic correctly.

## Utopian Sky
Attack highlight. Highlights where clones will attack. **Option: Open the preset and ebable 4 slices that are assigned to you to enable highlighting side where you have to go.**
```
~Lv2~{"Name":"P1 - Utopian Sky Attack highlight","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Unsafe","type":3,"refY":470.0,"radius":8.0,"overlayPlaceholders":true,"refActorNPCNameID":9708,"refActorComparisonType":6,"includeRotation":true,"LimitDistance":true,"LimitDistanceInvert":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":15.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorUseTransformation":true,"refActorTransformationID":4},{"Name":"Highlight slice North","type":4,"Enabled":false,"offY":8.6,"radius":10.2,"coneAngleMin":135,"coneAngleMax":225,"color":4278255389,"Filled":false,"fillIntensity":1.0,"thicc":8.0,"refActorNPCNameID":9708,"refActorComparisonType":6,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":100.55936,"DistanceSourceY":80.40511,"DistanceSourceZ":-9.536743E-07,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":1000.0,"RenderEngineKind":2},{"Name":"Highlight slice NorthEast","type":4,"Enabled":false,"offY":8.6,"radius":10.2,"coneAngleMin":135,"coneAngleMax":225,"color":4278255389,"Filled":false,"fillIntensity":1.0,"thicc":8.0,"refActorNPCNameID":9708,"refActorComparisonType":6,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":113.91994,"DistanceSourceY":86.817444,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":1000.0,"RenderEngineKind":2},{"Name":"Highlight slice East","type":4,"Enabled":false,"offY":8.6,"radius":10.2,"coneAngleMin":135,"coneAngleMax":225,"color":4278255389,"Filled":false,"fillIntensity":1.0,"thicc":8.0,"refActorNPCNameID":9708,"refActorComparisonType":6,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":118.637344,"DistanceSourceY":100.38822,"DistanceSourceZ":4.7683716E-07,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":1000.0,"RenderEngineKind":2},{"Name":"Highlight slice SouthEast","type":4,"Enabled":false,"offY":8.6,"radius":10.2,"coneAngleMin":135,"coneAngleMax":225,"color":4278255389,"Filled":false,"fillIntensity":1.0,"thicc":8.0,"refActorNPCNameID":9708,"refActorComparisonType":6,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":113.361694,"DistanceSourceY":112.93541,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":1000.0,"RenderEngineKind":2},{"Name":"Highlight slice South","type":4,"Enabled":false,"offY":8.6,"radius":10.2,"coneAngleMin":135,"coneAngleMax":225,"color":4278255389,"Filled":false,"fillIntensity":1.0,"thicc":8.0,"refActorNPCNameID":9708,"refActorComparisonType":6,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":99.534836,"DistanceSourceY":118.7924,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":1000.0,"RenderEngineKind":2},{"Name":"Highlight slice SouthWest","type":4,"Enabled":false,"offY":8.6,"radius":10.2,"coneAngleMin":135,"coneAngleMax":225,"color":4278255389,"Filled":false,"fillIntensity":1.0,"thicc":8.0,"refActorNPCNameID":9708,"refActorComparisonType":6,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":85.76875,"DistanceSourceY":112.95349,"DistanceSourceZ":9.536743E-07,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":1000.0,"RenderEngineKind":2},{"Name":"Highlight slice West","type":4,"Enabled":false,"offY":8.6,"radius":10.2,"coneAngleMin":135,"coneAngleMax":225,"color":4278255389,"Filled":false,"fillIntensity":1.0,"thicc":8.0,"refActorNPCNameID":9708,"refActorComparisonType":6,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":80.2612,"DistanceSourceY":100.00394,"DistanceSourceZ":-9.536743E-07,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":1000.0,"RenderEngineKind":2},{"Name":"Highlight slice NorthWest","type":4,"Enabled":false,"offY":8.6,"radius":10.2,"coneAngleMin":135,"coneAngleMax":225,"color":4278255389,"Filled":false,"fillIntensity":1.0,"thicc":8.0,"refActorNPCNameID":9708,"refActorComparisonType":6,"includeRotation":true,"LimitDistance":true,"DistanceSourceX":85.61012,"DistanceSourceY":86.09313,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":1000.0,"RenderEngineKind":2}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"Match":">40158)"}]}
```

Spreads and stacks:
```
~Lv2~{"Name":"P1 - Utopian sky spreads","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Self","type":1,"radius":5.1,"color":3372213760,"Filled":false,"fillIntensity":0.5,"overlayTextColor":3372218624,"overlayVOffset":2.0,"overlayText":"<<< Spread >>>","refActorPlaceholder":[],"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Other","type":1,"radius":5.1,"color":3355508731,"fillIntensity":0.188,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":">40155)","MatchDelay":14.0}],"MaxDistance":5.1,"UseDistanceLimit":true,"DistanceLimitType":1}
```
```
~Lv2~{"Name":"P1 - Utopian sky stack","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Self","type":1,"radius":5.0,"color":3355487743,"Filled":false,"fillIntensity":0.5,"overlayTextColor":3355481343,"overlayVOffset":2.0,"thicc":4.0,"overlayText":">>> STACK <<<","refActorPlaceholder":[],"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":">40154)","MatchDelay":14.0}]}
```

## [WIP] Protean pairs/spreads
```
~Lv2~{"Name":"FRU P1 Cyclonic Break Flame","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":5.5,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4278190335,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":5.0,"overlayText":">> Pair <<","refActorComparisonType":1,"refActorType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"Match":"(9707>40144)"}]}
```
```
~Lv2~{"Name":"FRU P1 Cyclonic Break Lightning","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":5.5,"color":4294967040,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"overlayText":"<<< Spread >>>","refActorComparisonType":1,"refActorType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"Match":"(9707>40148)"}]}
```
```
~Lv2~{"Name":"FRU P1 Illusion_ Cyclonic Break Flame","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":5.5,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":5.0,"overlayText":"PAirs","refActorComparisonType":1,"refActorType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"Match":"(9708>40329)","MatchDelay":2.0}]}
```
```
~Lv2~{"Name":"FRU P1_Phantom_Cyclic Break Lightning","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":5.5,"color":4278190335,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"overlayText":"Spread","refActorComparisonType":1,"refActorType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"Match":"(9707>40330)"}]}
```

## [WIP] Burnt strike/Circle explosions

```
~Lv2~{"Name":"FRU P1 Chain Explosion","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"サークル","type":1,"radius":10.0,"color":4278190335,"fillIntensity":0.3,"refActorRequireBuff":true,"refActorBuffId":[4166],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"refActorComparisonType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```
```
~Lv2~{"Name":"FRU P1 Illusion - Burn Strike","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"雷","type":3,"refY":50.0,"radius":10.0,"color":4278190335,"fillIntensity":0.3,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40163],"refActorUseCastTime":true,"refActorCastTimeMax":10.0,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"炎","type":3,"refY":50.0,"radius":5.0,"color":4278255611,"fillIntensity":0.3,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40161],"refActorUseCastTime":true,"refActorCastTimeMin":4.0,"refActorCastTimeMax":10.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0}]}
```
```
~Lv2~{"Name":"FRU P1_Halo_flame","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"焔_小","type":1,"radius":5.5,"color":4278255605,"fillIntensity":0.3,"refActorNPCNameID":9710,"refActorRequireCast":true,"refActorCastId":[40152],"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"焔_大","type":1,"radius":10.0,"color":4278255103,"fillIntensity":0.3,"refActorNPCNameID":9710,"refActorRequireCast":true,"refActorCastId":[40153],"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"雷_小","type":1,"radius":5.5,"color":4278255599,"fillIntensity":0.3,"refActorNPCNameID":9711,"refActorRequireCast":true,"refActorCastId":[40152],"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"雷_大","type":1,"radius":10.0,"color":4278255611,"fillIntensity":0.3,"refActorNPCNameID":9711,"refActorRequireCast":true,"refActorCastId":[40153],"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```


## [Script] Fall of Faith

It highlights positions and tells whether it's fire or lightning, and which turn it is.

Configuration:

- You need to set priorities, for example: MT, ST, H1, H2, D1, D2, D3, D4.
- You also need to configure where each person should go, depending on the turn they are targeted.

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P1%20Fall%20of%20Faith.cs
```

## [Script] Burn Strike (Tower)

It highlights positions where towers you need to go to are.

Configuration:
- You need to set priorities, for example: H1, H2, D1, D2, D3, D4.

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P1%20Burn%20Strike%20Tower.cs
```
