# NodeScript Implementation Roadmap

## Phase 1: Foundation ✅

- [x] Window hosting system (`HostWindow`, `IHaveATitle`, `ISupportClosing`)
- [x] NodeScript editor launches in separate window
- [x] Project documentation structure
- [ ] Research: Visual programming UIs for non-programmers
- [ ] Research: Avalonia graph/canvas libraries
- [ ] **Decision**: Visual style (Blueprint vs Flowchart vs Custom)

## Phase 2: Data Model (Not Started)

- [ ] Define node graph data structure
  - [ ] Base `Node` class/interface
  - [ ] `Connection`/`Edge` class
  - [ ] `NodeGraph` container
- [ ] Node types (concrete implementations)
  - [ ] `OnArgumentNode` (trigger)
  - [ ] `GetBlockNode` (action)
  - [ ] `ControlDoorNode` (action)
  - [ ] `WaitForStateNode` (flow control)
- [ ] Connection/edge model
  - [ ] Execution flow edges
  - [ ] Data flow edges (if needed)
- [ ] Serialization format
  - [ ] JSON schema design
  - [ ] Save/load implementation
  - [ ] File format version

## Phase 3: Visual Editor (UI) (Not Started)

### Canvas Infrastructure
- [ ] Canvas control for node graph
  - [ ] Pan/zoom functionality
  - [ ] Grid/snap-to-grid (optional)
  - [ ] Selection rectangle (multi-select)
- [ ] Node rendering
  - [ ] Node visual component (UserControl)
  - [ ] Port/pin rendering (if applicable)
  - [ ] Node sizing/layout
- [ ] Connection rendering
  - [ ] Bezier curves or straight lines
  - [ ] Connection hit testing
  - [ ] Arrow direction indicators

### Interaction
- [ ] Node palette/toolbox
  - [ ] Categorized node list
  - [ ] Search/filter
  - [ ] Drag-drop to canvas
- [ ] Node manipulation
  - [ ] Select/deselect nodes
  - [ ] Move nodes (drag)
  - [ ] Delete nodes (Delete key)
  - [ ] Copy/paste (future)
- [ ] Connection creation
  - [ ] Drag from output to input
  - [ ] Visual feedback during drag
  - [ ] Connection validation
  - [ ] Delete connections
- [ ] Properties panel
  - [ ] Show selected node properties
  - [ ] Edit node configuration
  - [ ] Validation feedback

## Phase 4: Airlock MVP Nodes (Not Started)

### Minimum Node Set
- [ ] `OnArgumentNode`
  - [ ] Argument pattern matching
  - [ ] Execution output
- [ ] `GetBlockNode`
  - [ ] Block name/group input
  - [ ] Type filtering (optional)
  - [ ] Block reference output
- [ ] `ControlDoorNode`
  - [ ] Block reference input
  - [ ] Open/Close/Toggle action
  - [ ] Execution flow
- [ ] `WaitForStateNode`
  - [ ] Block reference input
  - [ ] State condition (Closed/Open/etc.)
  - [ ] Timeout handling (optional)

### Testing
- [ ] Can build airlock graph in editor
- [ ] Nodes serialize/deserialize correctly
- [ ] Visual validation (all nodes connected)

## Phase 5: Code Generation (Not Started)

### Generator Infrastructure
- [ ] Graph validation
  - [ ] No disconnected nodes
  - [ ] No cycles (if not allowed)
  - [ ] Required properties filled
- [ ] Graph → IR translation
  - [ ] Build execution order
  - [ ] Resolve block references
  - [ ] Identify await points (yield return)
- [ ] Code emission
  - [ ] Generate `Program` class
  - [ ] Generate `Main(string argument, UpdateType updateSource)`
  - [ ] Generate coroutine state machine
  - [ ] Generate block lookup code
  - [ ] Set UpdateFrequency appropriately

### Output
- [ ] Write `.g.cs` file (or use source generator)
- [ ] Mark as generated code
- [ ] Add header comment ("DO NOT EDIT")
- [ ] Trigger project rebuild

## Phase 6: Testing & Iteration (Not Started)

### In-Game Testing
- [ ] Create test world in Space Engineers
- [ ] Build physical airlock with doors
- [ ] Load generated script onto PB
- [ ] Test: Button press → airlock cycles
- [ ] Debug: Why doesn't it work? (inevitable)

### Iteration
- [ ] Gather feedback from test users (non-programmers)
- [ ] Identify UX pain points
- [ ] Refine node design
- [ ] Add missing features for MVP
- [ ] Performance testing (instruction count)

### Documentation
- [ ] User guide (how to use NodeScript)
- [ ] Tutorial (build airlock step-by-step)
- [ ] Troubleshooting guide

## Future Phases (Post-MVP)

- [ ] More node types (Variables, Math, Logic)
- [ ] Multiple workflows per file
- [ ] Subgraphs/functions (reusable logic)
- [ ] Visual debugging (step through execution)
- [ ] Template library (airlock, piston door, etc.)
- [ ] Community sharing (workshop?)

---

*Last updated: 2026-02-17*
