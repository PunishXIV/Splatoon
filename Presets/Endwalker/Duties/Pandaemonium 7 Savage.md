[EN] Stack tankbuster:
```
~Lv2~{"Name":"P7S Stack Tankbuster","Group":"P7S","ZoneLockH":[1086],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":9.0,"color":1342242601,"refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"MatchIntl":{"En":"Agdistis begins casting Condensed Aero II."}}]}
```

[EN] Spread tankbuster:
```
~Lv2~{"Name":"P7S Spread Tankbuster","Group":"P7S","ZoneLockH":[1086],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":9.0,"color":1342242792,"refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"MatchIntl":{"En":"Agdistis begins casting Dispersed Aero II."}}]}
```

[International] First exaflare safespot finder
```
~Lv2~{"Name":"P7S First Exaflare Safespot","Group":"P7S","ZoneLockH":[1086],"DCond":5,"ElementsL":[{"Name":"","type":3,"refY":40.0,"radius":7.0,"color":1342242303,"refActorNPCNameID":11374,"refActorRequireCast":true,"refActorCastId":[30767],"refActorComparisonType":6,"includeRotation":true}],"UseTriggers":true,"Triggers":[{"TimeBegin":10.0,"Duration":60.0}]}
```

[International] Birds line AOE
```
~Lv2~{"Name":"P7S Immature Stymphalide Line","Group":"P7S","ZoneLockH":[1086],"ElementsL":[{"Name":"","type":3,"refY":50.0,"radius":4.0,"color":1174405375,"refActorNPCNameID":11379,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}]}
```

[International] Circle behemoth AOE
```
~Lv2~{"Name":"P7S Immature Io Circle AOE","Group":"P7S","ZoneLockH":[1086],"ElementsL":[{"Name":"","type":1,"radius":10.0,"color":1006633215,"refActorNPCNameID":11378,"refActorComparisonType":6,"onlyVisible":true,"Filled":true}]}
```

[International] Spread: first preset displays AOE circle around you when debuff is about to expire. 
```
~Lv2~{"Name":"P7S Spreads","Group":"P7S","ZoneLockH":[1086],"ElementsL":[{"Name":"3397 - spread","type":1,"radius":6.2,"color":1175846656,"overlayBGColor":4278190080,"overlayTextColor":4280024832,"overlayVOffset":2.0,"overlayFScale":2.0,"overlayText":"<<< SPREAD >>>","refActorNameIntl":{"En":"*"},"refActorRequireBuff":true,"refActorBuffId":[3397,3308,3310,3391,3392,3393],"refActorUseBuffTime":true,"refActorBuffTimeMax":8.0,"refActorType":1,"Filled":true}]}
```

[International] Spread: highlights players with debuff if you're standing too close to them. **This is preset for DPS, if you are tank, replace placeholders to `<t2>`, `<h1>` and `<h2>`, if you're healer, replace them to `<t1>`, `<t2>`, `<h2>`**.
```
~Lv2~{"Name":"P7S Other Players Spreads","Group":"P7S","ZoneLockH":[1086],"ElementsL":[{"Name":"3397 - spread","type":1,"radius":6.2,"color":3355508706,"overlayBGColor":4278190080,"overlayTextColor":4280024832,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":4.0,"overlayText":"<<< SPREAD >>>","refActorPlaceholder":["<d2>","<d3>","<d4>"],"refActorRequireBuff":true,"refActorBuffId":[3397,3308,3310,3391,3392,3393],"refActorUseBuffTime":true,"refActorBuffTimeMax":6.0,"refActorComparisonType":5}],"MaxDistance":6.2,"UseDistanceLimit":true,"DistanceLimitType":1}
```

