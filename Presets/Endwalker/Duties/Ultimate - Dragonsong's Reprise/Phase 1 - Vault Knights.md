[International] Empty/Full Dimension Ring: Places a ring around Ser Grinnaux that displays the edge of Empty/Full Dimension when it is being cast. Additionally shows a red danger zone around Ser Grinnaux if he is casting Full Dimension to tell you to go out.
```
DSR P1 Empty/Full Dimension Ring~{"ZoneLockH":[968],"DCond":5,"Elements":{"1":{"type":1,"radius":2.0,"thicc":5.0,"refActorNPCNameID":3639,"refActorComparisonType":6,"includeHitbox":true},"2":{"type":1,"radius":2.0,"color":503316735,"thicc":5.0,"refActorNPCNameID":3639,"refActorRequireCast":true,"refActorCastId":[25307],"refActorComparisonType":6,"includeHitbox":true,"Filled":true}},"UseTriggers":true,"Triggers":[{"TimeBegin":17.2,"Duration":6.0},{"TimeBegin":98.2,"Duration":6.0}],"Phase":1}
```

### Hyperdimensional Slash (Spreads and Stacks)
[International] Slash Safespots: Places a circle showing roughly how far away you need to be with blue lines for individual positioning to help with uptime.
```
DSR P1 Slash Safespots~{"ZoneLockH":[968],"DCond":5,"Elements":{"Circle":{"refX":100.0,"refY":100.0,"radius":9.5,"thicc":5.0,"refActorType":1},"Pos1":{"type":2,"refX":107.86864,"refY":105.34262,"offX":108.24861,"offY":104.72231,"radius":0.0,"color":3372023808,"thicc":15.0},"Pos2":{"type":2,"refX":104.14739,"refY":108.542496,"refZ":-9.536743E-07,"offX":104.73123,"offY":108.235725,"offZ":4.7683716E-07,"radius":0.0,"color":3372023808,"thicc":15.0},"Pos3":{"type":2,"refX":95.846466,"refY":108.54908,"refZ":4.7683716E-07,"offX":95.26983,"offY":108.24042,"radius":0.0,"color":3372023808,"thicc":15.0},"Pos4":{"type":2,"refX":92.13487,"refY":105.32302,"refZ":4.7683716E-07,"offX":91.76572,"offY":104.722046,"offZ":-4.7683716E-07,"radius":0.0,"color":3372023808,"thicc":15.0},"Pos5":{"type":2,"refX":91.76442,"refY":95.25914,"refZ":-4.7683716E-07,"offX":92.13521,"offY":94.65761,"offZ":4.7683716E-07,"radius":0.0,"color":3372023808,"thicc":15.0},"Pos6":{"type":2,"refX":95.85496,"refY":91.464424,"refZ":-2.3841858E-07,"offX":95.25607,"offY":91.75623,"offZ":2.3841858E-07,"radius":0.0,"color":3372023808,"thicc":15.0},"Pos7":{"type":2,"refX":104.14046,"refY":91.447266,"offX":104.74052,"offY":91.77505,"radius":0.0,"color":3372023808,"thicc":15.0},"Pos8":{"type":2,"refX":107.85812,"refY":94.680984,"refZ":9.536743E-07,"offX":108.23496,"offY":95.28196,"radius":0.0,"color":3372023808,"thicc":15.0}},"UseTriggers":true,"Triggers":[{"TimeBegin":37.0,"Duration":15.0}],"Phase":1}
```

