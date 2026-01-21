> [!Warning]
>
> Something doesn't installs?
> Make sure Splatoon is updated to the very latest version!!!

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
[Script] [WIP] [Beta] Clones 2 (Replication 2) script

Version 4 should have tether fixed and now both tethers are highlighted, but still watch it and verify it

> [!Warning]
>
> Configuration required. You may reconfigure this script for ANY strat, however, it's not easy.
> Supports only strats that autoresolve near/far (such as banana codex)

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/M12S%20P2%20Clones%202.cs
```
## Community configurations
> [!Important]
>
> These are presets submitted by the community. Because script is also still in progress, configurations aren't guaranteed to be accurate and aren't guaranteed not to be broken as development continues.

### EU & NA https://raidplan.io/plan/SFa6J6wDrU9PlCJ4 
```
{"TargetScriptName":"SplatoonScriptsOfficial.Duties.Dawntrail@M12S_P2_Clones_2","ConfigurationName":"","Configuration":"GygGAJwHduwASt1dPTyr3P19V22ViAdH0fPZhB+ritBq1lvqD+1HKGaSmU0hfnbFEE7oGLW2+lp/vtLolMLViokkt8U9kWikRImYW8NaJilF0A7njvmrfqxPPKukgsK7chHz30FXUbX/uYam1n+lSfZ0dbSD+JMvJPSaSutaa+9O0wiIHA2r7eq78mUvC76GpED985PIBz5pEB42PT2r0gFuB+Q9lSCTd8ho4MmgOTIf7ualUB+P1VnVWJ53jqf5PNBPzfqrijN7L1Ks1XN/8jOn8afSIBzJCYQM+9PHM3CmsgQVOzrUAbYBvthixclkeWSRNMYBfrxM41CN+RtBaQ8MQNRsRutbUdZSsF5Lw95NvHR4W+9toGl+/pIRNg1n0bzO7DM4yFNjWTgAo98HwImAqQMJs5vtvFrfWRvBRFrEEhWhIDKFdFau+1OglD5NUbUNN0QghiK/p7A5r3FrqSPduJAJnU15VTaT0JJqKnASwinXkg31J57avogpt+SJlnpwKWeh4QE=","Overrides":null}
```

### CN strat (?)
```
{"TargetScriptName":"SplatoonScriptsOfficial.Duties.Dawntrail@M12S_P2_Clones_2","ConfigurationName":"盗火","Configuration":"G2QEIJyHsRsiYnrf+V0qRcrd33ed7XmVf/pCjrYANX5umVomJ9TP05Ol/hQFSbApRNlGuNAZuqb+CX5bFVhsExoeDijToijLooSKqkBecEIzt4bzrkH53jFU9rTop/WBeT7f9+TB4c/pAOI5feqomF9/b0FEGRnl6LDHgOOrMGA9mj3X/Qe60nnEySv65+eJ79/B50nknUg3r85TWATW9Hse7fVpBDE9EGQkW8Hfbo//105IrMuNVDED6Vu8g3xqcqRuxg7nLKFdB2aFd8vcVowou4zTuytkPKBq21BqOQuLkJ0Asi1f3mrBD0aFl4pcnO1qZTrklXrfrXQBfZruhZ0bXY25iRJyip5M1AoZUJe0a7brUPcsAgnrVd0GscDNMahLX76BamHPjLp3YtBmj5bNAFQW3wDbYwElq9TwKaeB2wUOUtNZjm6KwGC4KH4l1+2d7EymhvZGcDKrGNDRWOsyq12RzECut6l7TUbeqqNqXMBoigxkPtW2RkA4CNrG7l7VolnV93yCDir2Ag==","Overrides":null}
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

