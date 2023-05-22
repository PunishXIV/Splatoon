# Object table
Object table is a list of objects that are dynamically changed in game and usually can be interacted in some form. Included are players, NPCs, pets, mounts, treasure coffers, aetherytes, entrances, and many more. Housing objects are not included into this table.
# Object types
* GameObject. It's a base for all objects in object table. Every object in object table is GameObject.
* Character. Is a GameObject that additionally can have hp, mp, class, level and some other properties. 
* BattleChara. It's a Character that can have extra properties like status effects and can cast spells. All players, enemies and friendly NPCs that can fight are BattleChara.
  * BattleNPC. Non-player BattleChara.
  * PlayerCharacter. Player BattleChara.
* Companion. Is a Character. 
