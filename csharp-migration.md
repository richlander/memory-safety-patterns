# C# Memory Safety Migration Plan

This document describes the planned migration tooling for transitioning C# codebases from the current memory safety model (v1) to the expanded model (vNext/v2). The goal is **lossless state transition**: no information about intent or audit status is lost during migration.

## Design Principles

The migration follows the precedent set by [nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-migration-strategies), which introduced the concept of "nullable oblivious" code—code written before the nullable context was enabled. Similarly, we introduce **"memory safety oblivious"** as a trackable state for code that compiled without unsafe acknowledgment under the old model.

### Core Requirements

1. **Code compiles throughout** - You can commit before migration, run the tool, and commit after migration. Both commits compile.
2. **No implicit decisions** - Every place where the old model allowed unsafe code without acknowledgment becomes an explicit marker in the new model.
3. **Audit trail preserved** - Git history shows exactly what happened: tool-generated markers vs. human decisions.
4. **LLM-readable history** - An LLM reading your git history can understand exactly what you've done because the migration was non-lossy.

## The Migration Workflow

```
┌─────────────────────────────────────────────────────────────────────────┐
│  1. Commit (v1 mode)     │  Code compiles under current unsafe rules    │
├─────────────────────────────────────────────────────────────────────────┤
│  2. Run migration tool   │  Tool analyzes, adds markers, enables v2     │
├─────────────────────────────────────────────────────────────────────────┤
│  3. Commit (v2 mode)     │  Code compiles under new unsafe rules        │
├─────────────────────────────────────────────────────────────────────────┤
│  4. Audit over time      │  Address TODOs, make explicit decisions      │
└─────────────────────────────────────────────────────────────────────────┘
```

## What the Migration Tool Does

### 1. Enable v2 Mode

Sets the project property to enable the expanded unsafe definition:

```xml
<PropertyGroup>
  <MemorySafetyVersion>2</MemorySafetyVersion>
</PropertyGroup>
```

### 2. Mark Oblivious Methods

Methods that call unsafe APIs but had no unsafe context are marked with TODO comments and given the `unsafe` modifier to ensure compilation:

```csharp
// TODO (MSv2): This method is memory safety oblivious and needs to be audited.
// The migration tool detected calls to unsafe APIs (Unsafe.As) but this method
// was never in an unsafe context. Decision needed:
//   - PROPAGATE: Keep `unsafe` on method signature (current state)
//   - SUPPRESS: Remove `unsafe`, add unsafe block inside, document safety justification
public static unsafe ref TTo ReinterpretAs<TFrom, TTo>(ref TFrom source)
{
    return ref Unsafe.As<TFrom, TTo>(ref source);
}
```

### 3. Mark Oblivious Callsites (Optional)

At callsites that reference dependencies still in v1 mode, the tool can optionally add warnings:

```csharp
// TODO (MSv2): Dependency 'LegacyLib' is in memory safety v1 mode.
// Safety posture of LegacyLib.DoSomething() is unknown.
LegacyLib.DoSomething();
```

This is analogous to how nullable warnings appear when calling into nullable-oblivious code.

### 4. Generate Migration Report

The tool generates a report summarizing the migration:

```
Memory Safety Migration Report
==============================
Project: MyProject.csproj
Migration: v1 → v2

Methods marked as oblivious:     47
  - In src/Core/:                12
  - In src/Interop/:             35

Callsites to v1 dependencies:    23
  - LegacyLib:                   15
  - OldInterop:                   8

Next steps:
  1. Review each TODO (MSv2) marker
  2. Decide: PROPAGATE or SUPPRESS for each method
  3. Document safety justifications for SUPPRESS decisions
  4. Update or replace v1 dependencies
```

## The Oblivious State

"Memory safety oblivious" means: **this code compiled without unsafe acknowledgment under v1, and no human has yet decided whether it should propagate or suppress unsafety under v2.**

This is distinct from:
- **Intentionally propagating**: The author decided callers should handle unsafety
- **Intentionally suppressing**: The author verified safety and documented the justification

The TODO marker preserves this distinction. When you see a TODO, you know no decision has been made. When the TODO is removed, you know a human reviewed it.

### Two Forms of Oblivious

C# will have two distinct oblivious cases:

1. **Dependencies on v1 libraries**: The library was compiled under the old model. We don't know its safety posture because the v2 definition of "unsafe" is broader. Even if the library doesn't set `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`, it might still call APIs like `Unsafe.As` or `Marshal.*` that are now considered unsafe in v2.

2. **Post-migration TODO markers**: Methods in your own code that were implicitly unsafe in v1 (called unsafe APIs without acknowledgment) and need human audit decisions.

### Rust Has a Similar Concept

Rust's cargo-geiger uses `?` to indicate crates with no unsafe usage but missing `#![forbid(unsafe_code)]`:

```
0/0        0/0          0/0    0/0     0/0      ?  either 1.5.2
```

This is analogous to our v1 dependency case—the author hasn't made their safety posture explicit. The difference is that Rust's `unsafe` definition hasn't changed, so `?` means "probably safe but not guaranteed." In C#, v1 libraries are more problematic because the definition of unsafe *has* expanded.

### C#'s Advantage: AllowUnsafeBlocks

C# has an advantage over languages without explicit opt-in: `<AllowUnsafeBlocks>` must be set to use pointer syntax in both v1 and v2. This provides a baseline signal:

| AllowUnsafeBlocks | v1 Mode | v2 Mode |
|-------------------|---------|---------|
| `false` (default) | No pointers, but may call `Unsafe.As` etc. | No pointers, no unsafe API calls |
| `true` | Pointers allowed, may call anything | Full unsafe access, all calls tracked |

