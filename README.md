# MDK²-SE
_(Malware's Development Kit for SE)_

[![Build and Deploy](https://github.com/malware-dev/mdk2/actions/workflows/buildwithartefacts.yml/badge.svg?branch=main)](https://github.com/malware-dev/mdk2/actions/workflows/dotnet.yml)

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

## Getting Started
See the [wiki home page](https://github.com/malforge/mdk2/wiki) for instructions on how to get started on various IDEs/editors.

- - -

_Space Engineers is trademarked to Keen Software House. This toolkit is fan-made, and its developer has no relation to Keen Software House._

_<a href="https://www.flaticon.com/free-icons/toast" title="toast icons">Toast icons created by Freepik - Flaticon</a>_
