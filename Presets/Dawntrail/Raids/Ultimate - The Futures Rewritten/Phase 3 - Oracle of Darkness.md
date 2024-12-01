## [WIP] Ultimate Relativity
It highlights positions.
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P3%20Ultimate%20Relativity.cs
```

Provide complete navigation with two modes: **Priority-Based Mode** and **Marker-Based Mode.**

### Priority Mode
In this mode, write the player names prioritized as follows:
- Players on the **west side** are listed at the top.
- Players on the **east side** are listed at the bottom.

For example, for the following strategy:  
[Strategy Example](https://docs.google.com/presentation/d/1kkdv5vc8-RLneJDyRNHQ5kZa4ZFnSfKZKKO5FyJudzM)

```
H1 H2 T1 T2 R1 R2 M1 M2
```
Fill in the configuration accordingly.

And please uncheck **Base Orientation is North**.

### Marker Mode
This mode allows you to execute commands when a debuff is applied.
- Write the commands corresponding to each debuff.
- Set directions based on each marker.

For example, for the following DPS settings in the given strategy:  
[Strategy Example](https://x.com/PoneKoni/status/1862307791781900513)

```
/mk attack <me>
/mk stop2 <me>
/mk bind3 <me>
/mk bind3 <me>

NorthWest
NorthEast
South
North
SouthWest
SouthEast
West
East
```
Fill in the configuration accordingly.
And please check **Base Orientation is North**.

To prevent marker assignment conflicts, use **Random Wait** as necessary.

## [WIP] Apocalypse Script
Display the first three aoe ranges
```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P3%20Apocalypse.cs
```

## Unknown mechanic
```
~Lv2~{"Name":"P3-RightLeft Guide","Group":"FRU","ZoneLockH":[1238],"ElementsL":[{"Name":"right","type":1,"offX":2.78,"offY":-0.92,"radius":1.0,"fillIntensity":0.5,"thicc":1.9,"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/common/eff/m0489_stlp_right_c0d1.avfx","refActorVFXMax":6000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"left","type":1,"offX":-2.6,"offY":-1.1,"radius":1.0,"fillIntensity":0.5,"thicc":1.9,"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/common/eff/m0489_stlp_left01f_c0d1.avfx","refActorVFXMax":6000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
```

