# Brief #

The following are old instructions for an old version of ACT.  Jump to the bottom and read the notes first. (These instructions are derived from the original instructions at [vilemagra's blog](http://vilemagra.com/blog/?p=28).)

# Downloads #

Here's a listing of files you'll need:

  * **ACT parser** from the [ACT download page](http://advancedcombattracker.com/download.php)
  * **Aion plugin** for ACT from ACT download page or from the project downloads page
  * **system.ovr** from [project downloads page](http://code.google.com/p/act-aion-plugin/downloads/list)

# Installation #

## Setup Aion ##
  * Download **system.ovr**.
  * Copy **system.ovr** to your Aion root folder.  (That folder should contain files like system.cfg or version.ini)
  * Start Aion (or if it is already started, then exit and start it again)
    * We want Aion to read system.ovr and create the Chat.log file for ACT later.

## Install and Setup ACT ##
  * Download the **ACT parser**.
    * If you are unsure of which one, just go with the "Advanced Combat Tracker Setup" installer. (With the installer, you just run ACT-Setup.exe to set up ACT.)
  * Run ACT and keep the window open
  * Under _General Options_ tab, open the _Miscellaneous_ grouping, and enter your character's name in the textbox for "Default character name if not defined by log", and hit Apply.
  * Under _About_ tab, in the _Log File_ grouping, click the **Open Log** button, select the **Chat.log** file (same place as you put system.ovr file).

## Install Aion Plugin ##
  * Download and unzip the Aion Plugin. The unzipped file is named **AionParse.dll**
  * Copy **AionParse.dll** to the ACT installation folder. (This isn't necessary, but it'll make it easier to find the file in the next step.)
  * Switch over to the ACT window, under _Plugins_ tab, _Plugin Listing_ tab, click the **Browse** button and select the **AionParse.dll**
    * Click the **Add Plugin** button, and you should see AionParse.dll section appear. (Clicking on the section will show version number of the plugin.)
    * Check the **Enable** checkbox for the AionParse.dll section, and an AionParse.dll tab will appear next to Plugin Listing tab.
  * Under the _AionParse.dll_ tab, confirm that your character name is there and not the generic "YOU" name.
    * This tab contains a lot of experimental stuff. Defaults should just be fine for people not intending to poke in the internals of the plugin.

## system.ovr ##
For those who are curious, system.ovr file is simply a plain text file. The one on the download page simply contains the following:
```
g_chatlog = "1"
log_IncludeTime = "1"
log_Verbosity = "1"
log_FileVerbosity = "1"
```

# Running ACT #
  * Start ACT (if not already started)
  * Start Aion (if not already started)
  * Play Aion =)

# Notes for Updated ACT #

Some updates to above instructions for new version of ACT (time of writing is ACT 3.1.2):
  * Running ACT parser will automatically open a wizard to download plugins.  There is no longer any need to install the Aion plugin separately.
  * Default character name is actually located under _Options_ tab, _Data Correction_ section, 