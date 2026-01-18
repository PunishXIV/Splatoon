>[!Important]
>
>[Also import ToolerOfLight's presets. ](https://github.com/ToolerofLight/myfiles/blob/main/Splatoon/%5B7.x%5D%E9%BB%84%E9%87%91%E3%81%AE%E3%83%AC%E3%82%AC%E3%82%B7%E3%83%BC/%E9%9B%B6%E5%BC%8F/%5B7.4%5D%E3%82%A2%E3%83%AB%E3%82%AB%E3%83%87%E3%82%A3%E3%82%A2%E3%83%98%E3%83%93%E3%83%BC%E7%B4%9A4%E5%B1%A4%E9%9B%B6%E5%BC%8F.md) 

# Replication 1

[Script] Clones highlight for Replication 1
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/M12S%20P2%20Clones%201.cs
```

Replication 1 Debuff and aoe size highlight
```
~Lv2~{"Name":"M12S P2 Stack/spread on clones","Group":"M12S P2","ZoneLockH":[1327],"ConditionalAnd":true,"ElementsL":[{"Name":"","type":1,"radius":2.5,"Donut":0.5,"color":3355508223,"fillIntensity":0.5,"overlayTextColor":3355506687,"overlayVOffset":2.0,"thicc":4.0,"overlayText":"Stack","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[3323],"Conditional":true,"Nodraw":true},{"Name":"fire down","type":1,"radius":0.0,"color":3355487743,"Filled":false,"fillIntensity":0.5,"overlayTextColor":3355506687,"overlayVOffset":3.0,"thicc":0.0,"overlayText":"Go to dark clone","refActorRequireBuff":true,"refActorBuffId":[3323],"refActorRequireBuffsInvert":true,"refActorType":1},{"Name":"dark down","type":1,"radius":0.0,"Donut":0.2,"color":3358850816,"Filled":false,"fillIntensity":0.5,"overlayTextColor":3355508484,"overlayVOffset":3.0,"thicc":0.0,"overlayText":"Go to fire clone","refActorRequireBuff":true,"refActorBuffId":[3323],"refActorType":1},{"Name":"","type":1,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[46303,46303,46304,46347,46349,46350,47576,46303,46304,46347,46349,46350,47576],"refActorComparisonType":6,"Conditional":true,"Nodraw":true},{"Name":"dark down","type":1,"radius":4.5,"Donut":0.5,"color":3358850816,"fillIntensity":0.5,"overlayTextColor":3355508484,"overlayVOffset":2.0,"thicc":4.0,"overlayText":"Stack","refActorRequireBuff":true,"refActorBuffId":[3323],"refActorType":1},{"Name":"fire down","type":1,"radius":5.0,"color":3355487743,"Filled":false,"fillIntensity":0.5,"overlayTextColor":3355506687,"overlayVOffset":2.0,"thicc":4.0,"overlayText":"Spread","refActorRequireBuff":true,"refActorBuffId":[3323],"refActorRequireBuffsInvert":true,"refActorType":1}]}
```

# Replication 2
[Script] [Beta] Clones 2 (Replication 2) script

Version 4 should have tether fixed and now both tethers are highlighted, but still watch it and verify it

> [!Warning]
>
> Configuration required. You may reconfigure this script for ANY strat, however, it's not easy.
> Supports only strats that autoresolve near/far (such as banana codex)

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/M12S%20P2%20Clones%202.cs
```

# 3 Blood Mana

Will highlight which orbs have to be picked. There are million strats, so it will not resolve which one exactly is for you or if you are alpha/beta. You can add extra conditional elements for that, if you want. 
```
~Lv2~{"Name":"M12S P2 Mana highlighter","Group":"M12S P2","ZoneLockH":[1327],"DCond":5,"UseTriggers":true,"Freezing":true,"FreezeFor":10.0,"Triggers":[{"Type":2,"Duration":1.0,"Match":"ActionEffect|46333","MatchDelay":1.0}],"ElementsL":[{"Name":"AOE close 1","type":1,"refActorDataID":19206,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":110.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"AOE far 1","type":1,"offY":3.0,"radius":4.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayText":"Pick me!","refActorDataID":19206,"refActorComparisonType":3,"includeRotation":true,"onlyVisible":true,"tether":true,"LimitDistance":true,"LimitDistanceInvert":true,"DistanceSourceX":110.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"RotationOverride":true,"RotationOverridePoint":{"X":90.0,"Y":100.0}},{"Name":"Donut close 1","type":1,"refActorDataID":19207,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":110.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"Donut far 1","type":1,"offY":3.0,"radius":4.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayText":"Pick me!","refActorDataID":19207,"refActorComparisonType":3,"includeRotation":true,"onlyVisible":true,"tether":true,"LimitDistance":true,"LimitDistanceInvert":true,"DistanceSourceX":110.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"RotationOverride":true,"RotationOverridePoint":{"X":90.0,"Y":100.0}},{"Name":"Blue fan close 1","type":1,"refActorDataID":19208,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":110.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"Blue fan far 1","type":1,"offY":3.0,"radius":4.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayText":"Pick me!","refActorDataID":19208,"refActorComparisonType":3,"includeRotation":true,"onlyVisible":true,"tether":true,"LimitDistance":true,"LimitDistanceInvert":true,"DistanceSourceX":110.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"RotationOverride":true,"RotationOverridePoint":{"X":90.0,"Y":100.0}},{"Name":"red fan close 1","type":1,"refActorDataID":19209,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":110.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"red fan far 1","type":1,"offY":3.0,"radius":4.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayText":"Pick me!","refActorDataID":19209,"refActorComparisonType":3,"includeRotation":true,"onlyVisible":true,"tether":true,"LimitDistance":true,"LimitDistanceInvert":true,"DistanceSourceX":110.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"RotationOverride":true,"RotationOverridePoint":{"X":90.0,"Y":100.0}},{"Name":"","Enabled":false},{"Name":"AOE close 1","type":1,"refActorDataID":19206,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":90.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"AOE far 1","type":1,"offY":3.0,"radius":4.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayText":"Pick me!","refActorDataID":19206,"refActorComparisonType":3,"includeRotation":true,"onlyVisible":true,"tether":true,"LimitDistance":true,"LimitDistanceInvert":true,"DistanceSourceX":90.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"RotationOverride":true,"RotationOverridePoint":{"X":110.0,"Y":100.0}},{"Name":"Donut close 1","type":1,"refActorDataID":19207,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":90.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"Donut far 1","type":1,"offY":3.0,"radius":4.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayText":"Pick me!","refActorDataID":19207,"refActorComparisonType":3,"includeRotation":true,"onlyVisible":true,"tether":true,"LimitDistance":true,"LimitDistanceInvert":true,"DistanceSourceX":90.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"RotationOverride":true,"RotationOverridePoint":{"X":110.0,"Y":100.0}},{"Name":"Blue fan close 1","type":1,"refActorDataID":19208,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":90.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"Blue fan far 1","type":1,"offY":3.0,"radius":4.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayText":"Pick me!","refActorDataID":19208,"refActorComparisonType":3,"includeRotation":true,"onlyVisible":true,"tether":true,"LimitDistance":true,"LimitDistanceInvert":true,"DistanceSourceX":90.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"RotationOverride":true,"RotationOverridePoint":{"X":110.0,"Y":100.0}},{"Name":"red fan close 1","type":1,"refActorDataID":19209,"refActorComparisonType":3,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":90.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"red fan far 1","type":1,"offY":3.0,"radius":4.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayText":"Pick me!","refActorDataID":19209,"refActorComparisonType":3,"includeRotation":true,"onlyVisible":true,"tether":true,"LimitDistance":true,"LimitDistanceInvert":true,"DistanceSourceX":90.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"RotationOverride":true,"RotationOverridePoint":{"X":110.0,"Y":100.0}}]}
```

Netherworld-far and Netherworld-near
```
~Lv2~{"Name":"三运 alpha 远近分摊","Group":"M12S P2","ZoneLockH":[1327],"ElementsL":[{"Name":"buff alpha1","type":1,"refActorRequireBuff":true,"refActorBuffId":[4769],"refActorType":1,"Conditional":true,"Nodraw":true},{"Name":"阴界远景","type":1,"radius":5.0,"color":3355508525,"Filled":false,"fillIntensity":0.5,"overlayFScale":3.0,"thicc":20.0,"overlayText":"靠近","overlayTextIntl":{"En":"In"},"refActorModelID":4659,"refActorRequireCast":true,"refActorCastId":[46380,46380],"refActorComparisonType":1,"onlyVisible":true},{"Name":"buff alpha2","type":1,"refActorRequireBuff":true,"refActorBuffId":[4769],"refActorType":1,"Conditional":true,"Nodraw":true},{"Name":"阴界近景","type":1,"radius":5.0,"color":3355508570,"Filled":false,"fillIntensity":0.5,"overlayFScale":3.0,"thicc":20.0,"overlayText":"远离","overlayTextIntl":{"En":"Out"},"refActorModelID":4659,"refActorRequireCast":true,"refActorCastId":[46379,46379],"refActorComparisonType":1,"onlyVisible":true}]}
~Lv2~{"Name":"三运 beta 远近分摊","Group":"M12S P2","ZoneLockH":[1327],"ElementsL":[{"Name":"buff beta1","type":1,"refActorRequireBuff":true,"refActorBuffId":[4771],"refActorType":1,"Conditional":true,"Nodraw":true},{"Name":"阴界远景","type":1,"radius":5.0,"color":3372155132,"Filled":false,"fillIntensity":0.5,"overlayFScale":3.0,"thicc":20.0,"overlayText":"远离分摊","overlayTextIntl":{"En":"Out + Stack"},"refActorModelID":4659,"refActorRequireCast":true,"refActorCastId":[46380,46380],"refActorComparisonType":1,"onlyVisible":true},{"Name":"buff beta2","type":1,"refActorRequireBuff":true,"refActorBuffId":[4771],"refActorType":1,"Conditional":true,"Nodraw":true},{"Name":"阴界近景","type":1,"radius":5.0,"color":3371564800,"Filled":false,"fillIntensity":0.5,"overlayFScale":3.0,"thicc":20.0,"overlayText":"靠近分摊","overlayTextIntl":{"En":"In + Stack"},"refActorModelID":4659,"refActorRequireCast":true,"refActorCastId":[46379,46379],"refActorComparisonType":1,"onlyVisible":true}]}
```

# Idyllic Dream
> [!Warning]
>
> Do not install both scripts! Pick one.

Green Tower Knockback Position
```
~Lv2~{"Name":"风塔击飞站位","Group":"M12S P2","ZoneLockH":[1327],"DCond":5,"UseTriggers":true,"Triggers":[{"TimeBegin":395.0,"Duration":20.0}],"ElementsL":[{"Name":"FAR TOWER","type":1,"offX":2.5,"refActorDataID":2015013,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMin":15.0,"DistanceMax":25.0},{"Name":"CLOSE TOWER","type":1,"offX":-2.5,"refActorDataID":2015013,"refActorComparisonType":3,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":15.0}]}
```

Green Knockback tower tether
```
~Lv2~{"Name":"风塔击飞预测","Group":"M12S P2","ZoneLockH":[1327],"MaxDistance":3.0,"UseDistanceLimit":true,"DistanceLimitType":1,"ElementsL":[{"Name":"击飞预测","type":1,"radius":0.0,"color":4288413440,"Filled":false,"fillIntensity":0.0,"overlayBGColor":4294914815,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":1.4,"thicc":3.0,"refActorDataID":2015013,"refActorComparisonType":3,"tether":true,"ExtraTetherLength":25.0,"LineEndB":1,"DistanceSourceX":83.93563,"DistanceSourceY":96.76696,"DistanceSourceZ":-7.1526574E-07,"DistanceMax":10.0,"UseDistanceSourcePlaceholder":true,"mechanicType":5}]}
```

Brown Tower Reminder
```
~Lv2~{"Name":"土塔注意","Group":"M12S P2","ZoneLockH":[1327],"DCond":5,"UseTriggers":true,"Freezing":true,"FreezeFor":5.0,"IntervalBetweenFreezes":9999.0,"Triggers":[{"TimeBegin":400.0,"Duration":20.0}],"ElementsL":[{"Name":"土塔","type":1,"radius":3.0,"refActorDataID":2015015,"refActorComparisonType":3}]}
```

Heat buff notify
```
~Lv2~{"Name":"热病","Group":"M12S P2","ZoneLockH":[1327],"ElementsL":[{"Name":"热病点名","type":1,"radius":1.0,"overlayVOffset":1.0,"overlayFScale":2.0,"overlayText":"Don't MOVE","overlayTextIntl":{"En":"Don't MOVE","Jp":"Don't MOVE","De":"Don't MOVE","Fr":"Don't MOVE","Other":"热病别动"},"refActorRequireBuff":true,"refActorBuffId":[4768],"refActorType":1}]}
```

### [Script] M12S Idyllic Dream CN
> [!IMPORTANT]
>
> Supports 4A1 waymark order.
   Script auto-detects cardinal/intercardinal first and assigns players to left group (C3D4) or right group (A1B2) based on tethers.

Features:
- Boss clone AOE
- First Defamation Stack guidance (Warning: currently only supports CN server strat)
- Clone AOEs display
- Last boss clone AOE

Video demo:
https://youtu.be/GJApjyNryQo 

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/M12S%20Idyllic%20Dream.cs
```

### [Script] [WIP] [Beta] Idyllic Dream Global.
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/M12S%20P2%20Idyllic%20Dream%20Tired.cs
```
For this script, you NEED to either configure it or import configuration.
Config for tired guide: https://www.youtube.com/watch?v=pL5NGwkaTFs
```
{"TargetScriptName":"SplatoonScriptsOfficial.Duties.Dawntrail@M12S_P2_Idyllic_Dream_Tired","ConfigurationName":"EU Pastebin","Configuration":"iyCA77u/eyJUb3dlclBvc2l0aW9uIjoxLCJJc0dyb3VwMSI6dHJ1ZSwiUGlja3VwcyI6WzAsNCw1LDEsMiw2LDcsM119Aw==","Overrides":"G6KHAKwKbGNp0G9oDtc033e4pEG6rvd2f98tq+nrS3l3AjOtiITpmSUnUAmR0CmuPl1MUvnqlksCATBzs/L3G3uViML1VdZW1v3PtuQrVFU5yV2KOGC8uwyQPJL2QFYCkrB7E2bezG0Zs7w4Jzhg5zXEdsIG1VAL78e2Pd/K7AO1UyfE0ZV3PDhUV3Z+czCkVn/MbjzHy5eoCtD2skrP348lECijII0XqVBuH/L9/eABcQoTNrSYCqI5pk1odBlp1Put80y7Kst+u1GqBnA2XQPktd+uzycOr31toV+z17rGKO4c8oE8tUAnBTXeIftIQlFDw5b9EiqRJeq7BGJVhSdNmqvOinuMy9Vh5Di8Ardcl5t9XybMaXzD0VTOpijxc5oiwgDI5PbhSE8IZGXrD9ryBonYwAa6KtKj4mMFNHTuDCQUMy439NlUXgtO2w266TuJ0LykPsM68TWBTA38ci7SNTaLoPhDahrfl+/TSNN2laoSm+Ma4MErtR4xVyBPSMbpLqGRDYBABvLlQJbHBTXmfyYSXceiWCtGUZY0C6oAbaZ+pG06XVrJ7MLsiG3fJelbcgchBqkHfLkttSkbrBXLMZ7atZeOSntQpI2sX3fEr0wZCBArHqGDboMLxDSNWIaVaJxWfQBphy7YuL+znAcMAatN3jfKwMG3ovhUUPqiHxYkBKxkzch9sOChgMZ1mYmNI7WsJlFdBBmXv9M9xdcUMThz/+BLkcezRdnD7WBTeRcwQ2qHUt0egT2YcsyylOjxWV4eCTAoqRElolViFqj0edMxqGrS5iKBbQlCnziOxJDP8XqJZ+MDp1aABNTI3CkxFTkBTkQ4y7T8eRV/jzOy1UWxOpB0q3if2CuCBB9FDBGJUSPQeeMNmAR6WetgZacQyQB6FzHloGpZuTZIJhdtU0JWWahzUwWj9UuMhOX6TaycoyMpXQArwlURplaLM9fU2L44BbGmioFOXVvrPh00Wfxw5t/7rLTlJBJvk5E0uh73M0/MbR+mlgiMkDaxtdqvvLSAm82+wduKVOGS2zWW/VVI3s+PHliaYH6oUY9P2cIrPbcKbitoKcEfeIG+AzL2D5aYRNweYvSAzfV6NMSSbib377rL7KH9GBywAgMixNuhyqTCnx2BssdYeIoT0Ogf3o+68cvStSqm9U9wT/rMjeDh/SkRrMdtYRWbslP8R/mJOEzW9v/FdT94e2g/EgdfaCIrFJOT+qSEfgWiIilckl3ybQlVb2magwALoCN+Q7JzGnw3Gh5wbFNk2U4eD5AE+h/cxORazOHsEX7lVn7rJVmUYJT+cVnH2yL0piuBla2+kxkUwylcXyLzOIReA4M0Ic52362FwUOu6XohStpW3YNqbD4eroW4GSkXD8sMgczDNP8ggduC+uwqlrZzJfSvdKWr69GFS360z+vrKzO7Kd/ffSNjErMSMphtauy9eACAh0eAfEgZ4zV4ira0qnV36c9yPgSt9tRTYB6n/XoEAEC4wDSJMu4WEI40Hatj0oFIH9ybRuEeCQQAfNfAeHtnwRLkhUsCSxdVG8kLlHkZKLcRbVK/NAEAfPqFTiywHcfNa8/lGGVd+2OhfwEEAGzzrrdQtrZf"}
```
For EU

Stored AOEs (don't need with either scripts):
```
~Lv2~{"Name":"M12S P2 Twisted Vision (1st set of stored aoes)","Group":"M12S P2","ZoneLockH":[1327],"ConditionalAnd":true,"Freezing":true,"FreezeFor":33.0,"IntervalBetweenFreezes":5.0,"FreezeDisplayDelay":28.0,"ElementsL":[{"Name":"","type":1,"refActorNPCNameID":14379,"refActorRequireCast":true,"refActorCastId":[48098],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":2.0,"refActorComparisonType":6,"Conditional":true,"Nodraw":true},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.4,"refActorNPCNameID":14379,"refActorRequireCast":true,"refActorCastId":[46354],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true},{"Name":"","type":1,"radius":10.0,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[46353],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6}]}
~Lv2~{"Name":"M12S P2 Twisted Vision (2nd set of stored aoes)","Group":"M12S P2","ZoneLockH":[1327],"Freezing":true,"FreezeFor":25.0,"IntervalBetweenFreezes":5.0,"FreezeDisplayDelay":11.0,"ElementsL":[{"Name":"","type":1,"refActorNPCNameID":14379,"refActorRequireCast":true,"refActorCastId":[48098],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":2.0,"refActorComparisonType":6,"Conditional":true,"Nodraw":true},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[46352],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true},{"Name":"","type":1,"radius":10.0,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[48303],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[46352],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":3.1415927},{"Name":"","type":1,"offY":28.0,"radius":10.0,"Donut":40.0,"color":3355506687,"fillIntensity":0.2,"thicc":4.0,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[48303],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[46351],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":1.5707964},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[46351],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":4.712389}],"ForcedProjectorActions":[48303]}
```
