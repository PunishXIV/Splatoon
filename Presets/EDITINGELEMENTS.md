# Editing Elements
> [!caution]
> This page assumes you have read all of the content within 
[Element Types](/Presets/ELEMENTTYPES.md).

Once you have selected a draw type that you want, the next step is to edit the draw so it is more effective for what you want it to do.

>[!note]
>Depending on the element you have chosen, different editing options become available.

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
   - This will enable you to write on the element and can be drawn on any element to provide information.     This can be useful for giving you instructions mid fight, such as "STACK", "AVOID" or "MOVE HERE".
  ![safespot](/docs/images/walkthrough/ssexample.png)
