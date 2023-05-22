# Generic
[International] Multipreset, contains most of dives, lightning indication for Nael
```
~Lv2~{"Name":"Ucob multipreset / 巴哈","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"Megaflare dive / 百万核爆冲","type":3,"offY":50.0,"radius":6.0,"color":4278190335,"refActorNPCID":3210,"refActorRequireCast":true,"refActorCastId":[9953],"FillStep":1.0,"refActorComparisonType":4,"includeRotation":true},{"Name":"Cauterize / 小龙俯冲2","type":3,"offY":47.14,"radius":10.0,"thicc":4.2,"refActorNPCID":6957,"refActorRequireCast":true,"refActorCastId":[9931,9932,9933,9934,9935],"refActorComparisonType":4,"includeRotation":true},{"Name":"Lunar dive / 月流冲","type":3,"offY":50.0,"radius":4.0,"color":4278190335,"thicc":5.5,"refActorNPCID":2612,"refActorRequireCast":true,"refActorCastId":[9923],"FillStep":1.0,"refActorComparisonType":4,"includeRotation":true},{"Name":"Twisting dive / 旋风冲","type":3,"offY":50.0,"radius":4.0,"color":4278190335,"refActorNPCID":1482,"refActorRequireCast":true,"refActorCastId":[9906],"FillStep":1.0,"refActorComparisonType":4,"includeRotation":true},{"Name":"Cauterize / 小龙俯冲5","type":3,"offY":47.14,"radius":10.0,"thicc":4.2,"refActorNPCID":2631,"refActorRequireCast":true,"refActorCastId":[9931,9932,9933,9934,9935],"refActorComparisonType":4,"includeRotation":true},{"Name":"Cauterize / 小龙俯冲3","type":3,"offY":47.14,"radius":10.0,"thicc":4.2,"refActorNPCID":2630,"refActorRequireCast":true,"refActorCastId":[9931,9932,9933,9934,9935],"refActorComparisonType":4,"includeRotation":true},{"Name":"Cauterize / 小龙俯冲4","type":3,"offY":47.14,"radius":10.0,"thicc":4.2,"refActorNPCID":2632,"refActorRequireCast":true,"refActorCastId":[9931,9932,9933,9934,9935],"refActorComparisonType":4,"includeRotation":true},{"Name":"Cauterize / 小龙俯冲1","type":3,"offY":47.14,"radius":10.0,"thicc":4.2,"refActorNPCID":6958,"refActorRequireCast":true,"refActorCastId":[9931,9932,9933,9934,9935],"refActorComparisonType":4,"includeRotation":true},{"Name":"Thunderstruck / 雷点名","type":1,"radius":4.0,"color":1325334664,"refActorPlaceholder":[],"refActorComparisonAnd":true,"refActorRequireBuff":true,"refActorBuffId":[466],"Filled":true}]}
```

# Suffer (Bahamut Prime)
[EN] QMT Divebombs: 
```
~Lv2~{"Name":"QMT Divebombs","Group":"UCOB","ZoneLockH":[733],"DCond":5,"ElementsL":[{"Name":"Twin Dive","type":3,"offY":67.52,"radius":1.0,"color":1160062402,"thicc":1.0,"refActorNPCNameID":1482,"refActorRequireCast":true,"refActorCastId":[9906,27531],"refActorComparisonType":6,"includeHitbox":true,"includeOwnHitbox":true,"includeRotation":true,"onlyVisible":true,"Filled":true},{"Name":"Bahamut Dive","type":3,"offY":67.52,"radius":1.0,"color":1160062402,"thicc":1.0,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[3008,9953,23378,24676],"refActorComparisonType":6,"includeHitbox":true,"includeOwnHitbox":true,"includeRotation":true,"onlyVisible":true,"Filled":true},{"Name":"Nael Dive","type":3,"offY":67.52,"radius":1.0,"color":1160062402,"thicc":1.0,"refActorNPCNameID":2612,"refActorRequireCast":true,"refActorCastId":[9923],"refActorComparisonType":6,"includeHitbox":true,"includeOwnHitbox":true,"includeRotation":true,"onlyVisible":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":25.0,"MatchIntl":{"En":"Bahamut Prime uses Quickmarch Trio"}}]}
```

[EN] BFT Nael Finder: 
```
~Lv2~{"Name":"BFT/FRT Nael Location","Group":"UCOB","ZoneLockH":[733],"DCond":5,"ElementsL":[{"Name":"Nael Tether","type":1,"radius":0.0,"color":4290828558,"thicc":5.0,"overlayText":"Nael","refActorModelID":647,"refActorComparisonType":1,"onlyVisible":true,"tether":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"MatchIntl":{"En":"Bahamut Prime readies Blackfire Trio"},"MatchDelay":6.0}]}
```

