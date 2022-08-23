# NppPlugin .NET package for VS2019 and beyond for Notepad++ 32bit and 64 bit...

What is this? Its a simple template for very fast and easy building plugins for Notepad++ in C#/.Net

This is a fork of UFO's plugin package updated for VS2015, 2017 and 2019

[![Build status](https://ci.appveyor.com/api/projects/status/5f0ui9y2ujugh6wt/branch/master?svg=true)](https://ci.appveyor.com/project/kbilsted/notepadpluspluspluginpack-net/branch/master)
[![License](http://img.shields.io/badge/License-Apache_2-red.svg?style=flat)](http://www.apache.org/licenses/LICENSE-2.0)
[![Stats](https://img.shields.io/badge/Code_lines-5,6_K-ff69b4.svg)]()
[![Stats](https://img.shields.io/badge/Doc_lines-3,3_K-ff69b4.svg)]()


## Getting started
  1. Download a [release](https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net/releases/) 
  2. Place the visual studio project template (the `NppPlugin.zip`) in the visual studio path (typically `"My Documents\Visual Studio 20xx\Templates\ProjectTemplates\Visual C#\"`)
  3. If you intend to debug Notepad++ itself (and not just the plugin) ensure you have installed **Visual C++** from the visual studio installer<br>
  ![install CPP](/documentation/installcpp.png)
  4. Create a new project inside Visual studio using `file -> new -> project -> visual C# -> Notepad++ project`
  5. Build (building will copy the dll to the `Notepad++/plugins` folder)
  6. Start Notepad++ and activate your plugin from the plugins menu


## Upgrading to a newer version
  * Upgrading the pluging package
    * replacing the `NppPluginXXXX.zip` from your visual studio (typically `"My Documents\Visual Studio 20xx\Templates\ProjectTemplates\Visual C#\"`) with a newer version

  * Upgrading plugings using the plugin pack. 
    * Delete the folder `PluginInfrastructure` and copy over that folder from a newer version of `NppPluginXXXX.zip`
    * Open visual studio 
      * Click "show all files" in the "solution explorer"
      * Select the new files, Right-click and choose "include in project"


## Plugins using this pluginpack

  * https://github.com/kbilsted/NppPluginGuidHelper
  * https://github.com/zkirkland/FirstUpper
  * https://github.com/kbilsted/NppPluginCutNCopyLine
  * https://github.com/kbilsted/NppPluginRebaseAssister
  * https://github.com/nea/MarkdownViewerPlusPlus
  * https://github.com/AnnaVel/RtfHelper
  * https://github.com/jokedst/CsvQuery
  * https://github.com/wakatime/notepadpp-wakatime
  * https://github.com/alex-ilin/WebEdit
  * https://github.com/Fruchtzwerg94/PlantUmlViewer
  
If your plugin is not on the list, please make a PR with a link to it.. :-)


## How to start coding plugins

First read the the demo-plugin code. It is actively being maintaned - see https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net/tree/master/Demo%20Plugin it is a spin-off of Don Ho's
    http://download.tuxfamily.org/nppplugins/NppPluginDemo/NppPluginDemo.zip

#### Overall plugin architecture

Plugins can interact with Notepad++ or the underlying Scintilla engine. The plugin pack provides two classes to make this interaction easier. This is `NotepadPlusPlusGateway` and `ScintillaGateWay` which are thin layers making interaction more pleasant (and testable!). 

If you are interested in low-level access you can use the `Win32` api also included in the plugin pack. 

The architecture of the plugin is.


                   +-----------+ +-----------+
                   | Scintilla | | Notepad++ |
                   +-----------+ +-----------+
                        ^             ^
                        |             |
               +--------+--------+----+------------+                   
               |                 |                 |
     +------------------+ +----------------+ +-----------+ 
     | ScintillaGateway | | NotepadGateway | | Win32     |
     +------------------+ +----------------+ +-----------+ 
          ^                     ^                ^        
          |                     |                |        
          +-----------------+---+----------------+                   
                            |              
                       +-----------+ 
                       | Plugin    |
                       +-----------+ 

### How to debug plugins
  * Install both 32 bit and 64 bit versions of Notepad++ (if you intend to publish for both)
  * Give yourself write permissions to the Notepad++ plugin folders
    * Usually `C:\Program Files (x86)\Notepad++\plugins\` (for 32 bit) and `C:\Program Files\Notepad++\plugins\` (for 64 bit)
    * Or run Visual Studio as administrator (not recommended)
  * In Visual Studio, choose Platform to debug (x86 or x64)
  * Make sure Notepad++ is not running
  * Start Debugging (F5 by default)

Or you can attach to a running process:
  * start notepad++
  * in Visual Studio: debug -> attach to process... -> notepad++.exe

you can now set breakpoints and step-execute. (currently not working in v6.9.2 https://github.com/notepad-plus-plus/notepad-plus-plus/issues/1964) 
  
   * you can make this process easier by setting the "start action" in the project -> properties -> debug to start notepad++.exe - then you can simply build and hit `F5`.

### Working with dependencies
To use external dependencies such as libraries in your plugin you can proceed in two ways:
* clone the dependency sources into your plugin project (e.g. as a shared project)
* add the dependency as reference (i.e. via NuGet) and merge it into the plugin *.dll* via [ILMerge][2] (*preferred*)

#### Shared Projects
One possibility is to include dependencies by adding the required source code into a new **Shared Project** in the plugin-development solution.

Assuming you are using Visual Studio to add a Shared Project to your solution (*New Project -> Shared Project*) and copy the required sources into that project. Double-check that the Shared Project is part of your main plugin-project references. If not, add the reference. You are now ready to use the included sources in your plugin and they will be compiled into the final plugin *.dll*.

*Note: Do not include properties etc. from other dependencies when copying the sources, as the main project defines the e.g. assembly information.*

#### References
The prefered way to use dependencies is via **References**, in the best-case through [*NuGet*][3].

Via NuGet you are able to add packages to your project, certain versions etc. through a global repository. Package information is stored in your project and other developers can gather the same, correct versions through the global repository without committing masses of data.

To use included references in your compiled plugin you need to merge these into the *.dll* as Notepad++ is not fond of locating non-plugin *.dll's* in its folder. [ILMerge][2] was developed for the purpose of merging managed libraries. It can be installed via NuGet and used manually. 

The best way is to install [MSBuild.ILMerge.Task][1] via NuGet in your plugin project. It will include [ILMerge][2] as dependency and attach itself to your Visual Studio build process. By that, with every build you compile a merged plugin *.dll* including all your custom references to use. ILMerge will add configuration files to your project: *ILMerge.props* and *ILMergeOrder.txt*. Refer to the official homepage for more information about the configuration possibilities.

*Note: To use ILMerge in your plugin you have to change the **Target Framework** of your plugin project to at least .NET Framework 4.0 (CP). ILMerge can work with .NET 3.5 and below, but requires additional configuration and adaptation. If you do not required the extreme backwards compatibility, upgrade the .NET Framework target as quick and easy solution.*

### 32 vs 64 bit
Notepad++ comes in two versions, 32 bit and 64 bit. Unfortunately this means plugins need to create two versions as well.

Using this template you can switch between the two using the Visual Studio "Target Platform" drop-down.

When publishing your plugin you should build in Release mode for both x86 and x64 and publish both resulting dll's (e.g. `bin/Release/myPlugin.dll` and `/bin/Release-x64/MyPlugin.dll`)



## Requirements

  * Works with .NET Runtime 4.0 and above 
  * UNICODE plugins only.
  * Works on Notepad++ 7.6.3 and above
  
For Notepad++ 7.6 to 7.6.2 no release works out of the box due to plugin directories changing with every release.

### v0.95.00 pre-release
Latest version as of Jan 2021 contains x64 fixes and updated HOW-TO-INSTALL.txt  
Tested on VS2019 and latest Notepad++ both x86 and x64 versions.  

### v0.94.00
The last version to work with Notepad++ v7.5.9 or below (newer versions of notepad++ use a different plugin structure, so you need to use a newer version of this framework). 
If you copy the binaries to the right place, you may still get things working.
	
### v0.93.96	

The last known version to run vs2015 is v0.93.96 (https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net/releases) with a little fidling you may be able to get newer versions to run 2015 as well. I just haven't tested it.




## Versioning
Until we reach v1.0 expect a bit of chaos and breaking changes.

From v1.0 and onwards we will turn over to semantic versioning

    
## Credits
  * For the main work on the plugin package
    * A Guy called "Ufo" - merging in v0.5 http://sourceforge.net/projects/sourcecookifier/files/other%20plugins/NppPlugin.NET.v0.5.zip/download and v0.7 https://bitbucket.org/uph0/npppluginnet 
  * The DllExport technique:
    * Dark Daskin: http://www.codeproject.com/KB/dotnet/DllExporter.aspx
    * Robert Giesecke: http://sites.google.com/site/robertgiesecke/Home/uploads/csharpprojecttemplateforunmanagedexports https://www.nuget.org/packages/UnmanagedExports

And of course the people helping out with pull requests! Much appreciated!


## About me

I blog at http://firstclassthoughts.co.uk/ on code readability and quality



## Notes

  [1]: https://www.nuget.org/packages/MSBuild.ILMerge.Task/
  [2]: https://www.nuget.org/packages/ilmerge
  [3]: https://www.nuget.org/
