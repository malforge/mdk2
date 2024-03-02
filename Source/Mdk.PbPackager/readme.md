# MDK Analyzers

---

The game [Space Engineers](https://store.steampowered.com/app/244850/Space_Engineers/)
(by [Keen Software House](https://www.keenswh.com/), no affiliation) supports
scripting and modding using the C# language. This project allows scripters to use full projects
for their scripts, with separate code files and mixin (shared project) libraries, by reading
the projects and compiling them into a single script file the game can use.

_This package is part of the MDK2 project._

---

### Usage:

Usage description pending: This is an emergency release due to the problems with MDK1 and the latest Visual Studio 2022
version. A more detailed description will be added later.

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


- **By default, MDK will only pack the project if you have selected the `Release` configuration, and not the `Debug` configuration.**

    `<MdkBuildConfiguration>all</MdkBuildConfiguration>` - This option will allow you to specify at which configuration the
    packager should build the project. By default, this is set to `Release`. You can either specify your own configuration,
    or you can set it to `all` to build all configurations.

---

_Disclaimer:_

_These tools are an independent creation and is not endorsed, sponsored, nor affiliated with Keen Software House.
"Space Engineers" is a trademark of Keen Software House. All trademarks and copyrights used are properties of their
respective owners. The use of "Space Engineers" in these tools is for reference purposes only and does not imply
any association or endorsement._ 

