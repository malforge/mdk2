# NodeScript Open Questions & Design Decisions

## Design Questions (Need Answers)

### 1. Visual Style
**Question**: Blueprint-style (pins + wires), Flowchart-style (boxes + arrows), or Custom?

**Options**:
- **Blueprint**: Industry standard, powerful, clear data flow
  - Pros: Very expressive, familiar to Unreal users
  - Cons: Can be overwhelming for beginners, many wires
- **Flowchart**: Simpler, more intuitive for non-programmers
  - Pros: Clearer sequential flow, less visual clutter
  - Cons: Less flexible for complex data passing
- **Custom Hybrid**: Simple execution flow + data connections only when needed
  - Pros: Balance simplicity and power
  - Cons: Non-standard, need to design from scratch

**Status**: Undecided - need to prototype and test with target users

---

### 2. Node Graph Structure
**Question**: Single workflow per file, or multiple workflows in one file?

**Considerations**:
- Single workflow: Simpler, one graph per file
- Multiple workflows: More flexible, can have "airlock_open" and "airlock_close" in same file

**Status**: Start with single workflow for MVP, consider multiple later

---

### 3. Block Resources (DESIGN DECISION)
**Question**: How should block resources work?

**Decision Made**:
- **Block Resource = Node** in the graph (not separate panel)
- Acts as **data source** node
- Outputs block reference(s) that connect to action/event nodes
- Properties configure selection strategy (specific name, wildcard, group, type filter)
- Same Block node can feed multiple consumers (actions AND events)

**Benefits**:
- Visual clarity - see exactly which blocks are used where
- Reusability - one Block node, many consumers
- Consistent with node paradigm

**Still Undecided**:
- Should Block nodes be cached/deduplicated? (Two nodes with same pattern → one field in generated code?)
- Design-time validation of block existence?

---

### 4. Block Selection UX
**Question**: How do users specify which block to control?

**Options**:
- **Text entry**: User types "Airlock Outer Door"
  - Pros: Simple, flexible
  - Cons: Typos, no validation until runtime
- **Dropdown**: Populated from GridTerminalSystem at design time
  - Pros: No typos, see available blocks
  - Cons: Requires game connection? Or scan project?
- **Property panel**: Browse/search blocks in properties
  - Pros: Good UX
  - Cons: Complex to implement
- **Runtime only**: Just generate code that searches by name
  - Pros: Simplest for MVP
  - Cons: No design-time validation

**Status**: Lean towards text entry for MVP, dropdown later

---

### 4. Error Handling Strategy
**Question**: What happens when a block doesn't exist or is broken?

**Options**:
- **Ignore**: Code fails silently (bad UX)
- **Log to PB**: Write error to PB's DetailedInfo
- **Halt workflow**: Stop execution, show error state
- **Try/catch**: Continue with null checks

**Code Gen Question**: Do we wrap every block access in try/catch?

**Status**: Undecided - need to design error reporting UX

---

### 5. Validation Strategy
**Question**: How do we validate graphs before code generation?

**Checks Needed**:
- All nodes connected (no orphans)
- Required properties filled in
- No cycles in execution flow (if disallowed)
- Type compatibility (if we have typed data flow)

**When to Validate**:
- Real-time (as user edits)
- On save
- Before code generation

**Status**: Real-time + pre-generation validation

---

### 6. Debugging Support
**Question**: How do users debug when generated code doesn't work?

**Options**:
- **No debugging**: Just view generated code (programmer-friendly, not user-friendly)
- **Execution trace**: Show which nodes executed (log to PB DetailedInfo)
- **Visual debugging**: Highlight active node in editor (requires game connection)
- **Step through**: Pause execution, step node-by-node (very advanced)

**Status**: Defer to post-MVP, but design with debugging in mind

---

### 7. Multiple Instances Problem
**Question**: Can user have multiple airlocks? How do they differentiate?

**Scenario**: Two airlocks, "North Airlock" and "South Airlock"

**Options**:
- **Separate graphs**: North.nodescript, South.nodescript (duplication)
- **Templates**: Define once, instantiate with different block names
- **Arguments**: Pass airlock name as argument (complex)
- **Block groups**: Use groups instead of individual block names

**Status**: Not needed for MVP (single airlock), revisit for templates later

---

### 8. State Management
**Question**: Do we need variables in the MVP?

**Use Case**: "Don't start airlock cycle if one is already running"

**Options**:
- **No variables**: Keep it simple, allow concurrent cycles (might be fine?)
- **Boolean flags**: Add Get/Set Variable nodes
- **Implicit**: Code gen detects and auto-manages "workflow busy" flag

**Status**: Try without variables first, add if needed

---

### 9. Connection Types
**Question**: Just execution flow, or execution + data flow?

**Execution Flow**: "Do this, then that"
**Data Flow**: "Pass this block reference to that node"

**Options**:
- **Execution only**: Simpler, nodes have implicit inputs (block name as property)
- **Execution + Data**: More flexible, clearer dependencies

**Example**:
```
Execution only:
[Get Block "Outer"] → [Close Door "Outer"]

Execution + Data:
[Get Block "Outer"] → [Close Door]
        ↓ (data)        ↑
```

**Status**: Undecided - impacts node design significantly

---

### 10. Code Generation Timing
**Question**: When does code generation happen?

**Options**:
- **Manual**: User clicks "Generate Code" button
- **On save**: Auto-generate when .nodescript file saved
- **On build**: Source generator runs during MSBuild
- **Real-time**: Generate as user edits (expensive, probably not)

**Status**: Manual for MVP, source generator for v2

---

## Technical Risks

### 1. Canvas Performance
**Risk**: Avalonia doesn't have a mature node editor library - may need custom canvas

**Mitigation**: 
- Research existing Avalonia canvas/diagram libraries
- Prototype simple canvas early
- Keep node count reasonable for MVP (< 50 nodes)

---

### 2. Complexity Creep
**Risk**: Feature requests will push beyond "simple automation"

**Mitigation**: 
- Strict scope discipline
- "MVP first" mindset
- Document "future features" but don't implement

---

### 3. PB Instruction Limits
**Risk**: Coroutine state machine might hit 50k instruction limit

**Mitigation**: 
- Keep generated code simple
- Avoid complex state machines in MVP
- Test in-game early

---

### 4. User Experience
**Risk**: We (as programmers) may build something too complex for target audience

**Mitigation**: 
- User testing with actual non-programmers
- Iterate on feedback
- "Grandma test": Could someone's grandma build an airlock?

---

### 5. Avalonia Learning Curve
**Risk**: Limited experience with Avalonia for complex UI

**Mitigation**: 
- Start with simple prototype
- Leverage existing controls where possible
- Research Avalonia canvas examples

---

## Success Criteria

MVP is successful if:
- ✅ A non-programmer can build a working airlock controller in <30 minutes
- ✅ Generated code compiles and runs in Space Engineers
- ✅ System is obviously extensible to other simple automation
- ✅ Users don't need to understand C#, classes, methods, UpdateFrequency, etc.
- ✅ We learned enough to design v2 properly

---

*Last updated: 2026-02-17*
