# Starcraft EPD Editor
Shoutout to [@jjf28](https://github.com/jjf28) 

[The official thread](http://www.staredit.net/topic/17518/)

This contains archive for the latest development version of the editor.<br />
The sources for DLL file will not to be released, as the map protection is a thing that should be respected.<br />
This source contains unreleased features, with some bugs created, removed, or worse, undetermined.


## Modules
* #### Parser
* * Parses TrigEdit format, which is output (and input) from (to) the DLL library.
* * Do not mess with this, because it holds the key to trigger copying
* #### UI
* * Most of the UI is *XAML* with backing class, though those are in top-level directory
* #### Data
* * The core module
* * Most of them are already at their final version (the trigger is for sure). 
* * Default memory contains raw vanilla 1.16.1 memory scan for default values. If new actions are added, this should be updated
* * Action/Condition is not to be messed up with
* * EPDAction is something to mess with. Adding memory blocks should be done statically with support classes, just the way it is.
* #### wnd
* * Additional option windows
* * All dialog boxes should be stored here

<br />
## Future of the editor
I won't be able to resume the planned work, hence the release. It took a long time and great effort to put this together, and hopefully it will live for a long time (just like Starcraft). If you find a bug, create issue here (or make a post on the forums) and it will be fixed in the future (not in a matter of minutes like it used to be). 

The official update server should be up for a few more months, but will most likely end for the sake of *org* TLD (There's a plan to take it back), the infamous calculator could be integrated directly into this.
