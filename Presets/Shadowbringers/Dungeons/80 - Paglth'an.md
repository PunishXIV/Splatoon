### Paglth'an

Presets contributed by `EnjoyingTofu`.

## Amhuluk

Draws Levin ball AOEs, Wide Blaster and AOEs for Towers.

```
~Lv2~{"Name":"Amhuluk","Group":"80 - Paglth'an","ZoneLockH":[938],"ElementsL":[{"Name":"Levin ball AOE","type":1,"radius":5.0,"fillIntensity":0.347,"refActorDataID":12706,"refActorComparisonType":3,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Big Levin ball AOE","type":1,"radius":11.0,"fillIntensity":0.352,"refActorDataID":12707,"refActorComparisonType":3,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Wide Blaster","type":4,"radius":25.0,"coneAngleMin":-30,"coneAngleMax":30,"refActorNPCNameID":10075,"refActorRequireCast":true,"refActorCastId":[24773],"refActorUseCastTime":true,"refActorCastTimeMax":15.0,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":3.1415927,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Tower Lightning","type":1,"radius":10.0,"fillIntensity":0.33,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[2574],"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```

## Magitek Core

Draws Magitek Core Cannons and Missiles.

```
~Lv2~{"Name":"Magitek Core Cannons Left","Group":"80 - Paglth'an","ZoneLockH":[938],"DCond":5,"ElementsL":[{"Name":"Line AOE","type":2,"refX":-184.90016,"refY":25.1655,"refZ":-24.94868,"offX":-184.5367,"offY":58.97852,"offZ":-24.74221,"radius":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":12.0,"Match":"MapEffect: 8, 16, 32"}]}
~Lv2~{"Name":"Magitek Core Cannons Middle","Group":"80 - Paglth'an","ZoneLockH":[938],"DCond":5,"ElementsL":[{"Name":"Line AOE","type":2,"refX":-174.77644,"refY":25.139702,"refZ":-24.882702,"offX":-174.52415,"offY":59.08445,"offZ":-25.033607,"radius":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":12.0,"Match":"MapEffect: 9, 16, 32"}]}
~Lv2~{"Name":"Magitek Core Cannons Right","Group":"80 - Paglth'an","ZoneLockH":[938],"DCond":5,"ElementsL":[{"Name":"Line AOE","type":2,"refX":-164.6422,"refY":24.999943,"refZ":-24.884409,"offX":-164.471,"offY":58.944,"offZ":-24.823174,"radius":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":12.0,"Match":"MapEffect: 10, 16, 32"}]}
~Lv2~{"Name":"Magitek Core Missiles","Group":"80 - Paglth'an","ZoneLockH":[938],"ElementsL":[{"Name":"Line AOE","type":3,"refX":1.0,"offX":-4.72,"radius":1.02,"refActorDataID":12722,"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":1.5707964,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```

## Lunar Bahamut

Draws Lunar Flare patterns.

```
~Lv2~{"Name":"Lunar Bahamut","Group":"80 - Paglth'an","ZoneLockH":[938],"ElementsL":[{"Name":"Lunar Flare Big","type":1,"radius":11.0,"refActorModelID":480,"refActorRequireCast":true,"refActorCastId":[23370],"refActorComparisonType":1,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Lunar Flare Small","type":1,"radius":6.0,"refActorModelID":480,"refActorRequireCast":true,"refActorCastId":[23371],"refActorComparisonType":1,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```