[International] Purgation stack safe spot. Remembers when first stack was dropped and highlights that spot.
```
~Lv2~{"Name":"P7S Purgation safe spot drop","Group":"P7S","ZoneLockH":[1086],"ElementsL":[{"Name":"","type":1,"radius":3.0,"color":4294573824,"thicc":5.0,"overlayText":"Stacks here","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[3311],"refActorUseBuffTime":true,"refActorBuffTimeMax":0.5,"onlyTargetable":true}],"Freezing":true,"FreezeFor":65.0,"IntervalBetweenFreezes":65.0}
```

[International] Bough of Attis - OUT
```
~Lv2~{"Name":"P7S Bough of Attis - OUT","Group":"P7S","ZoneLockH":[1086,1085],"ElementsL":[{"Name":"","type":1,"offY":18.5,"offZ":-1.0,"radius":20.0,"color":1342177535,"refActorNPCNameID":11374,"refActorRequireCast":true,"refActorCastId":[30753],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"AdditionalRotation":5.398303,"Filled":true},{"Name":"","type":1,"offY":18.5,"offZ":-1.0,"radius":20.0,"color":1342177535,"refActorNPCNameID":11374,"refActorRequireCast":true,"refActorCastId":[30753],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"AdditionalRotation":0.884882,"Filled":true}]}
```

[International] Bough of Attis - left/right swipe
```
~Lv2~{"Name":"P7S 树根突刺左(触发器)","Group":"P7S","ZoneLockH":[1086],"DCond":5,"ElementsL":[{"Name":"左左左","type":2,"refX":81.68074,"refY":82.804276,"offX":105.04228,"offY":124.92065,"radius":12.0,"color":1693776274}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(11374>30755)"}]}
```
```
~Lv2~{"Name":"P7S 树根突刺右(触发器)","Group":"P7S","ZoneLockH":[1086],"DCond":5,"ElementsL":[{"Name":"右右右","type":2,"refX":119.07414,"refY":82.98595,"offX":94.3094,"offY":124.40893,"radius":12.0,"color":1693776274}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(11374>30756)"}]}
```

[International] Bullish Swipe and Bullish Slash Display
```
~Lv2~{"Name":"P7S Bullish Swipe and Slash","Group":"P7S","ZoneLockH":[1086],"ElementsL":[{"Name":"Bullish Swipe","type":4,"refY":41.18,"offX":0.12,"offY":0.38,"radius":17.84,"coneAngleMin":-46,"coneAngleMax":48,"color":3369457109,"thicc":0.0,"refActorNPCID":11380,"refActorRequireCast":true,"refActorCastId":[30744],"refActorCastTimeMax":3.1000001,"FillStep":0.763,"refActorComparisonType":4,"includeRotation":true,"onlyVisible":true,"LineAddPlayerHitboxLengthXA":true,"LineAddPlayerHitboxLengthYA":true,"LineAddPlayerHitboxLengthZA":true,"Filled":true},{"Name":"Bullish Slash","type":4,"refY":41.18,"radius":50.0,"coneAngleMin":-20,"coneAngleMax":20,"color":3355443436,"refActorNPCID":11380,"refActorRequireCast":true,"FillStep":50.0,"refActorComparisonType":4,"includeRotation":true,"onlyVisible":true,"LineAddPlayerHitboxLengthXA":true,"LineAddPlayerHitboxLengthYA":true,"LineAddPlayerHitboxLengthZA":true,"Filled":true,"DistanceSourceX":116.65025,"DistanceSourceY":90.95252,"DistanceSourceZ":0.0099983215},{"Name":"fensan","type":1,"radius":2.51,"refActorComparisonType":7,"includeRotation":true,"Filled":true,"refActorVFXPath":"vfx/lockon/eff/m0796_trg_AE_0a1.avfx","refActorVFXMax":13385}],"Phase":1}
```

