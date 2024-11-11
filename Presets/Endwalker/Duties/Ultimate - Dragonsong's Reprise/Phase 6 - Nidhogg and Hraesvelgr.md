[International] P6 Arena Quarter
Helps with keeping melee uptime when Nidhogg dives one half of the arena in addition to Hraesvelgr cleaving the other half leaving one quarter safe. (exact trigger to be improved):
```
~Lv2~{"Name":"DSR P6 Arena Quarter","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"P6 Quarter","type":2,"refX":100.0,"refY":80.0,"offX":100.0,"offY":120.0,"radius":0.0,"color":1677721855,"FillStep":1.0},{"Name":"P6 Quarter 2","type":2,"refX":80.0,"refY":100.0,"offX":120.0,"offY":100.0,"radius":0.0,"color":1677721855,"FillStep":1.0}],"UseTriggers":true,"Triggers":[{"TimeBegin":660.0,"Duration":240.0}],"Phase":2}
```

[EN, JP] Tether to your healer during Akh Afah. Don't forget to actually put your healer's name here.
```
~Lv2~{"Name":"P6 Healer Group tether","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":0.0,"color":3356425984,"thicc":10.0,"refActorName":"YOUR HEALER'S NAME HERE","tether":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"MatchIntl":{"En":"Nidhogg readies Akh Afah.","Jp":"フレースヴェルグは「アク・アファー」の構え。"}}]}
```

[EN, JP] Akh Afah north and south spots. Tip: enable tether to your designated spot.
```
~Lv2~{"Name":"DSR P6 Akh Afah spots / アク・アファー","Group":"DSR","DCond":5,"ElementsL":[{"Name":"N / 北側","refX":100.0,"refY":95.3,"radius":1.0,"thicc":5.0},{"Name":"S / 南側","refX":100.0,"refY":104.6,"refZ":-1.9073486E-06,"radius":1.0,"color":3372213760,"thicc":5.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"MatchIntl":{"En":"Nidhogg readies Akh Afah.","Jp":"フレースヴェルグは「アク・アファー」の構え。"}}],"Phase":2}
```

[International] P6 Hallowed Wings
```
~Lv2~{"Name":"DSR P6 Hallowed Wings (main)","Group":"DSR","ZoneLockH":[968],"ElementsL":[{"Name":"Right","type":3,"refX":11.0,"refY":44.0,"offX":11.0,"radius":11.0,"color":1190788864,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27942,27943],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Left","type":3,"refX":-11.0,"refY":44.0,"offX":-11.0,"radius":11.0,"color":1190788864,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27939,27940],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true}],"Phase":2}
```

[EN, JP] P6 First hallowed wings party safe spot
```
~Lv2~{"Name":"DSR P6 Hallowed Wings (1) Safe spots","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Party far","type":1,"offX":-11.0,"offY":40.0,"radius":3.0,"color":3356032768,"overlayBGColor":4293984511,"overlayTextColor":4278190080,"overlayFScale":3.0,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27942,27939],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party far","type":1,"offX":11.0,"offY":40.0,"radius":3.0,"color":3356032768,"overlayBGColor":4293984511,"overlayTextColor":4278190080,"overlayFScale":3.0,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27942,27939],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party far","type":1,"offX":-11.0,"offY":18.0,"radius":3.0,"color":3356032768,"overlayBGColor":4293984511,"overlayTextColor":4278190080,"overlayFScale":3.0,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27942,27939],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party far","type":1,"offX":11.0,"offY":18.0,"radius":3.0,"color":3356032768,"overlayBGColor":4293984511,"overlayTextColor":4278190080,"overlayFScale":3.0,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27942,27939],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party near","type":1,"offX":-11.0,"offY":4.0,"radius":3.0,"color":3356032768,"overlayBGColor":4279631616,"overlayTextColor":4278190080,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27943,27940],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party near","type":1,"offX":11.0,"offY":4.0,"radius":3.0,"color":3356032768,"overlayBGColor":4279631616,"overlayTextColor":4278190080,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27943,27940],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party near","type":1,"offX":-11.0,"offY":26.0,"radius":3.0,"color":3356032768,"overlayBGColor":4279631616,"overlayTextColor":4278190080,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27943,27940],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party near","type":1,"offX":11.0,"offY":26.0,"radius":3.0,"color":3356032768,"overlayBGColor":4279631616,"overlayTextColor":4278190080,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27943,27940],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":30.0,"MatchIntl":{"En":"I swore to Shiva─swore that I would not take the lives of men... Stop me, I prithee!","Jp":"我はシヴァに誓ったのだ…… 決して人を殺めはしないと……頼む……！"}}],"Phase":2}
```

