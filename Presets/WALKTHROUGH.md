# Walkthrough
> [!caution]
>This walkthrough assumes you have read all of the documents located within the preset area and have a basic understanding of its content.

<details open>
<summary>Reading Requirements</summary>
<ul>
  <li>
    
[Element Types](/Presets/ELEMENTTYPES.md) </li>
  
<li>
    
[Editing Elements](/Presets/EDITINGELEMENTS.md) </li>

<li>
    
[Trigger Types](/Presets/TRIGGERTYPES.md) </li>
</ul>  
</details>

<details open>
<summary>Programme Requirements</summary>
<ul>
  <li>
    
[Dalamud](https://github.com/goatcorp/Dalamud)</li>
  
<li>

[Splatoon](https://github.com/PunishXIV/Splatoon)</li>

<li>
    
[A Realm Recorded](https://github.com/UnknownX7/ARealmRecorded) with a recording of The Bowl Of Embers Extreme that shows up to crimson cyclone. </li>

<li>
    
[ACT](https://advancedcombattracker.com/download.php) and [Trigevent](https://triggevent.io/) with a log of the recording from A Realm Recorded. </li>

</ul>  
</details>

> [!important]
> Each section of the walkthrough is split up into different parts so you can focus on which element you want to draw. You can access each part of the walkthrough by looking at the heading and pressing the arrow underneath.

## Creating your preset

<details open>
  
<summary>Walkthrough</summary></summary>
<ul>
  <li>
    
  Step 1: Create a layout and call it EX - The Bowl Of Embers Extreme.
  ![layoutcreation](/docs/images/walkthrough/createlayout.png)
  
  </li>
  
  <li>
    
  Step 2: Add an element and name it something that will enable you to identify which element it is later on.
  ![elementcreation](/docs/images/walkthrough/elementcreate.png)
  
  </li>
</ul>

</details>
  
## Creating a line AOE element

<details open>
  
<summary>Walkthrough</summary></summary>

This section will teach you how to create a cone element. For this particular section, we will be using the skill 'Crimson Cyclone'. 

![lines](/docs/images/walkthrough/ifritlines.gif)

[Bossmod](https://github.com/awgil/ffxiv_bossmod/blob/b86d8927452fb6481141f811e93270e4d0c3f714/BossMod/Modules/RealmReborn/Extreme/Ex4Ifrit/Ex4IfritEnums.cs) lists Crimson Cyclone as a skill with a 3.0s cast, range 44 with 6 radius. These values are useful to help us know how long and wide we need to make our element. The fact it has a cast time means we can enable the draw to show the second Ifrit begins casting the skill, giving us plenty of time to see the safe spots.
<ul>
  <li>
    
Step 1: Set your element type to 'line relative to object'
![coneoption](/docs/images/walkthrough/lineobject.png)</li>

<li>
  
Step2: Find the NPC ID    
 - We need to find the NPC ID to enable splatoon to know which NPC the skill is going to be cast from. In this case, the NPC is Ifrit. Splatoon enables you to grab the NPC ID by targetting the NPC and clicking the target button once you have set the Single attribute to NPC. This shows us that Ifrit has an NPC ID of 0x4A1.
![target](/docs/images/walkthrough/targetoption.png)</li>

<li>
  
Step 3: While casting and Skill ID
 - We need to find the skill ID to enable us to create the element. In this case, we already know that the skill name is Crimson Cyclone, so we can look through our Trigevent log and see that Crimson Cyclone has a skill ID of 1532. For some fights, Splatoon already gives us the ID of the skill when we type it in. By ticking the box next to 'While casting', we are telling Splatoon that we want this element to draw when Ifrit is casting this skill.</li>
![whilecasting](/docs/images/walkthrough/whilecasting.png)

<li>

Step 4: Setting the width and length of the element
 - We know that the element has a range of 44 and a radius of 6. Sometimes we know that because of bossmod and other times, particularly in new fights, we have to do some trial and error testing. We want to make sure that we have accounted for rotation by ticking the "account for rotation" box.
![account for rotation](/docs/images/walkthrough/accountrotate.png)
 - We then want to make Point A have Y:44 (the length) and change the radius to be 6.
![yandradius](/docs/images/walkthrough/yandradius.png)

</li>



  
<li>




