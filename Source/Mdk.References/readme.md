# MDK References

---

### Note: It does seem like VS2022 is required for stable performance. However this is unconfirmed.

The game [Space Engineers](https://store.steampowered.com/app/244850/Space_Engineers/)
(by [Keen Software House](https://www.keenswh.com/), no affiliation) supports 
scripting and modding using the C# language. This package aims to automatically detect
the location of the Space Engineers installation, and referencing the assembly DLLs required
to write scripts and mods.

_This package is part of the MDK2 project._

---

### Usage:
Simply reference the nuget package in your mod project. It should automatically do its thing.

### Copying DLLs locally
By default, the assemblies are referenced directly from their location in the Space Engineers
game installation folder. If you want to run unit tests, you will need to have those assemblies
copied locally. To do so, add the following to the initial PropertyGroup in your project file:
```xml
 <SpaceEngineersBinCopyLocal>true</SpaceEngineersBinCopyLocal>
```
**Keep in mind:** These assemblies belong to Keen Software House. You are _not_ allowed to 
pack and redistribute these dlls, this is the entire reason why this package exist to automate
their reference for you.

---

_Disclaimer:_

_These tools are an independent creation and is not endorsed, sponsored, nor affiliated with Keen Software House.
"Space Engineers" is a trademark of Keen Software House. All trademarks and copyrights used are properties of their
respective owners. The use of "Space Engineers" in these tools is for reference purposes only and does not imply
any association or endorsement._