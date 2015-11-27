# Introduction #

DRAFT


# Getting Started #

There's a lot of pieces you need to get before you even start. Get the source, and various required assemblies.

Download ACT itself and extract it, (if you haven't already).  This project act-aion-plugin is just a plugin for ACT, so you'll still need ACT. You can get ACT at [ACT Downloads](http://advancedcombattracker.com/download.php)  Note the location of your "Advanced Combat Tracker.exe" for reference later.

Get a mercurial client. [HgTortoise](http://tortoisehg.bitbucket.org/) is one suggestion for Windows.

Get the source for act-aion-plugin. Go to Source tab of the project, and run:
`hg clone https://USERNAME@code.google.com/p/act-aion-plugin/`
or with HgTortoise, create a new folder, right-click and open HgTortoise clone menu item, and paste the https path above into Source field and Clone.  (When pushing changes back to the server, you don't use your gmail password, you use your [GoogleCode password](https://code.google.com/hosting/settings))

Optionally for VS C# Express, download [StyleCop](http://stylecop.codeplex.com/) for MSBuild. If you add StyleCop and running VS Express, ensure that the path for StyleCop in the .csproj file matches your MSBuild StyleCop.targets file under ProgramFilesx86 as per [StyleCop documentation](http://stylecop.codeplex.com/wikipage?title=Running%20StyleCop%20in%20VS2005%20or%20VS%20Express&referringTitle=Documentation).  If you don't use StyleCop, remove the ImportProject line for your .csproj before opening the project.

Open the project in Visual Studio 2010 (or VS2010 C# Express).  In the solution explorer, under References, remove the broken "Advanced Combat Tracker" reference and Add Reference to the location of your "Advanced Combat Tracker.exe".