[EN, JP] P6 Second hallowed wings party safe spot
```
~Lv2~{"Name":"DSR P6 Hallowed Wings (2) Safe spots","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Party far","type":1,"offX":-15.0,"offY":40.0,"radius":3.0,"color":3356032768,"overlayBGColor":4293984511,"overlayTextColor":4278190080,"overlayFScale":3.0,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27942,27939],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party far","type":1,"offY":40.0,"radius":3.0,"color":3356032768,"overlayBGColor":4293984511,"overlayTextColor":4278190080,"overlayFScale":3.0,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27942,27939],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party far","type":1,"offX":15.0,"offY":40.0,"radius":3.0,"color":3356032768,"overlayBGColor":4293984511,"overlayTextColor":4278190080,"overlayFScale":3.0,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27942,27939],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party near","type":1,"offX":15.0,"offY":4.0,"radius":3.0,"color":3356032768,"overlayBGColor":4279631616,"overlayTextColor":4278190080,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27943,27940],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party near","type":1,"offY":4.0,"radius":3.0,"color":3356032768,"overlayBGColor":4279631616,"overlayTextColor":4278190080,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27943,27940],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Party near","type":1,"offX":-15.0,"offY":4.0,"radius":3.0,"color":3356032768,"overlayBGColor":4279631616,"overlayTextColor":4278190080,"thicc":5.0,"refActorNPCNameID":4954,"refActorRequireCast":true,"refActorCastId":[27943,27940],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":60.0,"MatchIntl":{"En":"Nidhogg readies Wroth Flames","Jp":"ニーズヘッグは「邪念の炎」の構え。"}}],"Phase":2}
```

[EN, JP] Tankbusters during Hallowed Wings:
```
~Lv2~{"Name":"DSR P6 TankBusters","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Marks","type":1,"radius":10.0,"color":1191116816,"refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"MatchIntl":{"En":"Hraesvelgr readies Hallowed Wings.","Jp":"フレースヴェルグは「ホーリーウィング」の構え。"}}],"Phase":2}
```

[EN, JP] Nidhogg's Cauterize:
```
~Lv2~{"Name":"DSR P6 Nidhogg's Cauterize","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"1","type":3,"refY":12.0,"offY":56.0,"radius":11.0,"color":1509968639,"refActorNPCNameID":3458,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"Match":"I swore to Shiva─swore that I would not take the lives of men... Stop me, I prithee!","MatchIntl":{"Jp":"我はシヴァに誓ったのだ"},"MatchDelay":14.0}],"Phase":2}
```

[EN, JP] Hraesvelgr's Cauterize:
```
~Lv2~{"Name":"DSR P6 Hraesvelgr's Cauterize","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"1","type":3,"refY":12.0,"offY":56.0,"radius":11.0,"color":1509968639,"refActorNPCNameID":4954,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"Match":"Nidhogg readies Akh Morn.","MatchIntl":{"Jp":"ニーズヘッグは「アク・モーン」の構え。"}}],"Phase":2}
```

