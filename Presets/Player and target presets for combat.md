Pixel perfect-like dot and optional circle around player
```
Player dot~{"ZoneLockH":[],"DCond":4,"Elements":{"AOE circle":{"type":1,"Enabled":false,"refX":-75.0829,"refY":19.932276,"refZ":18.000315,"radius":5.0,"refActorType":1,"includeOwnHitbox":true},"Player dot":{"type":1,"refX":-75.66832,"refY":22.850508,"refZ":18.05013,"radius":0.0,"refActorType":1}},"Triggers":[]}
```

Machinist/Bard AOE cone:
```
Machinist/Bard AoE cones~{"ZoneLockH":[],"Elements":{"Machinist/Bard AOE cone(2)":{"type":3,"refY":12.0,"radius":0.0,"refActorType":1,"includeRotation":true,"AdditionalRotation":0.7853982},"Machinist/Bard AOE cone":{"type":3,"refY":12.0,"radius":0.0,"refActorType":1,"includeRotation":true,"AdditionalRotation":5.497787}},"JobLock":2155872256,"Triggers":[]}
```

20yalm ring around target for melee gap closers
```
20y Ring~{"ZoneLockH":[],"DCond":4,"Elements":{"20y Ring":{"type":1,"refX":-588.177,"refY":-840.44836,"refZ":30.110699,"radius":20.0,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true}},"JobLock":158916411392,"Triggers":[]}
```

