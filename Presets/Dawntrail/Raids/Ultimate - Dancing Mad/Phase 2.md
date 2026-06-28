

## General casts

Recommended to import.
```
~Lv2~{"Name":"Dmad P2 Clones bait","Group":"Dancing Mad (Ultimate) P2 - NXIV","ZoneLockH":[1363],"ElementsL":[{"Name":"Kefka Casts","type":1,"radius":5.5,"Donut":0.1,"color":3370385663,"fillIntensity":0.4,"refActorNPCNameID":7131,"refActorRequireCast":true,"refActorCastId":[47826,47827],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":8.0,"refActorComparisonType":6,"Conditional":true,"Nodraw":true},{"Name":"Aoe 1","type":1,"radius":5.0,"Donut":0.1,"color":3370385663,"fillIntensity":0.4,"refActorModelID":4967,"TargetAlteration":1100,"refActorComparisonType":1,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":9.0,"IsDead":false},{"Name":"Aoe 2","type":1,"radius":5.0,"Donut":0.1,"color":3370385663,"fillIntensity":0.4,"refActorModelID":4967,"TargetAlteration":1101,"refActorComparisonType":1,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":9.0,"IsDead":false},{"Name":"Aoe 3","type":1,"radius":5.0,"Donut":0.1,"color":3370385663,"fillIntensity":0.4,"refActorModelID":4967,"TargetAlteration":1102,"refActorComparisonType":1,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":9.0,"IsDead":false},{"Name":"Aoe 4","type":1,"radius":5.0,"Donut":0.1,"color":3370385663,"fillIntensity":0.4,"refActorModelID":4967,"TargetAlteration":1103,"refActorComparisonType":1,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":100.0,"DistanceMax":9.0}],"ForcedProjectorActions":[]}
~Lv2~{"Name":"Dmad P2 Wings of Destruction","Group":"Dancing Mad (Ultimate) P2 - NXIV","ZoneLockH":[1363],"ElementsL":[{"Name":"LeftSide","type":4,"radius":20.0,"coneAngleMin":180,"coneAngleMax":360,"refActorDataID":19506,"refActorRequireCast":true,"refActorCastId":[47821],"refActorComparisonType":3,"includeRotation":true},{"Name":"RightSide","type":4,"radius":20.0,"coneAngleMax":180,"refActorDataID":19506,"refActorRequireCast":true,"refActorCastId":[47822],"refActorComparisonType":3,"includeRotation":true}]}
~Lv2~{"Name":"Dmad P2 - All Things Ending","Group":"Dancing Mad (Ultimate) P2 - NXIV","ZoneLockH":[1363],"ElementsL":[{"Name":"Cone","type":4,"radius":40.0,"coneAngleMin":-90,"coneAngleMax":90,"color":3355508706,"fillIntensity":0.1,"castAnimation":1,"animationColor":1073742079,"pulseSize":4.0,"pulseFrequency":2.0,"thicc":3.0,"refActorNPCNameID":7131,"refActorRequireCast":true,"refActorCastId":[47836],"refActorComparisonType":6,"includeRotation":true},{"Name":"Cone","type":4,"radius":40.0,"coneAngleMin":90,"coneAngleMax":270,"color":3355508706,"fillIntensity":0.1,"castAnimation":1,"animationColor":1073742079,"pulseSize":4.0,"pulseFrequency":2.0,"thicc":3.0,"refActorNPCNameID":7131,"refActorRequireCast":true,"refActorCastId":[47837],"refActorComparisonType":6,"includeRotation":true}],"BlacklistedProjectorActions":[47837,47836]}
```

## [Script] Forsaken Visualizer

This script just displays order of mechanics, your (or other players) markers, and visualizes attacks coming from players in towers. You can use it in conjunction with other scripts. This does not solves mechanic.

