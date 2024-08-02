### Extra Preset Contributors
- Kari
- lilly

# Scripts
### Narrowing/Widening witch hunt
**You must configure your bait order**
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/R4S%20Witch%20Hunt.cs
```

### Electrope edge - counter and explosion resolver
**You must configure your safe spots if you want to display explosion resolver**
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/R4S%20Electrope%20Edge.cs
```

### Cannon - unsafe blast
Displays blast that is unsafe for your debuff. Does not requires configuration.
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/R4S%20Unsafe%20Cannon.cs
```

### Chain Lightning
Does not requires configuration
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/R4S%20Chain%20Lightning.cs
```

# Presets

### Pairs and Spreads (WIP)
```
~Lv2~{"Name":"R4S Pairs","Group":"Arcadion 4","ZoneLockH":[1232],"DCond":5,"ElementsL":[{"Name":"Self","type":1,"radius":6.0,"color":3355901696,"Filled":false,"fillIntensity":0.5,"originFillColor":1677721855,"endFillColor":1677721855,"overlayTextColor":3355901696,"overlayFScale":1.5,"overlayText":"< PAIRS >","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":7.5,"Match":"vfx/common/eff/m0888_stlp01"}]}
~Lv2~{"Name":"R4S Spread","Group":"Arcadion 4","ZoneLockH":[1232],"DCond":5,"ElementsL":[{"Name":"Self","type":1,"radius":6.0,"Filled":false,"fillIntensity":0.5,"originFillColor":1677721855,"endFillColor":1677721855,"overlayTextColor":3355443455,"overlayFScale":1.5,"overlayText":"< SPREAD >","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":7.5,"Match":"vfx/common/eff/m0888_stlp02"}]}
```

### Bewitching Flight
```
~Lv2~{"Name":"R4S Bewitching Flight","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Lines","type":3,"refY":40.0,"radius":2.0,"refActorNameIntl":{"En":"Wicked Thunder"},"refActorRequireCast":true,"refActorCastId":[38377],"includeHitbox":true,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1},{"Name":"Lingering Lines","type":3,"offY":40.0,"radius":8.0,"refActorNameIntl":{"En":"Wicked Thunder"},"refActorRequireCast":true,"refActorCastId":[37561,764,1052,1406,1471,1523,1691,1822,1923,1935,2119,2431,3034,3201,3375,3505,3706,6234,6290,6781,7105,11114,12819,13726,14106,14172,14680,14811,14868,14904,15101,15102,15568,15569,17512,18547,19124,19147,19186,20322,20324,21429,21430,21431,21432,21433,21434,21480,21592,23263,23446,24244,24517,24518,24540,24541,24837,25722,26341,26896,26987,27021,27098,27099,27100,27102,27103,27104,27105,27201,27202,27203,27204,27205,27206,28465,28658,29333,29657,30183,30184,30764,30781,31041,31189,31383,33078,33091,33140,34018,34073,34075,34116,34118,35127,35137,35159,35165,35188,35194,35441,35443,35522,35554,35608,35687,36575,36591,36738,37561,37612,37626,37709,38378,38598,39358],"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1},{"Name":"Gun Battery Appear","type":3,"refY":40.0,"radius":2.5,"refActorModelID":4225,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":5.0,"refActorComparisonType":1,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1},{"Name":"Gun Battery Cast","type":3,"refY":40.0,"radius":2.5,"refActorModelID":4225,"refActorRequireCast":true,"refActorCastId":[38379],"refActorCastTimeMin":0.5,"refActorCastTimeMax":10.0,"refActorComparisonType":1,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1}]}
```

### Sidewise Spark
```
~Lv2~{"Name":"R4S Sidewise Spark","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Left 1","type":4,"radius":40.0,"coneAngleMin":-180,"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/common/eff/m0888_cast04_c0t1.avfx","refActorVFXMax":6692,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1},{"Name":"Left 2","type":4,"radius":40.0,"coneAngleMin":-180,"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/common/eff/m0888_cast06_c0t1.avfx","refActorVFXMax":6700,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1},{"Name":"Right 1","type":4,"radius":40.0,"coneAngleMax":180,"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/common/eff/m0888_cast03_c0t1.avfx","refActorVFXMax":6700,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1},{"Name":"Right 2","type":4,"radius":40.0,"coneAngleMax":180,"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/common/eff/m0888_cast05_c0t1.avfx","refActorVFXMax":6700,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1},{"Name":"Replica Left","type":4,"radius":40.0,"coneAngleMin":-180,"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/common/eff/m0888_stlp15_c0t1.avfx","refActorVFXMax":4989,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1},{"Name":"Replica Right","type":4,"radius":40.0,"coneAngleMax":180,"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/common/eff/m0888_stlp14_c0t1.avfx","refActorVFXMax":4997,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1}]}
```

