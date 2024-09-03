> [!important]
>This page combines all current information on elements and their triggers. It should be used as an information bank for when you are creating your own presets to be used with Splatoon.
>Each section of the document is split up into different parts so you can focus on reading information you want to know about. You can click the arrow underneath each section labelled `information`.

## Element Types

<details>
  
<summary>Information</summary>

This area lists the common types of draws that you can find within Splatoon. Before creating your own presets, it is important you understand each type of draw and what it can do.

## Circles at fixed co-ordinates.

Circle draws can mark areas of the map with a circular shape. They can be used in a variety of ways such as marking an area to stand that you know is safe or reminding you where a tower is going to spawn that you need to take. They can be permanent draws, which stay on the map for the entire fight or they can be triggered by a specific mechanic in a fight. Support on understanding triggers and how to use them can be found [here](/Presets/TRIGGERTYPES.md).

![circlefixed](/docs/images/walkthrough/ccfixed.jpg)

## Circles relative to an object

Circle draws can also be used so that they are relative to an object or NPC. This means that, as the NPC moves, the circle draw can move with them. This is particularly useful in fights that are boss relative, where you are having difficulty finding your position and need a quick reminder. They can also rotate with the NPC/Object, so they are always at a fixed position relative to the NPC/Object you need.

![circlerelative](/docs/images/walkthrough/ccmovement.gif)

## Lines at fixed co-ordinates

Line draws can start at one part of the map and end at another. These can be used on mechanics where the line could tell you where you need to go to be safe for the next mechanic. It is possible to have an arrow at the end of the line to make this even more obvious.

![linefixed](/docs/images/walkthrough/lfixed.jpg)
![linearrow](/docs/images/walkthrough/larrow.jpg)

## Lines relative to an object

Line draws can also be used so that they are relative to an object or NPC. Much like the circles, they move as the NPC moves and can also be configured to rotate so they are always at a fixed position relative to the NPC/Object you need.

![Line Movement](/docs/images/walkthrough/lmovement.gif)

## Cone relative to an object

Cone draws can be extremely useful to show you how far an NPC attack reaches or how wide it is. These are particularly useful if the NPC attack is not usually telegraphed.

![conerelative](/docs/images/walkthrough/ccone.png)

</details>

## Editing Elements

<details>

<summary> Information </summary>

Once you have selected a draw type that you want, the next step is to edit the draw so it is more effective for what you want it to do.

Depending on the element you have chosen, different editing options become available.

![ccediting](/docs/images/walkthrough/ccediting.png)
![ccrelativeediting](/docs/images/walkthrough/ccrelativeediting.png)

- **Reference Position and Offsets**
   - These two options move the element along the X,Y and Z axis. These can be changed independently to enable you to precisely put the element where you want. The cursor icon allows you to place the element on the screen at the location of your cursor. This makes placing circular elements particularly easy.
- **Stroke**
   - This enables you to change the colour of the element. You might find it useful to colour the element green for a safespot, for example.
  
![stroke](/docs/images/walkthrough/ccolours.png)
- **Thickness**
   - This changes the thickness of the line surrounding the element. Thicker lines might be easier to see on different maps.
     
![thickness](/docs/images/walkthrough/cthick.png)
- **Fill**
   - This changes the amount of colour within the element. At higher fill levels, you may not be able to see the floor at all.
     
  ![fill](/docs/images/walkthrough/cfill.png)
- **Radius**
   - This changes the size of the element. A higher value makes the element bigger and a smaller value makes it smaller.
     
  ![radius](/docs/images/walkthrough/esize.png)
- **Account for rotation**
   - This option ensures that the element rotates fixed to the object if it changes its direction. For example, if you want an element to be pointing east from an NPC and the NPC turns, this option will keep the element east.
- **Targetted Object**
   - Game object with specific data is *usually* the option you want to use when drawing object relative elements. You can either type the name of the NPC, use an NPC ID number or target the NPC in game and press the "target" button.
- **Single attribute**
   - For basic element drawing, NPC IDs will perform most of what you are looking for. However, for draws that you want to be shown when the boss uses a certain skill or animation, other options such as "VFX Path" are available. For elements you want to show during mechanics with debuffs, Icon ID can work particularly well.
- **Overlay Text**
   - This will enable you to write on the element and can be drawn on any element to provide information.
  
  ![safespot](/docs/images/walkthrough/ssexample.png)

</details>

## Conditions

<details>

<summary> Information </summary>

This area gives additional information on some of the conditions you can set when creating your elements.

![Trigger Types](/docs/images/walkthrough/ttypes.png)

- **Display Conditions**
  - Always shown draws the element on the screen permanently.
  - Only in combat draws the element when you begin combat with an NPC.
  - Only in instance draws the element when you enter a particular instance.
  - On trigger only draws the element on the screen when certain conditions are met, such as a boss using a certain skill. This is particularly power as it enables elements to be drawn that are not permanent throughout the fight. This reduces on screen clutter and can be used in some cases to draw multiple things at once.
 
- **Zone whitelist and blacklist**
  - Zone whitelist makes the elements only appear in certain instances, such as a particular fight.
  - Zone blacklist ensures the elements do not appear in certain instances.

