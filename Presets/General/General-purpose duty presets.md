Marker Placement Helper: Puts lines through the cardinals and intercardinals, a ring around the targeted enemy and a ring in the center of arena to help with placing markers.
Note: Works perfectly in DSR and some other Savage instances.
```
Markers~{"DCond":2,"Elements":{"N/S":{"type":2,"refX":100.0,"refY":200.0,"refZ":-3.8146973E-06,"offX":100.0,"radius":0.0,"color":4244635903,"thicc":5.0},"E/W":{"type":2,"refX":200.0,"refY":100.0,"refZ":-3.8146973E-06,"offY":100.0,"radius":0.0,"color":4261413119,"thicc":5.0},"NE/SW":{"type":2,"refX":200.0,"refZ":-3.8146973E-06,"offY":200.0,"radius":0.0,"color":4278190335,"thicc":5.0},"NW/SE":{"type":2,"refZ":-3.8146973E-06,"offX":200.0,"offY":200.0,"radius":0.0,"color":4278190335,"thicc":5.0},"Center":{"refX":100.0,"refY":100.0,"radius":10.0,"thicc":5.0}},"UseTriggers":true,"Triggers":[{"Type":1}]}
```

Alternative Marker Placement Helper: Places lines and circle around targeted enemy instead of using coordinates. For use with arenas with weird coordinates.
```
Target Lines~{"DCond":2,"Elements":{"E-W":{"type":3,"refX":100.0,"offX":-100.0,"radius":0.0,"thicc":5.0,"refActorType":2},"N-S":{"type":3,"refY":100.0,"offY":-100.0,"radius":0.0,"thicc":5.0,"refActorType":2},"NE/SW":{"type":3,"refX":100.0,"refY":-100.0,"offX":-100.0,"offY":100.0,"radius":0.0,"thicc":5.0,"refActorType":2},"NW-SE":{"type":3,"refX":100.0,"refY":100.0,"offX":-100.0,"offY":-100.0,"radius":0.0,"thicc":5.0,"refActorType":2},"Ring":{"type":1,"radius":2.0,"thicc":5.0,"refActorType":2,"includeHitbox":true}},"UseTriggers":true,"Triggers":[{"Type":1}]}
```