### Electromines
```
~Lv2~{"Name":"R4S Electromines","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Spark","type":3,"refY":-5.0,"offY":5.0,"radius":5.0,"refActorModelID":4226,"refActorRequireCast":true,"refActorCastId":[38345],"refActorComparisonType":1,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1},{"Name":"Spark II","type":3,"refY":-15.0,"offY":15.0,"radius":15.0,"fillIntensity":0.345,"originFillColor":1157628159,"endFillColor":1157628159,"refActorModelID":4226,"refActorRequireCast":true,"refActorCastId":[38346],"refActorComparisonType":1,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1}]}
```

### Ion Cannon - early display
```
~Lv2~{"Name":"R4S Cannon","Group":"Arcadion 4","ZoneLockH":[1232],"DCond":5,"ElementsL":[{"Name":"Left","type":3,"refX":5.0,"refY":40.0,"offX":5.0,"radius":15.0,"refActorNPCNameID":13057,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"AdditionalRotation":6.0423303,"LimitRotation":true,"RotationMax":-3.1241393,"RotationMin":-0.017453292,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]},{"Name":"Right","type":3,"refX":-5.0,"refY":40.0,"offX":-5.0,"radius":15.0,"refActorNPCNameID":13057,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"AdditionalRotation":0.24085544,"LimitRotation":true,"RotationMax":0.017453292,"RotationMin":3.1241393,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":15.0,"Match":">38356)","MatchDelay":10.0}]}
```

### Stampeding Thunder (cannon blasts)
```
~Lv2~{"Name":"R4S Stampeding Thunder","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Dead Platform","type":3,"refX":-15.0,"refY":20.0,"offX":15.0,"offY":20.0,"radius":20.0,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[36399],"refActorCastTimeMax":2.3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[],"mechanicType":1}]}
```

### Fulminous Field
Only proteans, import all
```
~Lv2~{"Name":"R4S Fulminous Field 2","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Rotated","type":4,"refY":30.0,"radius":30.0,"coneAngleMin":-15,"coneAngleMax":15,"fillIntensity":0.35,"originFillColor":1677721855,"endFillColor":1677721855,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[37118],"includeRotation":true,"AdditionalRotation":0.3926991,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}],"Freezing":true,"FreezeFor":6.0,"FreezeDisplayDelay":3.0}
~Lv2~{"Name":"R4S Fulminous Field 3","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Normal","type":4,"refY":30.0,"radius":30.0,"coneAngleMin":-15,"coneAngleMax":15,"fillIntensity":0.35,"originFillColor":1677721855,"endFillColor":1677721855,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[37118],"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}],"Freezing":true,"FreezeFor":9.0,"FreezeDisplayDelay":6.0}
~Lv2~{"Name":"R4S Fulminous Field 4","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Rotated","type":4,"refY":30.0,"radius":30.0,"coneAngleMin":-15,"coneAngleMax":15,"fillIntensity":0.35,"originFillColor":1677721855,"endFillColor":1677721855,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[37118],"includeRotation":true,"AdditionalRotation":0.3926991,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}],"Freezing":true,"FreezeFor":12.0,"FreezeDisplayDelay":9.0}
~Lv2~{"Name":"R4S Fulminous Field 5","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Normal","type":4,"refY":30.0,"radius":30.0,"coneAngleMin":-15,"coneAngleMax":15,"fillIntensity":0.35,"originFillColor":1677721855,"endFillColor":1677721855,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[37118],"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}],"Freezing":true,"FreezeFor":15.0,"FreezeDisplayDelay":12.0}
~Lv2~{"Name":"R4S Fulminous Field 6","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Rotated","type":4,"refY":30.0,"radius":30.0,"coneAngleMin":-15,"coneAngleMax":15,"fillIntensity":0.35,"originFillColor":1677721855,"endFillColor":1677721855,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[37118],"includeRotation":true,"AdditionalRotation":0.3926991,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}],"Freezing":true,"FreezeFor":18.0,"FreezeDisplayDelay":15.0}
~Lv2~{"Name":"R4S Fulminous Field 7","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Normal","type":4,"refY":30.0,"radius":30.0,"coneAngleMin":-15,"coneAngleMax":15,"fillIntensity":0.35,"originFillColor":1677721855,"endFillColor":1677721855,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[37118],"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}],"Freezing":true,"FreezeFor":21.0,"FreezeDisplayDelay":18.0}
```