[International] Famine's Harvest  Security position (trigger)
```
~Lv2~{"Name":"P7S  Famine's Harvest  Security position (trigger)","Group":"","ZoneLockH":[1086],"DCond":5,"ElementsL":[{"Name":"Station1","type":1,"offX":11.3,"offY":4.22,"offZ":-1.0,"radius":1.0,"thicc":2.5,"refActorNPCID":11374,"refActorComparisonType":4,"includeRotation":true,"onlyVisible":true,"Filled":true},{"Name":"Station2","type":1,"offX":19.6,"offY":17.66,"offZ":-1.0,"radius":1.0,"thicc":2.5,"refActorNPCID":11374,"refActorComparisonType":4,"includeRotation":true,"onlyVisible":true,"Filled":true},{"Name":"Station3","type":1,"offX":7.92,"offY":36.56,"offZ":-1.0,"radius":1.0,"thicc":2.5,"refActorNPCID":11374,"refActorComparisonType":4,"includeRotation":true,"onlyVisible":true,"Filled":true},{"Name":"Station4","type":1,"offX":-8.38,"offY":34.14,"offZ":-1.0,"radius":1.0,"refActorNPCID":11374,"refActorComparisonType":4,"includeRotation":true,"onlyVisible":true,"Filled":true},{"Name":"Station5","type":1,"offX":-19.1,"offY":17.74,"offZ":-1.0,"radius":1.0,"refActorNPCID":11374,"refActorComparisonType":4,"includeRotation":true,"onlyVisible":true,"Filled":true},{"Name":"Station6","type":1,"offX":-12.18,"offY":3.82,"offZ":-1.0,"radius":1.0,"refActorNPCID":11374,"refActorComparisonType":4,"includeRotation":true,"onlyVisible":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"TimeBegin":0.5,"Duration":25.0,"Match":"(11374>31311)"}]}
```

[International] Death's Harvest and War's Harvest  seek  12 oclock (trigger)
```
~Lv2~{"Name":"P7S Death's Harvest and War's Harvest  seek  12 oclock (trigger)","Group":"P7S","ZoneLockH":[1086],"DCond":5,"ElementsL":[{"Name":"12 oclock （1）","type":1,"radius":2.0,"color":1006633207,"overlayText":"12 oclock","refActorNPCNameID":11378,"refActorComparisonType":6,"onlyVisible":true,"tether":true,"Filled":true,"LimitDistance":true,"DistanceSourceX":99.996,"DistanceSourceY":116.3884,"DistanceSourceZ":0.010002136,"DistanceMax":2.0},{"Name":"12 oclock （2）","type":1,"radius":2.0,"color":1007159536,"overlayText":"12 oclock","refActorNPCNameID":11378,"refActorComparisonType":6,"onlyVisible":true,"tether":true,"Filled":true,"LimitDistance":true,"DistanceSourceX":114.29969,"DistanceSourceY":91.99989,"DistanceSourceZ":0.010000229,"DistanceMax":2.0},{"Name":"12 oclock （3）","type":1,"radius":2.0,"color":1007159536,"overlayText":"12 oclock","refActorNPCNameID":11378,"refActorComparisonType":6,"onlyVisible":true,"tether":true,"Filled":true,"LimitDistance":true,"DistanceSourceX":85.7224,"DistanceSourceY":91.65758,"DistanceSourceZ":0.010002136,"DistanceMax":2.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":25.0,"Match":"(11374>31312)","ResetOnTChange":false},{"Type":2,"Duration":25.0,"Match":"(11374>31313)","ResetOnTChange":false}]} 
```

[International] Find Towers
```
~Lv2~{"Name":"P7S Find Towers","Group":"","ZoneLockH":[1086],"ElementsL":[{"Name":"Tower","type":1,"radius":5.0,"color":1342177535,"overlayText":"Towers","refActorNPCID":2013075,"refActorRequireAllBuffs":true,"refActorRequireBuffsInvert":true,"refActorComparisonType":4,"tether":true,"Filled":true,"LimitDistanceInvert":true,"DistanceSourceX":105.63135,"DistanceSourceY":85.99965,"DistanceSourceZ":-1.9073486E-06}]}
```
