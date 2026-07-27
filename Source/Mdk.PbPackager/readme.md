# MDK Programmable Block Packager

---

The game [Space Engineers](https://store.steampowered.com/app/244850/Space_Engineers/)
(by [Keen Software House](https://www.keenswh.com/), no affiliation) supports
scripting and modding using the C# language. This project allows scripters to use full projects
for their scripts, with separate code files and mixin (shared project) libraries, by reading
the projects and compiling them into a single script file the game can use.

_This package is part of the MDK2 project._

---

### Usage:

Reference the NuGet package in your script project and it packs on build. For full instructions,
see the [documentation site](https://malforge.github.io/spaceengineers/mdk2/).

Most day-to-day options live in an `mdk.ini` file next to your project. The MSBuild properties
below control the packaging step itself.

#### Important options:
Currently, modifying the options is a little bit cumbersome.

Right-click your project file in the solution explorer, and select "Unload Project".
The project file _should_ open in the editor. If it doesn't, right-click the project 
file and select "Edit Project File".

Make sure your input caret is at the very top of the file (`ctrl+home`), and press `ctrl+f`. Type in </PropertyG.
It should now highlight an end tag `</PropertyGroup>`. This is the end of the first property group and where we will
add our options.

 - **By default, MDK shows an informational bar at the bottom of the window, and will prompt you for input.**

    `<MdkInteractive>no</MdkInteractive>` - This option will disable the interactive mode, and will switch off the informational
    bar at the bottom of the window. This is useful for CI/CD systems, or if you just don't want to be bothered by it.  


- **By default, MDK packs on every build configuration.**

    `<MdkBuildConfiguration>Release</MdkBuildConfiguration>` - This option controls which configuration the
    packager runs for. It defaults to `all`. Set it to a specific configuration name, such as `Release`, if you
    only want packing to happen there.

---

_Disclaimer:_

_These tools are an independent creation and is not endorsed, sponsored, nor affiliated with Keen Software House.
"Space Engineers" is a trademark of Keen Software House. All trademarks and copyrights used are properties of their
respective owners. The use of "Space Engineers" in these tools is for reference purposes only and does not imply
any association or endorsement._ 

