# NodeScript

A node-based visual programming interface for building Space Engineers programmable block scripts.

## Project Vision

NodeScript is a visual programming system designed to enable **non-programmers** to create simple automation scripts without writing code.

### Target Audience
- Space Engineers players who don't program
- Focus on simple automation (airlocks, doors, pistons, rotors)
- **NOT** for complex systems (inventory management, autopilots)

### Core Philosophy
**Declarative over Imperative**: Users define *what* should happen, not *how* it happens. The system handles boilerplate, state management, and PB script structure automatically.

## MVP Scope: Airlock Controller

The minimum viable product will support building a complete airlock cycle controller using visual nodes.

## Documentation

- **[Architecture.md](Architecture.md)** - Technical architecture, execution model, PB constraints
- **[Nodes.md](Nodes.md)** - Node types, visual design, interaction model
- **[EditorDesign.md](EditorDesign.md)** - Canvas UI, right-click workflow, visual design
- **[Implementation.md](Implementation.md)** - Implementation phases, roadmap, status
- **[Questions.md](Questions.md)** - Open questions, design decisions, risks

## Current Status

**Phase**: Prototype - Working Canvas with Interactive Nodes
**Last Updated**: 2026-02-17

### Completed
- ✅ Window hosting system
- ✅ NodeScript editor launches in separate window
- ✅ Core architecture planning
- ✅ Node design concepts
- ✅ MVP scope definition (airlock controller)
- ✅ Block node as data source design decision
- ✅ NodifyM.Avalonia integration (v1.1.9, MIT license)
- ✅ Interactive canvas with pan/zoom/grid
- ✅ Right-click menu for node creation
- ✅ Block node prototype (draggable, selectable)
- ✅ Hub theme integration

### In Progress
- 🔨 Additional node types (OnArgument, WaitForState, Actions)
- 🔨 Connector/pin system for wiring nodes

### Next Milestones
1. Implement all MVP node types
2. Add connection drawing between nodes
3. Basic code generation to PB script
4. Save/load node graphs
- ✅ Editor interaction model (right-click driven)

### In Progress
- 🔄 Canvas prototype with NodifyM.Avalonia (installed and building)
- 🔄 Testing pan/zoom and empty state

### Completed (Session)
- ✅ Library research (NodifyM.Avalonia selected - MIT license)
- ✅ Package installation (v1.1.9)
- ✅ License attribution added to About view
- ✅ Basic canvas setup with correct namespace (`NodifyM.Avalonia.Controls`)
- ✅ Fixed property binding issues (removed non-existent ViewportLocation)
- ✅ Build successful - ready for testing

### Next
- Properties editing UI for nodes
- Connection rendering infrastructure
- Additional node types (OnArgument, ControlDoor)

---

*This is a living project - documentation will evolve as design decisions are made.*