The gap is v1 libraries with `AllowUnsafeBlocks=false` that call APIs now marked unsafe in v2. Tooling must scan v1 dependencies for these calls to provide accurate auditing.

## Comparison with Nullable Migration

| Aspect | Nullable | Memory Safety |
|--------|----------|---------------|
| Oblivious state | `#nullable disable` context | `// TODO (MSv2)` marker |
| Default assumption | Reference is not-null | Method propagates unsafety |
| Staged enablement | Warnings → Annotations | v2 mode + oblivious markers |
| Tooling | Flow analysis, IDE hints | Migration tool, geiger-style auditing |

## Planned Tooling

### Migration Tool (`dotnet safety migrate`)

Performs the v1 → v2 transition as described above.

```bash
# Migrate a project
dotnet safety migrate MyProject.csproj

# Migrate entire solution
dotnet safety migrate MySolution.sln

# Preview without changes
dotnet safety migrate --dry-run MyProject.csproj
```

### Audit Tool (`dotnet safety audit`)

Inspired by [cargo-geiger](https://github.com/geiger-rs/cargo-geiger), this tool quantifies unsafe usage across a project and its dependencies.

```bash
# Audit a project
dotnet safety audit MyProject.csproj
```

Example output (modeled on cargo-geiger's format):

```
Metric output format: x/y
    x = unsafe code used by the build
    y = total unsafe code found in the assembly

Symbols:
    🔒  = No unsafe usage, declares <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
    ❓  = No unsafe usage found, missing explicit unsafe policy
    ☢️  = Unsafe usage found
    ⚠️  = Memory safety oblivious (v1 mode or unaudited TODOs)

Methods  Blocks   Calls    Oblivious  Dependency

0/0      1/1      3/3      0          ☢️  MyProject 1.0.0
0/0      5/5      12/12    5          ☢️  ├── MyProject.Core 1.0.0
?/?      ?/?      ?/?      ?          ⚠️  │   └── LegacyLib 2.1.0 (v1 - no data)
0/0      0/0      0/0      0          🔒  ├── Newtonsoft.Json 13.0.3
12/47    89/312   156/892  0          ☢️  └── System.Runtime 9.0.0

12/47    95/318   171/907  5

Summary:
  Dependencies: 5 (4 v2 mode, 1 v1 mode)
  Oblivious methods: 5 (need audit decisions)
  v1 dependencies: LegacyLib 2.1.0 ← consider upgrading
```

The output tracks:
- **Methods**: unsafe method signatures (propagating unsafety)
- **Blocks**: unsafe blocks (suppressed unsafety)
- **Calls**: calls to unsafe APIs
- **Oblivious**: TODO markers from migration (not yet audited)

For v1 dependencies (like `LegacyLib` above), the tool scans the IL for calls to APIs marked unsafe in v2, even though the library was compiled without that knowledge. This provides visibility into v1 libraries that *would* require `<AllowUnsafeBlocks>` if recompiled under v2.

### Assembly Metadata

To support auditing of compiled assets (`.dll`, `.nupkg`), the memory safety posture should be recorded in assembly metadata:

```csharp
[assembly: MemorySafetyVersion(2)]
[assembly: ContainsUnsafeCode(true)]
[assembly: ObliviousMethodCount(0)]  // 0 = fully audited
```

This enables:
- Auditing binary dependencies without source
- NuGet package filtering by safety posture
- Build policies that reject v1 or oblivious dependencies

## Why This Matters

### The Swift Gap

Swift's SE-0458 has no equivalent to this migration model:
- No "oblivious" state—code either has `unsafe` or doesn't
- No migration tooling to capture implicit decisions
- Warnings can be silenced with `unsafe` but no audit trail remains
- Precompiled frameworks lose all safety information

### The Rust Standard

Rust doesn't need migration tooling because `unsafe` was well-defined from the start. However, [cargo-geiger](https://github.com/geiger-rs/cargo-geiger) provides the auditing capability we want:

```
Metric output format: x/y
    x = unsafe code used by the build
    y = total unsafe code found in the crate

Symbols:
    🔒  = No `unsafe` usage found, declares #![forbid(unsafe_code)]
    ❓  = No `unsafe` usage found, missing #![forbid(unsafe_code)]
    ☢️  = `unsafe` usage found

Functions  Expressions  Impls  Traits  Methods  Dependency

0/0        1/1          0/0    0/0     0/0      ☢️  my_crate 0.1.0
0/0        13/83        0/3    0/1     0/3      ☢️  ├── itertools 0.12.1
0/0        0/0          0/0    0/0     0/0      ❓  │   └── either 1.5.2
1/1        4/4          0/0    0/0     0/0      ☢️  └── other_dep 0.1.0

1/1        18/88        0/3    0/1     0/3
```

Key strengths:
- Quantifies unsafe across entire dependency tree
- Source-based analysis (Rust crates are distributed as source)
- Clear symbols indicate safety posture at a glance
- Integrates with `cargo-crev` for trust/audit workflows

### The C# Opportunity

By combining nullable-style migration with geiger-style auditing, C# can achieve:
- Lossless migration from the legacy model
- Full visibility into dependency safety posture
- Auditable trail of human decisions vs. tool-generated markers

## Examples in This Repository

See [`csharpnext-keyword/MemoryLib/UnsafeApi.cs`](csharpnext-keyword/MemoryLib/UnsafeApi.cs) for examples of:
- `ImplicitlyUnsafeApi` - Methods with TODO markers (oblivious state)
- The migration tool's expected output format
- PROPAGATE vs. SUPPRESS decision patterns
