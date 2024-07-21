# Worqor Lar Dor (Extreme)

Contributions by `constmar`, `.leathen.`, and `limiana`.

## [International] Spikesicle

```
~Lv2~{"Name":"Spikesicle","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"ElementsL":[{"Name":"25","type":1,"radius":20.0,"Donut":5.0,"fillIntensity":0.494,"originFillColor":570425599,"endFillColor":570425599,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[36853],"refActorUseCastTime":true,"refActorCastTimeMax":1.4,"mechanicType":1},{"Name":"30","type":1,"radius":25.0,"Donut":5.0,"fillIntensity":0.494,"originFillColor":570425599,"endFillColor":570425599,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[36854],"refActorUseCastTime":true,"refActorCastTimeMax":1.4,"mechanicType":1},{"Name":"35","type":1,"radius":30.0,"Donut":5.0,"fillIntensity":0.494,"originFillColor":570425599,"endFillColor":570425599,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[36855],"refActorUseCastTime":true,"refActorCastTimeMax":1.4,"mechanicType":1},{"Name":"40","type":1,"radius":35.0,"Donut":5.0,"fillIntensity":0.494,"originFillColor":570425599,"endFillColor":570425599,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[36856,36857],"refActorUseCastTime":true,"refActorCastTimeMax":1.4,"mechanicType":1},{"Name":"Line","type":3,"offY":40.0,"radius":2.5,"fillIntensity":0.17,"originFillColor":570425599,"endFillColor":570425599,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[36857],"includeRotation":true,"mechanicType":1}]}
```

## [International] Ice Boulder

```
~Lv2~{"Name":"Ice Boulder","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"ElementsL":[{"Name":"Ice Boulder Cast","type":1,"Enabled":false,"radius":13.0,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[39261],"onlyVisible":true},{"Name":"Ice Boulder Radius","type":1,"radius":13.0,"color":1342242792,"fillIntensity":1.0,"originFillColor":1677721855,"endFillColor":1677721855,"refActorModelID":3090,"refActorRequireCast":true,"refActorCastReverse":true,"refActorCastId":[39261],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":15.0,"refActorUseOvercast":true,"refActorObjectLife":true,"refActorLifetimeMin":2.5,"refActorLifetimeMax":99.0,"refActorComparisonType":1,"onlyVisible":true}],"UseTriggers":true}
```

## [International] Spikesickle Starting Position