### Exaflare - initial cast
Will display exaflare directions during it's initial cast, enabling you to easily see safe spot.
```
~Lv2~{"Name":"R4S Exaflares hint","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"","type":3,"refY":40.0,"offY":6.0,"radius":2.0,"color":3355506687,"fillIntensity":0.2,"originFillColor":1157628159,"endFillColor":1157628159,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[38389],"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}]}
```

### Exaflare - aftercast safe spot
Will display precise safe spot after initial cast, but will cover screen in a LOT of elements. 
```
~Lv2~{"Name":"R4S Exaflares safe zone","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"","type":3,"refY":40.0,"offY":6.0,"radius":6.0,"color":3355506687,"fillIntensity":0.2,"originFillColor":1157628159,"endFillColor":1157628159,"refActorName":"*","refActorRequireCast":true,"refActorCastId":[38389],"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}],"Freezing":true,"FreezeFor":12.0,"FreezeDisplayDelay":8.0}
```

### Wicked Special cleaves
```
~Lv2~{"Name":"R4S End Phase Room Cleaves","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Wicked Special Left","type":3,"refX":12.5,"refY":30.0,"offX":12.5,"radius":7.5,"color":3355508735,"fillIntensity":0.2,"originFillColor":1157628159,"endFillColor":1157628159,"refActorName":"Wicked Thunder","refActorRequireCast":true,"refActorCastId":[38418],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":6.0,"refActorUseOvercast":true,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]},{"Name":"Wicked Special Right","type":3,"refX":-12.5,"refY":30.0,"offX":-12.5,"radius":7.5,"color":3355508735,"fillIntensity":0.2,"originFillColor":1157628159,"endFillColor":1157628159,"refActorName":"Wicked Thunder","refActorRequireCast":true,"refActorCastId":[38418],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":6.0,"refActorUseOvercast":true,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]},{"Name":"Wicked Special Middle","type":3,"refY":30.0,"radius":10.0,"color":3355508735,"fillIntensity":0.2,"originFillColor":1157628159,"endFillColor":1157628159,"refActorName":"Wicked Thunder","refActorRequireCast":true,"refActorCastId":[38416],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":6.0,"refActorUseOvercast":true,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherConnectedWithPlayer":[]}]}
```

### Replica names (temporary preset)
```
~Lv2~{"Name":"R4S Replica Names (temp)","Group":"Arcadion 4","ZoneLockH":[1232],"ElementsL":[{"Name":"Gun","type":1,"radius":0.0,"overlayTextColor":4294967295,"overlayVOffset":4.0,"overlayFScale":2.0,"overlayText":"LINE","refActorName":"Wicked Replica","DistanceMax":200.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorUseTransformation":true,"refActorTetherConnectedWithPlayer":[],"refActorTransformationID":7},{"Name":"Gun Line","type":3,"Enabled":false,"refY":6.0,"offY":-2.0,"radius":5.0,"color":3355508735,"Filled":false,"fillIntensity":0.2,"originFillColor":1157628159,"endFillColor":1157628159,"overlayTextColor":4294967295,"overlayVOffset":4.0,"overlayFScale":2.0,"overlayText":"LINE","refActorName":"Wicked Replica","includeRotation":true,"DistanceMax":200.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorUseTransformation":true,"refActorTetherConnectedWithPlayer":[],"refActorTransformationID":7},{"Name":"Wing","type":1,"radius":0.0,"overlayTextColor":4294967295,"overlayVOffset":4.0,"overlayFScale":2.0,"overlayPlaceholders":true,"overlayText":"DONUT","refActorName":"Wicked Replica","DistanceMax":200.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorUseTransformation":true,"refActorTetherConnectedWithPlayer":[],"refActorTransformationID":31},{"Name":"Wing Donut","type":1,"Enabled":false,"radius":5.0,"Donut":8.0,"color":3355508735,"fillIntensity":0.2,"originFillColor":1677721855,"endFillColor":1677721855,"overlayTextColor":4294967295,"overlayVOffset":4.0,"overlayFScale":2.0,"overlayPlaceholders":true,"refActorName":"Wicked Replica","onlyVisible":true,"DistanceMax":200.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorUseTransformation":true,"refActorTetherConnectedWithPlayer":[],"refActorTransformationID":31},{"Name":"Wing Donut (Mini)","type":1,"radius":5.0,"color":3355508735,"Filled":false,"fillIntensity":0.2,"originFillColor":1677721855,"endFillColor":1677721855,"overlayTextColor":4294967295,"overlayVOffset":4.0,"overlayFScale":2.0,"overlayPlaceholders":true,"refActorName":"Wicked Replica","onlyVisible":true,"DistanceMax":200.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorUseTransformation":true,"refActorTetherConnectedWithPlayer":[],"refActorTransformationID":31}]}
```
