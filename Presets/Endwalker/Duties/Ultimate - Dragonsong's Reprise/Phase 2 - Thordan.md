[EN, JP] Ascalon's Mercy Move Reminder: Flashes "MOVE" on the screen when Ascalon's Mercy is fully cast to remind you to move. 

(While Splatoon isn't really designed for being general-purpose trigger system, it can be used as such)
```
~Lv2~{"Name":"DSR P2 Move Trigger","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":0.0,"overlayBGColor":4278190335,"overlayVOffset":3.0,"overlayFScale":8.0,"thicc":0.0,"overlayText":"MOVE","refActorType":1}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"King Thordan readies Ascalon's Mercy Concealed.","Jp":"騎神トールダンは「インビジブル・アスカロンメルシー」の構え。"},"MatchDelay":2.6}],"Phase":2}
```

[International] Thordan Jump Tether: Tethers Thordan when he jumps to make it easier to locate him during Strength and Sanctity
```
~Lv2~{"Name":"DSR P2 Thordan Jump Tether","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":0.0,"color":3372158464,"overlayBGColor":4294911744,"overlayVOffset":3.0,"thicc":19.9,"refActorNPCNameID":3632,"refActorComparisonType":6,"onlyVisible":true,"tether":true}],"UseTriggers":true,"Triggers":[{"TimeBegin":49.5,"Duration":3.0},{"TimeBegin":102.0,"Duration":10.0}],"Phase":2}
```

## Strength of the Ward
[EN, JP] Divebomb Helper: Shows both divebomb safespots
```
~Lv2~{"Name":"DSR P2 Strength Divebombs","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Ser Vellguine","type":3,"refY":50.0,"radius":4.0,"color":1677721855,"refActorNPCNameID":3636,"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyUnTargetable":true},{"Name":"Ser Ignasse","type":3,"refY":50.0,"radius":4.0,"color":1677721855,"refActorNPCNameID":3638,"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyUnTargetable":true},{"Name":"Ser Paulecrain","type":3,"refY":50.0,"radius":4.0,"color":1677721855,"refActorNPCNameID":3637,"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyUnTargetable":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":7.0,"MatchIntl":{"En":"King Thordan readies Strength of the Ward.","Jp":"騎神トールダンは「蒼天の陣：雷槍」の構え。"},"MatchDelay":8.0}],"Phase":2}
```

[EN] Heavy Impact Rings: Places rings around Ser Guerrique indicating the size of the quake rings from Heavy Impact [Translation required: trigger]
```
DSR P2 Quake markers~{"ZoneLockH":[968],"DCond":5,"Elements":{"Quake marker":{"type":1,"radius":6.0,"Donut":0.0,"color":4293721856,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true},"2":{"type":1,"radius":12.0,"Donut":0.0,"color":4293721856,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true},"3":{"type":1,"radius":18.0,"Donut":0.0,"color":4293721856,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true},"4":{"type":1,"radius":24.0,"Donut":0.0,"color":4293721856,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true}},"UseTriggers":true,"Triggers":[{"Type":2,"Duration":12.0,"MatchIntl":{"En":"Ser Paulecrain readies Spiral Thrust"}}],"Phase":2}
```

[EN] Sequential Heavy Impact Rings: Displays the quake markers sequentially instead of all at once.
```
DSR P2 Strength Quake 1~{"ZoneLockH":[968],"DCond":5,"Elements":{"1":{"type":1,"radius":6.0,"color":4278190335,"thicc":4.0,"refActorName":"Ser Guerrique","includeRotation":true,"onlyUnTargetable":true},"2":{"type":1,"radius":0.0,"thicc":5.0,"refActorName":"Ser Guerrique","tether":true}},"UseTriggers":true,"Triggers":[{"TimeBegin":35.0,"Duration":10.0},{"Type":3,"Match":"Ser Guerrique uses Heavy Impact.","MatchDelay":1.9}],"Phase":2}
```
```
[EN] DSR P2 Strength Quake 2~{"ZoneLockH":[968],"DCond":5,"Elements":{"1":{"type":1,"radius":12.0,"color":4278190335,"thicc":4.0,"refActorName":"Ser Guerrique","includeRotation":true,"onlyUnTargetable":true}},"UseTriggers":true,"Triggers":[{"Type":2,"TimeBegin":41.0,"Duration":3.8,"Match":"Ser Guerrique readies Heavy Impact.","MatchDelay":6.0}],"Phase":2}
```
```
[EN] DSR P2 Strength Quake 3~{"ZoneLockH":[968],"DCond":5,"Elements":{"1":{"type":1,"radius":18.0,"color":4278190335,"thicc":4.0,"refActorName":"Ser Guerrique","includeRotation":true,"onlyUnTargetable":true}},"UseTriggers":true,"Triggers":[{"Type":2,"TimeBegin":43.5,"Duration":3.8,"Match":"Ser Guerrique readies Heavy Impact.","MatchDelay":7.9}],"Phase":2}
```
```
[EN] DSR P2 Strength Quake 4~{"ZoneLockH":[968],"DCond":5,"Elements":{"1":{"type":1,"radius":24.0,"color":4278190335,"thicc":4.0,"refActorName":"Ser Guerrique","includeRotation":true,"onlyUnTargetable":true}},"UseTriggers":true,"Triggers":[{"Type":2,"TimeBegin":45.5,"Duration":1.9,"Match":"Ser Guerrique readies Heavy Impact.","MatchDelay":9.8}],"Phase":2}
```

[EN, JP] [Untested] A big line predicting Thordan location
```
~Lv2~{"Name":"◆絶竜詩【P2】Find Thordan early|雷槍/聖騎士エルムノスト","Group":"◆絶竜詩【P2】","DCond":5,"ElementsL":[{"Name":"yoko","type":3,"refX":-24.0,"refY":5.0,"offX":-24.0,"offY":-5.0,"radius":0.3,"color":3372167936,"refActorNPCNameID":3640,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true,"AdditionalRotation":1.5707964},{"Name":"左ななめ","type":3,"refY":28.0,"refZ":0.1,"offX":-2.0,"offY":24.0,"offZ":0.1,"radius":0.3,"refActorNPCNameID":3640,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true},{"Name":"右ななめ","type":3,"refY":28.0,"refZ":0.1,"offX":2.0,"offY":24.0,"offZ":0.1,"radius":0.3,"refActorNPCNameID":3640,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true},{"Name":"tate","type":3,"refY":24.0,"offY":18.0,"radius":0.2,"color":3372167936,"refActorNPCNameID":3640,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true},{"Name":"migiue hidarisita","type":2,"Enabled":false,"refX":117.985756,"refY":82.13104,"offX":82.016975,"offY":117.873405,"offZ":2.3841858E-07,"radius":0.0,"color":3355508651},{"Name":"hidari migisita","type":2,"Enabled":false,"refX":82.0192,"refY":82.152534,"offX":117.968445,"offY":117.84381,"offZ":2.3841858E-07,"radius":0.0,"color":3355508651},{"Name":"縦","type":2,"Enabled":false,"refX":99.98872,"refY":77.0,"refZ":-4.7683716E-07,"offX":100.00099,"offY":123.0,"offZ":2.3841858E-07,"radius":0.0,"color":3355508651},{"Name":"横","type":2,"Enabled":false,"refX":123.0,"refY":99.99874,"offX":77.0,"offY":100.00416,"radius":0.0,"color":3355508651},{"Name":"C","type":2,"refX":100.02311,"refY":119.634766,"offX":100.02311,"offY":121.0,"radius":0.0,"color":3355508651},{"Name":"2","type":2,"refX":113.893585,"refY":113.76301,"offX":114.81648,"offY":114.71568,"offZ":9.536743E-07,"radius":0.0,"color":3355508651},{"Name":"B","type":2,"refX":119.63167,"refY":100.017006,"offX":121.0,"offY":100.017006,"radius":0.0,"color":3355508651},{"Name":"1","type":2,"refX":113.850105,"refY":86.22235,"refZ":3.8146973E-06,"offX":114.687004,"offY":85.39957,"radius":0.0,"color":3355508651},{"Name":"3","type":2,"refX":86.007355,"refY":113.893654,"offX":85.14036,"offY":114.780846,"radius":0.0,"color":3355508651},{"Name":"D","type":2,"refX":80.365585,"refY":99.99159,"offX":79.0,"offY":100.01565,"radius":0.0,"color":3355508651},{"Name":"4","type":2,"refX":85.05639,"refY":85.17678,"refZ":9.536743E-07,"offX":86.01939,"offY":86.119255,"offZ":-9.536743E-07,"radius":0.0,"color":3355508651},{"Name":"A","type":2,"refX":100.0,"refY":79.0,"refZ":-4.7683716E-07,"offX":100.02658,"offY":80.3605,"offZ":-1.9073486E-06,"radius":0.0,"color":3355508651}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":25.0,"Match":"騎神トールダンの「蒼天の陣：雷槍」","MatchIntl":{"En":"King Thordan readies Strength of the Ward."},"MatchDelay":5.0}]}
```

[International] Party Positions: Places blue circles on the spots where the party stack, two tankbusters and 3 defam dives should be.
```
DSR P2 Strength Positions~{"ZoneLockH":[968],"DCond":5,"Elements":{"1":{"type":1,"offX":6.3,"offY":3.25,"radius":0.3,"color":3372154880,"thicc":4.0,"refActorDataID":12604,"refActorComparisonType":3,"includeRotation":true},"2":{"type":1,"offX":-6.3,"offY":3.25,"radius":0.3,"color":3372154880,"thicc":4.0,"refActorDataID":12604,"refActorComparisonType":3,"includeRotation":true},"3":{"type":1,"offY":2.5,"radius":0.3,"color":3372154880,"thicc":4.0,"refActorDataID":12604,"refActorComparisonType":3,"includeRotation":true},"4":{"type":1,"offY":43.0,"radius":0.3,"color":3372154880,"thicc":4.0,"refActorDataID":12604,"refActorComparisonType":3,"includeRotation":true},"5":{"type":1,"offX":20.0,"offY":26.0,"radius":0.3,"color":3372154880,"thicc":4.0,"refActorDataID":12604,"refActorComparisonType":3,"includeRotation":true},"6":{"type":1,"offX":-20.0,"offY":26.0,"radius":0.3,"color":3372154880,"thicc":4.0,"refActorDataID":12604,"refActorComparisonType":3,"includeRotation":true}},"UseTriggers":true,"Triggers":[{"TimeBegin":52.0,"Duration":8.0}]}
```

## Sanctity of the Ward
[International] DRK Tether: Locates the DRK (Ser Zephirin) with a tether during Sanctity of the Ward for use with the DRK Relative strat
```
~Lv2~{"Name":"DSR P2 Sanctity DRK Tether","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":0.0,"color":3372154880,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"onlyVisible":true,"tether":true}],"UseTriggers":true,"Triggers":[{"TimeBegin":100.8,"Duration":9.2}],"Phase":2}
```

[International] DRK Safespots: Indicates the two possible safespots for the DRK Relative strat on both sides of the arena.
```
~Lv2~{"Name":"DSR P2 Sanctity DRK Starting Spots","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"G1 CCW","type":1,"offX":4.0,"offY":-5.0,"radius":0.5,"color":3372158208,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true},{"Name":"G1 CW","type":1,"offX":-4.0,"offY":-5.0,"radius":0.5,"color":3372158208,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true},{"Name":"G2 CCW","type":1,"offX":4.0,"offY":35.0,"radius":0.5,"color":3372158208,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true},{"Name":"G2 CW","type":1,"offX":-4.0,"offY":35.0,"radius":0.5,"color":3372158208,"thicc":5.0,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}],"UseTriggers":true,"Triggers":[{"TimeBegin":100.8,"Duration":9.2}],"Phase":2}
```

[International] PLD Facing Arrows: Places an arrow on both PLDs (Ser Adelphel and Ser Janlenoux) that shows which direction they're facing so you know whether to move Clockwise or Counter-Clockwise.
```
~Lv2~{"Name":"DSR P2 Sanctity PLD Arrows","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Adelphel","type":3,"refY":4.0,"radius":0.0,"color":3372154880,"thicc":10.0,"refActorNPCNameID":3634,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true},{"Name":"Adelphel 2","type":3,"refY":4.0,"offX":0.5,"offY":1.5,"radius":0.0,"color":3372154880,"thicc":10.0,"refActorNPCNameID":3634,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true},{"Name":"Adelphel 3","type":3,"refY":4.0,"offX":-0.5,"offY":1.5,"radius":0.0,"color":3372154880,"thicc":10.0,"refActorNPCNameID":3634,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true},{"Name":"Janlenoux","type":3,"refY":4.0,"radius":0.0,"color":3372154880,"thicc":10.0,"refActorNPCNameID":3635,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true},{"Name":"Janlenoux 2","type":3,"refY":4.0,"offX":0.5,"offY":1.5,"radius":0.0,"color":3372154880,"thicc":10.0,"refActorNPCNameID":3635,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true},{"Name":"Janlenoux 3","type":3,"refY":4.0,"offX":-0.5,"offY":1.5,"radius":0.0,"color":3372154880,"thicc":10.0,"refActorNPCNameID":3635,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}],"UseTriggers":true,"Triggers":[{"TimeBegin":100.8,"Duration":7.0}],"Phase":2}
```

[EN, JP] [Untested] Display possible safespots where you need to move on Brightspheres
```
~Lv2~{"Name":"◆絶竜詩【P2】Sanctity brightsphere safespots 聖杖/MK-SE2","Group":"◆絶竜詩【P2】","DCond":5,"ElementsL":[{"Name":"01","refX":103.32077,"refY":80.019066,"refZ":-4.7683716E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"02","refX":111.547455,"refY":83.35025,"refZ":-9.536743E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"03","refX":116.39796,"refY":88.11824,"refZ":9.536743E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"04","refX":119.9646,"refY":96.63612,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"05","refX":119.94625,"refY":103.332954,"refZ":4.7683716E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"06","refX":116.53317,"refY":111.625496,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"07","refX":111.77236,"refY":116.38963,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"08","refX":103.35565,"refY":119.87426,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"09","refX":96.70261,"refY":119.89157,"refZ":-9.536743E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"10","refX":88.35822,"refY":116.45416,"refZ":-9.536743E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"11","refX":83.64629,"refY":111.781685,"refZ":-4.7683716E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"12","refX":80.10479,"refY":103.29403,"refZ":-4.7683716E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"13","refX":80.11111,"refY":96.66466,"refZ":-9.536743E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"14","refX":83.535866,"refY":88.31299,"refZ":9.536743E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"15","refX":88.195404,"refY":83.58982,"refZ":-9.536743E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0},{"Name":"16","refX":96.62547,"refY":80.02041,"refZ":9.536743E-07,"radius":0.2,"color":3355508651,"overlayBGColor":3355508651,"overlayTextColor":3355443200,"thicc":4.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":25.0,"Match":"蒼天の陣：聖杖」の構え。","MatchIntl":{"En":"King Thordan readies Sanctity of the Ward."}}]}
```

[International] DSR P2 Sanctity - DPS meteor beacons except self. Very easily see whether you need to swap with someone or not. If you are playing healer, modify placeholders to be `<h2>`, `<t1>`, `<t2>`. If you are playing tank - `<h1>`, `<h2>`, `<t2>`.
```
~Lv2~{"Name":"DSR P2 DPS meteors","Group":"DSR","ZoneLockH":[968],"ElementsL":[{"Name":"","type":3,"offZ":8.0,"radius":0.0,"color":3372154896,"thicc":50.0,"refActorPlaceholder":["<d2>","<d3>","<d4>"],"refActorRequireBuff":true,"refActorBuffId":[562],"refActorUseBuffTime":true,"refActorBuffTimeMin":18.0,"refActorBuffTimeMax":50.0,"refActorComparisonType":5}],"Phase":2}
```

[EN] [Beta] DSR P2 Sanctity - display opposite towers when meteor is on YOU. 
```
~Lv2~{"Name":"DSR P2 Opposite towers","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"","type":3,"refZ":10.0,"radius":0.0,"thicc":27.0,"refActorModelID":480,"refActorPlaceholder":[],"refActorNPCNameID":3640,"refActorComparisonAnd":true,"refActorComparisonType":6}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"MatchIntl":{"En":"You suffer the effect of Prey"}}],"MinDistance":26.5,"MaxDistance":100.0,"UseDistanceLimit":true,"DistanceLimitType":1,"Phase":2}
```

[Untested] [EN] DSR P2 Sanctity 2nd tower - Displays appropriate 2nd N/S tower to take after running your meteor.
```
~Lv2~{"Name":"DSR P2 Meteor 2nd Tower","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"N1","refX":100.0,"refY":82.0,"refZ":-3.8146973E-06,"radius":3.0,"color":4278255413,"thicc":5.0,"refActorModelID":480,"refActorPlaceholder":[],"refActorNPCNameID":3640,"refActorComparisonAnd":true,"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":97.154175,"DistanceSourceY":117.32654,"DistanceSourceZ":-3.8146973E-06},{"Name":"N2","refX":100.0,"refY":82.0,"refZ":-3.8146973E-06,"radius":3.0,"color":1677786933,"thicc":5.0,"refActorModelID":480,"refActorPlaceholder":[],"refActorNPCNameID":3640,"refActorComparisonAnd":true,"refActorComparisonType":6,"Filled":true,"LimitDistance":true,"DistanceSourceX":97.154175,"DistanceSourceY":117.32654,"DistanceSourceZ":-3.8146973E-06},{"Name":"S1","refX":100.0,"refY":118.0,"refZ":-3.8146973E-06,"radius":3.0,"color":4278255413,"thicc":5.0,"refActorModelID":480,"refActorPlaceholder":[],"refActorNPCNameID":3640,"refActorComparisonAnd":true,"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":97.154175,"DistanceSourceY":117.32654,"DistanceSourceZ":-3.8146973E-06},{"Name":"S2","refX":100.0,"refY":118.0,"refZ":-3.8146973E-06,"radius":3.0,"color":1677786933,"thicc":5.0,"refActorModelID":480,"refActorPlaceholder":[],"refActorNPCNameID":3640,"refActorComparisonAnd":true,"refActorComparisonType":6,"Filled":true,"LimitDistance":true,"DistanceSourceX":97.154175,"DistanceSourceY":117.32654,"DistanceSourceZ":-3.8146973E-06}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"MatchIntl":{"En":"You suffer the effect of Prey"},"MatchDelay":17.0}],"MaxDistance":20.0,"UseDistanceLimit":true,"DistanceLimitType":1}
```

### Scripts

[International][Beta][Untested] DSR P2 Sanctity First
- Settings are required.
- Displays the spread positions, clockwise and counterclockwise.
- Please set the name of the pair you should focus on and the usual spread position. For example,

Pair to focus on: Hoge Hogee

Spread position: Opposite Zephiran

In this case, input:
```
Hoge Hogee
ZephiranFaceToFace
```

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Endwalker/Dragonsong's%20Reprise/P2%20Sanctity%20Of%20The%20Ward%20First.cs
```

[International][Beta][Untested] DSR P2 Sanctity Second
- Settings are not required.
- Displays the next tower to step on according to your spread position.
- Locks the face when gaze guidance is needed.

> [!CAUTION]
> The gaze-locking feature is unstable. If you can manage it yourself, it is recommended to turn it off.

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Endwalker/Dragonsong's%20Reprise/P2%20Sanctity%20Of%20The%20Ward%20Second.cs
```

## Final phase
[International] [Untested] Ultimate End pizza slices
```
~Lv2~{"Name":"◆絶竜詩【P2】ULTIMATE-END-PIZZA","Group":"◆絶竜詩【P2】","DCond":1,"ElementsL":[{"Name":"001","type":3,"refY":5.0,"radius":0.01,"thicc":1.0,"refActorNPCNameID":3632,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"002","type":3,"refY":5.0,"radius":0.01,"color":3372155032,"refActorNPCNameID":3632,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"AdditionalRotation":2.0943952},{"Name":"003","type":3,"refY":5.0,"radius":0.01,"color":3372155032,"refActorNPCNameID":3632,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"AdditionalRotation":4.1887903},{"Name":"大振り→","type":1,"offX":2.0,"overlayBGColor":592141,"overlayTextColor":3355443200,"overlayText":"→","refActorNPCNameID":3632,"refActorRequireCast":true,"refActorCastId":[25536],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"Filled":true},{"Name":"大振り←","type":1,"offX":-2.0,"color":3372171264,"overlayBGColor":592141,"overlayTextColor":3355443200,"overlayText":"←","refActorNPCNameID":3632,"refActorRequireCast":true,"refActorCastId":[25537],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"Filled":true},{"Name":"騎竜神トールダン","type":1,"radius":8.0,"refActorNPCNameID":3632,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}]}
```
