[International] Ashing blaze left/right
```
~Lv2~{"Name":"P8S2 Ashing blaze","Group":"P8S-2","ZoneLockH":[1088],"ElementsL":[{"Name":"Right","type":3,"refX":-10.0,"refY":45.0,"offX":-10.0,"radius":10.0,"color":1677721855,"refActorNPCNameID":11402,"refActorRequireCast":true,"refActorCastId":[31192],"refActorUseCastTime":true,"refActorCastTimeMax":7.0,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true},{"Name":"Left","type":3,"refX":10.0,"refY":45.0,"offX":10.0,"radius":10.0,"color":1677721855,"refActorNPCNameID":11402,"refActorRequireCast":true,"refActorCastId":[31191],"refActorUseCastTime":true,"refActorCastTimeMax":7.0,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true}],"Phase":2}
```

[International] End of Days line AOE
```
~Lv2~{"Name":"P8S2 End of Days","Group":"P8S-2","ZoneLockH":[1088],"ElementsL":[{"Name":"","type":3,"refY":40.0,"radius":5.0,"color":1677787131,"refActorNPCNameID":11406,"refActorRequireCast":true,"refActorCastId":[31371],"refActorCastTimeMax":7.0,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true}],"Phase":2}
```

[Script] Limitless Desolation tower highlight. Assumes that tanks and healers take left, and dps - right half.
```
https://github.com/NightmareXIV/Splatoon/raw/master/SplatoonScripts/Duties/Endwalker/P8S2%20Limitless%20Desolation.cs
```

[International] Early see Dominion towers (mechanic after high concept 2)
```
~Lv2~{"Name":"P8S2 Dominion Towers","Group":"P8S-2","ZoneLockH":[1088],"ElementsL":[{"Name":"","type":1,"radius":3.0,"color":4278190335,"thicc":5.0,"refActorNPCNameID":11402,"refActorRequireCast":true,"refActorCastId":[31196],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":7.0,"refActorComparisonType":6},{"Name":"","type":1,"radius":3.0,"color":4278255611,"thicc":5.0,"refActorNPCNameID":11402,"refActorRequireCast":true,"refActorCastId":[31196],"refActorUseCastTime":true,"refActorCastTimeMax":3.0,"refActorComparisonType":6}],"Phase":2}
```

[Script] Highlight your dominion tower. **Requires configuration.**
```
https://github.com/NightmareXIV/Splatoon/raw/master/SplatoonScripts/Duties/Endwalker/P8S2%20Dominion.cs
```

# Untested section
Natural Alignment reverse overhead marker
```
~Lv2~{"Name":"煉獄編零式4層【マジックインヴァージョン】","Group":"煉獄編零式4層","ZoneLockH":[1088],"DCond":1,"ElementsL":[{"Name":"01","type":3,"refZ":3.0,"offX":-0.7,"offZ":3.7,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"FaceMe":true},{"Name":"02","type":3,"refX":-0.7,"refZ":3.7,"offZ":4.4,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"FaceMe":true},{"Name":"03","type":3,"refZ":3.0,"offX":0.7,"offZ":3.7,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"FaceMe":true},{"Name":"04","type":3,"refX":0.7,"refZ":3.7,"offZ":4.4,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"FaceMe":true},{"Name":"05","type":3,"refZ":4.4,"offZ":3.0,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"FaceMe":true},{"Name":"06","type":3,"refX":-0.7,"refZ":3.7,"offX":0.7,"offZ":3.7,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"FaceMe":true},{"Name":"01-02","type":3,"refZ":3.0,"offX":-0.7,"offZ":3.7,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":1.5707964,"FaceMe":true},{"Name":"02-02","type":3,"refX":-0.7,"refZ":3.7,"offZ":4.4,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":1.5707964,"FaceMe":true},{"Name":"03-02","type":3,"refZ":3.0,"offX":0.7,"offZ":3.7,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":1.5707964,"FaceMe":true},{"Name":"04-02","type":3,"refX":0.7,"refZ":3.7,"offZ":4.4,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":1.5707964,"FaceMe":true},{"Name":"05-02","type":3,"refZ":4.4,"offZ":3.0,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":1.5707964,"FaceMe":true},{"Name":"06-02","type":3,"refX":-0.7,"refZ":3.7,"offX":0.7,"offZ":3.7,"radius":0.0,"thicc":5.0,"refActorRequireBuff":true,"refActorBuffId":[3349],"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":1.5707964,"FaceMe":true}]}
```
