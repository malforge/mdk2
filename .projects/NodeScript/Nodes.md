# NodeScript Nodes and UI Design

## Design Methodology

**The Challenge**: We (a 35-year programmer + an AI) are designing for non-programmers. How do we make good UX decisions?

### Approaches

1. **Research Existing Systems**
   - Study proven beginner-friendly tools (Scratch, Blockly, IFTTT, Zapier)
   - What patterns do they use? What do non-programmers find intuitive?
   - Space Engineers has tutorial systems - what mental models do SE players have?

2. **Start Simple, Iterate**
   - Build the simplest possible thing first
   - Get it in front of actual users (SE players who don't code)
   - Iterate based on real feedback, not assumptions

3. **Documentation-First Design**
   - Try to write the tutorial before building the UI
   - "To build an airlock: Step 1... Step 2..."
   - If it's hard to explain in simple terms → too complex

4. **Leverage Mental Models**
   - Non-programmers understand: recipes, flowcharts, cause-and-effect
   - "When this happens, do that" is universal
   - Avoid: execution contexts, scope, types, null references

5. **Prototype Multiple Options**
   - Build quick mockups of 2-3 visual styles
   - Show to non-technical friends/family
   - "Which one makes sense to you?"

### Decision Framework

For each design choice, ask:
- **Can I explain this to my mom?** (or someone who's never programmed)
- **What's the simplest version?** (YAGNI for UX)
- **Does it match real-world mental models?** (recipe, instruction manual, flowchart)
- **Can we test it cheaply?** (mockup, paper prototype)

---

**Target Audience**: Non-programmers who find code intimidating.

### Inspiration Research Needed
- **Blueprint (Unreal Engine)**: Execution pins + data pins, visual dataflow
- **Scratch**: Block-stacking, very beginner-friendly
- **Node-RED**: Flow-based, event-driven (closest to our model?)
- **Blockly**: Google's visual programming (block-based)

**Question**: What's most intuitive for someone who's never programmed?

### Initial Thoughts
- **Event-driven flowchart**: Start nodes (triggers) → action nodes → flow control
- Avoid overwhelming visual complexity (too many wires = confusing)
- Clear cause-and-effect relationship: "When this happens → do that"

---

## Block Resources (Core Concept)

### Block Resource as a Node

**Key Decision**: Block Resources are **nodes in the graph**, not a separate configuration panel.

A **Block Node**:
- Appears in the graph like any other node
- **Is a data source**: Outputs block reference(s)
- **Can be connected to**:
  - Action nodes (input: which blocks to control)
  - Event/polling nodes (input: which blocks to monitor)
- **Configuration**: Node properties define selection strategy

### Why Blocks as Nodes?
- **Visual clarity**: See exactly which blocks are used where
- **Reusability**: One Block node can feed multiple actions/events
- **No hidden config**: Everything is visible in the graph
- **Consistent**: Same paradigm for all node types
- **Performance**: Still cached - code generator deduplicates identical block lookups

### Block Selection Strategies

A Block node can match blocks via:

1. **Specific Name**: Exact match
   - `"Airlock Outer Door"` → finds that one specific block

2. **Wildcard Pattern**: Pattern matching
   - `"* Airlock Inner Door"` → finds all blocks ending with "Airlock Inner Door"
   - `"Airlock *"` → finds all blocks starting with "Airlock"

3. **Block Group**: Group membership
   - `Group "Airlocks"` → finds all blocks in the "Airlocks" group

4. **Type Filter** (Optional): Combine with above
   - `"Airlock *" (Doors only)` → only door blocks matching pattern

### Block Node Behavior

**In Actions** (e.g., Close Door):
- Acts on **ALL** matched blocks simultaneously
- Example: Block node matches 3 doors → Close Door closes all 3

**In Events** (e.g., OnOpened):
- **MVP**: Trigger when **ANY** matched block changes state
- **Future**: Support quantifiers (ALL, NONE, COUNT > N)

### Block Node Properties

**Required**:
- **Selection Strategy**: Dropdown (Specific Name, Wildcard, Group)
- **Pattern/Name/Group**: Text input for the search criteria

**Optional**:
- **Display Name**: Optional label for the node (defaults to pattern)
- **Type Filter**: Dropdown (Any, Door, Piston, Rotor, Light, etc.)

**Example**:
```
Selection: Wildcard
Pattern: * Outer Door
Type: Door
Display: "Outer Doors"
```

---

## Node Categories (MVP)

### 0. Data Sources
Nodes that provide data to other nodes.

**Block Node**:
- Defines which block(s) to interact with
- Selection: Specific name, wildcard, or group
- Type filter: Optional (Door, Piston, Rotor, etc.)
- Output: Block reference(s) (data pin)
- Used by: Action nodes, Event trigger nodes, Wait nodes

### 1. Triggers
Nodes that start workflows when conditions are met.

**On Argument**:
- Starts when PB receives matching argument string
- Configuration: Argument pattern (exact match or contains)
- Example: "airlock", "open_outer", "*door*"
- Output: Execution flow to next node

**When Block State** (Event Trigger):
- Starts when a block changes state
- Input: Block reference (from Block node)
- Configuration: Event type (OnOpened, OnClosed, OnOpening, OnClosing, etc.)
- Condition: ANY (MVP) / ALL (future)
- Output: Execution flow to next node
- Example: "When Outer Door opens → start workflow"

**Always (Polling)** (Future):
- Starts every time Main() runs
- For continuous monitoring/automation
- Not needed for MVP

### 2. Actions
Nodes that do something (control blocks).

**Control Door**:
- Opens, closes, or toggles door(s)
- Input: Block reference (from Block node) - data pin
- Input: Execution flow - exec pin
- Configuration: Action (Open/Close/Toggle)
- Output: Execution flow to next node - exec pin
- Acts on ALL blocks from the Block node

**Control Piston** (Future):
- Extend/Retract/Reverse piston(s)
- Same pattern as Control Door

**Set Variable** (Future):
- Sets a boolean/number flag
- For state tracking ("airlock busy")
- Not needed for minimal MVP

### 3. Flow Control
Nodes that control workflow execution.

**Wait For State**:
- Pauses workflow until condition is met
- Input: Block reference (from Block node) - data pin
- Input: Execution flow - exec pin
- Configuration: Condition (Door Closed, Door Open, Piston Extended, etc.)
- Output: Execution flow when condition met - exec pin
- Uses `yield return` internally to pause
- Timeout? Failover if condition never met? (TBD)
- Configuration: Condition (Door Closed, Door Open, Piston Extended, etc.)
- Uses `yield return` internally to pause
- Timeout? Failover if condition never met?

**Sequence** (Implicit):
- Nodes execute in order (follow execution flow)
- May not need explicit node - just wire connections?

**Delay** (Future):
- Wait for X seconds
- Useful for "wait 5 seconds then open door"
- Can be added after MVP

### 4. State/Variables (Future)
Nodes for managing script state.

**Boolean Variable**:
- Get/Set boolean flag
- Example: "airlock_busy" to prevent concurrent cycles

**Number Variable**:
- Counter, timer, etc.

*Not needed for MVP if we keep it simple.*

## Node Visual Design (TBD)

### Blueprint-Style (Execution + Data Pins)
```
┌─────────────────────────┐
│  On Argument "airlock"  │
│                         │
│ [→] Exec Out           │
└─────────────────────────┘
        ↓
┌─────────────────────────┐
│  Get Block "Outer Door" │
│                         │
│ [→] Exec               │
│ [ ] Block → (reference)│
└─────────────────────────┘
        ↓
┌─────────────────────────┐
│  Close Door             │
│ (Block) ← [ ]          │
│           [→] Exec Out │
└─────────────────────────┘
```

**Pros**: Clear data flow, very powerful, industry standard
**Cons**: Can be overwhelming for beginners, many wires

### Flowchart-Style (Simpler)
```
┌─────────────────────────┐
│  When argument          │
│  is "airlock"           │
└───────────┬─────────────┘
            ↓
┌─────────────────────────┐
│  Close door             │
│  "Outer Door"           │
└───────────┬─────────────┘
            ↓
┌─────────────────────────┐
│  Wait until             │
│  door is closed         │
└───────────┬─────────────┘
            ↓
```

**Pros**: Simpler, less intimidating, clearer flow
**Cons**: Less flexible, harder to express complex data flow

### Hybrid Approach (Custom)
- Execution flow is implicit (sequential, top-to-bottom or left-to-right)
- Data connections only when needed (block references)
- Configuration via node properties panel (not pins)

**Decision Needed**: Which style for MVP?

## Airlock MVP Example

### User Story
"When I press the 'airlock' button, I want:
1. Outer door closes
2. Wait for outer door fully closed
3. Inner door opens
4. Wait for inner door fully open"

### Node Graph (Flowchart-Style)
```
[On Argument: "airlock"]
         ↓
[Close Door: "Airlock Outer"]
         ↓
[Wait For: Door Closed]
         ↓
[Open Door: "Airlock Inner"]
         ↓
[Wait For: Door Open]
```

### Node Graph (Blueprint-Style)
```
[On Argument: "airlock"] → [Get Block: "Airlock Outer"] → [Close Door] → [Wait Closed]
                                    ↓ (Block ref)              ↑
                                    
                            [Get Block: "Airlock Inner"] → [Open Door] → [Wait Open]
                                    ↓ (Block ref)              ↑
```

**Question**: Which is clearer for a non-programmer?

## Interaction Model

### Adding Nodes
- **Toolbox/Palette**: Categorized list (Triggers, Actions, Flow)
- **Right-click menu**: Context-sensitive (show relevant nodes)
- **Search**: Type to find node

### Connecting Nodes
- **Drag from output pin to input pin** (Blueprint-style)
- **Click node A, click node B** (simpler, less precise)
- **Auto-connect**: When dropping node, auto-wire to selected node?

### Configuration
- **Properties panel**: Select node → edit properties on right side
- **Inline editing**: Double-click node to edit inline
- **Dropdowns**: For enums (Open/Close, Door/Piston, etc.)

### Canvas Navigation
- **Pan**: Middle-mouse drag or Ctrl+drag
- **Zoom**: Mouse wheel or pinch gesture
- **Fit to view**: Button to center/zoom all nodes

## Next Questions to Answer

1. **Visual style**: Blueprint, Flowchart, or Custom?
2. **Block selection**: How do users specify which block to control?
   - Dropdown (populated from PB's GridTerminalSystem)?
   - Manual text entry?
   - "Wizard" that scans your grid?
3. **Error feedback**: How do we show errors?
   - Red underline on invalid nodes?
   - Error list panel?
   - Toast notifications?
4. **State management**: Do we need variables in MVP, or keep it stateless?

---

*Last updated: 2026-02-17 - Initial draft for discussion*
