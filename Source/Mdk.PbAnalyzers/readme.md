# MDK Analyzers

---

The game [Space Engineers](https://store.steampowered.com/app/244850/Space_Engineers/)
(by [Keen Software House](https://www.keenswh.com/), no affiliation) supports 
scripting and modding using the C# language, but limited by a built-in whitelist to 
protect against dangerous or destructive code. This analyzer aims to inform the 
developer when they're trying to use parts of the .NET framework or game API they are 
not allowed to use.

_This package is part of the MDK2 project._

---

### Usage:
Simply reference the nuget package in your mod project. It should automatically start 
analyzing your project.

Nuget unfortunately does not notify you when there's updates to nuget packages, so 
you should check manually now and again, especially after game updates.

---

_Disclaimer:_

_These tools are an independent creation and is not endorsed, sponsored, nor affiliated with Keen Software House.
"Space Engineers" is a trademark of Keen Software House. All trademarks and copyrights used are properties of their
respective owners. The use of "Space Engineers" in these tools is for reference purposes only and does not imply
any association or endorsement._
