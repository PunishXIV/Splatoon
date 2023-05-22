[International] [Script] [Configuration required] Program Loop complete resolution script.
- Made for the resolution way described in https://docs.google.com/document/d/1CzWZJ3kdvCtK6i0bT83kyNK1Wydrs3fvI52vVuCKiGM/edit
- Brightly displays tethers; correctly taken tethers display AOE and display in green colors. 
- Displays when you have to take tether or tower. 
- Explicitly highlights your designated tower at a time when you need to take it; you can configure it to be CW or CCW from certain position in settings; swap partners supported - configure name of your partners and if you have same debuff as they do - your direction will be reversed;
- Highlights tether that you're supposed to take, according to priority you have set before.
- Explicitly highlights your designated tether drop spot once you have picked up your tether, according to the priority you have set before.
- Everything is configurable. You can disable individual functions, change colors of elements, remove things you don't want to see. By default, all functions are enabled.
```
https://github.com/NightmareXIV/Splatoon/raw/master/SplatoonScripts/Duties/Endwalker/The%20Omega%20Protocol/Program%20Loop.cs
```

[International] [Script] Pantoraktor. Simply displays upcoming line AOE and bomb AOE. If it's your - it will be red (by default)
```
https://github.com/NightmareXIV/Splatoon/raw/master/SplatoonScripts/Duties/Endwalker/The%20Omega%20Protocol/Pantokrator.cs
```

[International] P1 Basic Mulipreset / 基本繪制 (Some circles instead of ranges)
```
~Lv2~{"Name":"P1 Basic Mulipreset / 基本繪制","Group":"Omega / 絶オメガ検証戦","ZoneLockH":[1122],"ElementsL":[{"Name":"Tower Finder / 塔 ","type":1,"radius":2.5,"Donut":0.5,"color":4278255612,"thicc":3.0,"refActorNPCID":2013245,"refActorObjectLife":true,"refActorLifetimeMin":0.0,"refActorLifetimeMax":9.0,"refActorComparisonType":4,"tether":true},{"Name":"Tower Reminder / 進塔提醒","type":1,"overlayBGColor":2684354560,"overlayTextColor":4278253567,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":">>> !!! TOWER !!! <<<","refActorRequireBuff":true,"refActorBuffId":[3456],"refActorUseBuffTime":true,"refActorBuffTimeMax":11.0,"refActorComparisonType":1,"onlyVisible":true},{"Name":"Laser / 集合提醒 (激光)","type":1,"radius":4.52,"color":4294940160,"overlayBGColor":3355443200,"overlayTextColor":4294940160,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":4.6,"overlayText":" >>> STACK <<< ","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[3507,3508,3509,3510],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"FillStep":0.778,"refActorComparisonType":5,"onlyVisible":true},{"Name":"Missile / 分散提醒 (射弾)","type":1,"radius":5.0,"color":3355507967,"overlayBGColor":3355443200,"overlayTextColor":4278255600,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":4.9,"overlayText":" <<< OUT >>> ","refActorRequireBuff":true,"refActorBuffId":[3424,3495,3496,3497],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"refActorComparisonType":1,"onlyVisible":true},{"Name":"Induced AOE / 靠近AOE","type":1,"radius":3.0,"color":4278255612,"overlayBGColor":3472883712,"overlayTextColor":4278255615,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":3.0,"overlayText":" <<< SPREAD >>> ","refActorComparisonType":7,"includeRotation":true,"FaceMe":true,"refActorVFXPath":"vfx/lockon/eff/lockon5_t0h.avfx","refActorVFXMax":3000}]}
```

[International] In line debuff numbers
```
~Lv2~{"Name":"◆サークルプログラム","Group":"◆絶オメガ","ZoneLockH":[1122],"ElementsL":[{"Name":"1st","type":1,"offZ":2.76,"radius":0.0,"color":4294965504,"overlayBGColor":4294965504,"overlayTextColor":3355443200,"thicc":5.0,"overlayText":"1st","refActorRequireBuff":true,"refActorBuffId":[3004],"refActorComparisonType":1,"onlyVisible":true},{"Name":"2nd","type":1,"offZ":2.76,"radius":0.0,"color":3364749567,"overlayBGColor":3364749567,"thicc":5.0,"overlayText":"2nd","refActorRequireBuff":true,"refActorBuffId":[3005],"refActorComparisonType":1,"onlyVisible":true},{"Name":"3rd","type":1,"offZ":2.76,"radius":0.0,"color":3372156928,"overlayBGColor":3372156928,"thicc":5.0,"overlayText":"3rd","refActorRequireBuff":true,"refActorBuffId":[3006],"refActorComparisonType":1,"onlyVisible":true},{"Name":"4th","type":1,"offZ":2.76,"radius":0.0,"color":3359113471,"overlayBGColor":3359113471,"thicc":5.0,"overlayText":"4th","refActorRequireBuff":true,"refActorBuffId":[3451],"refActorComparisonType":1,"onlyVisible":true}]}
```