- **Job Lock**
  - Job lock enables you to make certain elements appear only when you are on a particular class. For example, where a boss fight might have different safe spots for healer and red mage, you can create a draw that shows the healer safe spots or red mage safe spots depending on what class you are on.

The triggers listed below are advanced triggers and results can vary. You should experiment with these and understand them fully before using them in your presets.

- **Distance Limit**
  - This option can allow you to draw an element when you are close to it. For example, an element could be triggered when you are within a certain distance to remind you that you need to SPREAD and are too close to your partner.
  
![distancetrig](/docs/images/walkthrough/distancetrigger.gif)

- **Freeze**
  - This option allows you to trigger a draw and then redraw that draw in the same location every X seconds. For example, where AoEs are usually not drawn for you and alternate in a pattern, this trigger can allow you to draw the pattern each time it changes.

![freezetrig](/docs/images/walkthrough/freezetrigger.gif)

- **Enable Trigger**
  - This option allows you to trigger a draw when a certain condition is met. For example, when the boss casts a certain skil, when a debuff applies on you or when a certain map object appears. They can be used one after the other to draw different elements too.
 
  ![triggerexample](/docs/images/walkthrough/triggerexample.gif)
  
</details>

## Relative Objects Additional Settings

<details>

<summary>Information</summary>

This information area details the different settings you can use when creating elements that are relative to an object. There are more settings available but these are the ones I have used personally. These settings do not apply when you are creating fixed elements!

![robjectsettings](/docs/images/walkthrough/robjectssettings.png)

- **Single Attribute**
  - Name (case-insensitive, partial) will allow you to search for the NPC you want your element to be relative to. This is their name shown in the game. Splatoon will offer you the chance to convert the name into their NPC ID, which is recommended when creating international presets.
  - Model ID will allow you to draw the element from a certain model that matches the ID you put in.
  - Object ID will allow you to draw the element from a certain object that matches the ID you put in. This has very rare use case scenarios as object ID tends to change on each instance.
  - Data ID will allow you to draw the element from a certain piece of data that matches the ID you put in.
  - NPC ID will allow you to draw the element from a certain NPC that matches the ID you put in. This is useful for NPCs that are invisible and cannot be targetted but exist to cast spells during fights.
  - VFX Path will allow you to draw the element using a VFX that occurs within a fight. This is useful for spells that do not have different IDs but are triggered when the boss activates a directional attack that uses different VFXs.
  - Icon ID will allow you to draw the element when an Icon appears, whether it be a debuff, status effect or positive effect you place on yourself or the boss.
 
- **Targetability**
   - Targetable only will mean the element will only be drawn when a targetable ID placed in the "single attribute" section has been spawned.
   - Untargetable only will mean the element will only be drawn when an untargetable ID placed in the "single attribute" section has been spawned.
   - Visible characters only will mean the element appears when a matching ID placed in the "single attribute" section has been spawned and is also visible to the player.
   - Unticking the `visible characters only` option is necessary when you want to draw elements from invisble NPCs.

- **While casting**
  - This option enables you to set the element to be drawn when the ID placed in `single attribute` has cast a certain spell. You can place the spell in the box by using its written name or the spell ID.
  - `Limit by cast time` becomes available when you tick `while casting`. This means that you can set the element to be drawn within a set amount of time. For example, if a spell has a 7 second cast time and you set it to 0 - 1, the element will be drawn as long as all other condiitions are met. This is particularly useful when you want to `freeze` an element within a certain time period.
 
- **Status Requirement**
  - This option enables you to set the element to be drawn when a character or NPC is affected by a certain status. For example, they have a poison stack or a certain status debuff that they need to solve a mechanic.

- **Distance limit**
  - This option enables you to set the element to be drawn when you are within a certain distance away from the `single attribute`. This is useful if you want something to be draw when you enter a tower or a knockback area.

- **Rotation Limit**
  - This option sets a minimum and maximum limit on the amount an element can move when the boss moves. This prevents the draw from being incorrect if the boss turns a certain amount.

- **Object life time**
  - This option links the element drawn to the lifetime of an object. For example, if the object lasts 7 seconds and creates a knockback, the element can be drawn for 7 seconds and then be removed.

- **Tether info**
  - This option links the element to the creation or expiration of a tether. This is useful in mechanics where the boss tethers an NPC to delay an AOE being cast or where you are tethered to an NPC and must stay within a certain range.
 
- **Offset**
  - These co-ordinates can be changed so the element is drawn away from the ID in `Single attribute`. This is useful for invisible actors where the spell ID may originate at the boss location but the safe area is actually a certain distance away.

- **Radius**
  - This option changes how much space the element you are drawing covers. It can be useful when trying to account for the bosses hitbox.
  - `+targethitbox` is useful when the spell hit box is only as wide as the boss itself. This means you don't need to fiddle with radius values.
  - `Donut` changes the way radius works by drawing the element outside of the radius rather than inside it. This is useful where danger areas might be outside of a specific spot and you want to show people the radius of that danger area.
  - `line end style` only becomes applicable if you have set the radius of the element to 0. It enables you to change the end of the element into a shape, such as an arrow. This is useful if you need to tell somebody where to go.

- **Overlay text**
  - This option draws text on your element and can be useful if you want to highlight a `safespot` or the fact that someone has `1 stack` of a particular debuff.  
  




  
</details>
