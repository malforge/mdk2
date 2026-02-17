# NodeScript Editor Design

## Visual Design Philosophy

**Goal**: Clean, uncluttered workspace that puts focus on the node graph.

**Key Principles**:
- Right-click driven workflow (no permanent toolbars/palettes)
- Empty state guides new users ("Right Click to Start")
- Viewport starts centered at graph origin (0,0)
- Minimal chrome - maximize canvas space

---

## Canvas Infrastructure

### Viewport Management
- **Initial State**: Graph origin (0,0) centered in viewport
- **Pan**: Click-drag on empty space (or middle mouse button)
- **Zoom**: Mouse wheel (center on cursor position)
- **Reset View**: Button/shortcut to recenter at 0,0

### Empty State
When no nodes exist:
- Display centered text: **"Right Click to Start"**
- Subtle styling (low opacity, hint color)
- Disappears when first node added
- Reappears if all nodes deleted

### Context Menu
Right-click on empty canvas space:
- **Add Node** submenu:
  - Data Sources → Block
  - Triggers → On Argument, When Block State
  - Actions → Control Door, Control Piston
  - Flow Control → Wait For State
- Appears at cursor position
- Node spawns at click location (graph coordinates)

### Grid (Future)
- Optional background grid for alignment
- Snap-to-grid toggle
- Not needed for prototype

---

## Node Prototype: Block Node

### Visual Design (Prototype)
**Just the node body** - no pins/ports yet.

**Node Structure**:
```
┌─────────────────────┐
│  🔷 Block           │  ← Header (icon + type name)
├─────────────────────┤
│ Pattern: * Door    │  ← Properties (editable)
│ Type: Door         │
└─────────────────────┘
```

**Styling**:
- Rounded corners (4-8px)
- Drop shadow for depth
- Border color indicates node category (data source = blue?)
- Background: Slightly transparent or solid (TBD based on library)

**States**:
- Default
- Hover (subtle highlight)
- Selected (bright border/glow)
- Dragging (semi-transparent)

### Interaction (Prototype)
- **Select**: Click node
- **Drag**: Click-drag on node body
- **Deselect**: Click empty canvas
- **Delete**: Select + Delete key (future)

### Properties (Prototype)
Block node properties (inline editing for prototype):
- **Selection Strategy**: Dropdown (Specific Name, Wildcard, Group)
- **Pattern/Name/Group**: Text input
- **Type Filter**: Dropdown (Any, Door, Piston, Rotor, etc.)

For prototype: Just display as text, make editable later.

---

## Technology Decisions

### Canvas Library
**Research Needed**: Evaluate Avalonia node graph libraries:
- **Nodify** - Popular WPF library, Avalonia port exists?
- **NodeEditor** - Avalonia-native?
- **Custom Canvas** - Build from scratch with Avalonia primitives

**Requirements**:
- Pan/zoom support
- Node positioning/dragging
- Connection rendering (for later)
- Avalonia compatibility
- Active maintenance

**Decision**: TBD after research

### Styling Approach
Create dedicated `NodeEditorStyles.axaml`:
- Node visual templates
- Color palette (category colors)
- Typography (node labels, property text)
- Shadow/border effects
- Empty state text style

Merged into `NodeScriptEditorView.axaml` via `<StyleInclude>`

---

## Prototype Scope

**Include**:
- Canvas with pan/zoom centered at 0,0
- "Right Click to Start" empty state text
- Right-click context menu → Add Block node
- Single Block node visual (just body)
- Node dragging
- Node selection

**Exclude** (for later):
- Connection pins/ports
- Multiple node types
- Connection rendering
- Properties editing UI
- Serialization
- Code generation

**Success Criteria**:
- Can right-click → add Block node
- Node appears at click location
- Can drag node around canvas
- Can select/deselect node
- Empty state text shows/hides correctly

---

*Last updated: 2026-02-17*
