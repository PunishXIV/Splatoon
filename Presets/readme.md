A collection of user-submitted presets. 

# Definitions
In general, there are 2 types of presets.
- Normal preset. Consists of one or few layouts. 
- Script. It is basically a microplugin that runs in a specific instance. Used when mechanics are too complex to be contained in normal presets.

# Tags meaning
Presets may contain tags.

[EN], [JP], [DE], [FR] - means that preset/script will work with the following client languages. Usually to port it to another language you will have to add some localized lines from game's log into triggers or object name.

[International] - preset/script does not relies on data affected by client language and can be used on any game language without modifications.

[Untested] - there is no data about whether this preset/script works correctly or at all. 

[Beta] - the preset/script passed initial testing but it may contain problems, not cover all possible patterns or still undergoing development and may be significantly changed in future. 

# How to import preset
First of all: do not blindly import everything - you will just end up with visual mess on your screen. Find presets for mechanics that you want to display first.

After you have decided, you need to copy preset code into your clipboard by pressing button on the right side of code block:

![](/docs/images/preset_import/image_3.png)

And then, to import your preset into Splatoon plugin, open it up with `/splatoon` command, proceed to "Layouts" tab and press "Import from clipboard" button.

![](/docs/images/preset_import/image_4.png)

If you have done everything correctly, you should have preset added into the plugin.