> [!Warning]
>
> It is required that you configure the script. 

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Forsaken.cs
```

### [Strongly recommended] Declutter configuration for Forsaken Visualizer

This configuration will only provide visual indicators and it has tuned them to be less cluttery than default values. It is recommended to use forsaken visualizer script in conjunction with one of the scripts below so you can always micro-adjust. 

```
{"TargetScriptName":"SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad@P2_Forsaken","ConfigurationName":"Declutter","Configuration":"G+0BIBwJ2RnmRarDyeJPqaPwbm0zJEM/Ln8QdOuLwmB0+fhhIAuvlom1+3u/81/PEwspxO8HpKhAhqO7o/FoArNt+Uj6VBTU9EJk+4zD+64QQE7QuVpjsLM8NymLzvsbX3FvrThENhmRR3n9n/I9c1yWHEwURYTSDDVNziK6SWB0Ne+EWjaQZcsTP9IpRZHAu7J65oe6gP+uVGAzxxdg8JWdU3lAvAV60XCzWol6IPTEHrxRdIrkxaRVFc2MjRQXao99RqjJ0qsYPs3bR53qmDY=","Overrides":"G0coYJwFdiwPKAwfVYvD1HUNsvv7rkT6dDqVv95JKe0YF7dqx2yb9h82dc3P0IYN+UMxDinD/9rvN78RScTk2n/oDJF39ux+RDQyNPM3ey+iFhKEEMgRQiIkLYFlOUOKzJ8NJumWctq9zYn1yIa6g489wXOq484x/uAwqJiA1z6ghqVRepE1ucv+i0LYnhHjQ0qIsz2PwJYRSZeaUxyavDWlFB5OFHon0sCUS+QNVW52jwm8NJPPpKGRtZe4Mv+B7h+grbq+KI9DLj9PD3RyrCtQWTnTKbapUHHbL2qPqzghsE0ilJqM4MxA00eYmKvNBp0jXBtDN8m5upyBxKEGw5U3z4AIuVOmjTWXSd4FMNeQegWJV6K8EzQOQ8wImjZVOCFCtUtCHNPu3NlltUDSp9QA20XClLz7YOGCvqfkzva2Ehud9lYw+zEukNhF58w6liHtMjL+vMgL4ao7fD4v27PK2Fcv8QPGFRQk+XKehnBVqWCXU5zkes72CBpF4Uo7Ej4dmHljWY2tdj8+bcKnBaWCnQFeUEh3Ub+4Lfw6/RcvPVvlEidcMfoyCxgjFQ6OdUBjF3QTni9w4G5gAIYYtWbPuUCct+hBJjqU4hgKQacThWZ8l1ExuMAZi4kxbnJOlSSzTrJOj38OVTaQTFKiRlJul+84u0h20ZM6hyrbSEYURM6HtAAwdNOOWHk5cNU45W7E5SwKoZJZIJmhHLFkmDhSXXCwEHB4WtZk03UW5eDJDrosB/LwKrsqAvuZsbxqooD260WWp5mbtZbWsKyWx6WGIN/iG6mfsDwfhQqv0Ffl9c0EDAW09FLwhHhxHOD4xMGBYT0wz1/fkPNk8VGctODFObmSKMCCJy4CysVJcW6DJykx+v9NmV/bCIGrRDFyhNGljdnb+WMuYWvu1EGcAGg72DmzZoulCy56W/Ze+5QFoeUyamgROZd8nXSnNCSOE6uYbZz+4W+Zj6foX2hGb4iyUTFD7TAabu3XYRTXkZUVYR/0drkDx0xl21LJPn2mChz9MVSZfwINAIhdnjG0j8QKQWdRBRgowECCgfd0rHWAoCYghHh9vo1zySsD7Ui0uT7Watqjb7p4eI7zv+1RHmywL1PX+O17l2u8wMajtFpfZrDaqmB/dQ=="}
```

## [YOU'RE HERE FOR THIS] Forsaken solver scripts.
You must only use one solver script at a time.
- [Forsaken Fixed Partner (Recommended for mainstream NA/EU strats)](https://github.com/PunishXIV/Splatoon/blob/main/Presets/Dawntrail/Raids/Ultimate%20-%20Dancing%20Mad/Phase%202/1.%20Forsaken%20Fixed%20Partner%20script.md)
- [Forsaken beta guide (Recommended for mainstream JP strats)](https://github.com/PunishXIV/Splatoon/blob/main/Presets/Dawntrail/Raids/Ultimate%20-%20Dancing%20Mad/Phase%202/2.%20Forsaken%20beta%20guide.md)
- [Forsaken individual scripts (Mostly contain individual scripts for specific JP strats)](https://github.com/PunishXIV/Splatoon/blob/main/Presets/Dawntrail/Raids/Ultimate%20-%20Dancing%20Mad/Phase%202/3.%20Forsaken%20individual%20scripts.md)

## **[Script]** P2 Trine guide

Self-only helper for P2 Trine. It shows:
- half-room safe-side wait;
- first Trine dodge position;
- final tankbuster spread position.

The script solves the route from Trine telegraph objects and Trine action positions. The final tankbuster split uses the script priority:
- Priority 1 tank: near / MT position.
- Priority 2 tank: far / OT position.

Script file in this repository:
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Trine_Beta.cs
```

## **[Script] [Beta]** P2 Trine Effects

This script displays Trine's effects in order (3-1-3).
Use this if 'Trine guide' is unavailable.

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/Dancing%20Mad/P2_Trine_Effects.cs
```
