## Trigger Types

> [!caution]
> This page assumes you have read all of the content within 
[Element Types](https://github.com/HairyTofu/Splatoon/blob/17ea0e48e4666727f8fa120c2cbf8ac4a27e12b2/Presets/ELEMENT%20TYPES.md) and 
[Editing Elements](https://github.com/HairyTofu/Splatoon/blob/3e4788a53d9193406244839c1c2d6dccb336cb44/Presets/EDITING%20ELEMENTS.md) as adding triggers to elements are more complex than simply drawing them.

### Trigger Settings
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

>[!IMPORTANT]
>The triggers listed below are advanced triggers and results can vary. You should experiment with these and understand them fully before using them in your presets.

- **Distance Limit**
  - This option can allow you to draw an element when you are close to it. For example, an element could be triggered when you are within a certain distance to remind you that you need to SPREAD and are too close to your partner.
![distancetrig](/docs/images/walkthrough/distancetrigger.gif)

- **Freeze**
  - This option allows you to trigger a draw and then redraw that draw in the same location every X seconds. For example, where AoEs are usually not drawn for you and alternate in a pattern, this trigger can allow you to draw the pattern each time it changes.
![freezetrig](/docs/images/walkthrough/freezetrigger.gif)

- **Enable Trigger**
  - This option allows you to trigger a draw when a certain condition is met. For example, when the boss casts a certain skil, when a debuff applies on you or when a certain map object appears. They can be used one after the other to draw different elements too.
![triggerexample](/docs/images/walkthrough/triggerexample.gif)
