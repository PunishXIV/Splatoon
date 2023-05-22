# [⚠ For installation instructions please click here. ⚠](https://github.com/NightmareXIV/MyDalamudPlugins#installation)
# Splatoon for FFXIV
## [TOP presets are here](https://github.com/NightmareXIV/Splatoon/tree/master/Presets/Endwalker%20content/Duties/Ultimate%20-%20The%20Omega%20Protocol)
Splatoon plugin allows you to put infinite amount of waymarks in the world of different size, colors, allows to add custom text to them as well. 
<p align="center"><img src="https://raw.githubusercontent.com/NightmareXIV/Splatoon/master/Splatoon/res/icon.png"></p>

|Do you need waymarks in a location where you can't place them but you don't want to risk messing with in-game waymarks? Say goodbye to suspicious waymark presets from party finder! Splatoon allows you to create INFINITE amount of waymarks for yourself in any location you could ever imagine! And on top of that, Splatoon operates FULLY on your side and NEVER sends anything to game servers, so there is no risk of ban!|
|---|

# Install
Dalamud repository:

`https://raw.githubusercontent.com/NightmareXIV/MyDalamudPlugins/main/pluginmaster.json`

Detailed instructions available here: https://github.com/NightmareXIV/MyDalamudPlugins

# CHINESE PLAYERS - ATTENTION!
Chinese fork of Dalamud currently intentionally prevents usage of Splatoon by adding it into banned plugins list. Banned plugin list was originally created to prevent unstable plugins from loading on updates. However, Chinese forkers of Dalamud decided to abuse it to attempt to prevent you from using the plugin for their personal reasons. 
## Easy way to unblock Splatoon and all other plugins:
Simply download this program https://github.com/NightmareXIV/UnbanPluginsCN/releases/latest and run it before you run the game/inject Dalamud. Just keep this program running and you should be good.
## Old, manual way:
- Download [unbanPlugins.bat](https://github.com/NightmareXIV/MyDalamudPlugins/raw/main/cn/unbanPlugins.bat) file (right click - save link as...)
- Place it next to `Dalamud.Updater.exe`
- Launch `Dalamud.Updater.exe` and wait until all pending updates are completed
- Launch `unbanPlugins.bat`
- Start Dalamud

# FAQ
#### Is Splatoon safe to use?
- Splatoon is as safe to use as other tools like ACT, official Dalamud plugins, texture mods. Splatoon does not modifies any data in game in any way - it only reads it and displays additional overlay on top of the game.
#### Is there risk of ban while using Splatoon?
- The risk level is the same as using any official Dalamud plugins, ACT, TexTools, which is: currently safe if you do not acknowledge use of it in game. Splatoon does not include any automation, does not modify any in-game data and does not modify any network data. It is not possible for other players or game masters to figure out whether you are using Splatoon or not.
#### Can Splatoon automatically detect and display hidden AOEs?
- No. It is on you to configure Splatoon to display something at specific place and/or at specific time, or import presets that have been made by other users.
#### Is it safe to stream, record or take screenshots while using Splatoon?
- No. If you are using Splatoon, you must refrain from streaming, recording and taking screenshots while any layouts or plugin menu is visible.
#### Is it safe to talk about Splatoon and exchange presets in game?
- No. You must keep discussion and presets sharing strictly outside of the game, including DMs and party chat. 

# Feature overview
#### Circles at fixed coordinates
Create circles, dots or simple labels at fixed coordinates anywhere in the world. Add some text to them, configure their color, thickness and other options as you wish.
#### Circles relative to object position
Same stuff as above, but relative to any targeted enemy, yourself or any object selected by it's name. Display range of your ability, enemy's hitbox, easily track and locate aether currents, highlight NPCs that are difficult to find, mark your friend to easily find them in crowded RP places or highlight your partner in a raid mechanic. 
#### Lines between fixed coordinates
Visually split an arena into sectors to make navigation easier
#### Lines relative to object position
With rotation support! Display line mechanics such as Fatebreaker's Burnt strike, display area of your machinist's or bard's AoE, draw an arrow above your character to never lose it in crowded places.
#### Designed to be robust and withstand updates
Failsafe mode ensures that Splatoon will be ready to use as soon as Dalamud is updated to be used in current game's patch. Some features might be disabled if game updated functions that Splatoon uses, but core stuff will be always available for use. Additionally, Splatoon will always be developed with intent to never break old version's configs.
#### Exporting and importing element sets
Easily share your layouts with friends or communities. All settings will be preserved!
#### Zone, job lock, various display conditions
Any set of elements may be set to be displayed only in specified zones, only when using specified jobs, only in combat, duty, etc. 
#### Tether to an object
You can enable tether feature for any element you create, which will draw a line between object and your position, allowing for even easier location of an object
#### Distance limit
Any set of elements supports limiting drawing distance by either measuring distance to element itself or distance to current target
#### Splatoon Find
Quickly find that annoying to find NPC or quest target without needing to create an element for it by utilising `/sf <partial name>` command. Auto-resets on zone change.
#### Triggers
Any set of elements supports simple triggers. You can show/hide sets based on certain combat time or based on any boss phrase to avoid screen clutter. Display your waymarks when you actually need them.
#### Web API
Splatoon can be extrenally controlled by utilizing web API. You can find detailed description below. Integrade it with Cactbot or Triggernometry and create interactive visual fight guide right in game!
#### Command control
Splatoon supports controlling elements via commands.
* `/splatoon enable <layout>` - enables layout
* `/splatoon disable <layout>` - disables layout
* `/splatoon enable <layout>~<element>` - enables element inside layout
* `/splatoon disable <layout>~<element>` - disables element inside layout
* `/splatoon settarget <layout>~<element>` - if element is Circle/line relative to object position, and selected object is an Object with specified name, sets the name of the object to currently targeted object
#### Backup system
Automatic backup system will ensure that you always can rollback if your config became corrupted or you have accidentally deleted something important.

# WARNING!
This project is in beta test. 
* Expect bugs! But critical bugs that could potentially break/crash the game should be fixed by now.
* ~~Always keep backup of your configuration!~~ Plugin will do backups for you now!
* Gui sucks, I'll do something with it later (never)

# Web API (beta)
Splatoon now supports web API to remotely manage layouts and elements.
Request http://127.0.0.1:47774/ with parameters specified in table.
**I'm actively accepting suggestions about web API.** 
**Note: params are QueryString params, not JSON**
<table>
  <tr>
    <th>Parameter</td>
    <th>Usage</td>
  </tr>
  <tr>
    <td>enable</td>
    <td>Comma-separated names of already existing in Splatoon layouts or elements that you want to enable. If you want to enable layout simply pass it's name, if you want to enable specific element, use <code>layoutName~elementName</code> pattern.</td>
  </tr>
  <tr>
    <td>disable</td>
    <td>Same as <code>enable</code>, but will disable elements instead</td>
  </tr>
  <tr>
    <td colspan="2">Note: disabling always done before enabling. You can pass both parameters in one request. For example you can pass all known elements in disable parameter to clean up display, and then enable only ones that are currently needed. Passing same name of element in both <code>enable</code> and <code>disable</code> parameters will always result in element being enabled.</td>
  </tr>
  <tr>
    <td>elements</td>
    <td>Directly transfer encoded element into Splatoon without need of any preconfiguration from inside plugin. They are temporary and will not be stored by Splatoon between restarts.
<ul>
  <li>To obtain encoded layout/element, press <b>Copy as HTTP param</b> button inside Splatoon plugin. These buttons are located inside every layout and element.</li>
      <li> Multiple comma-separated values allowed.</li>
      <li> Can contain layouts and elements at the same time. To obtain layout/element code, use appropriate button inside Splatoon configuration after setting them up.</li>
      <li> If you are exporting layout, it's display conditions, zone/job lock, etc are preserved. If you are exporting element, no display conditions and locks will be attached to it. You do not need to enable layouts/elements before exporting, it will be done automatically.</li>
      </ul>
  </td>
  </tr>
  <tr>
    <td>namespace</td>
    <td>Add elements to specific named namespace instead of default one. If you are not using <code>destroyAt</code> parameter, always specify namespace so you can destroy element manually later. This will apply to all layouts/elements passed in current request. Namespaces are not unique: you can reuse same namespace in further queries to add more layouts/elements to a single namespace.</td>
  </tr>
  <tr>
    <td>destroyAt</td>
    <td>Passing this parameter let you specify when layouts/elements you have defined in request should be destroyed automatically by Splatoon. This parameter can take the following values:
  <ul>
    <li><code>NEVER</code> or <code>0</code> - do not use auto-destroy. This is default value. </li>
    <li><code>COMBAT_EXIT</code> - destroy layouts/elements next time player exits combat.</li>
    <li><code>TERRITORY_CHANGE</code> - destroy layouts/elements next time player changes territory (enters/exits dungeon, for example)</li>
    <li>Numeric value greater than 0 - destroy layouts/elements after this much <b>milli</b>seconds have passed.</li>
      </ul>
      This will apply to all layouts/elements passed in current request. <b>You can send multiple comma-separated values, as soon as any specified condition is met, elements will be removed.</b>
  </td>
  </tr>
  <tr>
    <td>destroy</td>
    <td>Comma-separated namespaces that will be destroyed. All elements that were added under namespace you specified will be destroyed at once. Destruction is always processed before addition of new layouts/elements, so if you want to clear your namespace from possible remainings from previous additions, just pass it's name in <code>destroy</code> parameter as well.</td>
  </tr>
  <tr>
    <td>raw</td>
    <td>By default you have to pass layouts/elements in encoded format. However that makes it difficult to edit from outside of Splatoon. Should you require this possibility - hold CTRL while copying layout/element from Splatoon to obtain it in urlencoded JSON format to which you can easily make changes and then pass it to <code>raw</code> parameter in your query. <b>Only one raw layout/element can be passed in a single query</b>, but you can freely pass encoded and raw at the same time.</td>
  </tr>
</table>
<b>In addition to all this, you may send element/layout inside POST request body in raw, non-encoded format. To get pretty-printed json of layout/element, hold ALT while pressing "Copy as HTTP param" button.</b> Only one layout/element per query in the body is allowed.
<br>
There is no difference between sending everything in one query and sending one layout/element per query. It also doesn't matters if you want to primarily use encoded or raw format. Just do it as you personally prefer.

## Examples for Triggernometry
Show standard/technical step radius while dancing: https://gist.github.com/Limiana/8788c387bfc5fcfd76499ef4e46d37d9
