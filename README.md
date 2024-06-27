# MDK²-SE
_(Malware's Development Kit for SE)_

[![Build and Deploy](https://github.com/malware-dev/mdk2/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/malware-dev/mdk2/actions/workflows/dotnet.yml)

A toolkit to help with ingame script (programmable block) development for Keen Software House's space sandbox Space Engineers. It helps you create a ready-to-code project for writing ingame scripts, and provides an analyzer which warns you if you're trying to use something that is not allowed in Space Engineers. It also provides a code minifier to make your deployed script as small as possible - albeit somewhat unreadable.

### General features
* Helps you create a fully connected script project in Visual Studio, with all references in place
* Class templates for normal utility classes and extension classes
* Tells you if you're using code that's not allowed in Space Engineers (whitelist checker)
* Deploys multiple classes into a single PB script, which then is placed in the local Workshop space for easy access in-game - no copy/paste needed
* Supports optional code minifying: Fit more code within the limits of the programmable block
* Allows real reusable code libraries through the use of Visual Studio's Shared Project

### Remarks:
* Requires that you have the game installed, but does _not_ require you to have it running

## Important Note
This is a project I pretty much made for _myself_. I'm publishing it in case someone else might have a use for it. Fair warning: Make requests, by all means, but if your request is not something I myself have any use for, someone else is gonna have to do the work. I'm fully employed, and this is a spare-time project. I'll be working on it on and off.

## Contribution
Sure thing! I'd be happy to accept contributions to the project. I'm especially grateful for help with features that I might not personally need. Since my time is limited, I mostly focus on features I plan to use. So, extra features, like mod support, depend on community contributions. However, I won't merge contributions blindly. They need to meet a certain standard, and I reserve the right to reject features I don't like. Just a heads-up! 😄

## Quick-start
### Creating a New Pure MDK2 Project

An installer will be available soon, but you can manually install the template for now:

1. Open a terminal or console:
   - Press the Windows `Start` button, type `cmd`, and press enter to open `Command Prompt`.
2. In the console, type `dotnet new install Mal.Mdk2.ScriptTemplates` and press enter.
3. You should now find the templates in Visual Studio and can create fully modern MDK2 projects.
   - JetBrains Rider should also have this template available now, so you no longer need to use Visual Studio.
   - VSCode too! While I'm not too familiar with VSCode, I believe you'll have to use the `dotnet new` command directly with the new templates to create new projects:
     - `dotnet new mdk2pbscript` creates a new script project in the current folder.
     - `dotnet new mdk2pbmixin` creates a new mixin project in the current folder.

### Installing MDK2 in Your MDK1 Project

_Note: This is not the recommended course of action at this point._

1. Right-click on your MDK1 project.
2. Select `Manage NuGet Packages`.
3. Find the search bar. To the right of the search bar, check the "Include prerelease" checkbox, then search for `Mal.`.
4. Install the `Mal.MDK2.PbPackager` package into your project.
5. Rebuild your project.

- - -

_Space Engineers is trademarked to Keen Software House. This toolkit is fan-made, and its developer has no relation to Keen Software House._
