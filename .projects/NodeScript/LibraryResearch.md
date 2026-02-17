# Avalonia Node Graph Library Research

## Research Date
2026-02-17

## Selected Library

### **NodifyM.Avalonia** ✅ CHOSEN
- **URL**: https://github.com/MakesYT/NodifyM.Avalonia (fork maintained by MakesYT)
- **NuGet**: `NodifyM.Avalonia` v1.1.9
- **License**: MIT (Copyright 2024 MakesYT)

**Why Chosen**:
- Auto-align feature (killer feature for non-programmers)
- Active maintenance (updated Feb 2026)
- Clean MVVM design
- Multi-selection support
- Grid lines built-in
- Simple integration

**Implementation Notes**:
- Requires styles loaded in App.axaml for event handling to work
- Uses `<controls:Node>` wrapper for interactive nodes
- Theming via DynamicResource - override color keys in Brushes.axaml
- Grid template: `<controls:LargeGridLine>` control

---

## Alternatives Evaluated

### 1. **NodeEditor by wieslawsoltes** ⭐ 253 stars
- **URL**: https://github.com/wieslawsoltes/NodeEditor
- **NuGet**: `NodeEditorAvalonia` (actively maintained, updated 14 days ago)
- **License**: MIT

**Pros**:
- Most mature and popular Avalonia node editor
- Built specifically for Avalonia from the ground up
- MVVM-first design with ReactiveUI integration
- Multiple NuGet packages (separation of concerns)
- Active development (updated 2 weeks ago)
- Comprehensive samples including LogicLab (digital logic simulator)
- Pan/zoom built-in
- View locator for node content resolution
- Well-documented with demo apps

**Packages**:
- `NodeEditorAvalonia` - Main controls and theme
- `NodeEditorAvalonia.Model` - Base interfaces
- `NodeEditorAvalonia.Mvvm` - ReactiveUI ViewModels

**Cons**:
- Might be more complex than needed for our use case
- ReactiveUI dependency (we don't use ReactiveUI elsewhere in MDK Hub)
- May require learning their specific architecture

---

### 2. **NodifyM.Avalonia by Maklith** ⭐ 197 stars
- **URL**: https://github.com/Maklith/NodifyM.Avalonia
- **NuGet**: `NodifyM.Avalonia` (recently updated 7 days ago)
- **License**: MIT

**Pros**:
- Port of popular WPF library [Nodify](https://github.com/miroiu/nodify)
- MVVM-focused design
- Built-in features we need:
  - Auto-align nodes (!)
  - Pan/zoom/select
  - Box selection
  - Auto-panning when dragging near edge
  - Text on connections
  - Multiple node selection
- Clean API
- Active development
- Good example project included

**Features**:
- Select, move, auto-align, auto-pan nodes
- Box selection with Ctrl+Drag
- Connection preview (PendingConnection)
- Dark/light themes built-in
- Connector management (Alt+Click to remove)

**Cons**:
- No minimap support (listed as "nonsupport")
- Smaller ecosystem than NodeEditor
- Less documentation than NodeEditor

---

### 3. **Avalonia_BluePrint** ⭐ 129 stars
- **URL**: https://github.com/1694439208/Avalonia_BluePrint
- **License**: Unknown (Chinese language repo)

**Pros**:
- Blueprint-style editor (similar to Unreal Engine)
- Visual style might match our needs

**Cons**:
- Last updated July 2024 (7 months ago - less active)
- Documentation in Chinese
- Unknown maintenance status
- Smaller community

---

### 4. **nodify-avalonia by trrahul** ⭐ 52 stars
- **URL**: https://github.com/trrahul/nodify-avalonia
- **Updated**: May 30, 2025 (8 months ago)

**Cons**:
- Less active than top two
- Smaller community
- Less clear documentation

---

## Recommendation

**Top Choice: NodifyM.Avalonia**

**Reasoning**:
1. **Auto-align feature** - This is a killer feature for usability (users won't need to manually align nodes)
2. **Active development** - Updated 7 days ago
3. **MVVM-first** - Designed for our architecture
4. **Simpler than NodeEditor** - Less complex, easier to integrate
5. **Features match our needs** - Pan/zoom, selection, box select, connection preview
6. **No heavy dependencies** - Doesn't require ReactiveUI
7. **Built-in themes** - Dark/light themes included

**Second Choice: NodeEditor (wieslawsoltes)**
- More mature and popular
- Better documentation
- Larger community
- But more complex and has ReactiveUI dependency

---

## Decision

**Use NodifyM.Avalonia** for the following reasons:
- Simpler integration path
- Auto-align nodes is perfect for non-programmer users
- Active development
- MVVM-friendly without forcing ReactiveUI
- Built-in features match our prototype needs exactly

If NodifyM proves insufficient, we can switch to NodeEditor or build custom.

---

*Research conducted: 2026-02-17*
