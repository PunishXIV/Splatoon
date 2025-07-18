[Script] Feather Rain 
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Stormblood/UWU%20Feather%20Rain.cs
```

Marks BOTH potential safespots for Titan's Upheaval Knockback. You will still have to determine the correct one yourself.
```
~Lv2~{"Name":"UWU Titan Upheaval","Group":"UWU","ZoneLockH":[777],"DCond":5,"ElementsL":[{"Name":"left","type":1,"offX":0.5,"offY":4.6,"radius":0.25,"thicc":5.0,"refActorType":2,"includeRotation":true},{"Name":"right","type":1,"offX":-0.5,"offY":4.6,"radius":0.25,"thicc":5.0,"refActorType":2,"includeRotation":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":4.0,"Match":"titan readies upheaval"}]}
```

Marks all potential safespots for Predation, and draws lines pointing away from Garuda. You will still need to determine the correct direction yourself based on Titan & Ultima positions.
```
~Lv2~{"Name":"UWU Predation","Group":"UWU","ZoneLockH":[777],"DCond":5,"ElementsL":[{"Name":"Direction","type":3,"refY":3.0,"offX":3.0,"offY":6.0,"radius":0.0,"thicc":5.0,"refActorName":"Garuda","includeRotation":true,"onlyVisible":true},{"Name":"Direction2","type":3,"refY":3.0,"offX":-3.0,"offY":6.0,"radius":0.0,"thicc":5.0,"refActorName":"Garuda","includeRotation":true,"onlyVisible":true},{"Name":"Safespot1","refX":100.0,"refY":100.0,"refZ":1.4305115E-06,"offX":-6.5,"offY":-17.8,"radius":0.25,"color":3372180480,"thicc":5.0},{"Name":"Safespot2","refX":100.0,"refY":100.0,"refZ":1.4305115E-06,"offX":6.5,"offY":-17.8,"radius":0.25,"color":3372180480,"thicc":5.0},{"Name":"Safespot3","refX":100.0,"refY":100.0,"refZ":1.4305115E-06,"offX":17.8,"offY":-6.5,"radius":0.25,"color":3372180480,"thicc":5.0},{"Name":"Safespot4","refX":100.0,"refY":100.0,"refZ":1.4305115E-06,"offX":17.8,"offY":6.5,"radius":0.25,"color":3372180480,"thicc":5.0},{"Name":"Safespot5","refX":100.0,"refY":100.0,"refZ":1.4305115E-06,"offX":6.5,"offY":17.8,"radius":0.25,"color":3372180480,"thicc":5.0},{"Name":"Safespot6","refX":100.0,"refY":100.0,"refZ":1.4305115E-06,"offX":-6.5,"offY":17.8,"radius":0.25,"color":3372180480,"thicc":5.0},{"Name":"Safespot7","refX":100.0,"refY":100.0,"refZ":1.4305115E-06,"offX":-17.8,"offY":6.5,"radius":0.25,"color":3372180480,"thicc":5.0},{"Name":"Safespot8","refX":100.0,"refY":100.0,"refZ":1.4305115E-06,"offX":-17.8,"offY":-6.5,"radius":0.25,"color":3372180480,"thicc":5.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":25.0,"Match":"The Ultima Weapon uses Ultimate Predation"}]}
```

Draws a tether to the center to remind you to bait Landslide during Titan Gaols. Also indicates positions for the backmost gaol so it'll get hit by the initial rock explosion. Again, you will have to determine the correct one yourself.
```
~Lv2~{"Name":"UWU Titan Gaols","Group":"UWU","ZoneLockH":[777],"DCond":5,"ElementsL":[{"Name":"center","refX":100.0,"refY":100.0,"color":3372180480,"thicc":5.0,"tether":true},{"Name":"left","refX":98.0,"refY":106.3,"color":3372180480,"thicc":5.0},{"Name":"right","refX":102.0,"refY":106.3,"color":3372180480,"thicc":5.0},{"Name":"left1","refX":106.3,"refY":98.0,"color":3372180480,"thicc":5.0},{"Name":"right1","refX":106.3,"refY":102.0,"color":3372180480,"thicc":5.0},{"Name":"left2","refX":93.7,"refY":98.0,"color":3372180480,"thicc":5.0},{"Name":"right2","refX":93.7,"refY":102.0,"color":3372180480,"thicc":5.0},{"Name":"left3","refX":102.0,"refY":93.7,"color":3372180480,"thicc":5.0},{"Name":"right3","refX":98.0,"refY":93.7,"color":3372180480,"thicc":5.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"Match":"titan uses upheaval"}]}
```

Simple border during Suppression to avoid running into feathers.
```
~Lv2~{"Name":"UWU Suppression Border","Group":"UWU","ZoneLockH":[777],"DCond":5,"ElementsL":[{"Name":"border","refX":100.0,"refY":100.0,"radius":15.0,"thicc":10.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":20.0,"Match":"The Ultima Weapon uses Ultimate Suppression"}]}
```

Shows indicators for several primal mechanics, for example Slipstream, Searing Wind, Ifrit Dashes
```
~Lv2~{"Name":"绝神兵","Group":"","ZoneLockH":[777],"ElementsL":[{"Name":"风神顺劈","type":4,"radius":12.0,"coneAngleMin":-45,"coneAngleMax":45,"color":1350102012,"thicc":5.0,"refActorNPCID":1644,"refActorRequireCast":true,"refActorCastId":[11091],"refActorComparisonType":4,"includeRotation":true,"Filled":true},{"Name":"火神冲","type":3,"offY":40.0,"radius":9.0,"color":1351848950,"thicc":0.0,"refActorNPCID":1185,"refActorRequireCast":true,"refActorCastId":[11103],"FillStep":5.0,"refActorComparisonType":4,"includeRotation":true},{"Name":"热风","type":1,"radius":15.0,"color":3355508715,"thicc":5.0,"refActorPlaceholder":["<h1>","<h2>"],"refActorRequireBuff":true,"refActorBuffId":[1578],"refActorComparisonType":5},{"Name":"泰坦落地","type":1,"radius":23.0,"color":1342177535,"thicc":10.0,"refActorNPCID":1801,"refActorRequireCast":true,"refActorCastId":[11110],"refActorComparisonType":4},{"Name":"冲锋","type":3,"offY":40.0,"radius":5.0,"color":1349975292,"thicc":0.0,"refActorNPCID":1185,"refActorRequireCast":true,"refActorCastId":[11104],"FillStep":5.0,"refActorComparisonType":4,"includeRotation":true},{"Name":"月环","type":1,"radius":8.0,"color":1349909939,"thicc":5.0,"refActorNPCID":1644,"refActorRequireCast":true,"refActorCastId":[11087],"refActorComparisonType":4},{"Name":"泰坦冲拳","type":3,"offY":40.0,"radius":3.0,"color":1014330871,"refActorNPCID":1801,"refActorRequireCast":true,"refActorCastId":[11120,11298],"refActorCastTimeMax":3.0,"FillStep":0.2,"refActorComparisonType":4,"includeRotation":true}]}
```