[EN, JP] Safe spots for Hyperdimensional Slash
```
~Lv2~{"Name":"◆絶竜詩【P1】シェイカー/Hyperdimensional Slash","Group":"◆絶竜詩【P1】","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"1","refX":82.85289,"refY":90.18456,"refZ":-5.722046E-06,"radius":1.3,"color":3369542911,"thicc":5.0},{"Name":"2","refX":90.09933,"refY":82.84264,"refZ":3.8146973E-06,"radius":1.3,"color":3369542911,"thicc":5.0},{"Name":"3","refX":109.8247,"refY":82.96349,"refZ":-3.8146968E-06,"radius":1.3,"color":3369542911,"thicc":5.0},{"Name":"4","refX":117.291046,"refY":90.11034,"refZ":-3.8146968E-06,"radius":1.3,"color":3369542911,"thicc":5.0},{"Name":"5","refX":117.146805,"refY":109.96192,"refZ":-5.7220454E-06,"radius":1.3,"color":3372172800,"thicc":5.0},{"Name":"6","refX":109.90151,"refY":117.17602,"refZ":-1.9073485E-06,"radius":1.3,"color":3372172800,"thicc":5.0},{"Name":"7","refX":90.12164,"refY":117.183,"refZ":-3.8146973E-06,"radius":1.3,"color":3372172800,"thicc":5.0},{"Name":"8","refX":82.9111,"refY":109.916756,"refZ":-5.722046E-06,"radius":1.3,"color":3372172800,"thicc":5.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":15.0,"Match":"聖騎士グリノーは「ハイパーディメンション」の構え。","MatchIntl":{"En":"Ser Grinnaux readies Hyperdimensional Slash."}}]}
```

### Shining Blade (Dashes)
[International] Knockback Tether: Tethers Adelphel when he jumps to help locate where you need to get knocked back to.
```
DSR P1 Knockback Tether~{"ZoneLockH":[968],"DCond":5,"Elements":{"1":{"type":1,"radius":0.0,"thicc":5.0,"refActorNPCNameID":3634,"refActorComparisonType":6,"tether":true}},"UseTriggers":true,"Triggers":[{"TimeBegin":53.6,"Duration":9.0,"ResetOnTChange":false}],"Phase":1}
```

[International] Aetherial Tear Circles: Puts red circles around all of the Aetherial Tears that indicates their death zone.
```
DSR P1 Aetherial Tear AoE~{"ZoneLockH":[968],"DCond":5,"Elements":{"1":{"type":1,"radius":9.0,"thicc":4.0,"refActorNPCNameID":3293,"refActorComparisonType":6},"2":{"type":1,"radius":9.0,"color":503316735,"thicc":4.0,"refActorNPCNameID":3293,"refActorComparisonType":6,"Filled":true}},"UseTriggers":true,"Triggers":[{"TimeBegin":44.8,"Duration":25.2}],"Phase":1}
```

### Holy Chains (Playstation)
[International] Ser Grinnaux Knockback Helper: Draws a small circle around the Ser Grinnaux's hitbox to help with the knockback.
```
DSR P1 Grinnaux Knockback~{"ZoneLockH":[968],"DCond":5,"Elements":{"1":{"type":1,"radius":2.0,"color":3370581760,"refActorNPCNameID":3639,"refActorComparisonType":6,"onlyTargetable":true}},"UseTriggers":true,"Triggers":[{"TimeBegin":75.0,"Duration":10.0}],"Phase":1}
```

### Planar Prison (Transition)
[EN] Brightwing Cone: Displays a cone from Charibert towards you that indicates the size of the cone. Disappears when Brightwing hits you.
```
DSR P1 Prison Cone~{"ZoneLockH":[968],"DCond":5,"Elements":{"1":{"type":4,"refX":-714.8652,"refY":-644.2318,"refZ":26.868929,"radius":10.0,"coneAngleMin":-15,"coneAngleMax":15,"refActorName":"Ser Charibert","includeRotation":true,"onlyTargetable":true,"Filled":true,"FaceMe":true}},"UseTriggers":true,"Triggers":[{"Type":2,"Match":"You suffer the effect of Planar Imprisonment."},{"Type":3,"Match":"You suffer the effect of Skyblind."}],"Phase":1}
```

[EN] Skyblind Circles: Displays a circle around all players who have Skyblind on them. Disappears when Skyblind drops onto the floor.
```
DSR P1 Prison Skyblind~{"DCond":5,"Elements":{"1":{"type":1,"radius":2.0,"thicc":4.0,"refActorRequireBuff":true,"refActorBuffId":[2661],"refActorComparisonType":1}},"UseTriggers":true,"Triggers":[{"Type":2,"Duration":60.0,"Match":"You suffer the effect of Planar Imprisonment."}],"Phase":1}
```
