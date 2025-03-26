## Wrath of the heavens
[EN, JP, DE] Display tethers (make tethers same as red line) and safe spot as blue marker
```
~Lv2~{"Name":"DSR P5 Wrath of the Heavens resolve","Group":"DSR","ZoneLockH":[903,968],"DCond":5,"ElementsL":[{"Name":"Right tether","type":3,"refY":43.0,"radius":0.0,"refActorNPCNameID":3638,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":6.2308254},{"Name":"Left tether","type":3,"refY":43.0,"radius":0.0,"refActorNPCNameID":3636,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":0.05235988},{"Name":"Blue marker safe spot","type":1,"offX":17.42,"offY":12.22,"radius":0.6,"color":4294901787,"thicc":7.6,"refActorNPCNameID":3984,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"MatchIntl":{"En":"King Thordan readies Wrath of the Heavens","Jp":"騎神トールダンは「至天の陣：風槍」の構え。","De":"Thordan setzt Himmel des Zorns ein."},"MatchDelay":8.0}],"Phase":2}
```

[EN, JP] Display safespot under Ser Grinnaux
```
~Lv2~{"Name":"DSR P5 Wrath safe spot","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":2.0,"thicc":5.0,"refActorName":"Ser Grinnaux","FillStep":1.0,"includeHitbox":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":20.0,"MatchIntl":{"En":"King Thordan readies Wrath of the Heavens","Jp":"騎神トールダンは「至天の陣：風槍」の構え。"},"MatchDelay":10.0}]}
```

[International] Display chain lightning radius around people.
```
~Lv2~{"Name":"DSR P5 Wrath of the Heavens Chain Lightning","Group":"DSR","ZoneLockH":[968],"ElementsL":[{"Name":"1","type":1,"offY":0.14,"radius":5.0,"color":1694433303,"refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[2833],"Filled":true}]}
```

[International] Display Ascalon's Mercy Revealed on you. Might be not precise.
```
~Lv2~{"Name":"DSR p5 thordan cleave","Group":"DSR","ZoneLockH":[968],"ElementsL":[{"Name":"Thordan cleave","type":4,"radius":20.0,"coneAngleMin":-15,"coneAngleMax":15,"color":2885746175,"refActorNPCNameID":3632,"refActorRequireCast":true,"refActorCastId":[25546,25547],"FillStep":1.0,"refActorComparisonType":6,"includeRotation":true,"Filled":true,"FaceMe":true}],"Phase":2}
```