# [Script] [WIP] [Beta] Idyllic Dream Global Script
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/M12S%20P2%20Idyllic%20Dream%20Tired.cs
```
For this script, you NEED to either configure it or import configuration.

### For EU
```
{"TargetScriptName":"SplatoonScriptsOfficial.Duties.Dawntrail@M12S_P2_Idyllic_Dream_Tired","ConfigurationName":"EU Pastebin","Configuration":"iyCA77u/eyJUb3dlclBvc2l0aW9uIjoxLCJJc0dyb3VwMSI6dHJ1ZSwiUGlja3VwcyI6WzAsNCw1LDEsMiw2LDcsM119Aw==","Overrides":"G6KHAKwKbGNp0G9oDtc033e4pEG6rvd2f98tq+nrS3l3AjOtiITpmSUnUAmR0CmuPl1MUvnqlksCATBzs/L3G3uViML1VdZW1v3PtuQrVFU5yV2KOGC8uwyQPJL2QFYCkrB7E2bezG0Zs7w4Jzhg5zXEdsIG1VAL78e2Pd/K7AO1UyfE0ZV3PDhUV3Z+czCkVn/MbjzHy5eoCtD2skrP348lECijII0XqVBuH/L9/eABcQoTNrSYCqI5pk1odBlp1Put80y7Kst+u1GqBnA2XQPktd+uzycOr31toV+z17rGKO4c8oE8tUAnBTXeIftIQlFDw5b9EiqRJeq7BGJVhSdNmqvOinuMy9Vh5Di8Ardcl5t9XybMaXzD0VTOpijxc5oiwgDI5PbhSE8IZGXrD9ryBonYwAa6KtKj4mMFNHTuDCQUMy439NlUXgtO2w266TuJ0LykPsM68TWBTA38ci7SNTaLoPhDahrfl+/TSNN2laoSm+Ma4MErtR4xVyBPSMbpLqGRDYBABvLlQJbHBTXmfyYSXceiWCtGUZY0C6oAbaZ+pG06XVrJ7MLsiG3fJelbcgchBqkHfLkttSkbrBXLMZ7atZeOSntQpI2sX3fEr0wZCBArHqGDboMLxDSNWIaVaJxWfQBphy7YuL+znAcMAatN3jfKwMG3ovhUUPqiHxYkBKxkzch9sOChgMZ1mYmNI7WsJlFdBBmXv9M9xdcUMThz/+BLkcezRdnD7WBTeRcwQ2qHUt0egT2YcsyylOjxWV4eCTAoqRElolViFqj0edMxqGrS5iKBbQlCnziOxJDP8XqJZ+MDp1aABNTI3CkxFTkBTkQ4y7T8eRV/jzOy1UWxOpB0q3if2CuCBB9FDBGJUSPQeeMNmAR6WetgZacQyQB6FzHloGpZuTZIJhdtU0JWWahzUwWj9UuMhOX6TaycoyMpXQArwlURplaLM9fU2L44BbGmioFOXVvrPh00Wfxw5t/7rLTlJBJvk5E0uh73M0/MbR+mlgiMkDaxtdqvvLSAm82+wduKVOGS2zWW/VVI3s+PHliaYH6oUY9P2cIrPbcKbitoKcEfeIG+AzL2D5aYRNweYvSAzfV6NMSSbib377rL7KH9GBywAgMixNuhyqTCnx2BssdYeIoT0Ogf3o+68cvStSqm9U9wT/rMjeDh/SkRrMdtYRWbslP8R/mJOEzW9v/FdT94e2g/EgdfaCIrFJOT+qSEfgWiIilckl3ybQlVb2magwALoCN+Q7JzGnw3Gh5wbFNk2U4eD5AE+h/cxORazOHsEX7lVn7rJVmUYJT+cVnH2yL0piuBla2+kxkUwylcXyLzOIReA4M0Ic52362FwUOu6XohStpW3YNqbD4eroW4GSkXD8sMgczDNP8ggduC+uwqlrZzJfSvdKWr69GFS360z+vrKzO7Kd/ffSNjErMSMphtauy9eACAh0eAfEgZ4zV4ira0qnV36c9yPgSt9tRTYB6n/XoEAEC4wDSJMu4WEI40Hatj0oFIH9ybRuEeCQQAfNfAeHtnwRLkhUsCSxdVG8kLlHkZKLcRbVK/NAEAfPqFTiywHcfNa8/lGGVd+2OhfwEEAGzzrrdQtrZf"}
```

### For JP kanatan

http://kanatan.info/archives/41400296.html
```
{"TargetScriptName":"SplatoonScriptsOfficial.Duties.Dawntrail@M12S_P2_Idyllic_Dream_Tired","ConfigurationName":"jp kanatan","Configuration":"G+gAIBwHdoy2mFPhS6aLkqXr3ZC1pX4hvXgWseQCiVG7v7q5aqhdmKIlKFZ5ej+H1/JQHtLBTVYsBjLonPW9P+BhoIFmSDQY7y1Kij/hQJFeav0Yr9NvfY8/K3qNPnW72v3wcac/t2uY3p+KQSBRaCwOH+iiHN/df+aTR5KJYgEVxSjVobZ0ZP27vOTz+KBNYKCGT5IRKfsD","Overrides":"G6wwACwLzMPQ+BH7lCy5Ljuw2inyct8VxYRy9/ddZzPl0TdCoXxmblYtt6jb9UJu8Fcnjln7zSydpEiIhA5jwDlMPi7RGVaGdrXfr4o2Ko1Uv5fCUDqsvEVFThC10AiRnMRqFGtkUuYp0+G9pdWeF2Us61g/1dBo+EWPE792DzROykIcfXme5KEK6r85qBmOxO7awQ5folxwvRu7kjfATE0giNUONKsgjqgzosl7cGHhvEsnymVfhqBFFygJGZRo9JcDfdqiMTxmXmkBfE2Bg6z2c+mvsJ5zHKFMtXKNJFSD1zhYxwOZFLddItrd20IkpD25L7EWGZFq0oclVK5QSa7yewgXt6XzMASq3XItWH3pYGFP7UpIoltVUWLn1CPHlAlZ6tKRnBCoytY7Ol6hEKtUMNKRMzMbKyChMxeAYF5cZih59HEPPtsMTld6yNG+bGO0/5NrApUaBGVXIFNDNxXNX0mVSYIvUaqqX3Kv/rq9g5+4IzMCMNjxSMVprqGzWwCBHSiQA1WeVNSp/yUSfcdUnBVTKA0tghyfDbJzCsTv0ow5wumLfelV0C1lgAiD9AOBXEdt5YEtcxwT8QEUZXEXQJE0MvU+ELsythCgVawyMFKElCZUVRIZ0aKiLFECGYcumP6+s730mfnMt/xWat+ZZ07JqUXrU5fmJ/OTb/Ot1P4yFw4kroWHWAiJZR0K1SlgTOHOGSm5poTBWHpnnjgFPF1UAtwBD/EF1JjaoFVzQJAAxp7ZjrIGfMHTbAeMSkrm2S87+LrwuMxz+paMOSXQHcEyRu1za8xneLskq/L7plqEBMTIPIgYJ06QJaIsS1+QFV7xRrq+KFUHim5O94m7IiBEFvFDJD6NQOaNNxAK6Lm+ZHZTSGRiuzyKezo0vyGZx5Qq5yjyqiKU/xCNpl5zJIIrO/gNQ0fSugW0BJcTTEcvxjLOs+hzimJVHQOZuuOsZ4zJOn3Y8J4MXIDndDP7+KeAqYdEZJLBow8C/NI//8C3FeE5KTWz+fStcKaivNyrKv7lvV2B3Q9Y/M8AAEDGbXUXT902gs8fTyD4tv91KX677lRN3aM2t1Lfkj5doQPWdnyPVe7trGlkHekx9PH4bBXJ1O9A4SW/5X9HmnMv"}
```

### For Jp Nukemaru

```
{"TargetScriptName":"SplatoonScriptsOfficial.Duties.Dawntrail@M12S_P2_Idyllic_Dream_Tired","ConfigurationName":"jp kanatan","Configuration":"G14BoJwH2TnpNFySftjRqPd2f98ZMK/UL6TNWXgiDVLoNEuXS1/Lm/hFqOm3e6PognWTdH5Ti2xTVLFA0DPC63VshnhbNkDDIIFakGC070gkfiGAIZ1UuzHao5/5Pv7Yoff3znbTgx90mfESt+e+avFnEEgUGovDB3OhFH1+aeLSIkk1CUNEMaOaDLUzv/324PFU3uy4cMoxnwEVA6krVnxAjoAviMX+6E3kz0HIOikrC59NoTICStiZ3Z/DBnJZdJgK6qpeSPJOXsDv","Overrides":"G6wwACwLzMPQ+BH7lCy5Ljuw2inyct8VxYRy9/ddZzPl0TdCoXxmblYtt6jb9UJu8Fcnjln7zSydpEiIhA5jwDlMPi7RGVaGdrXfr4o2Ko1Uv5fCUDqsvEVFThC10AiRnMRqFGtkUuYp0+G9pdWeF2Us61g/1dBo+EWPE792DzROykIcfXme5KEK6r85qBmOxO7awQ5folxwvRu7kjfATE0giNUONKsgjqgzosl7cGHhvEsnymVfhqBFFygJGZRo9JcDfdqiMTxmXmkBfE2Bg6z2c+mvsJ5zHKFMtXKNJFSD1zhYxwOZFLddItrd20IkpD25L7EWGZFq0oclVK5QSa7yewgXt6XzMASq3XItWH3pYGFP7UpIoltVUWLn1CPHlAlZ6tKRnBCoytY7Ol6hEKtUMNKRMzMbKyChMxeAYF5cZih59HEPPtsMTld6yNG+bGO0/5NrApUaBGVXIFNDNxXNX0mVSYIvUaqqX3Kv/rq9g5+4IzMCMNjxSMVprqGzWwCBHSiQA1WeVNSp/yUSfcdUnBVTKA0tghyfDbJzCsTv0ow5wumLfelV0C1lgAiD9AOBXEdt5YEtcxwT8QEUZXEXQJE0MvU+ELsythCgVawyMFKElCZUVRIZ0aKiLFECGYcumP6+s730mfnMt/xWat+ZZ07JqUXrU5fmJ/OTb/Ot1P4yFw4kroWHWAiJZR0K1SlgTOHOGSm5poTBWHpnnjgFPF1UAtwBD/EF1JjaoFVzQJAAxp7ZjrIGfMHTbAeMSkrm2S87+LrwuMxz+paMOSXQHcEyRu1za8xneLskq/L7plqEBMTIPIgYJ06QJaIsS1+QFV7xRrq+KFUHim5O94m7IiBEFvFDJD6NQOaNNxAK6Lm+ZHZTSGRiuzyKezo0vyGZx5Qq5yjyqiKU/xCNpl5zJIIrO/gNQ0fSugW0BJcTTEcvxjLOs+hzimJVHQOZuuOsZ4zJOn3Y8J4MXIDndDP7+KeAqYdEZJLBow8C/NI//8C3FeE5KTWz+fStcKaivNyrKv7lvV2B3Q9Y/M8AAEDGbXUXT902gs8fTyD4tv91KX677lRN3aM2t1Lfkj5doQPWdnyPVe7trGlkHekx9PH4bBXJ1O9A4SW/5X9HmnMv"}
```

# [Script] M12S Idyllic Dream CN
> [!Warning]
>
> Do not install both scripts! Pick one.

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

# Idyllic Dream presets

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

Idyllic Dream Tower Guider MMW
> See the strat [Here](https://github.com/user-attachments/assets/b98cb467-78ef-4563-bd21-f8dd9f47df04)
```
~Lv2~{"Name":"Buffed Far Close Guide MMW","InternationalName":{"Other":"引导远近点名站位 MMW"},"Group":"M12S P2","ZoneLockH":[1327],"ConditionalAnd":true,"ElementsL":[{"Name":"左近点名检查","type":1,"refActorRequireBuff":true,"refActorBuffId":[4767],"refActorType":1,"DistanceSourceX":86.0,"DistanceSourceY":100.0,"DistanceMax":1.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"左近点名距离检查","type":1,"refActorPlaceholder":["<me>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":86.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"Nodraw":true},{"Name":"左近点名","refX":94.74917,"refY":99.63798,"fillIntensity":0.5,"tether":true},{"Name":"左远点名检查","type":1,"radius":0.0,"refActorRequireBuff":true,"refActorBuffId":[4766],"refActorType":1,"DistanceSourceX":86.0,"DistanceSourceY":100.0,"DistanceSourceZ":-7.152657E-07,"DistanceMax":10.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"左远点名距离检查","type":1,"radius":0.0,"refActorPlaceholder":["<me>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":86.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"Nodraw":true},{"Name":"左远点名","refX":91.38865,"refY":107.42661,"fillIntensity":0.5,"tether":true},{"Name":"右近点名检查","type":1,"refActorRequireBuff":true,"refActorBuffId":[4767],"refActorType":1,"DistanceSourceX":86.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"右近点名距离检查","type":1,"refActorPlaceholder":["<me>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":114.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"Nodraw":true},{"Name":"右近点名","refX":105.03054,"refY":99.80684,"refZ":-3.8146973E-06,"tether":true},{"Name":"右远点名检查","type":1,"refActorRequireBuff":true,"refActorBuffId":[4766],"refActorType":1,"DistanceSourceX":86.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"ConditionalReset":true,"Nodraw":true},{"Name":"右远点名距离检查","type":1,"refActorPlaceholder":["<me>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":114.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"Nodraw":true},{"Name":"右远点名","refX":108.805916,"refY":92.572624,"tether":true}]}
~Lv2~{"Name":"Non buffed Ranged MMW","InternationalName":{"Other":"引导远近非点名远程站位 MMW"},"Group":"M12S P2","ZoneLockH":[1327],"ConditionalAnd":true,"DCond":5,"JobLockH":[24,28,33,40,5,7,23,25,26,27,31,35,36,38,42,6],"UseTriggers":true,"Triggers":[{"TimeBegin":403.0,"Duration":5.0}],"ElementsL":[{"Name":"非点名检查","type":1,"refActorRequireBuff":true,"refActorBuffId":[4766,4767],"refActorType":1,"Conditional":true,"ConditionalInvert":true,"ConditionalReset":true,"Nodraw":true},{"Name":"左半场检查","type":1,"refActorPlaceholder":["<me>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":86.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"Nodraw":true},{"Name":"Left Ranged Pos 左远程位置","refX":86.06062,"refY":90.88095,"refZ":-3.8146973E-06,"fillIntensity":0.5,"tether":true},{"Name":"非点名检查","type":1,"refActorRequireBuff":true,"refActorBuffId":[4766,4767],"refActorType":1,"Conditional":true,"ConditionalInvert":true,"ConditionalReset":true,"Nodraw":true},{"Name":"右半场检查","type":1,"refActorPlaceholder":["<me>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":114.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"Nodraw":true},{"Name":"Right Ranged Pos 右远程位置","refX":114.308,"refY":108.91028,"refZ":-3.8146973E-06,"fillIntensity":0.5,"tether":true}]}
~Lv2~{"Name":"No buff Melee MMW","InternationalName":{"Other":"引导远近非点名近战站位 MMW"},"Group":"M12S P2","ZoneLockH":[1327],"ConditionalAnd":true,"DCond":5,"JobLockH":[37,29,39,34,20,21,3,41,30,22,4,2,32,19,1],"UseTriggers":true,"Triggers":[{"TimeBegin":403.0,"Duration":5.0}],"ElementsL":[{"Name":"非点名检查","type":1,"refActorRequireBuff":true,"refActorBuffId":[4766,4767],"refActorType":1,"Conditional":true,"ConditionalInvert":true,"ConditionalReset":true,"Nodraw":true},{"Name":"左半场检查","type":1,"refActorPlaceholder":["<me>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":86.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"Nodraw":true},{"Name":"Left Mele Pos 左近战位置","refX":90.62298,"refY":100.51265,"refZ":7.6293945E-06,"tether":true},{"Name":"非点名检查","type":1,"refActorRequireBuff":true,"refActorBuffId":[4766,4767],"refActorType":1,"Conditional":true,"ConditionalInvert":true,"ConditionalReset":true,"Nodraw":true},{"Name":"右半场检查","type":1,"refActorPlaceholder":["<me>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":114.0,"DistanceSourceY":100.0,"DistanceMax":10.0,"Conditional":true,"Nodraw":true},{"Name":"Right Mele Pos 右近战位置","refX":108.81294,"refY":100.22826,"fillIntensity":0.5,"tether":true}]}
```

Stored AOEs (don't need with either scripts):
```
~Lv2~{"Name":"M12S P2 Twisted Vision (1st set of stored aoes)","Group":"M12S P2","ZoneLockH":[1327],"ConditionalAnd":true,"Freezing":true,"FreezeFor":33.0,"IntervalBetweenFreezes":5.0,"FreezeDisplayDelay":28.0,"ElementsL":[{"Name":"","type":1,"refActorNPCNameID":14379,"refActorRequireCast":true,"refActorCastId":[48098],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":2.0,"refActorComparisonType":6,"Conditional":true,"Nodraw":true},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.4,"refActorNPCNameID":14379,"refActorRequireCast":true,"refActorCastId":[46354],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true},{"Name":"","type":1,"radius":10.0,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[46353],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6}]}
~Lv2~{"Name":"M12S P2 Twisted Vision (2nd set of stored aoes)","Group":"M12S P2","ZoneLockH":[1327],"Freezing":true,"FreezeFor":25.0,"IntervalBetweenFreezes":5.0,"FreezeDisplayDelay":11.0,"ElementsL":[{"Name":"","type":1,"refActorNPCNameID":14379,"refActorRequireCast":true,"refActorCastId":[48098],"refActorUseCastTime":true,"refActorCastTimeMin":1.0,"refActorCastTimeMax":2.0,"refActorComparisonType":6,"Conditional":true,"Nodraw":true},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[46352],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true},{"Name":"","type":1,"radius":10.0,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[48303],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[46352],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":3.1415927},{"Name":"","type":1,"offY":28.0,"radius":10.0,"Donut":40.0,"color":3355506687,"fillIntensity":0.2,"thicc":4.0,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[48303],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true,"RotationOverride":true,"RotationOverridePoint":{"X":100.0,"Y":100.0}},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[46351],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":1.5707964},{"Name":"","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.4,"refActorNPCNameID":14380,"refActorRequireCast":true,"refActorCastId":[46351],"refActorCastTimeMin":1.0,"refActorCastTimeMax":999.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":4.712389}],"ForcedProjectorActions":[48303]}
```
