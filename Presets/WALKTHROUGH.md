# Walkthrough
> [!caution]
>This walkthrough assumes you have read all of the information located in the `Reading Requirements` section below.

<details>
<summary>Reading Requirements</summary>

[Information](/Presets/INFORMATION.md)

</details>

<details>
<summary>Programme Requirements</summary>
  
[Dalamud](https://github.com/goatcorp/Dalamud)

[Splatoon](https://github.com/PunishXIV/Splatoon)

[A Realm Recorded](https://github.com/UnknownX7/ARealmRecorded) with a recording of different bosses or dungeons you want to use. 
 
[ACT](https://advancedcombattracker.com/download.php) and [Trigevent](https://triggevent.io/) with a log of the recording from A Realm Recorded. 

</details>

> [!important]
> Each section of the walkthrough is split up into different parts so you can focus on which element you want to draw. You can access each part of the walkthrough by looking at the heading and pressing the arrow underneath. Each part of the walkthrough uses different fights to give examples because not every fight will need each 

## Creating your preset

<details>
  
<summary>Walkthrough</summary></summary>
<ul>
  <li>
    
  Step 1: Create a layout and call it EX - The Bowl Of Embers.
  ![layoutcreation](/docs/images/walkthrough/createlayout.png)
  
  </li>
  
  <li>
    
  Step 2: Add an element and name it something that will enable you to identify which element it is later on.
  ![elementcreation](/docs/images/walkthrough/elementcreate.png)
  
  </li>
</ul>

</details>

## Using Splatoon Logs

<details>
  
<summary>Walkthrough</summary>

The `logger`, `explorer` and `log` functions of Splatoon will become your best friends when creating your own presets. They can be found under the `Tools` section of Splatoon's settings menu.

![tools](/docs/images/walkthrough/toolex.png)

## Logger

![loggerfunction](/docs/images/walkthrough/loggerex.png)

Logger shows you all of the NPCs, objects and data within your current instance. Ticking the `viewer mode` ensures that only current npcs,objects and data are shown. This particular option is important when dealing with invisible NPCs that cast spells. Pressing the `find` button next to the Object ID allows you to figure out what NPC/Object is doing an action and to draw the element you want from the correct NPC. You will be surprised to see many different npcs that have the same name as the boss but are actually invisible. This is because they are `helpers` and are typically responsible for any aoe you see that is not coming from the boss themselves. 

## Explorer

![explorerex](/docs/images/walkthrough/explorerex.png)

Explorer enables you to pick any of the NPCs, Objects or Items viewable in the `viewer mode` of `logger` and get additional information on it. This typically includes information that is already available in the `logger` function but at a more indepth level and specifically for the object you select. This includes the `position` of an object, which could help you draw your elements on the map or `rotation` which might help you figure out what angle your elements need to be drawn. `Rotation` can specifically be useful when determining how much of an angle you need to give to your element when drawing AOEs that do not come directly out from the boss or the boss is constantly turning/moving.

## Logs

![logs](/docs/images/walkthrough/splatoonlogex.png)

Logs provide you with all the information you need to create your draws. They list every single event that occurs during an instance and can provide invaluable information such as spell IDs, enemy position and the time between two events. For example, where a boss casts a spell with a length of 8 seconds but there are multiple events that happen within that time, the log can help you unpick what is happening and at what time. For bosses that use multiple `hidden helpers`, it can also help you determine which `hidden helper` is casting first and where they are on the map. It cannot be understated how useful the `log` feature in Splatoon is.

</details>

## Creating a line AOE element

<details>
  
<summary>Walkthrough</summary></summary>

This section will teach you how to create a cone element. For this particular section, we will be using the skill 'Crimson Cyclone'. 

![lines](/docs/images/walkthrough/ifritlines.gif)


<ul>
  <li>
    
Step 1: Set your element type to 'line relative to object'. This will make the line attach to an object, rather than a set of points on the map.
![coneoption](/docs/images/walkthrough/lineobject.png)</li>

<li>
  
Step2: Find the NPC ID    
 - We need to find the NPC ID to enable splatoon to know which NPC the skill is going to be cast from. In this case, the NPC is Ifrit. Splatoon enables you to grab the NPC ID by targetting the NPC and clicking the target button once you have set the Single attribute to NPC. This shows us that Ifrit has an NPC ID of 0x4A1.
![target](/docs/images/walkthrough/targetoption.png)</li>

<li>
  
Step 3: While casting and Skill ID
 - Using the Splatoon `log` feature described previously, we can see that Ifrit readies the spell Crimson Cyclone and afterwards, a skill of 1532 is being cast by ifrit. We can assume that this skill ID 1532 relates to Crimson Cyclone.  For some fights, Splatoon already gives us the ID of the skill when we type it in. By ticking the box next to 'While casting', we are telling Splatoon that we want this element to draw when Ifrit is casting this skill.</li>
![whilecasting](/docs/images/walkthrough/whilecasting.png)

<li>

Step 4: Setting the width and length of the element
 - We know that Ifrit charges across the battle field so the radius of this must be the length of the map. If you play around with the Y axis co-ordinates, you will see that the end of the map is around the 44 mark. We can keep this at 44 so that the drawn is the entire length of the arena. We then need to set the radius of the skill. We know that the AOE line includes the body of Ifrit so we can assume that it is as wide as him. If you tick the `+targethitbox` option, you should notice that your element is now as wide as him. Sometimes this works and sometimes it is not based on the target hitbox and you will have to experiment on the radius yourself. In this case, the radius is approximately 6.
 - We want to make sure that we have accounted for rotation by ticking the "account for rotation" box, which can be found under the element type box.

![account for rotation](/docs/images/walkthrough/accountrotate.png)

 - We then want to make Point A have Y:44 (the length) and , if not using the `+targethitbox` option, set the radius to 6.
![yandradius](/docs/images/walkthrough/yandradius.png)

</li>


If you did everything correctly, your Crimson Cyclone element should draw correctly when Ifrit begins to cast, giving you time to find the safe spots.

![ifritlinedraw](/docs/images/walkthrough/ifritlinecomplete.gif)

</details>

## Improving a line AOE element using triggers.

<details>
  
<summary>Walkthrough</summary></summary>

This section will teach you how to expand upon the created line AOE using triggers. 

![ifritlinedraw](/docs/images/walkthrough/ifritlinecomplete.gif)

A big issue with the line AOE created above is that it requires Ifrit to be casting to display. In a mechanic where there are several NPCS that all cast the same spell ID and you are required to find multiple safe spots, it can be tricky. To this end, a trigger can be used to effectively draw ALL of the Ifrit line AOEs at the same time. This can create scenarios where there are evident safe spots within the mechanic that are not usually seen when solving them naturally.

<ul>
  <li>
    
  Step 1: Press the layout name you made earlier and press the group menu at the top of the page.
  
  ![groupname](/docs/images/walkthrough/groupname.png)
  
  </li>
  
  <li>
    
  Step 2: Scroll down to the bottom and type the name you want your grop of elements to be called and press "add".
  
  ![creategroup](/docs/images/walkthrough/creategroup.png)
  
  </li>

  <li>
    
  Step 3: The layout should now be under the group you created. From now on, when creating new layouts, you can assign them to this group so they appear under the heading. This is useful when creating more advanced elements, where some need triggers and some do not.
  
  ![grouped](/docs/images/walkthrough/grouped.png)
  
  </li>

  <li>
    
  Step 4: Change the display condition to "on trigger only" and down the bottom of the page, tick the "Enable Trigger" button. Make sure you change the option to "Show at log message" and put the log message to 1532 - the skill ID for crimson Cyclone.
  
  ![grouped](/docs/images/walkthrough/enabletrigger.png)
  
  </li>

   <li>
    
  Step 5: Now make sure you untick "While casting" and tick "Visible characters only" in the element options as we are now using a trigger rather than a cast to draw these elements.

  </li>
</ul>

If you did all the steps correctly, you should now notice that your elements draw on all of the Ifrits the moment the first begins their cast. This means you can see the safe spots instantly, rather than running around the arena dodging each ifrit!

![ifritlinedraw](/docs/images/walkthrough/infrittrigger.gif)

</details>

## Creating an element using helpers.

<details>

<summary>Walkthrough</summary>

In some cases, the boss will use `hidden actors` to cast spells for them. This is typical in fights where AOEs appear as if they are coming from outside of the boss. In some fights there can be dozens of `hidden actors` that are casting these skills. The `logger` is particularly useful here to determine which of the NPCs is casting the spell so you can retrieve the spell ID. Sometimes, the same NPC ID might be casting different spells as there could be more than one `hidden actor` active at that time. 

<ul>
  <li>
    
  Step 1: Press the layout name you made earlier and press the group menu at the top of the page.
  
  ![groupname](/docs/images/walkthrough/groupname.png)
  
  </li>
  
  <li>
    
  Step 2: Scroll down to the bottom and type the name you want your grop of elements to be called and press "add".
  
  ![creategroup](/docs/images/walkthrough/creategroup.png)
  
  </li>

  <li>
    
  Step 3: The layout should now be under the group you created. From now on, when creating new layouts, you can assign them to this group so they appear under the heading. This is useful when creating more advanced elements, where some need triggers and some do not.
  
  ![grouped](/docs/images/walkthrough/grouped.png)
  
  </li>

  <li>
    
  Step 4: Change the display condition to "on trigger only" and down the bottom of the page, tick the "Enable Trigger" button. Make sure you change the option to "Show at log message" and put the log message to 1532 - the skill ID for crimson Cyclone.
  
  ![grouped](/docs/images/walkthrough/enabletrigger.png)
  
  </li>

   <li>
    
  Step 5: Now make sure you untick "While casting" and tick "Visible characters only" in the element options as we are now using a trigger rather than a cast to draw these elements.

  </li>
</ul>

</details>

## Creating an element using a tether condition.

<details>

<summary>Walkthrough</summary>
  
</details>

## Creating an element using a status condition.

<details>

<summary>Walkthrough</summary>
  
</details>

## Creating an donut element.

<details>

<summary>Walkthrough</summary>
  
</details>
