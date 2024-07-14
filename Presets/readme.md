# Splatoon Presets

This is a repository of layout and script presets for Splatoon. These presets have been developed and provided by numerous people from the [Puni.sh Discord server](https://discord.gg/Zzrcc8kmvy). The scripts in this directory are vetted and verified to be trustworthy by the Spaltoon development team.

Above is a directory of folders leading to different types of content presets exist for. Navigate through those to find other `.md` files containing text or URLs for you to import presets or scripts with.

## Definitions

There are 2 types of presets:

- **Normal presets** consists of one or multiple layouts and are shared via a line of text.
- **Scripts** are basically microplugins that run in a specific instance. They are used when mechanics are too complex to be contained in normal presets. Scripts are imported from via a URL and not from a text box.

## Tags

Presets may contain the following tags:

- `[EN]`, `[JP]`, `[DE]`, and `[FR]` mean that preset or script will work with the indicated client language(s). Usually, porting it to another language will involve adding some localized lines from game's log to triggers or object names.

- `[International]` presets or scripts do not rely on data derived from client language and can be used on any game language without modifications.

- `[Untested]` presets or scripts have no data about whether they work correctly or at all. Please use caution when adding these. It's advised to try them out in Duty Recorder before using them in a duty or event.

- `[Beta]` incidates that the preset or script has passed initial testing but it may contain problems, not cover all possible patterns or is still undergoing development. They may be significantly changed in future, so check back for updates.

## Importing Presets

> [!IMPORTANT]  
> Do not blindly import everything. You will just end up with a visual mess on your screen. Find presets for the mechanics you want to display and import those.

Once you've decided that you want to import a preset, you need to copy the preset code into your clipboard by pressing button on the right side of code block:

![](/docs/images/preset_import/copy_button.png)

Then, import your preset into the Splatoon plugin by opening it with the `/splatoon` command in-game. Proceed to `Layouts` tab and then press the `Import from clipboard` button.

![](/docs/images/preset_import/ingame_import.png)

If you did everything correctly, the preset should be added to the list on the left side. If this isn't working, please visit the [Puni.sh Discord server](https://discord.gg/Zzrcc8kmvy) for support.

## Additional Sources of Presets

Below is a list of third-party repositories and websites that have additional Splatoon presets. If something is not here, please check these to see if what you are looking for was developed elsewhere.

> [!WARNING]
> If you are installing a script from a source other than this repo, please make sure you understand its code. Scripts have full access to your computer, similar to how plugins do. If you don't understand it, at least make a support request in their developer's Discord server before installing.

- https://github.com/adamchris1992/ffxivsplat
- https://github.com/cptjabberwock/SplatoonPresetsList/wiki
- https://github.com/Ksirashi/Presets
- https://github.com/cptjabberwock/SplatoonPresetsList/wiki

## Making Presets

### Tools to Make Presets

Most presets are not made directly in duties or events. They are created using recordings of them, provided by a first-party plugin called [A Realm Recorded](https://github.com/UnknownX7/ARealmRecorded). This is available directly through the `/xlplugins` command. It records _any_ duty and you can play them back at any inn.

[Triggevent](https://github.com/xpdota/event-trigger) is an addon for ACT that is helpful for figuring out triggers, effects, and other game events happening during a fight.

Lastly, [BossMod](https://github.com/awgil/ffxiv_bossmod) has a useful replay feature to capture all of the events that happen in a duty into an organized log file for you to reference when developing a preset. Another helpful feature of BossMod is when it gets updated for a certain fight, a list of all of the encounter's spells are published to their GitHub. See [here](https://github.com/awgil/ffxiv_bossmod/blob/master/BossMod/Modules/Dawntrail/Extreme/Ex1Valigarmanda/Ex1ValigarmandaEnums.cs) for an example.

### Naming

Presets should adhere to the following naming scheme:

- Groups are named after the dungeon, trial, boss name, or raid, prepended by it's abbreviation, difficulty, or level. Some examples (the text in parentheses is not required):

  - `EX1 - Worqor Lar Dor` (Dawntrail's first extreme trial)
  - `97 - Worqor Lar Door` (Dawntrail's first normal trial)
  - `P1S - Erichthonios` (Asphodelos: The First Circle, Pandæmonium's first boss on Savage difficulty)
  - `97 - Alexandria` (level 97 dungeon)

- Layouts are named after the mechanic or boss it covers. This may not be adhered to all the time, but please try to stay true to this when developing new presets. Some examples (the text in parentheses is not required):

  - `Avalanche` (for EX1 in Dawntrail)
  - `Half Full` (for EX2 in Dawntrail)
  - `Amalgam` (for the second boss of the level 100 dungeon Alexandria in Dawntrail)

- Elements you have free reign over, just try to be descriptive of what the element is. For example, during `Projection of Triumph` in Dawntrail's EX2, the elements are called `Line Donuts` and `Line Point-blanks`.

Most importantly, **make sure everything is _descriptive_**.
