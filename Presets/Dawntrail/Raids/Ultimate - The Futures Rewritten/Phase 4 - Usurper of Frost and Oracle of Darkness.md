## [WIP] Auto target switcher
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P4%20AutoTargetSwitcher.cs
```

# Darklit Dragonsong

## [WIP] Script
!!! Guide pending !!!
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P4%20Darklit.cs
```

## Hallowed Wings
```
~Lv2~{"Name":"P4 HallowedWings Left Right AOE","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"R","type":3,"refY":19.72,"radius":25.0,"refActorDataID":17833,"refActorRequireCast":true,"refActorCastId":[40228],"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":1.5707964,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"L","type":3,"refY":19.72,"radius":25.0,"refActorDataID":17833,"refActorRequireCast":true,"refActorCastId":[40227],"refActorComparisonType":3,"includeRotation":true,"AdditionalRotation":4.712389,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```

# Crystallize Time

## [WIP] Script
!!! Guide pending !!!
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P4%20Crystallize%20Time.cs
```

## Ice/Wind + Red debuff hint
```
~Lv2~{"Name":"P4 Red debuffs hint","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":2634023103,"overlayTextColor":4278255370,"overlayVOffset":1.0,"thicc":0.0,"overlayText":"Wind (SOUTHwest/east)","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2463,3263],"refActorRequireAllBuffs":true,"refActorBuffTimeMax":1.0,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":2634023094,"overlayTextColor":4294573824,"overlayVOffset":1.0,"thicc":0.0,"overlayText":"Ice (west/east)","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2462,3263],"refActorRequireAllBuffs":true,"refActorBuffTimeMax":1.0,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```

## [Beta] Traffic light explosions
Timings may be a little off
```
~Lv2~{"Name":"P4 - Crystallize traffic explosions (no tether)","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":12.0,"refActorNPCNameID":9823,"refActorComparisonType":6,"refActorTether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":999.0,"refActorIsTetherInvert":true,"refActorTetherConnectedWithPlayer":[]}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":4.0,"Match":"(12809>40240)","MatchDelay":23.0}]}
~Lv2~{"Name":"P4 - Crystallize traffic explosions (Late tether)","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":12.0,"refActorNPCNameID":9823,"refActorComparisonType":6,"refActorTether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":999.0,"refActorTetherParam2":133,"refActorTetherConnectedWithPlayer":[]}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":4.0,"Match":"(12809>40240)","MatchDelay":28.0}]}
~Lv2~{"Name":"P4 - Crystallize traffic explosions (Early tether)","Group":"FRU","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"","type":1,"radius":12.0,"refActorNPCNameID":9823,"refActorComparisonType":6,"refActorTether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":999.0,"refActorTetherParam2":134,"refActorTetherConnectedWithPlayer":[]}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.5,"Match":"(12809>40240)","MatchDelay":15.0}]}
```

## Better Tidal Light
```
~Lv2~{"Name":"P4 - Tidal Light","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"","type":3,"refY":10.0,"radius":20.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40253],"refActorUseCastTime":true,"refActorCastTimeMax":1.699,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```
