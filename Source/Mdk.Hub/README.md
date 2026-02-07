# MDK2 Hub

The MDK2 Hub is a desktop GUI application for managing your Space Engineers programmable block scripts and mods built with MDK2. Think of it as mission control for your projects - it shows your project list, manages NuGet package updates, lets you open projects in your IDE with a click, and receives build notifications from Visual Studio or Rider.

## Key Features

- **Project Overview**: See all your MDK2 projects in one place, sorted by most recently used
- **Package Management**: Check for and install MDK package updates across one or all projects
- **Quick Actions**: Open projects in your IDE, browse to project folders, copy deployed scripts
- **Build Integration**: Receive notifications when you build projects, automatically register new projects
- **Configuration Editor**: Visual editor for mdk.ini settings with validation
- **Cross-Platform**: Runs on Windows and Linux

## A Development Experiment

This Hub application is also an experiment in AI-assisted development. I wanted a proper GUI for MDK, but was skirting the edges of burnout and didn't have much energy for hobby projects - yet I still *really* wanted this tool to exist. I'd also been seeing more and more users asking for Linux support, some even managing to get MDK working on Linux through various workarounds. The Hub project became an opportunity to modernize the entire architecture and make proper Linux compatibility a reality for them.

So I tried "vibe coding" with LLMs. The premise: write a full UI application using minimal manual coding, relying heavily on AI assistance, and see how much oversight and correction would be needed from someone with software engineering experience.

The result? I'm happy with the Hub as it stands. It works, it's useful, and it does what I needed it to do. But it required a *lot* of corrections along the way. I still had to step in manually to fix architectural issues, catch mistakes, and guide design decisions. Working with an LLM felt like overseeing a junior programmer with short-term memory loss but a lot of theoretical knowledge - they could write code quickly, but needed constant course correction and context reminders.

**There's no way**, even with modern LLMs, for someone without software development experience to produce a well-written, maintainable application. Sure, it might *work* more or less, but the code would be fragile and break at a wrong look. You're not taking over my job just yet, robot ;)

And yet... the result does stand. Yes, there are still some architectural issues I'd like to revisit. Yes, it was more work than I initially hoped. But seeing as I *do* have that software engineering experience to provide oversight, it *did* work in the end. The Hub exists, it's useful, and it got built faster than if I'd written every line myself.

Draw your own conclusions about what this means for AI-assisted development.

## Getting Started

The Hub is distributed as part of MDK2. Install it via the installer or portable zip from the [releases page](https://github.com/malware-dev/MDK-SE/releases).

### For New Users

On first run, the Hub will:
1. Check for prerequisites (.NET SDK, templates)
2. Offer to install anything that's missing
3. Check for available updates

Once prerequisites are installed, you can create new projects:
- **Via the Hub**: Use the "New Project" action to create a script or mod project with a guided wizard
- **Via your IDE**: Visual Studio or Rider will have the MDK templates available in their "New Project" dialogs
- **Via command line**: Use `dotnet new mdk2pbscript` or `dotnet new mdk2mod`

Build your project in your IDE - the Hub will detect it automatically and add it to your project list.

### For Existing MDK 2.1 Users

The Hub will automatically find and import your existing MDK2 projects on first run. From there, just continue working as normal - the Hub will track your projects and notify you of available updates.

## For Contributors

The Hub is built with:
- **Avalonia UI** - Cross-platform .NET UI framework
- **.NET 9** - Modern C# and runtime features
- **Velopack** - Modern update/installation system
- **Mal.SourceGeneratedDI** - Compile-time dependency injection

Key architecture notes:
- Services live in `Features/` folders alongside their ViewModels and Views
- The `IShell` service manages overlays, toasts, and global UI state  
- Project configuration uses a layered system (default → main.ini → local.ini)
- Background update checking happens via `UpdateManager` and `ProjectUpdateChecker`

If you're diving into the code and wondering "why is this structured this way?" - sometimes it's intentional architecture, sometimes it's an AI suggestion I didn't catch and refactor. Feel free to improve things, just test thoroughly.

## License

See the [main repository](https://github.com/malware-dev/MDK-SE) for license information.