[International] Nidhogg's Hot tail/Hot wings:
```
~Lv2~{"Name":"DSR P6 Hot tail / Hot wings","Group":"DSR","ZoneLockH":[968],"ElementsL":[{"Name":"Hot tail","type":3,"refY":44.0,"radius":8.0,"color":1510006527,"refActorNPCNameID":3458,"refActorRequireCast":true,"refActorCastId":[27949,27950],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Hot wing 1","type":3,"refX":-13.0,"refY":44.0,"offX":-13.0,"radius":9.0,"color":1510006527,"refActorNPCNameID":3458,"refActorRequireCast":true,"refActorCastId":[27947,27948],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true},{"Name":"Hot wing 2","type":3,"refX":13.0,"refY":44.0,"offX":13.0,"radius":9.0,"color":1510006527,"refActorNPCNameID":3458,"refActorRequireCast":true,"refActorCastId":[27947,27948],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true}],"Phase":2}
```

[EN, JP] P6 Wroth flames movement pattern. Does NOT includes indication of which spot is safe. (Movement pattern: drop 1st and 2nd aoes in respective circles, do not cross red line until game's indicator disappears, then cross green line and rush to the middle)
```
~Lv2~{"Name":"DSR P6 WF Move SW","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Start","refX":80.0,"refY":115.0,"refZ":-3.8146973E-06,"radius":5.0,"overlayBGColor":1879048447,"thicc":5.0,"overlayText":"Start"},{"Name":"Move 2","refX":88.0,"refY":115.0,"refZ":-1.9073486E-06,"radius":5.0,"overlayBGColor":1879048447,"thicc":5.0,"overlayText":"Puddle 2"},{"Name":"First aoe line","type":2,"refX":91.0,"refY":109.0,"refZ":-1.9073484E-06,"offX":91.0,"offY":122.0,"offZ":-5.722046E-06,"radius":0.0,"thicc":5.0},{"Name":"Second AOE line","type":2,"refX":96.0,"refY":122.0,"refZ":-2.861023E-06,"offX":96.0,"offY":108.0,"offZ":-4.7683716E-06,"radius":0.0,"color":3355508484,"thicc":5.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":16.0,"MatchIntl":{"En":"Nidhogg readies Wroth Flames","Jp":"ニーズヘッグは「邪念の炎」の構え。"},"MatchDelay":5.0}],"MaxDistance":20.0,"DistanceLimitType":1}
```
```
~Lv2~{"Name":"DSR P6 WF Move SE","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Start","refX":120.0,"refY":115.0,"refZ":-3.8146973E-06,"radius":5.0,"overlayBGColor":1879048447,"thicc":5.0,"overlayText":"Start"},{"Name":"Move 2","refX":112.0,"refY":115.0,"refZ":-1.9073486E-06,"radius":5.0,"overlayBGColor":1879048447,"thicc":5.0,"overlayText":"Puddle 2"},{"Name":"First aoe line","type":2,"refX":109.0,"refY":109.0,"refZ":-1.9073484E-06,"offX":109.0,"offY":122.0,"offZ":-5.722046E-06,"radius":0.0,"thicc":5.0},{"Name":"Second AOE line","type":2,"refX":104.0,"refY":122.0,"refZ":-2.861023E-06,"offX":104.0,"offY":108.0,"offZ":-4.7683716E-06,"radius":0.0,"color":3355508484,"thicc":5.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":16.0,"MatchIntl":{"En":"Nidhogg readies Wroth Flames","Jp":"ニーズヘッグは「邪念の炎」の構え。"},"MatchDelay":5.0}],"MaxDistance":10.0}
```
```
~Lv2~{"Name":"DSR P6 WF Move NW","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Start","refX":80.0,"refY":85.0,"refZ":-3.8146973E-06,"radius":5.0,"overlayBGColor":1879048447,"thicc":5.0,"overlayText":"Start"},{"Name":"Move 2","refX":88.0,"refY":85.0,"refZ":-1.9073486E-06,"radius":5.0,"overlayBGColor":1879048447,"thicc":5.0,"overlayText":"Puddle 2"},{"Name":"First aoe line","type":2,"refX":91.0,"refY":91.0,"refZ":-1.9073484E-06,"offX":91.0,"offY":78.0,"offZ":-5.722046E-06,"radius":0.0,"thicc":5.0},{"Name":"Second AOE line","type":2,"refX":96.0,"refY":91.0,"refZ":-2.861023E-06,"offX":96.0,"offY":78.0,"offZ":-4.7683716E-06,"radius":0.0,"color":3355508484,"thicc":5.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":16.0,"MatchIntl":{"En":"Nidhogg readies Wroth Flames","Jp":"ニーズヘッグは「邪念の炎」の構え。"},"MatchDelay":5.0}],"MaxDistance":20.0,"DistanceLimitType":1}
```
```
~Lv2~{"Name":"DSR P6 WF Move NE","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Start","refX":120.0,"refY":85.0,"refZ":-3.8146973E-06,"radius":5.0,"overlayBGColor":1879048447,"thicc":5.0,"overlayText":"Start"},{"Name":"Move 2","refX":112.0,"refY":85.0,"refZ":-1.9073486E-06,"radius":5.0,"overlayBGColor":1879048447,"thicc":5.0,"overlayText":"Puddle 2"},{"Name":"First aoe line","type":2,"refX":109.0,"refY":91.0,"refZ":-1.9073484E-06,"offX":109.0,"offY":78.0,"offZ":-5.722046E-06,"radius":0.0,"thicc":5.0},{"Name":"Second AOE line","type":2,"refX":104.0,"refY":91.0,"refZ":-2.861023E-06,"offX":104.0,"offY":78.0,"offZ":-4.7683716E-06,"radius":0.0,"color":3355508484,"thicc":5.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":16.0,"MatchIntl":{"En":"Nidhogg readies Wroth Flames","Jp":"ニーズヘッグは「邪念の炎」の構え。"},"MatchDelay":5.0}],"MaxDistance":20.0,"DistanceLimitType":1}
```
```
~Lv2~{"Name":"DSR P6 WF Move Center","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"","refX":100.0,"refY":100.0,"radius":2.0,"color":3371433728,"thicc":5.0,"tether":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.0,"MatchIntl":{"En":"Nidhogg readies Wroth Flames","Jp":"ニーズヘッグは「邪念の炎」の構え。"},"MatchDelay":16.0}]}
```

[EN, JP] Wroth flames stack/spread aoes display
```
~Lv2~{"Name":"DSR P6 spread/stack debuff aoe markers","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Spread","type":1,"radius":5.0,"color":838861055,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[2758],"Filled":true},{"Name":"Stack","type":1,"radius":5.0,"color":841481984,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[2759],"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"MatchIntl":{"En":"Nidhogg readies Wroth Flames","Jp":"ニーズヘッグは「邪念の炎」の構え。"},"MatchDelay":19.5}],"Phase":2}
```


### Scripts
[International][Beta][Untested] P6 Wyrmsbreath First
- This strategy is triangle in the south.
- Settings are required.
  - Please enter the names and select positions for each:
    - Two players each for TriangleLowerLeft, TriangleLowerRight, and TriangleUpper.
    - One player each for UpperRight and UpperLeft.
  - If adjustments are needed, turn on "Swap if needed."

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Endwalker/Dragonsong's%20Reprise/P6%20Wyrmsbreath%20First.cs
```

[International][Beta][Untested] P6 Wroth Flames
- Settings are required.
  - Please set the priority list.
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Endwalker/Dragonsong's%20Reprise/P6%20Wroth%20Flames.cs
```

[International][Beta][Untested] P6 Auto Target Switcher
- Automatically switches between two targets.
- Settings are required depending on the job.
  - Lower the "Acceptable Percentage" and "Interval."

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Endwalker/Dragonsong's%20Reprise/P6%20AutoTargetSwitcher.cs
```