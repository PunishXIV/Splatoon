# P2 Transition:

Limit Cut Numbers: Shows everyone's numbers on their feet, so you can see if the group is split correctly/who needs to be in front/behind, who you need to avoid etc.
```
~Lv2~{"Name":"P2 Transition LC Numbers","Group":"TEA","ZoneLockH":[887],"Scenes":[1],"DCond":5,"ElementsL":[{"Name":"1","type":1,"color":255,"overlayFScale":2.0,"overlayText":"1","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/m0361trg_a1t.avfx","refActorVFXMax":10000},{"Name":"2","type":1,"color":255,"overlayFScale":2.0,"overlayText":"2","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/m0361trg_a2t.avfx","refActorVFXMax":11000},{"Name":"3","type":1,"color":255,"overlayFScale":2.0,"overlayText":"3","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/m0361trg_a3t.avfx","refActorVFXMax":14500},{"Name":"4","type":1,"color":255,"overlayFScale":2.0,"overlayText":"4","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/m0361trg_a4t.avfx","refActorVFXMax":15500},{"Name":"5","type":1,"color":255,"overlayFScale":2.0,"overlayText":"5","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/m0361trg_a5t.avfx","refActorVFXMax":19000},{"Name":"6","type":1,"color":255,"overlayFScale":2.0,"overlayText":"6","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/m0361trg_a6t.avfx","refActorVFXMax":20000},{"Name":"7","type":1,"color":255,"overlayFScale":2.0,"overlayText":"7","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/m0361trg_a7t.avfx","refActorVFXMax":23500},{"Name":"8","type":1,"color":255,"overlayFScale":2.0,"overlayText":"8","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/m0361trg_a8t.avfx","refActorVFXMax":24500}]}
```
Cruise Chaser Conal Cleaves: Shows CC's conal cleaves on odd marked players so you know where to stop. Assumes the person baiting is facing their character correctly.
```
~Lv2~{"Name":"P2 Transition CC Cleaves","Group":"TEA","ZoneLockH":[887],"Scenes":[1],"DCond":5,"ElementsL":[{"Name":"","type":4,"radius":34.0,"coneAngleMin":-45,"coneAngleMax":45,"refActorModelID":1606,"refActorUseCastTime":true,"refActorComparisonType":1,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true,"Filled":true}],"MaxDistance":6.0,"UseDistanceLimit":true,"DistanceLimitType":1}
```

[International] [Beta] [Script] P2 Transition script. Includes:
- early show exaflares
- mark bait locations for 1256 strat
- tether to the bait location if you are baiting
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Shadowbringers/TEA_P2_Transition.cs
```

[International] [Beta] [Script] P2 Transition 1211 script.
- show exaflares
- mark bait locations for 1211 strat

> [!NOTE]
> The displayed text is in Japanese, so please change it accordingly in the config.
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Shadowbringers/TEA%20P2%201211%20Transition.cs
```

# P2: BJ/CC
P2/P3 Chakram Line AOEs: Works for Wormhole as well
```
~Lv2~{"Name":"P2/P3 Chakram Line AOE","Group":"TEA","ZoneLockH":[887],"ElementsL":[{"Name":"","type":3,"offY":44.98,"radius":2.94,"refActorModelID":1425,"refActorComparisonType":1,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true,"Filled":true}]}
```