[International][Beta][Untested] Wrath of the heavens
- Settings are not required.

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Endwalker/Dragonsong's%20Reprise/P5%20Wrath%20of%20the%20Heavens.cs
```

## Death of the Heavens
[EN, JP] Relative north marker during Death of the Heavens
```
~Lv2~{"Name":"DSR P5 DeathOTH North","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Circle","type":1,"offY":-10.0,"radius":3.53,"color":4278253567,"overlayBGColor":4278253567,"overlayTextColor":4278190080,"overlayFScale":3.0,"thicc":7.8,"overlayText":"North","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true},{"Name":"Tether","type":3,"refY":5.8,"offY":-6.66,"radius":0.0,"color":4278253567,"overlayBGColor":4294901764,"overlayTextColor":4294967295,"overlayFScale":3.0,"thicc":5.0,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":25.0,"MatchIntl":{"En":"King Thordan readies Death of the Heavens","Jp":"騎神トールダンは「至天の陣：死刻」の構え。"},"MatchDelay":8.5}]}
```

[EN, JP] The second set of quakes seen in P5:
```
~Lv2~{"Name":"DSR P5 Death of the Heavens Quake Markers","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Quake marker","type":1,"radius":6.0,"color":4293721856,"Filled":false,"fillIntensity":0.39215687,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2","type":1,"radius":12.0,"color":4293721856,"Filled":false,"fillIntensity":0.39215687,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"3","type":1,"radius":18.0,"color":4293721856,"Filled":false,"fillIntensity":0.39215687,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"4","type":1,"radius":24.0,"color":4293721856,"Filled":false,"fillIntensity":0.39215687,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"MatchIntl":{"En":"King Thordan readies Death of the Heavens","Jp":"騎神トールダンは「至天の陣：死刻」の構え。"},"MatchDelay":15.0}],"Phase":2}
```

[EN, JP] Dive Markers - these are the dives when the four dooms go out, displaying the safe spots accurately, with correct timings too:
```
~Lv2~{"Name":"DSR P5 Death of the Heavens DIve Markers","Group":"DSR","ZoneLockH":[968],"DCond":5,"ElementsL":[{"Name":"Spear of the Fury","type":3,"refY":45.0,"radius":5.0,"color":1690288127,"refActorNPCNameID":3633,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true},{"Name":"Cauterize","type":3,"refY":30.0,"offY":-15.0,"radius":10.0,"color":1690288127,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true},{"Name":"Twisting Dive","type":3,"refY":45.0,"radius":5.0,"color":1690288127,"refActorNPCNameID":3984,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"MatchIntl":{"En":"King Thordan readies Death of the Heavens","Jp":"騎神トールダンは「至天の陣：死刻」の構え。"},"MatchDelay":13.0}],"Phase":2}
```

[EN, JP] [Untested] Initial knockback position markers. You may need to move them to match your markers.
```
~Lv2~{"Name":"◆絶竜詩【P5】死刻/PS散会目安","Group":"◆絶竜詩【P5】","DCond":5,"ElementsL":[{"Name":"中心","Enabled":false,"refX":100.0,"refY":99.99971,"radius":0.05,"color":3369542911,"thicc":5.0},{"Name":"死刻01","refX":99.997116,"refY":96.461464,"radius":0.7,"overlayBGColor":0,"overlayFScale":2.0,"thicc":5.0,"overlayText":"A"},{"Name":"死刻02","refX":102.51997,"refY":97.50191,"radius":0.7,"overlayBGColor":0,"overlayFScale":2.0,"thicc":5.0,"overlayText":"1"},{"Name":"死刻03","refX":103.53233,"refY":100.00354,"refZ":4.7683716E-07,"radius":0.7,"color":3355505151,"overlayBGColor":0,"overlayFScale":2.0,"thicc":5.0,"overlayText":"B"},{"Name":"死刻04","refX":102.47303,"refY":102.46072,"radius":0.7,"color":3355505151,"overlayBGColor":0,"overlayFScale":2.0,"thicc":5.0,"overlayText":"2"},{"Name":"死刻05","refX":99.993355,"refY":103.458015,"refZ":2.3841858E-07,"radius":0.7,"color":3372172800,"overlayBGColor":0,"overlayFScale":2.0,"thicc":5.0,"overlayText":"C"},{"Name":"死刻06","refX":97.55642,"refY":102.43156,"radius":0.7,"color":3372172800,"overlayBGColor":0,"overlayFScale":2.0,"thicc":5.0,"overlayText":"3"},{"Name":"死刻07","refX":96.530945,"refY":99.99951,"radius":0.7,"color":3372155131,"overlayBGColor":0,"overlayFScale":2.0,"thicc":5.0,"overlayText":"D"},{"Name":"死刻08","refX":97.508644,"refY":97.53243,"refZ":-2.3841858E-07,"radius":0.7,"color":3372155131,"overlayBGColor":0,"overlayFScale":2.0,"thicc":5.0,"overlayText":"4"}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":21.0,"Match":"騎神トールダンは「至天の陣：死刻」の構え。","MatchIntl":{"En":"King Thordan readies Death of the Heavens"},"MatchDelay":13.0}]}
```

[EN] DSR P5 Death of the Heaven Playstation Knockback
Red marker for doom, blue for non doom from the center of the arena, relativ north
```
~Lv2~{"Name":"DSR P5 Death of the Heaven Playstation Knockback","Group":"DSR","DCond":5,"ElementsL":[{"Name":"Doom Circle 1","type":1,"offX":2.5,"offY":9.0,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"Doom Circle 2","type":1,"offX":-2.5,"offY":9.0,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"Doom Square","type":1,"offX":1.95,"offY":10.9,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"Doom Triangle","type":1,"offX":-1.95,"offY":10.9,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"Non doom Square","type":1,"offX":-1.92,"offY":7.15,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"Non doom Triangle","type":1,"offX":1.95,"offY":7.15,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"Non doom X 1","type":1,"offY":6.12,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"Non doom X 2","type":1,"offY":11.94,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"Match":"King Thordan readies Death of the Heavens.","MatchIntl":{"En":"King Thordan readies Death of the Heavens."},"MatchDelay":25.0}]}
```

[Beta] Script for resolving dooms 
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Endwalker/DSR%20Dooms.cs
```



## Death of the Heavens - guide specific strats
[EN, JP] [Untested] Tight conga line positions. Tip: enable tether to your designated position.
```
~Lv2~{"Name":"◆絶竜詩【P5】Conga line/死刻/開幕スタンバイ位置","Group":"◆絶竜詩【P5】","DCond":5,"ElementsL":[{"Name":"死刻01","type":1,"offX":-0.8,"offY":9.0,"radius":0.6,"color":3355508651,"thicc":5.0,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":0.017453292},{"Name":"死刻02","type":1,"offX":-2.0,"offY":9.0,"radius":0.6,"color":3355508651,"thicc":5.0,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":0.017453292},{"Name":"死刻03","type":1,"offX":-3.2,"offY":9.0,"radius":0.6,"color":3355508651,"thicc":5.0,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":0.017453292},{"Name":"死刻04","type":1,"offX":-4.4,"offY":9.0,"radius":0.6,"color":3355508651,"thicc":5.0,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":0.017453292},{"Name":"死刻05","type":1,"offX":0.5,"offY":9.0,"radius":0.6,"color":3355508651,"thicc":5.0,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":0.017453292},{"Name":"死刻06","type":1,"offX":1.7,"offY":9.0,"radius":0.6,"color":3355508651,"thicc":5.0,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":0.017453292},{"Name":"死刻07","type":1,"offX":2.9,"offY":9.0,"radius":0.6,"color":3355508651,"thicc":5.0,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":0.017453292},{"Name":"死刻08","type":1,"offX":4.1,"offY":9.0,"radius":0.6,"color":3355508651,"thicc":5.0,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":0.017453292},{"Name":"◆スケール◆","type":3,"refY":8.5,"radius":0.5,"refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"MatchIntl":{"En":"King Thordan uses Death of the Heavens","Jp":"騎神トールダンの「至天の陣：死刻」"}}]}
```

[EN, JP] [Untested] Spread positions based on guide: https://www.youtube.com/watch?v=TBt_AgoHn80
```
~Lv2~{"Name":"◆絶竜詩【P5】死刻/ぬけまる","Group":"◆絶竜詩【P5】","DCond":5,"ElementsL":[{"Name":"宣告01","type":1,"offX":12.49,"offY":-7.76,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"1","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"宣告02","type":1,"offX":12.49,"offY":8.5,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"2","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"宣告03","type":1,"offX":-12.49,"offY":8.5,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"3","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"宣告04","type":1,"offX":-12.49,"offY":-7.76,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"4","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"無印01","type":1,"offX":20.5,"offY":8.5,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"1","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"無印02","type":1,"offX":12.49,"offY":24.76,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"2","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"無印03","type":1,"offX":-12.49,"offY":24.76,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"3","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"無印04","type":1,"offX":-20.5,"offY":8.5,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"4","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"Match":"騎神トールダンは「至天の陣：死刻」の構え。","MatchIntl":{"En":"King Thordan readies Death of the Heavens"},"MatchDelay":13.0}]}
```

[EN, JP] [Untested] Knockback positions based on guide: https://www.youtube.com/watch?v=3-moQ2GiABg
```
~Lv2~{"Name":"◆絶竜詩【P5】死刻/こまぞう改","Group":"◆絶竜詩【P5】","DCond":5,"ElementsL":[{"Name":"宣01","type":1,"offX":12.49,"offY":8.5,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"1","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"宣04","type":1,"offX":-12.49,"offY":8.5,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"4","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"無03","type":1,"offX":-12.49,"offY":-7.76,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"3","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"無01","type":1,"offX":20.5,"offY":8.5,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"1","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"宣02","type":1,"offX":12.49,"offY":24.76,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"2","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"宣03","type":1,"offX":-12.49,"offY":24.76,"radius":0.5,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"3","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"無04","type":1,"offX":-20.5,"offY":8.5,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"4","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true},{"Name":"無02","type":1,"offX":12.49,"offY":-7.76,"radius":0.5,"color":3372154884,"overlayBGColor":0,"overlayFScale":1.7,"thicc":6.0,"overlayText":"2","refActorNPCNameID":3641,"refActorComparisonType":6,"includeRotation":true,"onlyUnTargetable":true,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"MatchIntl":{"En":"King Thordan readies Death of the Heavens","Jp":"騎神トールダンは「至天の陣：死刻」の構え。"},"MatchDelay":13.0}]}
```

[International][Beta][Untested] Death of the Heavens
- It is based on the north orientation.
- Based on guide: https://www.youtube.com/watch?v=3-moQ2GiABg
- Settings are required.
  - Please set the priority list.
  - The first corresponds to the person positioned farthest left, and the eighth corresponds to the person positioned farthest right.
  - You can set the pre-positioning before "Playstation" as either vertical or horizontal.
- Locks the face when gaze guidance is needed.

> [!CAUTION]
> The gaze-locking feature is unstable. If you can manage it yourself, it is recommended to turn it off.


```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Endwalker/Dragonsong's%20Reprise/P5%20Death%20of%20the%20Heavens.cs
```

[International][Beta][Untested] Caster Limit Break
- Automatically uses LB2 to destroy 4 comets.
- When activating, type /limitBreak in the chat.
- Settings are required
  -  If you want to destroy the four comets in the north, please select NorthNorthWest.

> [!CAUTION]
> It may not activate in a single execution. It is recommended to create a macro that can execute it multiple times.
Additionally, it will not activate if LB2 is not fully charged.

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Endwalker/Dragonsong's%20Reprise/P5%20Caster%20Limit%20Break.cs
```