[EN] HFT Divebombs: 
```
~Lv2~{"Name":"HFT Divebombs","Group":"UCOB","ZoneLockH":[733],"DCond":5,"ElementsL":[{"Name":"Twin Dive","type":3,"offY":67.52,"radius":1.0,"color":1160062402,"thicc":1.0,"refActorNPCNameID":1482,"refActorRequireCast":true,"refActorCastId":[9906,27531],"refActorComparisonType":6,"includeHitbox":true,"includeOwnHitbox":true,"includeRotation":true,"onlyVisible":true,"Filled":true},{"Name":"Bahamut Dive","type":3,"offY":67.52,"radius":1.0,"color":1160062402,"thicc":1.0,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[3008,9953,23378,24676],"refActorComparisonType":6,"includeHitbox":true,"includeOwnHitbox":true,"includeRotation":true,"onlyVisible":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":25.0,"MatchIntl":{"En":"Bahamut Prime readies Heavensfall Trio"}}]}
```

[Script] [Configuration required] Heavensfall Trio 8 towers resolver 
```
https://github.com/NightmareXIV/Splatoon/raw/master/SplatoonScripts/Duties/Stormblood/UCOB%20Heavensfall%20Trio%20Towers.cs
```

[EN] After dodging the dives a ring will appear in the center, stand in the circle to reach the perfect knockback range without taking lethal damage from the heavensfall tower.
```
~Lv2~{"Name":"Heavensfall Knockback","Group":"UCOB","ZoneLockH":[733],"DCond":5,"ElementsL":[{"Name":"","refX":0.013264656,"refY":-0.29209137,"radius":9.74,"Donut":0.99}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":11.0,"Match":"Bahamut Prime readies Heavensfall Trio","MatchDelay":13.0}]}
```

[EN] [Untested] Grand Octet. **Might conflict with Multipreset, testing is required.**
```
~Lv2~{"Name":"Octet Nael","Group":"Octet Bahamut and Twintania","ZoneLockH":[733],"ElementsL":[{"Name":"Nael Indication and Hitbox","type":3,"offY":50.0,"radius":4.0,"color":4278193407,"thicc":5.5,"refActorNPCID":2612,"refActorRequireCast":true,"refActorCastId":[9923],"FillStep":1.0,"refActorComparisonType":4,"includeRotation":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Match":"Bahamut Prime readies Grand Octet."},{"Type":3,"Match":"Twintania readies Twisting Dive.","MatchDelay":5.0}]}
```
```
~Lv2~{"Name":"On hitbox color change and movement - dodge.","Group":"Octet Bahamut and Twintania","ZoneLockH":[733],"ElementsL":[{"Name":"Bahamut Dive Hitbox","type":3,"refY":50.0,"radius":6.0,"color":536865792,"thicc":0.0,"refActorModelID":2113,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true},{"Name":"Twintania Dive Hitbox","type":3,"refY":50.0,"radius":4.0,"color":274385399,"thicc":2.7,"refActorModelID":226,"refActorComparisonType":1,"includeRotation":true},{"Name":"Bahamut Dive Lock-in Indication","type":3,"offY":50.0,"radius":6.0,"color":4294914816,"refActorNPCID":3210,"refActorRequireCast":true,"refActorCastId":[9953],"FillStep":0.75,"refActorComparisonType":4,"includeRotation":true},{"Name":"Twintania Dive Lock-in Indication","type":3,"offY":50.0,"radius":4.0,"color":4278217215,"refActorNPCID":1482,"refActorRequireCast":true,"refActorCastId":[9906],"FillStep":0.75,"refActorComparisonType":4,"includeRotation":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Match":"Bahamut Prime readies Grand Octet.","MatchDelay":20.0},{"Type":3,"Match":"Twintania readies Twisting Dive.","MatchDelay":5.0}]}
```

