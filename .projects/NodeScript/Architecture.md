# NodeScript Architecture

## Project Structure

**NodeScript files** are stored in standard MDK2 PB projects alongside .cs files:
- File format: Proprietary (JSON/XML, TBD) storing the node graph
- **Code generation**: NodeScript files → generated C# code
  - Option 1: Manual generation (write .cs files, mark as generated)
  - Option 2: Source generator (TBD)
  - Generated code is **ephemeral** - will be overwritten on regeneration

## Execution Model

### Coroutine-Based Workflows
- Uses `yield return` to support async-style workflows
- Workflows pause/resume across PB `Main()` calls
- Support waiting for conditions (door fully closed, etc.)
- Failover handling (player cancels action mid-workflow)

### Programmable Block Constraints

Space Engineers PB scripts have specific limitations that shape NodeScript's design:

**No Event System**:
- Scripts run via `Main(string argument, UpdateType updateSource)`
- No native events for "button pressed" or "door opened"
- Must poll for state changes or react to arguments

**Arguments**:
- Come from button panels, timers, other blocks
- String-based (e.g., "open", "close", "airlock_inner")
- NodeScript must pattern-match arguments to trigger workflows

**State Polling**:
- Check block properties: `door.Status == DoorStatus.Closed`
- No callbacks or async/await
- Must be efficient (instruction count limits)

**UpdateFrequency**:
- Controls how often `Main()` runs
- Options: None, Once, Update1 (every tick), Update10, Update100
- NodeScript must set appropriate frequency for workflows

**Instruction Limits**:
- ~50,000 instructions per tick (approximate, varies)
- Complex state machines can hit limits
- May need to spread work across multiple ticks

## Code Generation Strategy (TBD)

Two potential approaches:

### Option 1: Direct File Generation
- NodeScript editor writes `.g.cs` files (generated code marker)
- Standard MSBuild compilation
- Simple, explicit, debuggable
- Users might accidentally edit generated files

### Option 2: Source Generator
- Roslyn source generator reads `.nodescript` files at build time
- Generates code into compilation (invisible to user)
- Cannot accidentally edit (files don't exist on disk)
- More complex to implement and debug
- Better user experience (no clutter)

**Decision**: Defer until Phase 5, start with Option 1 for MVP

## Event Model

NodeScript is **event-driven** from the user's perspective, but **polling-based** under the hood.

### Trigger Types

**Argument Trigger**:
- User specifies argument pattern (exact match or contains)
- When `Main(argument, updateSource)` called with matching argument → start workflow
- Example: "When argument is 'airlock' → run airlock cycle"

**Polling Trigger** (Future):
- Runs on every Update1/10/100 call
- Checks conditions: "When door A is open AND door B is closed → ..."
- More advanced, not needed for MVP

### Workflow Execution

Workflows execute as **coroutines**:
1. Trigger activates → create coroutine instance
2. Execute steps until `yield return` (e.g., "wait for door closed")
3. Pause execution, store state
4. Next `Main()` call → resume from saved state
5. Continue until workflow completes

Multiple workflows can run concurrently (each has own coroutine state).

## Data Model (To Be Designed)

### Node Graph Structure
- **Nodes**: Individual operations (Get Block, Wait For State, etc.)
- **Connections**: Edges between nodes (execution flow, data flow)
- **Metadata**: Node positions, zoom level, selected nodes

### Connection Types (TBD)
- **Execution Flow**: "Do this, then that" (sequential steps)
- **Data Flow**: Pass values between nodes (block reference, boolean, number)

**Question**: Does NodeScript use execution pins + data pins (Blueprint-style), or just execution flow with implicit data?

### Serialization Format (TBD)
- JSON (human-readable, easy to debug, diff-friendly)
- Binary (compact, faster to load, not git-friendly)
- Custom DSL (readable + compact, parsing overhead)

**Decision**: Likely JSON for MVP (ease of implementation)

---

*Last updated: 2026-02-17*