Nisi Debuffs: Shows everyone's Nisi debuffs at their feet for easier detection.
```
~Lv2~{"Name":"P2 Nisi Debuffs","Group":"TEA","ZoneLockH":[887],"ElementsL":[{"Name":"α","type":1,"color":255,"overlayBGColor":1895784192,"overlayFScale":1.5,"overlayText":"α","refActorRequireBuff":true,"refActorBuffId":[2222],"refActorComparisonType":1},{"Name":"β","type":1,"color":255,"overlayBGColor":1879073791,"overlayFScale":1.5,"overlayText":"β","refActorRequireBuff":true,"refActorBuffId":[2223],"refActorComparisonType":1},{"Name":"γ","type":1,"color":255,"overlayBGColor":1890844845,"overlayFScale":1.5,"overlayText":"γ","refActorRequireBuff":true,"refActorBuffId":[2137],"refActorComparisonType":1},{"Name":"δ","type":1,"color":255,"overlayBGColor":1879091971,"overlayFScale":1.5,"overlayText":"δ","refActorRequireBuff":true,"refActorBuffId":[2138],"refActorComparisonType":1}]}
```
Water/Lightning Debuffs: Shows the AOE for Lightning and Water when they're about to go off
```
~Lv2~{"Name":"P2 Water/Lightning Debuff","Group":"TEA","ZoneLockH":[887],"ElementsL":[{"Name":"Lightning AOE","type":1,"radius":7.39,"color":1679933629,"overlayVOffset":0.76,"refActorRequireBuff":true,"refActorBuffId":[1024,2143],"refActorUseBuffTime":true,"refActorBuffTimeMax":4.0,"refActorComparisonType":1,"Filled":true,"DistanceMax":8.0},{"Name":"Water AOE","type":1,"radius":7.39,"color":1689108513,"overlayVOffset":0.76,"refActorRequireBuff":true,"refActorBuffId":[1023,2142],"refActorUseBuffTime":true,"refActorBuffTimeMax":4.0,"refActorComparisonType":1,"Filled":true,"DistanceMax":8.0}],"MaxDistance":8.0,"UseDistanceLimit":true,"DistanceLimitType":1}
```
Water/Lightning Text Reminder: Adds Water/Lightning text at your feet if you have the debuff
```
~Lv2~{"Name":"P2 Water/Lightning Text Reminders","Group":"TEA","ZoneLockH":[887],"ElementsL":[{"Name":"Water Text Reminder","type":1,"radius":7.39,"color":11255841,"overlayVOffset":0.76,"overlayText":"!! WATER !!","refActorRequireBuff":true,"refActorBuffId":[1023,2142],"refActorBuffTimeMax":4.0,"refActorComparisonType":1,"Filled":true},{"Name":"Lightning Text Reminder","type":1,"radius":7.39,"color":11255841,"overlayVOffset":0.76,"overlayText":"!! LIGHTNING !!","refActorRequireBuff":true,"refActorBuffId":[1024,2143],"refActorBuffTimeMax":4.0,"refActorComparisonType":1,"Filled":true}]}
```
3rd Nisi Pass BPOG Positions: Marks where the nisi debuffs need to go for the BPOG line up. Only supports English at the moment.
```
~Lv2~{"Name":"P2 BPOG Positions","Group":"TEA","ZoneLockH":[887],"Scenes":[2],"DCond":5,"ElementsL":[{"Name":"α","refX":89.82,"refY":100.0,"color":3372172800,"thicc":5.0,"overlayText":"α"},{"Name":"γ","refX":92.80687,"refY":100.0,"color":3372155119,"thicc":5.0,"overlayText":"γ"},{"Name":"β","refX":96.385,"refY":100.0,"refZ":-1.9073486E-06,"color":3355478271,"thicc":5.0,"overlayText":"β"},{"Name":"δ","refX":99.91906,"refY":100.0,"refZ":1.9073486E-06,"color":3357277952,"thicc":5.0,"overlayText":"δ"}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":19.0,"MatchIntl":{"En":"Initiating new combat protocol... Commence final judgment!"},"MatchDelay":33.0}]}
```

[Jp] [Beta] [Script] Nisi

Show where to receive your nai-sai from the second time.

It works in another language, but the displayed text is Japanese.