Derived from [Hector's guide](https://www.youtube.com/watch?v=8HV2dk3jvNo0) on the boss. This shows where you should move depending on if Spikesickle starts on the left or right side of the arena.

```
~Lv2~{"Name":"Spikesicle Starting Position - right","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"DCond":5,"ElementsL":[{"Name":"","type":1,"offX":-17.0,"offY":13.0,"overlayText":"Begin here","refActorNPCNameID":12854,"refActorRequireCast":true,"refActorCastId":[36850],"refActorUseCastTime":true,"refActorCastTimeMax":0.4,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"tether":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"Match":"MapEffect: 5, 4, 2"}],"Freezing":true,"FreezeFor":10.0,"IntervalBetweenFreezes":60.0}
~Lv2~{"Name":"Spikesicle Starting Position - Left","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"DCond":5,"ElementsL":[{"Name":"","type":1,"offX":17.0,"offY":13.0,"overlayText":"Begin here","refActorNPCNameID":12854,"refActorRequireCast":true,"refActorCastId":[36850],"refActorUseCastTime":true,"refActorCastTimeMax":0.4,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"tether":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"Match":"MapEffect: 4, 4, 2"}],"Freezing":true,"FreezeFor":10.0,"IntervalBetweenFreezes":60.0}
```

## [International] Slithering Strike

```
~Lv2~{"Name":"Slithering Strike","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"ElementsL":[{"Name":"Huge Pointblank","type":4,"radius":24.0,"coneAngleMin":-90,"coneAngleMax":90,"fillIntensity":0.5,"originFillColor":1677721855,"endFillColor":1677721855,"refActorName":"*","refActorNameIntl":{"En":"*"},"refActorRequireCast":true,"refActorCastId":[36812],"includeRotation":true,"mechanicType":1}]}
```

## [International] Strangling Coil

```
~Lv2~{"Name":"Strangling Coil","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"ElementsL":[{"Name":"Donut","type":1,"offY":15.0,"radius":8.0,"Donut":35.0,"color":2013329407,"fillIntensity":0.663,"originFillColor":1677721855,"endFillColor":1677721855,"refActorName":"*","refActorNameIntl":{"En":"*"},"refActorRequireCast":true,"refActorCastId":[36813,36816],"includeRotation":true,"mechanicType":2}]}
```

## [International] Sussurant Breath

```
~Lv2~{"Name":"Sussurant Breath","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"ElementsL":[{"Name":"Cone","type":4,"offY":-10.0,"radius":50.0,"coneAngleMin":-40,"coneAngleMax":40,"color":4278190335,"fillIntensity":0.5,"originFillColor":1677721855,"endFillColor":1677721855,"refActorNPCNameID":12854,"refActorRequireCast":true,"refActorCastId":[36808,36805],"refActorComparisonType":6,"includeRotation":true,"mechanicType":1}]}
```

## [International] Mountain Fire

```
~Lv2~{"Name":"Mountain Fire","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"ElementsL":[{"Name":"Safe Zone","type":4,"radius":40.0,"coneAngleMin":15,"coneAngleMax":345,"color":3355508706,"fillIntensity":0.281,"originFillColor":1677721855,"endFillColor":1677721855,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[36889],"includeRotation":true,"AdditionalRotation":3.1415927,"mechanicType":2}]}
```

## [International] Debuffs

```
~Lv2~{"Name":"Worqor Lar Dor - Debuffs","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"ElementsL":[{"Name":"Calamity's Inferno Stack","type":1,"radius":4.0,"Donut":1.0,"color":3355508480,"fillIntensity":0.5,"originFillColor":1677721855,"endFillColor":1677721855,"overlayTextColor":4280024832,"overlayText":"STACK","refActorName":"*","refActorNameIntl":{"En":"*"},"refActorRequireBuff":true,"refActorBuffId":[3818],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"mechanicType":3},{"Name":"Calamity's Bolt Pointblank","type":1,"radius":5.0,"refActorName":"*","refActorNameIntl":{"En":"*"},"refActorRequireBuff":true,"refActorBuffId":[3823],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"mechanicType":1},{"Name":"Calamity's Bite Tankbuster","type":1,"radius":15.0,"fillIntensity":0.5,"originFillColor":1677721855,"endFillColor":1677721855,"refActorName":"*","refActorNameIntl":{"En":"*"},"refActorRequireBuff":true,"refActorBuffId":[3821],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0},{"Name":"Calamity's Flames Stack","type":1,"radius":4.0,"Donut":1.0,"color":3355508480,"fillIntensity":0.5,"originFillColor":1677721855,"endFillColor":1677721855,"overlayTextColor":4282056448,"overlayText":"STACK","refActorName":"*","refActorNameIntl":{"En":"*"},"refActorRequireBuff":true,"refActorBuffId":[3817],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"mechanicType":3},{"Name":"Calamity's Fulgur Pointblank","type":1,"radius":5.0,"refActorName":"*","refActorNameIntl":{"En":"*"},"refActorRequireBuff":true,"refActorBuffId":[3824],"refActorUseBuffTime":true,"refActorBuffTimeMax":3.0,"mechanicType":1},{"Name":"Calamity's Ember Stack","type":1,"radius":4.0,"Donut":1.0,"color":3355508480,"fillIntensity":0.5,"originFillColor":1677721855,"endFillColor":1677721855,"overlayTextColor":4280024832,"overlayText":"STACK","refActorName":"*","refActorNameIntl":{"En":"*"},"refActorRequireBuff":true,"refActorBuffId":[3819],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"mechanicType":3}]}
```

## [International] Hail of Feathers

```
~Lv2~{"Name":"Hail of Feathers","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"ElementsL":[{"Name":"Death Zone","type":1,"radius":20.0,"Filled":false,"fillIntensity":0.5,"originFillColor":1677721855,"endFillColor":1677721855,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[36170,36171,36361,36829,36830,36893,36894,36895,36896,36897,36898],"mechanicType":1}]}
```

## [International] Avalanche (North and South)

```
~Lv2~{"Name":"North Avalanche","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"DCond":5,"ElementsL":[{"Name":"North Unsafe","type":2,"refX":125.0,"refY":105.0,"refZ":4.7683716E-07,"offX":85.0,"offY":75.0,"offZ":1.9073486E-06,"radius":11.0,"color":3355451647,"fillIntensity":0.5,"originFillColor":1677721855,"endFillColor":1677721855,"mechanicType":1}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":9.0,"Match":"MapEffect: 3, 16, 32"}]}
```

```
~Lv2~{"Name":"South Avalanche","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"DCond":5,"ElementsL":[{"Name":"South Unsafe","type":2,"refX":112.76,"refY":123.36,"refZ":-9.536743E-07,"offX":75.0,"offY":95.0,"offZ":-9.536743E-07,"radius":11.0,"color":3355444991,"fillIntensity":0.5,"originFillColor":1677721855,"endFillColor":1677721855,"mechanicType":1}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":9.0,"Match":"MapEffect: 3, 1, 2"}]}
```

## [International] Thunderous Breath

```
~Lv2~{"Name":"Thunderous Breath","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"ElementsL":[{"Name":"Thunderous Breath","type":3,"offY":40.0,"radius":2.5,"fillIntensity":1.0,"originFillColor":1157628159,"endFillColor":1157628159,"refActorDataID":16770,"refActorComparisonType":3,"includeRotation":true,"onlyVisible":true,"mechanicType":1}]}
```

## [International] Arcane Sphere

```
~Lv2~{"Name":"Arcane Sphere","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"ElementsL":[{"Name":"","type":3,"refY":-40.0,"offY":40.0,"radius":2.5,"fillIntensity":0.4,"originFillColor":1157628159,"endFillColor":1157628159,"refActorModelID":4146,"refActorPlaceholder":[],"refActorNPCNameID":12857,"refActorComparisonAnd":true,"refActorRequireCast":true,"refActorCastReverse":true,"refActorCastId":[36802],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":50.0,"refActorUseOvercast":true,"refActorComparisonType":1,"includeRotation":true,"AdditionalRotation":2.3561945},{"Name":"","type":3,"refY":-40.0,"offY":40.0,"radius":2.5,"fillIntensity":0.4,"originFillColor":1157628159,"endFillColor":1157628159,"refActorModelID":4146,"refActorPlaceholder":[],"refActorNPCNameID":12857,"refActorComparisonAnd":true,"refActorRequireCast":true,"refActorCastReverse":true,"refActorCastId":[36802],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":50.0,"refActorUseOvercast":true,"refActorComparisonType":1,"includeRotation":true,"AdditionalRotation":1.5707964},{"Name":"","type":3,"refY":-40.0,"offY":40.0,"radius":2.5,"fillIntensity":0.4,"originFillColor":1157628159,"endFillColor":1157628159,"refActorModelID":4146,"refActorPlaceholder":[],"refActorNPCNameID":12857,"refActorComparisonAnd":true,"refActorRequireCast":true,"refActorCastReverse":true,"refActorCastId":[36802],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":50.0,"refActorUseOvercast":true,"refActorComparisonType":1,"includeRotation":true,"AdditionalRotation":0.7853982},{"Name":"","type":3,"refY":-40.0,"offY":40.0,"radius":2.5,"fillIntensity":0.4,"originFillColor":1157628159,"endFillColor":1157628159,"refActorModelID":4146,"refActorPlaceholder":[],"refActorNPCNameID":12857,"refActorComparisonAnd":true,"refActorRequireCast":true,"refActorCastReverse":true,"refActorCastId":[36802],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":50.0,"refActorUseOvercast":true,"refActorComparisonType":1,"includeRotation":true}]}
```

## [International] Ice Phase Void Zone

```
~Lv2~{"Name":"Ice Phase Void Zone","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"DCond":5,"ElementsL":[{"Name":"","type":2,"refX":78.0,"refY":85.0,"refZ":9.536743E-07,"offX":78.0,"offY":115.0,"radius":2.0,"color":4294901986,"fillIntensity":1.0,"originFillColor":1157628159,"endFillColor":1157628159},{"Name":"","type":2,"refX":122.0,"refY":85.0,"refZ":9.536743E-07,"offX":122.0,"offY":115.0,"radius":2.0,"color":4294901986,"fillIntensity":1.0,"originFillColor":1157628159,"endFillColor":1157628159},{"Name":"","type":2,"refX":124.0,"refY":117.0,"refZ":-1.9073486E-06,"offX":76.0,"offY":117.0,"radius":2.0,"color":4294901986,"fillIntensity":1.0,"originFillColor":1157628159,"endFillColor":1157628159}],"UseTriggers":true,"Triggers":[{"Type":2,"Match":"(12854>36817)"}]}
```

## [International] Volcanic Eruption

```
~Lv2~{"Name":"Volcanic Eruption - East","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"DCond":5,"ElementsL":[{"Name":"Volcano East","refX":112.22994,"refY":100.365715,"refZ":-9.536743E-07,"radius":20.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"Match":"MapEffect: 14, 64, 128"}]}
~Lv2~{"Name":"Volcanic Eruption - West","Group":"EX1 - Worqor Lar Dor","ZoneLockH":[1196],"DCond":5,"ElementsL":[{"Name":"Volcano East","refX":87.0,"refY":100.0,"refZ":-9.536743E-07,"radius":20.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"Match":"MapEffect: 15, 64, 128"}]}
```
