# A.S.S Trash mobs:

Belladona: (aoe not accurate, use as a guide to remember it's being cast)
```
~Lv2~{"Name":"Belladona","Group":"A.S.S Trash","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"AOE","type":1,"radius":10.0,"Donut":20.0,"thicc":5.0,"refActorNPCNameID":11514,"refActorRequireCast":true,"refActorCastId":[5215,12415,30041,31072,31096],"FillStep":2.0,"refActorComparisonType":6}]}
```

Kaluk: (cleaves are displayed bigger than they are, just don't pixel gap them) 
```
~Lv2~{"Name":"Kaluk","Group":"A.S.S Trash","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"Right Sweep","type":4,"radius":30.0,"coneAngleMin":-34,"coneAngleMax":180,"refActorNPCNameID":11510,"refActorRequireCast":true,"refActorCastId":[31075,31099],"refActorComparisonType":6,"includeRotation":true,"Filled":true},{"Name":"Left Sweep","type":4,"radius":30.0,"coneAngleMin":180,"coneAngleMax":394,"refActorNPCNameID":11510,"refActorRequireCast":true,"refActorCastId":[31076,31100],"FillStep":3.0,"refActorComparisonType":6,"includeRotation":true,"Filled":true},{"Name":"Creeping Ivy","type":4,"radius":10.0,"coneAngleMin":-45,"coneAngleMax":45,"refActorNPCNameID":11510,"refActorRequireCast":true,"refActorCastId":[8665,10548,12422,12534,12709,17527,18138,24559,27413,28216,30664,31077,31101],"refActorComparisonType":6,"includeRotation":true,"Filled":true}]}
```
Dryad: (slightly bigger than actual aoe)
```
~Lv2~{"Name":"Dryad","Group":"A.S.S Trash","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"Arboreal Storm","type":1,"radius":10.0,"refActorNPCNameID":11513,"refActorRequireCast":true,"refActorCastId":[441,2741,3191,5003,5437,13531,31063,31087],"refActorComparisonType":6,"includeHitbox":true}]}
```
Udumbara: (left/right are accurate, front/caress aren't perfect)
```
~Lv2~{"Name":"Udumbara","Group":"A.S.S Trash","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"Honeyed Right","type":4,"radius":30.0,"coneAngleMin":45,"coneAngleMax":225,"refActorNPCNameID":11511,"refActorRequireCast":true,"refActorCastId":[31068,31092],"refActorComparisonType":6,"includeRotation":true,"Filled":true},{"Name":"Honeyed Left","type":4,"radius":30.0,"coneAngleMin":45,"coneAngleMax":225,"refActorNPCNameID":11511,"refActorRequireCast":true,"refActorCastId":[31067,31091],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":1.5707964,"Filled":true},{"Name":"Honeyed Front","type":4,"radius":30.0,"coneAngleMin":-56,"coneAngleMax":56,"refActorNPCNameID":11511,"refActorRequireCast":true,"refActorCastId":[31069,31093],"refActorComparisonType":6,"includeRotation":true,"Filled":true},{"Name":"Bloody Caress","type":4,"radius":10.0,"coneAngleMin":-56,"coneAngleMax":56,"refActorNPCNameID":11512,"refActorRequireCast":true,"refActorCastId":[1943,2484,2755,3793,4670,4971,5020,5426,6421,7133,11831,12413,18148,18727,24457,26534,31071,31095],"refActorComparisonType":6,"includeRotation":true,"Filled":true}]}
```
dullahan: (accurate) 
```
~Lv2~{"Name":"Dullahan","Group":"A.S.S Trash","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"Blighted Gloom","type":1,"radius":10.0,"refActorNPCNameID":11506,"refActorRequireCast":true,"refActorCastId":[759,30688,31078,31102],"refActorComparisonType":6}]}
```

# Boss 1: Silkie

All Slippery soap variations: 
```
~Lv2~{"Name":"Slippery Soap","Group":"A.S.S Silkie","ZoneLockH":[1075,1076],"DCond":5,"ElementsL":[{"Name":"Chilling 1","type":3,"refY":30.0,"offY":-30.0,"radius":5.0,"thicc":0.0,"refActorNPCNameID":11369,"refActorRequireBuff":true,"refActorBuffId":[3298,3306],"FillStep":1.0,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"onlyVisible":true},{"Name":"Chilling 2","type":3,"refY":30.0,"offY":-30.0,"radius":5.0,"thicc":0.0,"refActorNPCNameID":11369,"refActorRequireBuff":true,"refActorBuffId":[3298,3306],"FillStep":1.0,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"onlyVisible":true,"AdditionalRotation":1.5707964},{"Name":"Bracing","type":1,"radius":5.0,"Donut":50.0,"thicc":5.0,"refActorNPCNameID":11369,"refActorRequireBuff":true,"refActorBuffId":[3297,3305],"FillStep":1.0,"refActorComparisonType":6},{"Name":"Fizzling 1","type":4,"radius":50.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11369,"refActorRequireBuff":true,"refActorBuffId":[3299,3307],"refActorComparisonType":6,"includeRotation":true,"Filled":true},{"Name":"Fizzling 2","type":4,"radius":50.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11369,"refActorRequireBuff":true,"refActorBuffId":[3299,3307],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":1.5707964,"Filled":true},{"Name":"Fizzling 3","type":4,"radius":30.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11369,"refActorRequireBuff":true,"refActorBuffId":[3299,3307],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":3.1415927,"Filled":true},{"Name":"Fizzling 4","type":4,"radius":30.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11369,"refActorRequireBuff":true,"refActorBuffId":[3299,3307],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":4.712389,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"TimeBegin":65.0,"Duration":10.0,"Match":"The silkie readies Slippery Soap."}]}
```
Fizzling Duster:
```
~Lv2~{"Name":"Fizzling Duster","Group":"A.S.S Silkie","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"Boss 1","type":4,"radius":30.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11369,"refActorRequireCast":true,"refActorCastId":[30557,30565,30570,30592,30600,30605],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":0.7853982,"Filled":true},{"Name":"Boss 2","type":4,"radius":30.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11369,"refActorRequireCast":true,"refActorCastId":[30557,30565,30570,30592,30600,30605],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":2.3561945,"Filled":true},{"Name":"Boss 3","type":4,"radius":30.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11369,"refActorRequireCast":true,"refActorCastId":[30557,30565,30570,30592,30600,30605],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":3.9269907,"Filled":true},{"Name":"Boss 4","type":4,"radius":30.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11369,"refActorRequireCast":true,"refActorCastId":[30557,30565,30570,30592,30600,30605],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":5.497787,"Filled":true},{"Name":"Adds 1","type":4,"radius":50.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11370,"refActorRequireCast":true,"refActorCastId":[30557,30565,30570,30592,30600,30605],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":0.7853982,"Filled":true},{"Name":"Adds 2","type":4,"radius":50.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11370,"refActorRequireCast":true,"refActorCastId":[30557,30565,30570,30592,30600,30605],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":2.3561945,"Filled":true},{"Name":"Adds 3","type":4,"radius":50.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11370,"refActorRequireCast":true,"refActorCastId":[30557,30565,30570,30592,30600,30605],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":3.9269907,"Filled":true},{"Name":"Adds 4","type":4,"radius":50.0,"coneAngleMin":22,"coneAngleMax":67,"refActorNPCNameID":11370,"refActorRequireCast":true,"refActorCastId":[30557,30565,30570,30592,30600,30605],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":5.497787,"Filled":true}]}
```
Chilling Duster:
```
~Lv2~{"Name":"Chilling Duster","Group":"A.S.S Silkie","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"Adds 1","type":3,"refY":50.0,"offY":-50.0,"radius":5.0,"thicc":0.0,"refActorNPCNameID":11370,"refActorRequireCast":true,"refActorCastId":[30520,30523,30527,30555,30563,30568,30590,30598,30603],"FillStep":1.0,"refActorComparisonType":6,"includeOwnHitbox":true,"includeRotation":true},{"Name":"Adds 2","type":3,"refY":50.0,"offY":-50.0,"radius":5.0,"thicc":0.0,"refActorNPCNameID":11370,"refActorRequireCast":true,"refActorCastId":[30520,30523,30527,30555,30563,30568,30590,30598,30603],"FillStep":1.0,"refActorComparisonType":6,"includeOwnHitbox":true,"includeRotation":true,"AdditionalRotation":1.5707964},{"Name":"Boss 1","type":3,"refY":50.0,"offY":-50.0,"radius":5.0,"thicc":0.0,"refActorNPCNameID":11369,"refActorRequireCast":true,"refActorCastId":[30520,30523,30527,30555,30563,30568,30590,30598,30603],"FillStep":1.0,"refActorComparisonType":6,"includeOwnHitbox":true,"includeRotation":true},{"Name":"Boss 2","type":3,"refY":50.0,"offY":-50.0,"radius":5.0,"thicc":0.0,"refActorNPCNameID":11369,"refActorRequireCast":true,"refActorCastId":[30520,30523,30527,30555,30563,30568,30590,30598,30603],"FillStep":1.0,"refActorComparisonType":6,"includeOwnHitbox":true,"includeRotation":true,"AdditionalRotation":1.5707964}]}
```
Bracing Duster:
```
~Lv2~{"Name":"Bracing Duster","Group":"A.S.S Silkie","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"Boss","type":1,"radius":5.0,"Donut":50.0,"thicc":5.0,"refActorNPCNameID":11369,"refActorRequireCast":true,"refActorCastId":[30521,30524,30528,30556,30564,30569,30591,30599,30604],"FillStep":1.0,"refActorComparisonType":6},{"Name":"Adds","type":1,"radius":5.0,"Donut":50.0,"thicc":5.0,"refActorNPCNameID":11370,"refActorRequireCast":true,"refActorCastId":[30521,30524,30528,30556,30564,30569,30591,30599,30604],"FillStep":1.0,"refActorComparisonType":6}]}
```
Forked Lightning:
```
~Lv2~{"Name":"Forked Lightning","Group":"A.S.S Silkie","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"","type":1,"radius":5.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[587],"refActorType":1}]}
```

# Boss 2: Gladiator

Rush of Might:
```
~Lv2~{"Name":"Rush of Might","Group":"A.S.S Gladiator","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"","type":3,"refX":-5.0,"refY":-30.0,"offX":-5.0,"offY":30.0,"radius":5.0,"thicc":1.0,"refActorNPCNameID":11387,"refActorRequireCast":true,"refActorCastId":[30266,30267,30268,30269,30270,30296,30297,30298,30299,30300,30618,30619,30620,30621,30622],"refActorUseCastTime":true,"refActorCastTimeMax":15.0,"refActorUseOvercast":true,"FillStep":0.646,"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyUnTargetable":true,"AdditionalRotation":1.5707964}]}
```
Thunderous Echo:
```
~Lv2~{"Name":"Thunderous Echo","Group":"A.S.S Gladiator","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"","type":1,"radius":5.0,"thicc":5.0,"overlayText":"Stack","refActorPlaceholder":["<1>","<2>","<3>","<4>"],"refActorRequireBuff":true,"refActorBuffId":[3293],"refActorBuffTimeMax":13.3,"refActorComparisonType":5}]}
```
Lingering Echoes:
```
~Lv2~{"Name":"Lingering Echoes","Group":"A.S.S Gladiator","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"","type":1,"radius":2.0,"thicc":5.0,"overlayText":"Spread","refActorPlaceholder":["<1>","<2>","<3>","<4>"],"refActorRequireBuff":true,"refActorBuffId":[3292],"refActorBuffTimeMax":13.3,"refActorComparisonType":5}]}
```
Echo of the fallen:
```
~Lv2~{"Name":"Echo of the fallen","Group":"A.S.S Gladiator","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"","type":1,"radius":5.0,"thicc":5.0,"overlayText":"Spread","refActorRequireBuff":true,"refActorBuffId":[3290],"refActorUseBuffTime":true,"refActorBuffTimeMax":3.0,"refActorType":1}]}
```
Scream of the fallen:
```
~Lv2~{"Name":"Scream of the fallen","Group":"A.S.S Gladiator","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"","type":1,"radius":10.0,"thicc":5.0,"overlayText":"Spread","refActorRequireBuff":true,"refActorBuffId":[3291],"refActorUseBuffTime":true,"refActorBuffTimeMax":3.0,"refActorType":1}]}
```
Nothing beside remains:
```
~Lv2~{"Name":"Nothing Beside Remains","Group":"A.S.S Gladiator","ZoneLockH":[1076,1075],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":5.0,"thicc":4.0,"refActorType":1}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"The gladiator of Sil'dih readies Nothing Beside Remains."}]}
```

# Boss 3
Brands Display - All Players: 
```
~Lv2~{"Name":"Brands - All","Group":"A.S.S Shadowcaster","ZoneLockH":[1076,1075],"ElementsL":[{"Name":"Placeholder Circles","type":1,"refActorPlaceholder":["<1>","<2>","<3>","<4>"],"refActorRequireBuff":true,"refActorBuffId":[3268,3269,3270,3271,3272,3273,3274,3275],"refActorComparisonType":5},{"Name":"One","type":1,"overlayVOffset":2.0,"overlayFScale":4.0,"thicc":0.0,"overlayText":"1","refActorPlaceholder":["<1>","<2>","<3>","<4>"],"refActorRequireBuff":true,"refActorBuffId":[3268,3272],"refActorComparisonType":5},{"Name":"Two","type":1,"overlayVOffset":2.0,"overlayFScale":4.0,"thicc":0.0,"overlayText":"2","refActorPlaceholder":["<1>","<2>","<3>","<4>"],"refActorRequireBuff":true,"refActorBuffId":[3269,3273],"refActorComparisonType":5},{"Name":"Three","type":1,"overlayVOffset":2.0,"overlayFScale":4.0,"thicc":0.0,"overlayText":"3","refActorPlaceholder":["<1>","<2>","<3>","<4>"],"refActorRequireBuff":true,"refActorBuffId":[3270,3274],"refActorComparisonType":5},{"Name":"Four","type":1,"overlayVOffset":2.0,"overlayFScale":4.0,"thicc":0.0,"overlayText":"4","refActorPlaceholder":["<1>","<2>","<3>","<4>"],"refActorRequireBuff":true,"refActorBuffId":[3271,3275],"refActorComparisonType":5}]}
```
Brands Display - Self only: 
```
~Lv2~{"Name":"Brands - Self","Group":"A.S.S Shadowcaster","ZoneLockH":[1075,1076],"ElementsL":[{"Name":"One","type":1,"overlayVOffset":2.0,"overlayFScale":4.0,"thicc":0.0,"overlayText":"1","refActorPlaceholder":["<1>","<2>","<3>","<4>"],"refActorRequireBuff":true,"refActorBuffId":[3268,3272],"refActorComparisonType":5,"refActorType":1},{"Name":"Two","type":1,"overlayVOffset":2.0,"overlayFScale":4.0,"thicc":0.0,"overlayText":"2","refActorPlaceholder":["<1>","<2>","<3>","<4>"],"refActorRequireBuff":true,"refActorBuffId":[3269,3273],"refActorComparisonType":5,"refActorType":1},{"Name":"Three","type":1,"overlayVOffset":2.0,"overlayFScale":4.0,"thicc":0.0,"overlayText":"3","refActorPlaceholder":["<1>","<2>","<3>","<4>"],"refActorRequireBuff":true,"refActorBuffId":[3270,3274],"refActorComparisonType":5,"refActorType":1},{"Name":"Four","type":1,"overlayVOffset":2.0,"overlayFScale":4.0,"thicc":0.0,"overlayText":"4","refActorPlaceholder":["<1>","<2>","<3>","<4>"],"refActorRequireBuff":true,"refActorBuffId":[3271,3275],"refActorComparisonType":5,"refActorType":1}]}
```

[International] Cryptic Portal 1 (v 3.0.0.0+ required)
```
~Lv2~{"Name":"Cryptic portal 1","Group":"","ZoneLockH":[1075,1076],"DCond":5,"ElementsL":[{"Name":"Right","type":1,"offY":10.0,"radius":12.1,"color":1358954240,"refActorComparisonType":8,"includeRotation":true,"AdditionalRotation":1.5707964,"Filled":true,"refActorObjectEffectData1":64,"refActorObjectEffectData2":128,"refActorObjectEffectMax":12000},{"Name":"Left","type":1,"offY":10.0,"radius":12.1,"color":1358954240,"refActorComparisonType":8,"includeRotation":true,"AdditionalRotation":4.712389,"Filled":true,"refActorObjectEffectData1":256,"refActorObjectEffectData2":512,"refActorObjectEffectMax":12000}],"UseTriggers":true,"Triggers":[{"Duration":60.0}]}
```

[International] Cryptic Portal 3 (v 3.0.0.0+ required)
```
~Lv2~{"Name":"Cryptic portal 3","Group":"","ZoneLockH":[1075,1076],"DCond":5,"ElementsL":[{"Name":"Right","type":3,"refX":10.0,"refY":30.0,"offX":10.0,"offY":-30.0,"radius":5.0,"color":1962147584,"refActorComparisonType":8,"includeRotation":true,"Filled":true,"refActorObjectEffectData1":64,"refActorObjectEffectData2":128,"refActorObjectEffectMax":12000},{"Name":"Left","type":3,"refX":-10.0,"refY":40.0,"offX":-10.0,"offY":-40.0,"radius":5.0,"color":1962930944,"refActorComparisonType":8,"includeRotation":true,"Filled":true,"refActorObjectEffectData1":256,"refActorObjectEffectData2":512,"refActorObjectEffectMax":12000}],"UseTriggers":true,"Triggers":[{"TimeBegin":190.0,"Duration":30.0}]}
```

# Untested section
[International] 
```
~Lv2~{"Name":"◆対象者確認","Group":"シラディハ水道","ZoneLockH":[1069,1075,1076],"ElementsL":[{"Name":"被魔法ダメージ増加","type":1,"radius":0.5,"color":3361538303,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[60,494,658,1138,2091,2941,3414],"refActorComparisonType":1,"onlyVisible":true},{"Name":"ファーストターゲット","type":1,"offZ":2.76,"radius":0.0,"color":3372156928,"overlayBGColor":3372156928,"thicc":5.0,"overlayText":"1st","refActorRequireBuff":true,"refActorBuffId":[3004],"refActorComparisonType":1,"onlyVisible":true},{"Name":"セカンドターゲット","type":1,"offZ":2.76,"radius":0.0,"color":3364749567,"overlayBGColor":3364749567,"thicc":5.0,"overlayText":"2nd","refActorRequireBuff":true,"refActorBuffId":[3005],"refActorComparisonType":1,"onlyVisible":true},{"Name":"連呪の残響","type":1,"offZ":2.76,"radius":0.0,"color":4280624598,"overlayBGColor":4280624598,"overlayTextColor":3355506687,"thicc":5.0,"overlayText":"!!","refActorRequireBuff":true,"refActorBuffId":[3292],"refActorComparisonType":1,"onlyVisible":true},{"Name":"白銀の呪い","type":1,"offX":-0.2,"offZ":2.76,"radius":0.0,"color":4294967295,"overlayBGColor":4294967295,"overlayTextColor":3355443200,"thicc":5.0,"overlayText":"銀","refActorPlaceholder":["<me>"],"refActorRequireBuff":true,"refActorBuffId":[3296],"refActorComparisonType":5,"includeRotation":true,"onlyVisible":true},{"Name":"黄金の呪い","type":1,"offX":0.2,"offZ":2.76,"radius":0.0,"color":4294967295,"overlayBGColor":4278253567,"overlayTextColor":4278190080,"thicc":5.0,"overlayText":"金","refActorPlaceholder":["<me>"],"refActorRequireBuff":true,"refActorBuffId":[3295],"refActorComparisonType":5,"includeRotation":true,"onlyVisible":true},{"Name":"一の呪印","type":1,"offZ":2.76,"radius":0.0,"color":0,"overlayBGColor":4278190091,"overlayTextColor":4293984511,"overlayFScale":1.2,"thicc":5.0,"overlayText":"","refActorPlaceholder":["<me>"],"refActorRequireBuff":true,"refActorBuffId":[3268],"refActorComparisonType":5,"includeRotation":true,"onlyVisible":true},{"Name":"二の呪印","type":1,"offZ":2.76,"radius":0.0,"color":0,"overlayBGColor":4278190091,"overlayTextColor":4293984511,"overlayFScale":1.2,"thicc":5.0,"overlayText":"","refActorPlaceholder":["<me>"],"refActorRequireBuff":true,"refActorBuffId":[3269],"refActorComparisonType":5,"includeRotation":true,"onlyVisible":true},{"Name":"三の呪印","type":1,"offZ":2.76,"radius":0.0,"color":0,"overlayBGColor":4278190091,"overlayTextColor":4293984511,"overlayFScale":1.2,"thicc":5.0,"overlayText":"","refActorPlaceholder":["<me>"],"refActorRequireBuff":true,"refActorBuffId":[3270],"refActorComparisonType":5,"includeRotation":true,"onlyVisible":true},{"Name":"四の呪印","type":1,"offZ":2.76,"radius":0.0,"color":0,"overlayBGColor":4278190091,"overlayTextColor":4293984511,"overlayFScale":1.2,"thicc":5.0,"overlayText":"","refActorPlaceholder":["<me>"],"refActorRequireBuff":true,"refActorBuffId":[3271],"refActorComparisonType":5,"includeRotation":true,"onlyVisible":true},{"Name":"火焔の呪印：一","type":1,"offZ":2.76,"radius":0.0,"color":0,"overlayBGColor":4278190091,"overlayTextColor":4278255395,"overlayFScale":1.2,"thicc":5.0,"overlayText":"","refActorPlaceholder":["<me>"],"refActorRequireBuff":true,"refActorBuffId":[3272],"refActorComparisonType":5,"includeRotation":true,"onlyVisible":true},{"Name":"火焔の呪印：二","type":1,"offZ":2.76,"radius":0.0,"color":0,"overlayBGColor":4278190091,"overlayTextColor":4278255395,"overlayFScale":1.2,"thicc":5.0,"overlayText":"","refActorPlaceholder":["<me>"],"refActorRequireBuff":true,"refActorBuffId":[3273],"refActorComparisonType":5,"includeRotation":true,"onlyVisible":true},{"Name":"火焔の呪印：三","type":1,"offZ":2.76,"radius":0.0,"color":0,"overlayBGColor":4278190091,"overlayTextColor":4278255395,"overlayFScale":1.2,"thicc":5.0,"overlayText":"","refActorPlaceholder":["<me>"],"refActorRequireBuff":true,"refActorBuffId":[3274],"refActorComparisonType":5,"includeRotation":true,"onlyVisible":true},{"Name":"火焔の呪印：四","type":1,"offZ":2.76,"radius":0.0,"color":0,"overlayBGColor":4278190091,"overlayTextColor":4278255395,"overlayFScale":1.2,"thicc":5.0,"overlayText":"","refActorPlaceholder":["<me>"],"refActorRequireBuff":true,"refActorBuffId":[3275],"refActorComparisonType":5,"includeRotation":true,"onlyVisible":true}]}
```

[International] Squeaky clean mechanic
```
~Lv2~{"Name":"◆水拭き-左","Group":"シラディハ水道","ZoneLockH":[1069,1075,1076],"DCond":5,"ElementsL":[{"Name":"左++","type":1,"offY":7.91,"radius":0.5,"color":4294901776,"thicc":5.0,"refActorNPCNameID":11369,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":4.468043}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"Match":"(11369>30549)"}]}
```
```
~Lv2~{"Name":"◆水拭き-右","Group":"シラディハ水道","ZoneLockH":[1069,1075,1076],"DCond":5,"ElementsL":[{"Name":"右++","type":1,"offY":7.91,"radius":0.5,"color":4294901776,"thicc":5.0,"refActorNPCNameID":11369,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":1.8151424}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"Match":"(11369>30550)"}]}
```