No configuration required.

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Shadowbringers/TEA%20P2%20Nisi.cs
```

# P3: Alexander Prime
Alexander Intermission Debuffs; Text reminder of what debuff you have, only supports English.

```
~Lv2~{"Name":"P3 Alexander Intermission Debuffs","Group":"TEA","ZoneLockH":[887],"Scenes":[7],"DCond":5,"ElementsL":[{"Name":"Aggravated Assault","type":1,"color":255,"overlayFScale":1.5,"overlayText":"Aggravated Assault","refActorRequireBuff":true,"refActorBuffId":[1121],"refActorComparisonType":1,"refActorType":1},{"Name":"Restraining Order","type":1,"color":255,"overlayFScale":1.5,"overlayText":"Restraining Order","refActorRequireBuff":true,"refActorBuffId":[1124],"refActorComparisonType":1,"refActorType":1},{"Name":"Christmas","type":1,"color":255,"overlayFScale":1.5,"overlayText":"Christmas","refActorRequireBuff":true,"refActorBuffId":[1123],"refActorComparisonType":1,"refActorType":1},{"Name":"No Debuff","type":1,"color":255,"overlayFScale":1.5,"overlayText":"No Debuff","refActorRequireBuff":true,"refActorBuffId":[1121,1123,1124],"refActorRequireBuffsInvert":true,"refActorComparisonType":1,"refActorType":1}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":7.0,"MatchIntl":{"En":"I am Alexander...the Creator. You...who would prove yourself worthy of your utopia...will be judged."},"MatchDelay":4.5}]}
```
Inception Starting Positions: Marks where tethers(orbs) need to go and where unmarked players need to go. Maybe require minor adjusting for unmarked players. Supports English at the moment.
```
~Lv2~{"Name":"P3 Inception Initial Baits","Group":"TEA","ZoneLockH":[887],"Scenes":[7],"DCond":5,"ElementsL":[{"Name":"","type":1,"offX":12.76,"offY":-8.66,"radius":1.0,"thicc":5.6,"overlayText":"Bait Orb","refActorModelID":1583,"refActorComparisonType":1,"includeRotation":true,"AdditionalRotation":3.1415927},{"Name":"","type":1,"offX":-12.92,"offY":-8.66,"radius":1.0,"thicc":5.6,"overlayText":"Bait Orb","refActorModelID":1583,"refActorComparisonType":1,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true,"AdditionalRotation":3.1485739},{"Name":"","type":1,"offX":-18.12,"offY":-21.5,"radius":1.0,"thicc":5.6,"overlayText":"Bait Orb","refActorModelID":1583,"refActorComparisonType":1,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true,"AdditionalRotation":1.7418387},{"Name":"","type":1,"offX":18.06,"offY":-21.54,"radius":1.0,"thicc":5.6,"overlayText":"Bait Orb","refActorModelID":1583,"refActorComparisonType":1,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true,"AdditionalRotation":4.5448375},{"Name":"","type":1,"offX":3.94,"offY":-28.46,"radius":1.0,"color":3372167936,"thicc":5.6,"overlayText":"AOE Explosion","refActorModelID":1583,"refActorComparisonType":1,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true,"AdditionalRotation":3.0927234},{"Name":"","type":1,"offX":-7.2,"offY":-28.46,"radius":1.0,"color":3372167936,"thicc":5.6,"overlayText":"AOE Explosion","refActorModelID":1583,"refActorComparisonType":1,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true,"AdditionalRotation":3.0333822},{"Name":"","type":1,"offX":4.0,"offY":-38.24,"radius":1.0,"color":3372167936,"thicc":5.6,"overlayText":"AOE Explosion","refActorModelID":1583,"refActorComparisonType":1,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true,"AdditionalRotation":3.36674},{"Name":"","type":1,"offX":-6.9,"offY":-38.34,"radius":1.0,"color":3372167936,"thicc":5.6,"overlayText":"AOE Explosion","refActorModelID":1583,"refActorComparisonType":1,"includeRotation":true,"onlyUnTargetable":true,"onlyVisible":true,"AdditionalRotation":2.8379054}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":20.0,"MatchIntl":{"En":"Alexander Prime uses Inception Formation."},"MatchDelay":5.0}]}
```
Heart AOE: Supports English
```
~Lv2~{"Name":"P3 - Heart Line AOE","Group":"TEA","ZoneLockH":[887],"Scenes":[7],"DCond":5,"ElementsL":[{"Name":"","type":3,"offY":48.0,"radius":10.01,"thicc":0.0,"refActorDataID":11422,"refActorComparisonType":3,"includeRotation":true,"onlyUnTargetable":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"MatchIntl":{"En":"Alexander Prime uses Inception."},"MatchDelay":1.0,"FireOnce":true}],"Freezing":true,"FreezeFor":6.0}
```

Wormhole Soak Order (untested) for strat https://ff14.toolboxgaming.space/?id=236244852760461&preview=1 :
```
~Lv2~{"Name":"TEA Wormhole soak 5","Group":"TEA","ZoneLockH":[887],"Scenes":[7],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":7.75,"color":3372154890,"overlayPlaceholders":true,"thicc":5.0,"overlayText":"1st","refActorDataID":2007519,"refActorComparisonType":3,"tether":true,"LimitDistance":true,"DistanceSourceX":88.0,"DistanceSourceY":100.0,"DistanceSourceZ":1.9073486E-06,"DistanceMax":10.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"vfx/lockon/eff/m0361trg_a5t.avfx spawned on me","MatchDelay":9.0}]}
~Lv2~{"Name":"TEA Wormhole soak 7","Group":"TEA","ZoneLockH":[887],"Scenes":[7],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":6.0,"color":3372154890,"overlayPlaceholders":true,"thicc":5.0,"overlayText":"2nd","refActorDataID":2007520,"refActorComparisonType":3,"tether":true,"LimitDistance":true,"DistanceSourceX":88.0,"DistanceSourceY":100.0,"DistanceSourceZ":1.9073486E-06,"DistanceMax":10.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"vfx/lockon/eff/m0361trg_a7t.avfx spawned on me","MatchDelay":13.0}]}
~Lv2~{"Name":"TEA Wormhole soak 1","Group":"TEA","ZoneLockH":[887],"Scenes":[7],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":3.0,"color":3372154890,"overlayPlaceholders":true,"thicc":5.0,"overlayText":"3rd","refActorDataID":2007521,"refActorComparisonType":3,"tether":true,"LimitDistance":true,"DistanceSourceX":88.0,"DistanceSourceY":100.0,"DistanceSourceZ":1.9073486E-06,"DistanceMax":10.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"vfx/lockon/eff/m0361trg_a1t.avfx spawned on me","MatchDelay":18.0}]}
~Lv2~{"Name":"TEA Wormhole soak 2","Group":"TEA","ZoneLockH":[887],"Scenes":[7],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":3.0,"overlayPlaceholders":true,"thicc":5.0,"overlayText":"3rd","refActorDataID":2007521,"refActorComparisonType":3,"tether":true,"LimitDistance":true,"DistanceSourceX":112.0,"DistanceSourceY":100.0,"DistanceSourceZ":1.9073486E-06,"DistanceMax":10.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"vfx/lockon/eff/m0361trg_a2t.avfx spawned on me","MatchDelay":18.0}]}
~Lv2~{"Name":"TEA Wormhole soak 6","Group":"TEA","ZoneLockH":[887],"Scenes":[7],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":7.75,"overlayPlaceholders":true,"thicc":5.0,"overlayText":"1st","refActorDataID":2007519,"refActorComparisonType":3,"tether":true,"LimitDistance":true,"DistanceSourceX":112.0,"DistanceSourceY":100.0,"DistanceSourceZ":1.9073486E-06,"DistanceMax":10.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"vfx/lockon/eff/m0361trg_a6t.avfx spawned on me","MatchDelay":9.0}]}
~Lv2~{"Name":"TEA Wormhole soak 8","Group":"TEA","ZoneLockH":[887],"Scenes":[7],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":6.0,"overlayPlaceholders":true,"thicc":5.0,"overlayText":"2nd","refActorDataID":2007520,"refActorComparisonType":3,"tether":true,"LimitDistance":true,"DistanceSourceX":112.0,"DistanceSourceY":100.0,"DistanceSourceZ":1.9073486E-06,"DistanceMax":10.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"vfx/lockon/eff/m0361trg_a8t.avfx spawned on me","MatchDelay":13.0}]}
```

[Jp] [Beta] [Script] Temporal Stasis

Show your bait position.

strat https://ff14.toolboxgaming.space/?id=860745463802461&preview=1

Configuration:

- Select where you stand during each debuff.

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Shadowbringers/TEA%20P2%20Temporal%20Stasis.cs
```

[Jp] [Beta] [Script] Wormhole Formation

Show your bait position.

strat https://www.youtube.com/watch?v=utfUGDM1Y9w&t=0s (JP)

It is called `34固定`.

No configuration required.

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Shadowbringers/TEA%20P3%20Wormhole%20Formation.cs
```

# P4: Perfect Alexander
Trines Dodges: Need to import first two, third is optional. Indicates where the first dodge is (where to start/3rd Trine) and where the safe spot is (1st Trine). Has an optional marker for 2nd Trines. Supports English.
```
~Lv2~{"Name":"P4 Trines 1","Group":"TEA","ZoneLockH":[887],"ElementsL":[{"Name":"","type":1,"radius":0.5,"color":4278190335,"thicc":1.0,"overlayText":"Final Dodge","refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[18574,18575,18576],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":15.0,"MatchIntl":{"En":"Perfect Alexander readies Almighty Judgment."}}],"Freezing":true,"FreezeFor":15.0,"IntervalBetweenFreezes":15.0}
```
```
~Lv2~{"Name":"P4 Trines 3","Group":"TEA","ZoneLockH":[887],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":0.5,"color":4294903808,"thicc":1.0,"overlayText":"First Dodge","refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[18574,18575,18576],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"Filled":true}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":15.0,"MatchIntl":{"En":"Perfect Alexander readies Almighty Judgment."},"MatchDelay":9.0}],"Freezing":true,"FreezeFor":5.0,"IntervalBetweenFreezes":5.0}
```
```
~Lv2~{"Enabled":false,"Name":"P4 Trines 2","Group":"TEA","ZoneLockH":[887],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":0.0,"color":65352,"thicc":10.2,"overlayText":"!!AVOID!!","refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[18574,18575,18576],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3}],"UseTriggers":true,"Triggers":[{"Type":2,"MatchIntl":{"En":"Perfect Alexander readies Almighty Judgment."},"MatchDelay":7.0}],"Freezing":true,"FreezeFor":15.0,"IntervalBetweenFreezes":15.0}
```

[Jp] [Beta] [Script] Fate Projection α

No configuration required.

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Shadowbringers/TEA%20P4%20Fate%20Projection%20α.cs
```

[JP] [Beta] [Script] Fate Projection β

No configuration required.

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Shadowbringers/TEA%20P4%20Fate%20Projection%20β.cs
```