3yalm ring around target for GCD range and a 2yalm ring around the target for auto attack range (auto attacks are a large portion of your damage, don't lose them)
```
Melee Range~{"ZoneLockH":[],"Elements":{"Auto Attack":{"type":1,"refX":-584.84045,"refY":-842.6976,"refZ":30.110699,"radius":2.1,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"GCD Range":{"type":1,"refX":-584.7163,"refY":-842.6814,"refZ":30.110699,"radius":3.0,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"pixel perfect":{"type":1,"Enabled":false,"refX":-594.2895,"refY":-836.6198,"refZ":30.07,"radius":0.0,"refActorType":1}},"Triggers":[]}
```

Another max melee and positionals ring
```
Melee Range~{"ZoneLockH":[],"Elements":{"Auto Attack":{"type":1,"refX":-584.84045,"refY":-842.6976,"refZ":30.110699,"radius":2.1,"color":4282139590,"overlayBGColor":0,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"GCD Range":{"type":1,"refX":-584.7163,"refY":-842.6814,"refZ":30.110699,"radius":3.0,"color":4282139590,"refActorName":"*","refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"pixel perfect":{"type":1,"Enabled":false,"refX":-594.2895,"refY":-836.6198,"refZ":30.07,"radius":0.0,"refActorType":1},"Flank Line Right 2":{"type":3,"refY":2.1,"offY":3.0,"radius":0.0,"color":4282139590,"refActorType":2,"includeRotation":true,"AdditionalRotation":5.497787,"LineAddHitboxLengthY":true,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthY":true,"LineAddPlayerHitboxLengthYA":true},"Flank Line Right 1":{"type":3,"refY":2.1,"offY":3.0,"radius":0.0,"color":4282139590,"refActorType":2,"includeRotation":true,"AdditionalRotation":3.9287362,"LineAddHitboxLengthY":true,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthY":true,"LineAddPlayerHitboxLengthYA":true},"Flank Line Left 1":{"type":3,"refY":2.1,"offY":3.0,"radius":0.0,"color":4282139590,"refActorName":"*","refActorType":2,"includeRotation":true,"AdditionalRotation":0.7853982,"LineAddHitboxLengthY":true,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthY":true,"LineAddPlayerHitboxLengthYA":true},"Flank Line Left 2":{"type":3,"refY":2.1,"offY":3.0,"radius":0.0,"color":4282139590,"refActorType":2,"includeRotation":true,"AdditionalRotation":2.3561945,"LineAddHitboxLengthY":true,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthY":true,"LineAddPlayerHitboxLengthYA":true}},"Triggers":[]}
```

Cardinal positions relative to player
```
Cardinals~{"Elements":{"N":{"type":1,"offY":-0.5,"overlayTextColor":3355443455,"thicc":0.0,"overlayText":"N","refActorType":1},"S":{"type":1,"offY":0.5,"overlayTextColor":3372206336,"thicc":0.0,"overlayText":"S","refActorType":1},"W":{"type":1,"offX":-0.5,"overlayTextColor":3371697391,"thicc":0.0,"overlayText":"W","refActorType":1},"E":{"type":1,"offX":0.5,"overlayTextColor":3355769060,"thicc":0.0,"overlayText":"E","refActorType":1}}}
```

Attack Range Rings (Universal)
```
Attack Range Rings~{"Elements":{"Melee Action Range":{"type":1,"radius":3.0,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"Melee Auto-Attack Range":{"type":1,"Enabled":false,"radius":3.1,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"Enemy Auto-Attack Range":{"type":1,"Enabled":false,"radius":2.1,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"Pixel Perfect":{"type":1,"radius":0.0,"color":4227858431,"refActorType":1},"Positional Line 1":{"type":3,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":0.7853982,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true},"Positional Line 2":{"type":3,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":2.3561945,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true},"Positional Line 3":{"type":3,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":3.9269907,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true},"Positional Line 4":{"type":3,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":5.497787,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true},"6y Ring":{"type":1,"Enabled":false,"radius":6.0,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"20y Ring":{"type":1,"Enabled":false,"radius":20.0,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"25y Ring":{"type":1,"Enabled":false,"radius":25.0,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true}}}
```
Attack Range Rings (Melee Jobs Only)
```
Attack Range Rings (Melee Jobs Only)~{"Elements":{"Melee Action Range":{"type":1,"Enabled":false,"radius":3.0,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"Melee Auto-Attack Range":{"type":1,"Enabled":false,"radius":3.1,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"Enemy Auto-Attack Range":{"type":1,"radius":2.1,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"20y Ring":{"type":1,"Enabled":false,"radius":20.0,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"Positional Line 1":{"type":3,"Enabled":false,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":0.7853982,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true},"Positional Line 2":{"type":3,"Enabled":false,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":2.3561945,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true},"Positional Line 3":{"type":3,"Enabled":false,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":3.9269907,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true},"Positional Line 4":{"type":3,"Enabled":false,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":5.497787,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true}},"JobLock":710288080926}
```
Attack Range Ring (Ranged Jobs Only)
```
Attack Range Rings (Ranged Jobs Only)~{"Elements":{"25y Ring":{"type":1,"radius":25.0,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true}},"JobLock":1488734650592}
```
6 Yalm Attack Range Ring (Samurai/Sage Only)
```
Attack Range Rings (SAM/SGE Only)~{"Elements":{"6y Ring":{"type":1,"radius":6.0,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true}},"JobLock":1116691496960}
```
Attack Range Rings (Positional Jobs Only)
```
Attack Range Rings (Positional Jobs Only)~{"Elements":{"Melee Action Range":{"type":1,"radius":3.0,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"Melee Auto-Attack Range":{"type":1,"Enabled":false,"radius":3.1,"color":4294967295,"refActorType":2,"includeHitbox":true,"includeOwnHitbox":true},"Positional Line 1":{"type":3,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":0.7853982,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true},"Positional Line 2":{"type":3,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":2.3561945,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true},"Positional Line 3":{"type":3,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":3.9269907,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true},"Positional Line 4":{"type":3,"refY":3.0,"radius":0.0,"color":4294967295,"refActorType":2,"includeRotation":true,"AdditionalRotation":5.497787,"LineAddHitboxLengthYA":true,"LineAddPlayerHitboxLengthYA":true}},"JobLock":551371669524}
```

DNC's standard and technical step radius while dancing:
```
~Lv2~{"Name":"DNC step circles around self","Group":"","ElementsL":[{"Name":"","type":1,"radius":15.0,"refActorRequireBuff":true,"refActorBuffId":[1818,1819],"refActorType":1}],"JobLock":274877906944}
```
