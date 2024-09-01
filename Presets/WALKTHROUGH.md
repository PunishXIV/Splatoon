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

This section will teach you how to create a line element. For this particular section, we will be using the skill 'Crimson Cyclone'. 

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

## Creating a circle AOE element

<details>
  
<summary>Walkthrough</summary></summary>

Occasionally, a simple circle AOE element can be used to show the unsafe areas of a skill. A good example of this is in `The Strayborough Deadwalk` first boss, where heads spawn above you and drop down to deal damage and a status effect.

![circlestray](/docs/images/walkthrough/circlestray.png)

For this, we need to look in our `log` to see what happens when one of these heads appear.

![circlestray2](/docs/images/walkthrough/circlelog.png)

As shown, an NPC appears in our `log` called `The noble noggin`. We can guess this is the head because there are four players and each player receives one of these heads above them. 
We can also guess that the skill that is responsible for the heads falling and causing the damage is `36532`, as this also occurs 4 times.

In my `logger` under `Tools` within Splatoon, I can see that each `The noble noggin` has a DATA ID of `4205`. I can put this in my `single attribute` box and set it to show on `Visible characters only`.
I can either set the radius to `+target hitbox` or a number value. Since its circular and the AOE effect may extend past the target, i'm going to tick `+target hitbox` and also add a radius of 1. 
Your element settings should look identical to these ones and the circle will draw each time the heads appear. Since these heads only appear to deliver the AOE and not for any other mechanic, we do not need to set a trigger. We can simply show the AOE each time the DATA ID appears and goes away.

![circlestray3](/docs/images/walkthrough/straysettings.png)




</details>


## Creating an element using helpers.

<details>

<summary>Walkthrough</summary>

In some cases, the boss will use `hidden actors` to cast spells for them. This is typical in fights where AOEs appear as if they are coming from outside of the boss. In some fights there can be dozens of `hidden actors` that are casting these skills. The `logger` is particularly useful here to determine which of the NPCs is casting the spell so you can retrieve the spell ID. Sometimes, the same NPC ID might be casting different spells as there could be more than one `hidden actor` active at that time. 

![firepeaks](/docs/images/walkthrough/firepeaks.png)

As you can see above, there are four AOEs that have been cast around an object. However, my log is telling me that the Boss NPC, `Gurfurlur`, has cast four different spells at the same time as the AOEs appearing.

![gurfurlurlog](/docs/images/walkthrough/gurgurlur36303.png)

In game, Gurfurlur did nothing but cast the initial spell, one time. Even more bizarre, my `logger` with `viewer mode` on is showing lots of different NPCs that have the same name.

![gurfurnpc](/docs/images/walkthrough/gurfurlurlog.png)

If you look closely, you can see all of them share the same Data ID `0x233c` except one, which has the Data ID `0x415F`. If you press the `Find` option next to the Object ID of the 0x415F, you can see that it points to the boss.

![findgurfur](/docs/images/walkthrough/gurfurlurfind.png)

If you press `Find` on one of the Data ID `0x233c` NPCs, you can see that it points right under the AOE marker. Data ID `0x233c` is what is known as an invisible actor, or `helper`. They are invisible for the whole fight but they have the same NPC Name. They are responsible for the majority of the AOEs you see in fights, particularly ones that do not come from the boss itself. In this case, we want to use the Data ID in the `single attribute` box for our element settings.

You will also notice in the log picture above that the NPCs are each casting a different spell. That is because for this mechanic, they each cast a spell that looks identicle but last a certain amount of time each. This creates a safe spot after the first explosion that you need to get into. 
After ticking `while casting` and putting in the spell IDs `36303, 36304, 36305` one at a time, I found that the first four NPCs all cast `36303`. This enables me to draw a unsafe element on those NPCs only and not the other NPCs who are casting the delayed mechanic.

![firepeaks3](/docs/images/walkthrough/firepeaksdraw.png)

![firepeaksgif](/docs/images/walkthrough/peaks.gif)

</details>

## Creating an element using a VFX condition.

<details>

<summary>Walkthrough</summary>

A VFX condition is useful in a few scenarios. The scenario described in this section refers to a boss who summons four NPCs, all of which have the same NPC ID and spell ID. Usually, a situation like this makes it difficult to draw elements for as if we just follow the steps in `Creating an element using helpers` and `Improving a line AOE element using triggers`, Splatoon will draw an element over all of the AOEs at the same time, which is not what we want. 

![vftogether](/docs/images/walkthrough/vfxexample1.png)

To make element draws, it is important to know the mechanic you are trying to draw for. In this case, there are four AOE markers being painted onto the floor by four NPCs. A few seconds after this, the boss will cast a tether on two of the minions, encasing them in ice and delaying their AOE. This means that two untethered NPCs are the safe zone, atleast for a few seconds. 

![tetherexample](/docs/images/walkthrough/tetherexample.png)

Looking at the `log` feature, I can see that there was a tether created at the exact moment it appeared in the game. However, the tether is not linked to a spell. The only other log entry that appears is a sudden VFX, which occurs twice. I don't think its a coincidence that two VFX elements are created at the same time as two tethers being shown, do you?

