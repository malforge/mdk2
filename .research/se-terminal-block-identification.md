# SE Terminal Block Identification Research

## Key Finding: `MyTerminalInterfaceAttribute`

Discovered via `Mdk.Extractor/ExtractorPlugin.cs`. Space Engineers marks block implementation
classes with **`[MyTerminalInterfaceAttribute]`** to link them to their API interface types.

```csharp
// From ExtractorPlugin.cs
var infoAttribute = block.GetType().GetCustomAttribute<MyTerminalInterfaceAttribute>();
var ingameType = infoAttribute.LinkedTypes.FirstOrDefault(t => t.Namespace?.EndsWith(".Ingame") ?? false);
```

- **`MyTerminalInterfaceAttribute.LinkedTypes`** — array of interface types linked to this block,
  including the `*.Ingame` interface (e.g., `Sandbox.ModAPI.Ingame.IMyThrust`)
- A block has this attribute **only if it is a terminal block** — non-terminal blocks (armor,
  structural, conveyors, etc.) do not have it

## Key Finding: `MyCubeBlockTypeAttribute`

Also found in `ExtractorPlugin.cs`:

```csharp
var attr = block.GetCustomAttribute<MyCubeBlockTypeAttribute>();
// attr.ObjectBuilderType.Name == "MyObjectBuilder_ThrustDefinition"
// → normalized TypeId == "Thrust"
```

- Maps a block implementation class to its `MyObjectBuilder_*` definition type
- The TypeId is derived by stripping `MyObjectBuilder_` prefix from the name

## How Mdk.Extractor Does It (Runtime Reflection)

The Extractor **actually runs the game**, spawns every `MyCubeBlockDefinition` in the world,
and checks `if (slim.FatBlock is not IMyTerminalBlock block)` to identify terminal blocks.
It then uses the two attributes above to extract TypeId and ingame interface.

This approach is **not usable in the Hub** because:
1. It requires running SE (not acceptable at Hub startup)
2. Runtime reflection across framework versions (.NET 9 Hub vs SE's runtime) would fail

## Cecil Approach for Hub

We can replicate this **statically** using **Mono.Cecil** to read SE DLLs without loading them
into the .NET 9 runtime.

### DLLs to Scan

From `E:\Steam\steamapps\common\SpaceEngineers\Bin64\`:
- `Sandbox.Game.dll` — contains `MyCubeBlock`, `MyTerminalBlock`, `MyCubeBlockTypeAttribute`
- `SpaceEngineers.Game.dll` — contains additional block implementations

Attribute namespaces (from `using` statements in `ExtractorPlugin.cs`):
- `MyTerminalInterfaceAttribute` — likely in `Sandbox.Game.Entities.Cube` or `VRage`
- `MyCubeBlockTypeAttribute` — likely in `Sandbox.Game.Entities.Cube`

### Algorithm

```
For each TypeDefinition in Sandbox.Game.dll + SpaceEngineers.Game.dll:
  1. Check for [MyCubeBlockTypeAttribute] → if present, extract ObjectBuilderType name → TypeId
  2. Check for [MyTerminalInterfaceAttribute] → if present, this is a terminal block
  3. If both → add TypeId to the "terminal block TypeIds" set
```

Result: a `HashSet<string>` of valid TypeIds (e.g., `{"Thrust", "Gyro", "Refinery", ...}`)
used to filter the SBC block list in `BlockDefinitionService`.

### Cecil NuGet Package

Package: **`Mono.Cecil`** (maintained, MIT-compatible license)
- Does not load assemblies into the runtime
- Works across .NET framework versions
- Already used transitively by many .NET tools

---

## Extractor Terminal Output Format

The Extractor already generates a terminal XML file via `-terminal <path>`:

```xml
<terminals>
  <block type="Sandbox.ModAPI.Ingame.IMyThrust" typedefinition="Thrust">
    <action name="IncreaseOverride" text="Increase Override" />
    <property name="Override" type="Single" />
  </block>
  ...
</terminals>
```

- `typedefinition` = the normalized TypeId (matches SBC files exactly)
- `type` = the full ingame interface name

This is the canonical mapping of TypeId → terminal block interface. The Hub could potentially
read a pre-generated version of this file instead of running Cecil every time, if one is shipped
with MDK. However, for correctness with user-installed game versions, Cecil analysis at load
time is preferred.

---

## Implications for BlockDefinitionService

1. Add `Mono.Cecil` package to `Mdk.Hub.csproj`
2. On load, scan SE DLLs with Cecil to build `HashSet<string> _terminalBlockTypeIds`
3. After loading SBC block definitions, filter `_blocks` to only include entries where
   `BlockId.TypeId` is in `_terminalBlockTypeIds`
4. Categories with no remaining blocks after filtering can be hidden or shown empty

This filtering should happen inside `BlockDefinitionService.LoadAsync()` as a post-processing step.