# Golden Bahamut Prime
Exaflare helper. Instruction: get close to first exaflare, **go into green box as soon as there will be free space**. Do not step into yellow circles.
**IMPORT ALL PRESETS LISTED BELOW.**
```
~Lv2~{"Name":"UCOB First exaflare safe line","Group":"UCOB","ZoneLockH":[733],"DCond":5,"ElementsL":[{"Name":"","type":3,"refY":-2.0,"offY":60.0,"radius":2.0,"color":1174470400,"thicc":4.0,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"FillStep":2.0,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.0,"Match":"(3210>9967)"}],"Freezing":true,"FreezeFor":18.0,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflare only first - 1","Group":"UCOB","ZoneLockH":[733],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":6.0,"color":1342242815,"overlayBGColor":1879048447,"overlayTextColor":4278190080,"overlayFScale":3.0,"overlayText":"--- FIRST ---","refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.0,"Match":"(3210>9967)"}],"Freezing":true,"FreezeFor":4.0,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflare only first - 2","Group":"UCOB","ZoneLockH":[733],"DCond":5,"ElementsL":[{"Name":"","type":1,"offY":8.0,"radius":6.0,"color":1342242815,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.0,"Match":"(3210>9967)"}],"Freezing":true,"FreezeFor":5.7,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflare only first - 3","Group":"UCOB","ZoneLockH":[733],"DCond":5,"ElementsL":[{"Name":"","type":1,"offY":16.0,"radius":6.0,"color":1342242815,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.0,"Match":"(3210>9967)"}],"Freezing":true,"FreezeFor":7.2,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflare only first - 4","Group":"UCOB","ZoneLockH":[733],"DCond":5,"ElementsL":[{"Name":"","type":1,"offY":24.0,"radius":6.0,"color":1342242815,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.0,"Match":"(3210>9967)"}],"Freezing":true,"FreezeFor":8.7,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflare only first - 5","Group":"UCOB","ZoneLockH":[733],"DCond":5,"ElementsL":[{"Name":"","type":1,"offY":32.0,"radius":6.0,"color":1342242815,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.0,"Match":"(3210>9967)"}],"Freezing":true,"FreezeFor":10.2,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflare only first - 6","Group":"UCOB","ZoneLockH":[733],"DCond":5,"ElementsL":[{"Name":"","type":1,"offY":40.0,"radius":6.0,"color":1342242815,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.0,"Match":"(3210>9967)"}],"Freezing":true,"FreezeFor":11.7,"IntervalBetweenFreezes":0.5}
```

# Deprecated
|Presets in this section are considered obsolete and are not recommended for use|
|---|

[International] Exaflare telegraphs: **all** upcoming explosions highlighted with yellow color. You must import all 6.
```
~Lv2~{"Name":"UCOB Exaflares ALL - 1","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"radius":6.0,"color":1342242815,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":4.0,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflares ALL - 2","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"offY":8.0,"radius":6.0,"color":1342242815,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":5.7,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflares ALL - 3","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"offY":16.0,"radius":6.0,"color":1342242815,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":7.2,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflares ALL - 4","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"offY":24.0,"radius":6.0,"color":1342242815,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":8.7,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflares ALL - 5","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"offY":32.0,"radius":6.0,"color":1342242815,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":10.2,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflares ALL - 6","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"offY":40.0,"radius":6.0,"color":1342242815,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":11.7,"IntervalBetweenFreezes":0.5}
```

[International] Exaflare telegraphs: **two upcoming** explosions highlighted with red color. You must import all 6. *Combining with previous presets together, all future exaflares will be highlighted with yellow while two upcoming will be highlighted with orange.*
```
~Lv2~{"Name":"UCOB Exaflares next 2 - 1","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"radius":6.0,"color":1677721855,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":4.0,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflares next 2 - 2","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"offY":8.0,"radius":6.0,"color":1677721855,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":5.7,"IntervalBetweenFreezes":0.5}
```
```
~Lv2~{"Name":"UCOB Exaflares next 2 - 3","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"offY":16.0,"radius":6.0,"color":1677721855,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":7.2,"IntervalBetweenFreezes":0.5,"FreezeDisplayDelay":4.0}
```
```
~Lv2~{"Name":"UCOB Exaflares next 2 - 4","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"offY":24.0,"radius":6.0,"color":1677721855,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":8.7,"IntervalBetweenFreezes":0.5,"FreezeDisplayDelay":5.7}
```
```
~Lv2~{"Name":"UCOB Exaflares next 2 - 5","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"offY":32.0,"radius":6.0,"color":1677721855,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":10.2,"IntervalBetweenFreezes":0.5,"FreezeDisplayDelay":7.2}
```
```
~Lv2~{"Name":"UCOB Exaflares next 2 - 6","Group":"UCOB","ZoneLockH":[733],"ElementsL":[{"Name":"","type":1,"offY":40.0,"radius":6.0,"color":1677721855,"refActorNPCNameID":3210,"refActorRequireCast":true,"refActorCastId":[9968],"refActorUseCastTime":true,"refActorCastTimeMax":0.25,"refActorComparisonType":6,"includeRotation":true,"Filled":true}],"Freezing":true,"FreezeFor":11.7,"IntervalBetweenFreezes":0.5,"FreezeDisplayDelay":8.7}
```