![tetherlog](/docs/images/walkthrough/tethercreate.png)

So, instead of an `NPC ID` being used in the `single attribute` box, we are going to choose `VFX Path` and copy the useful part of the log into this box. That would be `vfx/channeling/eff/chn_m0320_ice_0c2.avfx`. You'll notice `age` and two boxes next to where you put `vfx/channeling/eff/chn_m0320_ice_0c2.avfx`. You want to include the total time the tether exists ingame, which is from 0 seconds (when it appears) to 11 seconds (when it vanishes). I also want to make sure that the NPC is casting the current spell, which is Ice Scream as shown in the `log`. This has an ID of 36270 so I make sure I tick `while casting` and put in the correct spell ID.

I then make sure I tick `account for rotation` and that my drawn element is covering the side of the untethered minion, this is because it will be the first to explode. Remember, to be in line with the [Contribution](/Presets/CONTRIBUTING.md) document, you want to create an element that shows the UNSAFE area, not the SAFESPOT. In this mechanic, the tether causes the AOE in that area to become safe, so you want to draw an element that covers the unfrozen NPC. In this mechanic, a frozen and unfrozen NPC are always either N/S or W/E so the unsafe AOE can be drawn correctly without worrying about different patterns existing.

![finalexample](/docs/images/walkthrough/finalvfxex.png)

</details>

## Creating an element using a status condition and overlay text.

<details>

<summary>Walkthrough</summary>

A status condition can be placed upon you in many of the fights in Final Fantasy. Sometimes, in specific fights, these status conditions can tell you how to resolve a mechanic. In other cases, they simply increase the damage you take. In this scenario, we are going to look at the debuff `Vulnerability Up` and how we can create an element which warns us when we have one of these debuffs.

In this example, my character has been inflicted with one stack of `Vulnerability Up`. I know this because of the debuff icon that appears on my bar. 

![vuln1](/docs/images/walkthrough/vuln1.png)

After creating a new layout and adding a blank element, I change the element to `circle relative to object position` and change the target to `self` because I want the element to appear under my feet. I then tick the `Status Requirement` box and type in `Vulnerability Up` into the `Add all by name` box. It registers 21 elements for me which I can add to my list of status requirements.

Since I just want text to appear, I change the value of my `Stroke Thickness` to 0 and in my `Overlay text` box I simply type `one stack`. I name the element one stack so I can keep track of it later.
It appears like this when I have 1 stack of `Vulnerability Up`

![1stack](/docs/images/walkthrough/1stack.png)

However, what if I gain another stack and want the overlay text to change to show 2 stacks?

![2stack](/docs/images/walkthrough/2vuln.png)

To save me a job, I'm going to press the `copy to clipboard` button at the top of my 1 vuln stack element and then, instead of pressing `add element`, I'm going to press `Paste`. This pastes another identical element to the one you have already created. I'm going to name this '2 Vuln Stack'.

Now, to make it register that you have two stacks of the debuff, you need to check the `Check for status param` box and then type in `2`. You want to change the overlay text to say `two stacks`.

You now want to go back to the element you created earlier and click the same `Check for status param` box but include a `1` instead of a 2, so the elements are not overlapping.

![2stack](/docs/images/walkthrough/2stack.png)


>What do you think you would do if you have 3 vuln stacks? Why don't you try creating your own element to show on 3 stacks, following the steps above?


</details>

## Creating a donut element.

<details>

<summary>Walkthrough</summary>

Sometimes, an NPC may cast a spell that renders the entire battle field dangerous except for one small section. Naturally, you would want to just paint the safe spot with a colour. However, to be in line with the contributions document and to make sure your preset is allowed to be uploaded, you need to ensure you are painting the UNSAFE portion of the AOE. To do this, we use something called a donut.

![donut](/docs/images/walkthrough/unsafearena.png)

As the screenshot above shows, there is a small circle that is free from any dangerous area. This would be the safe spot. For this particular mechanic, the position of the safe spot does not change. This means that, however many dungeon runs you do, this safe spot will always be the same. This allows us to be slightly lazy in our design and we can create a circle element at that spot which is `triggered` on the cast of this particular spell.

Firstly, I created a circular element at my cursor position using the mouse button next to `reference position` which appears when I have selected `circle at fixed cordinates` as my element type.I changed the radius of the element so it filled the dangerous circle.

![circlular](/docs/images/walkthrough/dangercircle.png)

After that, next to the radius, is the option `Donut`. This inverts my dangerous radius outwards, covering the map rather than the circle inside. This type of draw would comply with the requirements of the contributions document, as it is drawing the unsafe area. I make the radius as large as the arena.

![uncirclular](/docs/images/walkthrough/unsafecircle.png)

Since `triggers` were covered in a previous area of the page, you should know that you are looking for the skill ID that the bird casts just as the dangerous area appears. Looking at `log`, I can see that the NPC casts `36284 - Windshot` just as the dangerous area appears. I can then go and tick `enable trigger` and type in the spell ID `36284`. However, to ensure that the element does not last forever, I am going to give it a duration of 5.5 seconds. I am then left with a draw as seen below.

![donutexample](/docs/images/walkthrough/donutexample.gif)

</details>
